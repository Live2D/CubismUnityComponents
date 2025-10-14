/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Rendering.Masking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Partial class that describes processing related to the rendering method added in Cubism 5.3 and later.
    /// </summary>
    public partial class CubismRenderController
    {
        #region Values

        /// <summary>
        /// Material property block for the model.
        /// </summary>
        private MaterialPropertyBlock _properties;

        /// <summary>
        /// <see cref="CommandBuffer"/>'s backing field.
        /// </summary>
        private CommandBuffer _commandBuffer;

        /// <summary>
        /// Command buffer for rendering.
        /// </summary>
        public CommandBuffer CommandBuffer
        {
            get { return _commandBuffer; }
        }

        /// <summary>
        /// Whether the model has a root part offscreen.
        /// </summary>
        private bool _hasRootPartOffscreen;

        /// <summary>
        /// Original viewport rect.
        /// </summary>
        private Rect _orgViewport;

        /// <summary>
        /// <see cref="MeshRenderer"/> for draw model.
        /// </summary>
        [SerializeField, HideInInspector]
        public MeshRenderer ModelCanvasRenderer;

        /// <summary>
        /// <see cref="MaskController"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismMaskController _maskController;

        /// <summary>
        /// Mask controller for this model.
        /// </summary>
        public CubismMaskController MaskController
        {
            get
            {
                return _maskController;
            }
            set
            {
                _maskController = value;
            }
        }

        /// <summary>
        /// Framebuffer for rendering.
        /// </summary>
        public RenderTexture ModelFrameBuffer { get; set; }

        /// <summary>
        /// Framebuffer for rendering.
        /// </summary>
        public RenderTexture RootFrameBuffer { get; set; }

        /// <summary>
        /// Current frame buffer used for rendering.
        /// Offscreen rendering uses this to render the model.
        /// </summary>
        public RenderTexture CurrentFrameBuffer { get; set; }

        /// <summary>
        /// Offscreen frame buffer used for rendering
        /// </summary>
        public RenderTexture OffscreenRenderingFrameBuffer { get; set; }

        /// <summary>
        /// Index of the current offscreen owner in the unmanaged array.
        /// </summary>
        public int CurrentOffscreenUnmanagedIndex { get; set; }

        #region SortedRenderers

        /// <summary>
        /// Whether the render order of the <see cref="CubismDrawable"/>s did change.
        /// </summary>
        internal bool DidChangeDrawableRenderOrder;

        [NonSerialized]
        private CubismRenderer[] _sortedRenderers;

        /// <summary>
        /// Sorted <see cref="Renderers"/>s.
        /// </summary>
        public CubismRenderer[] SortedRenderers
        {
            get
            {
                if (_sortedRenderers == null)
                {
                    SortRenderers();
                }

                return _sortedRenderers;
            }
            private set { _sortedRenderers = value; }
        }

        /// <summary>
        /// Sorts the <see cref="Renderers"/> by their draw object render order.
        /// </summary>
        private void SortRenderers()
        {
            if (Renderers == null)
            {
                return;
            }

            for (var i = 0; i < Renderers.Length; i++)
            {
                if (Renderers[i].DrawObjectType != CubismModelTypes.DrawObjectType.Offscreen)
                {
                    continue;
                }

                Renderers[i].SetDrawObjectRenderOrder(
                    Model.AllDrawObjectsRenderOrder[
                        DrawableRenderers.Length + Renderers[i].Offscreen.UnmanagedIndex]);
            }

            var renderers = new List<CubismRenderer>(Renderers);

            SortBySortingOrder(renderers);

            _sortedRenderers = renderers.ToArray();
        }

        private void SortBySortingOrder(List<CubismRenderer> renderers)
        {
            renderers.Sort(CompareBySortingOrder);
        }

        private int CompareBySortingOrder(CubismRenderer a, CubismRenderer b)
        {
            return a.MeshRenderer.sortingOrder - b.MeshRenderer.sortingOrder;
        }

        #endregion

        /// <summary>
        /// <see cref="DrawableRenderers"/>'s backing field.
        /// </summary>
        [NonSerialized]
        private CubismRenderer[] _drawableRenderers;

        /// <summary>
        /// Drawable <see cref="CubismRenderer"/>s.
        /// </summary>
        public CubismRenderer[] DrawableRenderers
        {
            get
            {
                if (_drawableRenderers == null)
                {
                    _drawableRenderers = Model.Drawables.GetComponentsMany<CubismRenderer>();
                }
                return _drawableRenderers;
            }
            private set { _drawableRenderers = value; }
        }

        /// <summary>
        /// <see cref="OffscreenRenderers"/>'s backing field.
        /// </summary>
        private CubismRenderer[] _offscreenRenderers;

        /// <summary>
        /// Offscreen <see cref="CubismRenderer"/>s.
        /// </summary>
        public CubismRenderer[] OffscreenRenderers
        {
            get
            {
                if (_offscreenRenderers == null && Model.Offscreens != null)
                {
                    _offscreenRenderers = Model.Offscreens.GetComponentsMany<CubismRenderer>();
                }
                return _offscreenRenderers;
            }
            private set { _offscreenRenderers = value; }
        }

        #endregion

        /// <summary>
        /// Initializes the render controller.
        /// </summary>
        /// <param name="renderers"></param>
        private void TryInitializeRenderersIsUsingBlendMode(CubismRenderer[] renderers)
        {
            // HACK: It doesn't work correctly when placed inside a function.
            if (renderers != null && renderers.Length != 0)
            {
                return;
            }

            // Initialize renderers.
            renderers = Array.Empty<CubismRenderer>();

            // Create renders and apply it to backing field...
            var drawables = Model.Drawables;

            var drawableRenderers = drawables.AddComponentEach<CubismRenderer>();
            Array.Resize(ref renderers, drawableRenderers.Length);
            Array.Copy(drawableRenderers, renderers, drawableRenderers.Length);

            for (var index = 0; index < renderers.Length; index++)
            {
                var targetRenderer = renderers[index];
                targetRenderer.DrawObjectType = CubismModelTypes.DrawObjectType.Drawable;
            }

            // Store drawable renderers.
            DrawableRenderers = drawableRenderers;

            var offscreens = Model.Offscreens;

            if (offscreens != null)
            {
                if (offscreens.Length > 0)
                {
                    _hasRootPartOffscreen = offscreens[0].OwnerIndex == 0;
                }

                var offscreenRenderers = offscreens.AddComponentEach<CubismRenderer>();
                Array.Resize(ref renderers, renderers.Length + offscreenRenderers.Length);
                Array.Copy(offscreenRenderers, 0, renderers, renderers.Length - offscreenRenderers.Length, offscreenRenderers.Length);

                for (var index = drawableRenderers.Length; index < renderers.Length; ++index)
                {
                    var targetRenderer = renderers[index];
                    targetRenderer.DrawObjectType = CubismModelTypes.DrawObjectType.Offscreen;
                }

                // Store offscreen renderers.
                OffscreenRenderers = offscreenRenderers;
            }

            // Store renderers.
            Renderers = renderers;
        }

        /// <summary>
        /// Called after all <see cref="CubismRenderer"/>s are initialized.
        /// </summary>
        /// <param name="renderers"></param>
        private void OnAfterRenderersInitialize(CubismRenderer[] renderers)
        {
            if (!Model.IsUsingBlendMode)
            {
                return;
            }

            // Set the render order and call `OnAfterAllRendererInitialize` method for each renderer.
            for (var i = 0; i < renderers.Length; i++)
            {
                var initRenderer = renderers[i];
                switch (initRenderer.DrawObjectType)
                {
                    case CubismModelTypes.DrawObjectType.Drawable:
                        initRenderer.SetDrawObjectRenderOrder(Model.AllDrawObjectsRenderOrder[initRenderer.Drawable.UnmanagedIndex]);
                        break;
                    case CubismModelTypes.DrawObjectType.Offscreen:
                        initRenderer.SetDrawObjectRenderOrder(Model.AllDrawObjectsRenderOrder[DrawableRenderers.Length + initRenderer.Offscreen.UnmanagedIndex]);
                        break;
                    default:
                        Debug.LogWarning($"Unknown draw object type: {initRenderer.DrawObjectType} for renderer: {initRenderer.name}");
                        break;
                }
                initRenderer.OnAfterAllRendererInitialize();
            }

            SortRenderers();
        }

        /// <summary>
        /// Does the model have an offscreen for the root part?
        /// </summary>
        /// <param name="targetRenderer">The <see cref="CubismRenderer"/> to check.</param>
        /// <returns>True if it has one, false if it does not.</returns>
        private bool HasRootPartOffscreen(CubismRenderer targetRenderer)
        {
            var parentPartIndex = -1;
            switch (targetRenderer.DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    parentPartIndex = targetRenderer.Drawable.ParentPartIndex;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    parentPartIndex = targetRenderer.Offscreen.OwnerIndex;
                    break;
                default:
                    Debug.LogWarning($"Unknown draw object type: {targetRenderer.DrawObjectType} for renderer: {targetRenderer.name}");
                    break;
            }

            CubismPart part = null;
            while (parentPartIndex > 0)
            {
                for (var partIndex = 0; partIndex < Model.Parts.Length; partIndex++)
                {
                    part = Model.Parts[partIndex];
                    if (part.UnmanagedIndex == parentPartIndex)
                    {
                        break;
                    }
                }

                parentPartIndex = part?.UnmanagedParentIndex ?? -1;
            }

            return parentPartIndex == 0;
        }

        /// <summary>
        /// Draws all draw objects in model for after Cubism 5.3.
        /// </summary>
        private void DrawObjects()
        {
            // Ready the frame buffer.
            TryInitializeFrameBuffers();

            // Setting up the command buffer.
            _commandBuffer.Clear();

            if ((OffscreenRenderers?.Length ?? 0) > 0)
            {
                // Clear offscreen rendering frame buffer.
                _commandBuffer.SetRenderTarget(OffscreenRenderingFrameBuffer);
                _commandBuffer.ClearRenderTarget(true, true, Color.clear);

                CubismOffscreenRenderTextureManager.GetInstance().ClearRenderTextures(_commandBuffer);
            }
            // Clear the system render texture for each model (used as a temporary buffer).
            _commandBuffer.SetRenderTarget(CubismCommonRenderFrameBuffer.GetInstance().CommonFrameBuffer);
            _commandBuffer.ClearRenderTarget(true, true, Color.clear);
            // Set the render target to the model framebuffer.
            _commandBuffer.SetRenderTarget(ModelFrameBuffer);
            _commandBuffer.ClearRenderTarget(true, true, Color.clear);

            // Reset the current frame buffer.
            CurrentFrameBuffer = ModelFrameBuffer;
            CurrentOffscreenUnmanagedIndex = -1;

            // If the model has an offscreen that serves as the rendering destination for all draw objects.
            if (_hasRootPartOffscreen
                && OffscreenRenderers != null
                && RootFrameBuffer == ModelFrameBuffer)
            {
                for (var i = 0; i < OffscreenRenderers.Length; i++)
                {
                    var offscreenRenderer = OffscreenRenderers[i];
                    if (offscreenRenderer.Offscreen.UnmanagedIndex != 0)
                    {
                        continue;
                    }

                    RootFrameBuffer = OffscreenRenderers[i].OffscreenFrameBuffer;
                    break;
                }
            }

            if (SortedRenderers == null)
            {
                return;
            }

            if (DidChangeDrawableRenderOrder)
            {
                SortRenderers();
            }

            // Begin rendering the model.
            for (var i = 0; i < SortedRenderers.Length; i++)
            {
                if (SortedRenderers[i] == null
                    || !SortedRenderers[i].gameObject.activeSelf
                    || !SortedRenderers[i].MeshRenderer.enabled)
                {
                    continue;
                }

                SortedRenderers[i].DrawObject(_commandBuffer, CurrentFrameBuffer);
            }

            SubmitDrawOffscreen();

            // Execute the command buffer.
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }

        /// <summary>
        /// Submits drawing offscreen for all offscreen renderers.
        /// </summary>
        private void SubmitDrawOffscreen()
        {
            while (
                (CurrentOffscreenUnmanagedIndex != -1)
                 && (CurrentFrameBuffer != ModelFrameBuffer)
             )
            {

                CubismRenderer offscreenRenderer = null;
                for (var offscreenRendererIndex = 0; offscreenRendererIndex < OffscreenRenderers.Length; offscreenRendererIndex++)
                {
                    if (OffscreenRenderers[offscreenRendererIndex].Offscreen.UnmanagedIndex != CurrentOffscreenUnmanagedIndex)
                    {
                        continue;
                    }

                    offscreenRenderer = OffscreenRenderers[offscreenRendererIndex];
                    break;
                }

                RenderTexture previousOffscreen = null;
                var currentOwnerIndex = offscreenRenderer?.Offscreen.OwnerIndex ?? -1;
                var parentIndex = Model.Parts[currentOwnerIndex].UnmanagedParentIndex;

                var previousIndex = -1;
                // Find the offscreen renderer with the previous offscreen unmanaged index.
                while (parentIndex != -1)
                {
                    var part = Model.Parts[parentIndex];
                    if (part.OffscreenIndex != -1)
                    {
                        for (var offscreenRendererIndex = 0; offscreenRendererIndex < OffscreenRenderers.Length; offscreenRendererIndex++)
                        {
                            var element = OffscreenRenderers[offscreenRendererIndex];

                            if (!element.isActiveAndEnabled
                                || !element.MeshRenderer.enabled)
                            {
                                continue;
                            }

                            if (element.Offscreen.UnmanagedIndex != part.OffscreenIndex)
                            {
                                continue;
                            }

                            previousOffscreen = OffscreenRenderers[offscreenRendererIndex].OffscreenFrameBuffer;
                            previousIndex = part.OffscreenIndex;
                            break;
                        }
                    }
                    parentIndex = Model.Parts[parentIndex].UnmanagedParentIndex;

                    if (previousOffscreen == null)
                    {
                        continue;
                    }

                    break;
                }

                if (previousOffscreen == null)
                {
                    previousOffscreen = RootFrameBuffer != CurrentFrameBuffer
                        ? RootFrameBuffer
                        : ModelFrameBuffer;
                }

                // If It can copy the parent offscreen, copy it to the current offscreen renderer.
                offscreenRenderer?.DrawOffscreen(_commandBuffer, previousOffscreen, offscreenRenderer);

                if (offscreenRenderer != null)
                {
                    offscreenRenderer.OffscreenFrameBuffer = null;
                }

                CurrentFrameBuffer = previousOffscreen;
                CurrentOffscreenUnmanagedIndex = previousIndex;
            }
        }

        /// <summary>
        /// Try to initialize the frame buffers.
        /// </summary>
        public void TryInitializeFrameBuffers(bool forceInit = false)
        {
            if (forceInit
                || (!Mathf.Approximately(CubismCommonRenderFrameBuffer.GetInstance().Size.Width, _orgViewport.width)
                    || !Mathf.Approximately(CubismCommonRenderFrameBuffer.GetInstance().Size.Height, _orgViewport.height)))
            {
                if (!ModelFrameBuffer)
                {
                    return;
                }

                // Initialize model frame buffer.
                ModelFrameBuffer.Release();
                ModelFrameBuffer.width = CubismCommonRenderFrameBuffer.GetInstance().Size.Width;
                ModelFrameBuffer.height = CubismCommonRenderFrameBuffer.GetInstance().Size.Height;
                ModelFrameBuffer.depth = 24;
                ModelFrameBuffer.format = RenderTextureFormat.ARGB32;
                ModelFrameBuffer.filterMode = FilterMode.Point;
                ModelFrameBuffer.Create();

                // Initialize offscreen frame buffer.
                if ((Model.Offscreens?.Length ?? 0) > 0)
                {
                    if (OffscreenRenderingFrameBuffer)
                    {
                        OffscreenRenderingFrameBuffer.Release();
                        OffscreenRenderingFrameBuffer.width = CubismCommonRenderFrameBuffer.GetInstance().Size.Width;
                        OffscreenRenderingFrameBuffer.height = CubismCommonRenderFrameBuffer.GetInstance().Size.Height;
                        OffscreenRenderingFrameBuffer.depth = 24;
                        OffscreenRenderingFrameBuffer.format = RenderTextureFormat.ARGB32;
                        OffscreenRenderingFrameBuffer.filterMode = FilterMode.Point;
                    }
                    else
                    {
                        OffscreenRenderingFrameBuffer = new RenderTexture(Screen.width, Screen.height, 24)
                        {
                            name = "OffscreenRenderingFrameBuffer",
                            filterMode = FilterMode.Point,
                            wrapMode = TextureWrapMode.Repeat,
                            format = RenderTextureFormat.ARGB32
                        };
                    }

                    OffscreenRenderingFrameBuffer.Create();
                }

                // Store the current viewport size.
                _orgViewport = new Rect(0, 0, CubismCommonRenderFrameBuffer.GetInstance().Size.Width, CubismCommonRenderFrameBuffer.GetInstance().Size.Height);

                // Set the frame buffer size.
                CubismCommonRenderFrameBuffer.GetInstance().SetFrameBufferSize(CubismCommonRenderFrameBuffer.GetInstance().Size.Width, CubismCommonRenderFrameBuffer.GetInstance().Size.Height);

                // Create and set the material property block.
                _properties = new MaterialPropertyBlock();

                _properties.SetTexture("_MainTex", ModelFrameBuffer);
                ModelCanvasRenderer?.SetPropertyBlock(_properties);
                RootFrameBuffer = ModelFrameBuffer;
            }
        }

        /// <summary>
        /// Initializes the frame buffer <see cref="OnEnable()"/>.
        /// </summary>
        private void InitializeFrameBufferOnEnable()
        {
            if (!Model.IsUsingBlendMode)
            {
                return;
            }

            CurrentOffscreenUnmanagedIndex = -1;

            // Create the command buffer.
            _commandBuffer = new CommandBuffer();

            // Create the model frame buffer.
            ModelFrameBuffer = new RenderTexture(Screen.width, Screen.height, 24)
            {
                name = "ModelFrameBuffer",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                format = RenderTextureFormat.ARGB32
            };
            CurrentFrameBuffer = ModelFrameBuffer;
            RootFrameBuffer = ModelFrameBuffer;

            // Create the offscreen rendering frame buffer if necessary.
            if ((Model.Offscreens?.Length ?? 0) > 0)
            {
                OffscreenRenderingFrameBuffer = new RenderTexture(Screen.width, Screen.height, 24)
                {
                    name = "OffscreenRenderingFrameBuffer",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat,
                    format = RenderTextureFormat.ARGB32
                };
            }

            MaskController = GetComponent<CubismMaskController>();

            // Try to initialize the frame buffers.
            TryInitializeFrameBuffers();
        }

        /// <summary>
        /// Called from Unity.
        /// </summary>
        private void OnDestroy()
        {
            // Release buffers.
            DestroyFrameBuffer();
        }

        /// <summary>
        /// Destroys the frame buffer <see cref="OnDestroy()"/>.
        /// </summary>
        private void DestroyFrameBuffer()
        {
            if (ModelFrameBuffer)
            {
                ModelFrameBuffer.Release();
                ModelFrameBuffer = null;
#if UNITY_EDITOR
                DestroyImmediate(ModelFrameBuffer);
#else
                Destroy(OffscreenRenderingFrameBuffer);
#endif
            }

            if (OffscreenRenderingFrameBuffer)
            {
                OffscreenRenderingFrameBuffer.Release();
                OffscreenRenderingFrameBuffer = null;
#if UNITY_EDITOR
                DestroyImmediate(OffscreenRenderingFrameBuffer);
#else
            Destroy(OffscreenRenderingFrameBuffer);
#endif
            }

            RootFrameBuffer = null;
        }
    }
}
