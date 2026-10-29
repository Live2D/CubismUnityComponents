/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Collections.Generic;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Live2D.Cubism.Rendering.URP.RenderingInterceptor
{
    /// <summary>
    /// Base class for rendering interceptors.
    /// Place custom rendering logic by inheriting from this class.
    /// </summary>
    [ExecuteInEditMode]
    public class CubismRenderingInterceptController : MonoBehaviour, ICubismUpdatable, ICubismRenderingInterceptor
    {
        /// <summary>
        /// Timing for invoking.
        /// </summary>
        public enum InvokeTiming
        {
            PreRendering,
            PostRendering,
        }

        /// <summary>
        /// Timing for invoking setting.
        /// </summary>
        [SerializeField]
        public InvokeTiming InvokeTimingSetting;

        /// <summary>
        /// Modes for invoking.
        /// </summary>
        public enum InterceptingMode
        {
            SortingOrder,
            ZDepth,
            Drawable,
        }

        /// <summary>
        /// Mode for invoking setting.
        /// </summary>
        [SerializeField]
        public InterceptingMode Mode;

        /// <summary>
        /// Group sorting order for the interceptor.
        /// </summary>
        [SerializeField]
        public int GroupSortingOrder = 0;

        /// <summary>
        /// Sorting order for the interceptor.
        /// </summary>
        [SerializeField]
        public int SortingOrder = 0;

        /// <summary>
        /// Target art mesh for the interceptor.
        /// </summary>
        [SerializeField]
        protected CubismDrawable _targetArtMesh;

        /// <summary>
        /// If attached <see cref="CubismRenderController"/>, reference.
        /// </summary>
        protected CubismRenderController _renderController;

        /// <summary>
        /// Renderer reference.
        /// </summary>
        protected Renderer _renderer;

        /// <summary>
        /// <see cref="_rederer"/>'s material reference.
        /// </summary>
        protected Material _material;

        /// <summary>
        /// Camera draw status.
        /// </summary>
        private struct CameraDrawStatus
        {
            public Camera Camera;
            public int LastRenderedPassCount;
        }

        /// <summary>
        /// Camera draw status array.
        /// </summary>
        private CameraDrawStatus[] _cameraDrawStatus;

        /// <summary>
        /// Render pass counter.
        /// </summary>
        private int _renderPassCounter = 0;

        /// <summary>
        /// Injection prediction for the rendering event currently being dispatched.
        /// </summary>
        /// <remarks>
        /// <see cref="CubismRenderingInterceptorsManager"/> asks every interceptor for this prediction
        /// before it dispatches the event, then hands the answer back when it does. Storing it here lets
        /// <see cref="TryDraw"/> reuse the decision instead of running <see cref="ShouldDrawForMode"/> a
        /// second time for the same event.
        /// </remarks>
        private bool _willInjectForCurrentEvent;

        /// <summary>
        /// Group sorting order for rendering.
        /// </summary>
        int ICubismRenderingInterceptor.RenderingGroupSortingOrder
        {
            get
            {
                return GroupSortingOrder;
            }
        }

        /// <summary>
        /// Sorting order for rendering.
        /// </summary>
        int ICubismRenderingInterceptor.RenderingSortingOrder
        {
            get
            {
                return SortingOrder;
            }
        }

        /// <summary>
        /// Execution order of the interceptor.
        /// </summary>
        public virtual int ExecutionOrder
        {
            get
            {
                return CubismUpdateExecutionOrder.CubismPhysicsController + 10;
            }
        }

        /// <summary>
        /// Whether this interceptor needs to be updated during editing.
        /// </summary>
        public virtual bool NeedsUpdateOnEditing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Whether a <see cref="CubismUpdateController"/> is attached to the same GameObject.
        /// </summary>
        public bool HasUpdateController
        {
            get;
            set;
        }

        /// <summary>
        /// Called by Unity when the component is enabled.
        /// </summary>
        public void OnEnable()
        {
            _renderController = GetComponent<CubismRenderController>();

            if (!_renderController)
            {
                _renderer = GetComponent<Renderer>();

                _renderer.enabled = false;

#if UNITY_EDITOR
                _material = _renderer.sharedMaterial;
#else
                _material = _renderer.material;
#endif
            }

            _cameraDrawStatus = Array.Empty<CameraDrawStatus>();

            CubismRenderingInterceptorsManager.GetInstance().AddInterceptors(this);

            RenderPipelineManager.beginContextRendering += OnBeginContextRendering;
        }

        /// <summary>
        /// Called by Unity when the component starts.
        /// </summary>
        public void Start()
        {
            HasUpdateController = TryGetComponent<CubismUpdateController>(out _);
        }

        /// <summary>
        /// Called by Unity when the component is disabled.
        /// </summary>
        public void OnDisable()
        {
            CubismRenderingInterceptorsManager.GetInstance().RemoveInterceptors(this);

            RenderPipelineManager.beginContextRendering -= OnBeginContextRendering;
        }

        /// <summary>
        /// Called by Unity at the beginning of context rendering.
        /// </summary>
        private void OnBeginContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            _renderPassCounter++;

            ReleaseDestroyedCameraDrawStatus();
        }

        /// <summary>
        /// Called by Unity during LateUpdate.
        /// </summary>
        public void LateUpdate()
        {
            // Skip if an update controller is present.
            if (HasUpdateController)
            {
                return;
            }

            OnLateUpdate();
        }

        /// <summary>
        /// Called during LateUpdate if no <see cref="CubismUpdateController"/> is present.
        /// </summary>
        public virtual void OnLateUpdate()
        {

        }

        /// <summary>
        /// Called before rendering for each pass.
        /// </summary>
        /// <param name="args"> Event arguments. </param>
        /// <param name="willInject"> The prediction the manager made for this event. </param>
        void ICubismRenderingInterceptor.OnPreRenderingForPass(CubismRenderedEventArgs args, bool willInject)
        {
            _willInjectForCurrentEvent = willInject;

            OnPreRendering(args);
        }

        /// <summary>
        /// Called before rendering for each pass.
        /// </summary>
        /// <param name="args"> Event arguments. </param>
        protected virtual void OnPreRendering(CubismRenderedEventArgs args)
        {
            // Skip if not invoking pre-rendering.
            if (InvokeTimingSetting != InvokeTiming.PreRendering)
            {
                return;
            }

            TryDraw(args);
        }

        /// <summary>
        /// Called after rendering for each pass.
        /// </summary>
        /// <param name="args"> Event arguments. </param>
        /// <param name="willInject"> The prediction the manager made for this event. </param>
        void ICubismRenderingInterceptor.OnPostRenderingForPass(CubismRenderedEventArgs args, bool willInject)
        {
            _willInjectForCurrentEvent = willInject;

            OnPostRendering(args);
        }

        /// <summary>
        /// Called after rendering for each pass.
        /// </summary>
        /// <param name="args"> Event arguments. </param>
        protected virtual void OnPostRendering(CubismRenderedEventArgs args)
        {
            // Skip if not invoking post-rendering.
            if (InvokeTimingSetting != InvokeTiming.PostRendering)
            {
                return;
            }

            TryDraw(args);
        }

        /// <summary>
        /// Attempts to draw the object based on the interceptor's settings.
        /// </summary>
        /// <param name="args"> Event arguments. </param>
        private void TryDraw(CubismRenderedEventArgs args)
        {
            var currentCamera = args.PassData.CameraData.Camera;

            // Find or create camera draw status.
            var statusIndex = FindCameraDrawStatusIndex(currentCamera);

            // Create a new entry if not found, reusing one left behind by a camera that is gone.
            if (statusIndex < 0)
            {
                statusIndex = AcquireCameraDrawStatusIndex();

                // Initialize the new entry.
                _cameraDrawStatus[statusIndex] = new CameraDrawStatus
                {
                    Camera = currentCamera,
                    LastRenderedPassCount = -1,
                };
            }

            // Get the current rendered frame count.
            if (_cameraDrawStatus[statusIndex].LastRenderedPassCount == _renderPassCounter)
            {
                return;
            }

            // The manager already ran ShouldDrawForMode for this event when it asked for the injection
            // prediction. Past the already-drawn check above, that prediction is exactly the mode
            // decision, so reuse it instead of evaluating the mode a second time.
            if (!_willInjectForCurrentEvent)
            {
                return;
            }

            if (!_renderController)
            {
                // Draw the object.
                args.CommandBuffer.DrawRenderer(_renderer, _material);
            }
            else
            {
                // Interceptor-driven models opt out of the common controller group (see
                // CubismRenderController.OnEnable), so the render pass never captured their root transform.
                // Capture it here, just before drawing, so the per-drawable transform paths read valid values
                // instead of the default (zero-scale / zero-rotation) snapshot, which would collapse the mesh.
                _renderController.CaptureRootTransform();

                for (var index = 0; index < _renderController.SortedRenderers.Length; index++)
                {
                    var targetRenderer = _renderController.SortedRenderers[index];

                    // Check whether to skip rendering.
                    CheckSkipRendering(targetRenderer);

                    if (targetRenderer.SkipRendering)
                    {
                        continue;
                    }

                    // Draw the object.
                    targetRenderer.DrawObject(args.CommandBuffer, args.PassData);
                }

                // Submit offscreen draws.
                _renderController.SubmitDrawOffscreen(args.CommandBuffer, args.PassData);
            }

            // Mark as drawn.
            _cameraDrawStatus[statusIndex].LastRenderedPassCount = _renderPassCounter;
        }

        /// <summary>
        /// Looks up the <see cref="CameraDrawStatus"/> entry for the given camera.
        /// </summary>
        /// <remarks>
        /// Deliberately a plain loop instead of <see cref="Array.FindIndex{T}(T[], Predicate{T})"/>: the lookup runs
        /// once per drawable per interceptor from the render pass, and a predicate capturing the camera would
        /// allocate a closure and a delegate on every call.
        /// </remarks>
        /// <param name="camera"> The camera to look up. </param>
        /// <returns> The index of the matching entry, or -1 when the camera is not registered yet.</returns>
        private int FindCameraDrawStatusIndex(Camera camera)
        {
            for (var index = 0; index < _cameraDrawStatus.Length; index++)
            {
                if (_cameraDrawStatus[index].Camera != camera)
                {
                    continue;
                }

                return index;
            }

            return -1;
        }

        /// <summary>
        /// Reserves a <see cref="CameraDrawStatus"/> slot for a camera that is not registered yet.
        /// </summary>
        /// <remarks>
        /// Slots left free by <see cref="ReleaseDestroyedCameraDrawStatus"/> are reused before the array grows,
        /// so the array settles at the number of cameras alive at the same time instead of counting every camera
        /// that ever rendered this interceptor.
        /// </remarks>
        /// <returns>The index of the reserved slot.</returns>
        private int AcquireCameraDrawStatusIndex()
        {
            for (var index = 0; index < _cameraDrawStatus.Length; index++)
            {
                if (_cameraDrawStatus[index].Camera)
                {
                    continue;
                }

                return index;
            }

            Array.Resize(ref _cameraDrawStatus, _cameraDrawStatus.Length + 1);

            return _cameraDrawStatus.Length - 1;
        }

        /// <summary>
        /// Frees the <see cref="CameraDrawStatus"/> entries whose camera was destroyed.
        /// </summary>
        /// <remarks>
        /// The surviving entries are packed to the front and the array is shrunk to match, so the references to
        /// the destroyed cameras are dropped and <see cref="FindCameraDrawStatusIndex"/> only ever scans cameras
        /// that are still rendering. Nothing is allocated while every entry is alive, which is the steady state.
        /// This runs once per context rendering rather than from the lookup itself, which is called once per draw
        /// object per interceptor and is kept free of the extra alive check.
        /// </remarks>
        private void ReleaseDestroyedCameraDrawStatus()
        {
            var aliveCount = 0;
            for (var index = 0; index < _cameraDrawStatus.Length; index++)
            {
                if (!_cameraDrawStatus[index].Camera)
                {
                    continue;
                }

                _cameraDrawStatus[aliveCount] = _cameraDrawStatus[index];
                aliveCount++;
            }

            if (aliveCount == _cameraDrawStatus.Length)
            {
                return;
            }

            Array.Resize(ref _cameraDrawStatus, aliveCount);
        }

        /// <summary>
        /// Pure predicate equivalent of the mode/timing checks inside <see cref="TryDraw"/>.
        /// Determines whether the configured <see cref="Mode"/> wants this drawable to trigger an injection.
        /// </summary>
        /// <remarks>
        /// Side-effect free. Mirrors the doDraw decision in the original switch — keep these in sync
        /// when adding new modes.
        /// </remarks>
        /// <param name="args"> Event arguments. </param>
        /// <returns>True when the mode would let <see cref="TryDraw"/> proceed to inject draws.</returns>
        private bool ShouldDrawForMode(CubismRenderedEventArgs args)
        {
            switch (Mode)
            {
                case InterceptingMode.SortingOrder:
                    return args.GroupSortingOrder == GroupSortingOrder
                        && args.SortingOrder >= SortingOrder;

                case InterceptingMode.ZDepth:
                {
                    // Calculate the distance from the camera to the renderer.
                    var directionToRenderer = transform.position - args.CameraPos;
                    var projection = Vector3.Project(directionToRenderer, args.CameraForward);
                    var distance = projection.magnitude;

                    // Determine whether to draw based on distance comparisons.
                    var canDraw = (args.Distance < distance || args.NextDistance > distance) &&
                                !(distance > args.Distance && distance > args.NextDistance &&
                                  distance > args.PreviousDistance);

                    if (args.PreviousDistance != null
                        && args.NextDistance != null
                        && canDraw)
                    {
                        return false;
                    }

                    return distance >= args.Distance;
                }

                case InterceptingMode.Drawable:
                    return _targetArtMesh && args.Drawable == _targetArtMesh;

                default:
                    // Preserve original TryDraw behaviour: unknown modes do not short-circuit doDraw.
                    return true;
            }
        }

        /// <summary>
        /// Predicts whether this interceptor would inject draw commands for the given event.
        /// </summary>
        /// <remarks>
        /// Used by the URP render pass to flush the drawable batcher around drawables that trigger
        /// an injection so injected DrawRenderer calls remain correctly ordered against batched
        /// drawables. Side-effect free: must not mutate <see cref="_cameraDrawStatus"/>.
        /// </remarks>
        /// <param name="args"> Event arguments. </param>
        /// <returns>True when <see cref="OnPreRendering"/>/<see cref="OnPostRendering"/> would call <see cref="TryDraw"/> and reach the injection step.</returns>
        bool ICubismRenderingInterceptor.WillInjectDraws(CubismRenderedEventArgs args)
        {
            // Only the configured timing actually invokes TryDraw — the other timing returns early.
            switch (InvokeTimingSetting)
            {
                case InvokeTiming.PreRendering:
                case InvokeTiming.PostRendering:
                    break;
                default:
                    return false;
            }

            // If we have already drawn for this pass on this camera, OnPre/OnPost will early-return.
            var currentCamera = args.PassData.CameraData.Camera;
            var statusIndex = FindCameraDrawStatusIndex(currentCamera);
            if (statusIndex >= 0
                && _cameraDrawStatus[statusIndex].LastRenderedPassCount == _renderPassCounter)
            {
                return false;
            }

            return ShouldDrawForMode(args);
        }

        /// <summary>
        /// Checks whether to skip rendering for the given renderer.
        /// </summary>
        /// <param name="targetRenderer"> The target renderer. </param>
        private void CheckSkipRendering(CubismRenderer targetRenderer)
        {
            targetRenderer.SkipRendering |= !targetRenderer
                                                    || !targetRenderer.gameObject.activeSelf
                                                    || !targetRenderer.MeshRenderer
                                                    || !targetRenderer.MeshRenderer.enabled;

            switch (targetRenderer.DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    targetRenderer.SkipRendering |= targetRenderer.DisplayedOpacity <= 0.0f;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    targetRenderer.SkipRendering |= targetRenderer.DisplayedOffscreenOpacity <= 0.0f;

                    if (!targetRenderer.SkipRendering)
                    {
                        break;
                    }

                    // Skip rendering for all child drawables and offscreens of the offscreen part.
                    targetRenderer.SkipDescendantRenderers();
                    break;
                default:
                    targetRenderer.SkipRendering = true;
                    break;
            }
        }
    }
}
