/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Utils;
using Live2D.Cubism.Rendering.URP.RenderingInterceptor;
using System;
using UnityEngine;
using Object = UnityEngine.Object;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Controls rendering of a <see cref="CubismModel"/>.
    /// </summary>
    [ExecuteInEditMode, CubismDontMoveOnReimport]
    public sealed partial class CubismRenderController : MonoBehaviour, ICubismUpdatable
    {
        /// <summary>
        /// Model opacity.
        /// </summary>
        /// <remarks>
        /// This is turned into a field to be available to <see cref="AnimationClip"/>s...
        /// </remarks>
        [SerializeField, HideInInspector]
        public float Opacity = 1f;

        /// <summary>
        /// <see cref="LastOpacity"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private float _lastOpacity;

        /// <summary>
        /// Last model opacity.
        /// </summary>
        private float LastOpacity
        {
            get { return _lastOpacity; }
            set { _lastOpacity = value; }
        }

        /// <summary>
        /// <see cref="MultiplyColorEnabled"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _isOverriddenModelMultiplyColors;

        /// <summary>
        /// Whether to override with multiply color from the model.
        /// </summary>
        public bool MultiplyColorEnabled
        {
            get { return _isOverriddenModelMultiplyColors; }
            set { _isOverriddenModelMultiplyColors = value; }
        }

        /// <summary>
        /// <see cref="ScreenColorEnabled"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _isOverriddenModelScreenColors;

        /// <summary>
        /// Whether to override with screen color from the model.
        /// </summary>
        public bool ScreenColorEnabled
        {
            get { return _isOverriddenModelScreenColors; }
            set { _isOverriddenModelScreenColors = value; }
        }

        /// <summary>
        /// <see cref="ModelMultiplyColor"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Color _modelMultiplyColor;

        /// <summary>
        /// Multiply colors used throughout the model.
        /// </summary>
        public Color ModelMultiplyColor
        {
            get { return _modelMultiplyColor; }
            set { _modelMultiplyColor = value; }
        }

        /// <summary>
        /// <see cref="ModelScreenColor"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Color _modelScreenColor;

        /// <summary>
        /// Screen colors used throughout the model.
        /// </summary>
        public Color ModelScreenColor
        {
            get { return _modelScreenColor; }
            set { _modelScreenColor = value; }
        }

        /// <summary>
        /// Sorting layer name.
        /// </summary>
        public string SortingLayer
        {
            get
            {
                return UnityEngine.SortingLayer.IDToName(SortingLayerId);
            }
            set
            {
                SortingLayerId = UnityEngine.SortingLayer.NameToID(value);
            }
        }

        /// <summary>
        /// <see cref="SortingLayerId"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _sortingLayerId;

        /// <summary>
        /// Sorting layer Id.
        /// </summary>
        public int SortingLayerId
        {
            get
            {
                return _sortingLayerId;
            }
            set
            {
                if (value == _sortingLayerId)
                {
                    return;
                }


                _sortingLayerId = value;


                // Apply sorting layer.
                var renderers = Renderers;


                for (var i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].OnControllerSortingLayerDidChange(_sortingLayerId);
                }

                SortRenderers();
            }
        }


        /// <summary>
        /// <see cref="SortingMode"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismSortingMode _sortingMode;

        /// <summary>
        /// <see cref="CubismDrawable"/> sorting.
        /// </summary>
        public CubismSortingMode SortingMode
        {
            get
            {
                return _sortingMode;
            }
            set
            {
                // Return early if same value given.
                if (value == _sortingMode)
                {
                    return;
                }


                _sortingMode = value;


                // Flip sorting.
                var renderers = Renderers;


                for (var i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].OnControllerSortingModeDidChange(_sortingMode);
                }

                SortRenderers();
            }
        }


        /// <summary>
        /// Order in sorting layer.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _sortingOrder;

        /// <summary>
        /// Order in sorting layer.
        /// </summary>
        public int SortingOrder
        {
            get
            {
                return _sortingOrder;
            }
            set
            {
                // Return early in case same value given.
                if (value == _sortingOrder)
                {
                    return;
                }


                _sortingOrder = value;


                // Apply new sorting order.
                var renderers = Renderers;


                for (var i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].OnControllerSortingOrderDidChange(SortingOrder);
                }

                SortRenderers();
            }
        }


        /// <summary>
        /// [Optional] Camera to face.
        /// </summary>
        [SerializeField]
        public Camera CameraToFace;



        /// <summary>
        /// <see cref="DrawOrderHandler"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Object _drawOrderHandler;

        /// <summary>
        /// Draw order handler proxy object.
        /// </summary>
        public Object DrawOrderHandler
        {
            get { return _drawOrderHandler; }
            set { _drawOrderHandler = value.ToNullUnlessImplementsInterface<ICubismDrawOrderHandler>(); }
        }


        /// <summary>
        /// <see cref="DrawOrderHandlerInterface"/> backing field.
        /// </summary>
        [NonSerialized]
        private ICubismDrawOrderHandler _drawOrderHandlerInterface;

        /// <summary>
        /// Listener for draw order changes.
        /// </summary>
        private ICubismDrawOrderHandler DrawOrderHandlerInterface
        {
            get
            {
                if (_drawOrderHandlerInterface == null)
                {
                    _drawOrderHandlerInterface = DrawOrderHandler.GetInterface<ICubismDrawOrderHandler>();
                }


                return _drawOrderHandlerInterface;
            }
        }


        /// <summary>
        /// <see cref="OpacityHandler"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Object _opacityHandler;

        /// <summary>
        /// Opacity handler proxy object.
        /// </summary>
        public Object OpacityHandler
        {
            get { return _opacityHandler; }
            set { _opacityHandler = value.ToNullUnlessImplementsInterface<ICubismOpacityHandler>(); }
        }


        /// <summary>
        /// <see cref="OpacityHandler"/> backing field.
        /// </summary>
        private ICubismOpacityHandler _opacityHandlerInterface;

        /// <summary>
        /// Listener for opacity changes.
        /// </summary>
        private ICubismOpacityHandler OpacityHandlerInterface
        {
            get
            {
                if (_opacityHandlerInterface == null)
                {
                    _opacityHandlerInterface = OpacityHandler.GetInterface<ICubismOpacityHandler>();
                }


                return _opacityHandlerInterface;
            }
        }


        /// <summary>
        /// <see cref="MultiplyColorHandler"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Object _multiplyColorHandler;

        /// <summary>
        /// Opacity handler proxy object.
        /// </summary>
        public Object MultiplyColorHandler
        {
            get { return _multiplyColorHandler; }
            set { _multiplyColorHandler = value.ToNullUnlessImplementsInterface<ICubismBlendColorHandler>(); }
        }


        /// <summary>
        /// <see cref="MultiplyColorHandler"/> backing field.
        /// </summary>
        private ICubismBlendColorHandler _multiplyColorHandlerInterface;

        /// <summary>
        /// Listener for blend color changes.
        /// </summary>
        private ICubismBlendColorHandler MultiplyColorHandlerInterface
        {
            get
            {
                if (_multiplyColorHandlerInterface == null)
                {
                    _multiplyColorHandlerInterface = MultiplyColorHandler?.GetInterface<ICubismBlendColorHandler>();
                }


                return _multiplyColorHandlerInterface;
            }
        }

        /// <summary>
        /// <see cref="ScreenColorHandler"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Object _screenColorHandler;

        /// <summary>
        /// Screen color handler proxy object.
        /// </summary>
        public Object ScreenColorHandler
        {
            get { return _screenColorHandler; }
            set { _screenColorHandler = value.ToNullUnlessImplementsInterface<ICubismBlendColorHandler>(); }
        }


        /// <summary>
        /// <see cref="MultiplyColorHandler"/> backing field.
        /// </summary>
        private ICubismBlendColorHandler _screenColorHandlerInterface;

        /// <summary>
        /// Listener for blend color changes.
        /// </summary>
        private ICubismBlendColorHandler ScreenColorHandlerInterface
        {
            get
            {
                if (_screenColorHandlerInterface == null)
                {
                    _screenColorHandlerInterface = ScreenColorHandler?.GetInterface<ICubismBlendColorHandler>();
                }


                return _screenColorHandlerInterface;
            }
        }

        /// <summary>
        /// The value to offset the <see cref="CubismDrawable"/>s by.
        /// </summary>
        /// <remarks>
        /// You only need to adjust this value when using perspective cameras.
        /// </remarks>
        [SerializeField, HideInInspector]
        private float _depthOffset = 0.00001f;

        /// <summary>
        /// Depth offset used when sorting by depth.
        /// </summary>
        public float DepthOffset
        {
            get { return _depthOffset; }
            set
            {
                // Return if same value given.
                if (Mathf.Abs(value - _depthOffset) < CubismMath.Epsilon)
                {
                    return;
                }


                // Store value.
                _depthOffset = value;


                // Apply it.
                var renderers = Renderers;


                for (var i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].OnControllerDepthOffsetDidChange(_depthOffset);
                }
            }
        }

        /// <summary>
        /// <see cref="Model"/>'s backing field.
        /// </summary>
        [NonSerialized]
        private CubismModel _cubismModel;

        /// <summary>
        /// Model the controller belongs to.
        /// </summary>
        public CubismModel Model
        {
            get
            {
                if (_cubismModel == null)
                {
                    _cubismModel = this.FindCubismModel();
                }

                return _cubismModel;
            }
        }


        /// <summary>
        /// <see cref="DrawablesRootTransform"/> backing field.
        /// </summary>
        private Transform _drawablesRootTransform;

        /// <summary>
        /// Root transform of all <see cref="CubismDrawable"/>s of the model.
        /// </summary>
        private Transform DrawablesRootTransform
        {
            get
            {
                if (_drawablesRootTransform == null)
                {
                    _drawablesRootTransform = Model.Drawables[0].transform.parent;
                }


                return _drawablesRootTransform;
            }
        }

        /// <summary>
        /// <see cref="Renderers"/>s backing field.
        /// </summary>
        [SerializeField]
        private CubismRenderer[] _renderers;

        /// <summary>
        /// <see cref="CubismRenderer"/>s.
        /// </summary>
        public CubismRenderer[] Renderers
        {
            get
            {
                if (_renderers == null)
                {
                    TryInitialize();
                }

                return _renderers;
            }
            private set { _renderers = value; }
        }


        /// <summary>
        /// multiply color buffer.
        /// </summary>
        private Color[] _newMultiplyColors;

        /// <summary>
        /// screen color buffer.
        /// </summary>
        private Color[] _newScreenColors;

        /// <summary>
        /// Maps drawable unmanaged index to index in <see cref="DrawableRenderers"/>; -1 if none.
        /// </summary>
        private int[] _rendererIndicesByDrawableIndices;

        /// <summary>
        /// When true, <see cref="_rendererIndicesByDrawableIndices"/> is rebuilt on the next
        /// <see cref="OnDynamicDrawableData"/>. Set whenever <see cref="DrawableRenderers"/> changes;
        /// the mapping is otherwise constant while the drawable composition is unchanged.
        /// </summary>
        private bool _rendererIndicesDirty = true;


        /// <summary>
        /// Model has update controller component.
        /// </summary>
        [HideInInspector]
        public bool HasUpdateController { get; set; }

        /// <summary>
        /// <see cref="IsInitialized"/>s backing field.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Is renderers initialized.
        /// </summary>
        [HideInInspector]
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
            private set
            {
                _isInitialized = value;
            }
        }

        /// <summary>
        /// Makes sure all <see cref="CubismDrawable"/>s have <see cref="CubismRenderer"/>s attached to them.
        /// </summary>
        public void TryInitialize()
        {
            IsInitialized = false;

            var model = Model;

#if UNITY_EDITOR
            // Rebuild the native model only when the drawable cache is stale, i.e. it references a released or
            // previous unmanaged model as a reimport produces. Rebuilding when it already matches the current
            // unmanaged model would reproduce the same mismatch, so it is skipped to avoid releasing and
            // recreating the native model on every initialization retry. Destroyed renderers and count/binding
            // mismatches are rebuilt below without touching native memory, and no early return is taken so a
            // recoverable model keeps being (re)initialized instead of leaving rendering stopped.
            if (model != null && model.IsDrawableCacheStale())
            {
                CubismModel.ResetNonSerializedFields(model);
                model.Revive();
            }
#endif

            // Discard every renderer cache that no longer matches the model, so they are rebuilt together and
            // stay consistent. The caches are only dropped when they are actually stale: the full cache is
            // serialized, so keeping a valid one spares the rebuild on every enable. A model without drawables
            // has nothing to rebuild from, so its caches are left untouched.
            if (model != null && model.Drawables != null && !AreCachedRenderersValid())
            {
                Renderers = null;
                DrawableRenderers = null;
                OffscreenRenderers = null;
            }

            // Try to get renderers.
            var renderers = _renderers;
            TryInitializeRenderers(renderers);

            if (_renderers == null
                || _renderers.Length < 1)
            {
                return;
            }

            // Make sure renderers are initialized.
            for (var i = 0; i < Renderers.Length; ++i)
            {
                var targetRenderer = Renderers[i];
                targetRenderer.TryInitialize(this);
                if (!HasRootPartOffscreen)
                {
                    continue;
                }

                HasRootPartOffscreen = CheckHasRootPartOffscreen(targetRenderer);
            }

            // Initialize sorting layer.
            // We set the backing field here directly because we pull the sorting layer directly from the renderer.
            _sortingLayerId = Renderers[0]
                .MeshRenderer
                .sortingLayerID;

            OnAfterRenderersInitialize(Renderers);

            IsInitialized = true;
        }

        /// <summary>
        /// Updates opacity if necessary.
        /// </summary>
        private void UpdateOpacity()
        {
            // Return if same value given.
            if (Mathf.Abs(Opacity - LastOpacity) < CubismMath.Epsilon)
            {
                return;
            }


            // Store value.
            Opacity = Mathf.Clamp(Opacity, 0f, 1f);
            LastOpacity = Opacity;


            // Apply opacity.
            var applyOpacityToRenderers = (OpacityHandlerInterface == null || Opacity > (1f - CubismMath.Epsilon));


            if (applyOpacityToRenderers && Renderers != null)
            {
                var renderers = Renderers;


                for (var i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].OnModelOpacityDidChange(Opacity);
                }
            }


            // Call handler.
            if (OpacityHandlerInterface != null)
            {
                OpacityHandlerInterface.OnOpacityDidChange(this, Opacity);
            }
        }

        /// <summary>
        /// Updates Blend Colors if necessary.
        /// </summary>
        private void UpdateDrawableBlendColors()
        {
            if (Renderers == null
                || !IsInitialized)
            {
                return;
            }

            var isMultiplyColorUpdated = false;
            var isScreenColorUpdated = false;

            if ((_newMultiplyColors?.Length ?? 0) != Renderers.Length)
            {
                _newMultiplyColors = new Color[Renderers.Length];
            }

            if ((_newScreenColors?.Length ?? 0) != Renderers.Length)
            {
                _newScreenColors = new Color[Renderers.Length];
            }

            for (var i = 0; i < Renderers.Length; i++)
            {
                if (!Renderers[i])
                {
                    continue;
                }

                var isUseUserMultiplyColor = (Renderers[i].DrawObjectMultiplyColorEnabled ||
                                              MultiplyColorEnabled);

                if (isUseUserMultiplyColor)
                {
                    // If you switch from a setting that uses the color of the model, revert to the color that was retained.
                    if (!Renderers[i].LastIsUseUserMultiplyColor)
                    {
                        Renderers[i].MultiplyColor = Renderers[i].LastMultiplyColor;
                        isMultiplyColorUpdated = true;
                    }
                    else if (Renderers[i].LastMultiplyColor != Renderers[i].MultiplyColor)
                    {
                        isMultiplyColorUpdated = true;
                    }

                    Renderers[i].LastMultiplyColor = Renderers[i].MultiplyColor;
                }
                else if (Renderers[i].LastIsUseUserMultiplyColor)
                {
                    Renderers[i].MultiplyColor = Renderers[i].LastMultiplyColor;
                    isMultiplyColorUpdated = true;
                }

                _newMultiplyColors[i] = Renderers[i].MultiplyColor;
                Renderers[i].LastIsUseUserMultiplyColor = isUseUserMultiplyColor;

                var isUseUserScreenColor = (Renderers[i].DrawObjectScreenColorEnabled ||
                                            ScreenColorEnabled);

                if (isUseUserScreenColor)
                {
                    // If you switch from a setting that uses the color of the model, revert to the color that was retained.
                    if (!Renderers[i].LastIsUseUserScreenColors)
                    {
                        Renderers[i].ScreenColor = Renderers[i].LastScreenColor;
                        isScreenColorUpdated = true;
                    }
                    else if (Renderers[i].LastScreenColor != Renderers[i].ScreenColor)
                    {
                        isScreenColorUpdated = true;
                    }

                    Renderers[i].LastScreenColor = Renderers[i].ScreenColor;
                }
                else if (Renderers[i].LastIsUseUserScreenColors)
                {
                    Renderers[i].ScreenColor = Renderers[i].LastScreenColor;
                    isScreenColorUpdated = true;
                }

                _newScreenColors[i] = Renderers[i].ScreenColor;
                Renderers[i].LastIsUseUserScreenColors = isUseUserScreenColor;
            }

            if (MultiplyColorHandler != null && isMultiplyColorUpdated)
            {
                MultiplyColorHandlerInterface.OnBlendColorDidChange(this, _newMultiplyColors);
            }

            if (ScreenColorHandler != null && isScreenColorUpdated)
            {
                ScreenColorHandlerInterface.OnBlendColorDidChange(this, _newScreenColors);
            }
        }

        /// <summary>
        /// Updates <see cref="DidChangeSorting"/> from direction changes.
        /// </summary>
        internal void UpdateDidChangeSortingFromZ(Vector3 cameraPosition)
        {
            // Return early if not sorting by depth.
            if (!SortingMode.SortByDepth())
            {
                return;
            }

            for (var i = 0; i < Renderers?.Length; i++)
            {
                var cubismRenderer = Renderers[i];

                if (!cubismRenderer)
                {
                    continue;
                }

                // Check if direction updated from last sorted.
                DidChangeSorting |= cubismRenderer.DidUpdateDirectionFromLastSorted(cameraPosition);
            }
        }

        /// <summary>
        /// Called by cubism update controller. Order to invoke OnLateUpdate.
        /// </summary>
        public int ExecutionOrder
        {
            get { return CubismUpdateExecutionOrder.CubismRenderController; }
        }

        /// <summary>
        /// Called by cubism update controller. Needs to invoke OnLateUpdate on Editing.
        /// </summary>
        public bool NeedsUpdateOnEditing
        {
            get { return true; }
        }

        /// <summary>
        /// Called by cubism update controller. Applies billboarding.
        /// </summary>
        public void OnLateUpdate()
        {
            // Fail silently...
            if (!enabled || !IsInitialized)
            {
                return;
            }

            // Update opacity if necessary.
            UpdateOpacity();

            // Updates Blend Colors if necessary.
            UpdateDrawableBlendColors();

            // Return early in case no camera is to be faced.
            if (CameraToFace == null)
            {
                return;
            }

            // Face camera.
            DrawablesRootTransform.rotation = (Quaternion.LookRotation(CameraToFace.transform.forward, Vector3.up));
        }

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void Start()
        {
            // Get cubism update controller.
            HasUpdateController = (GetComponent<CubismUpdateController>() != null);
        }

        /// <summary>
        /// Checks whether the cached renderer arrays still match the current model. Returns <c>false</c> when a
        /// cache is missing, its draw object count no longer matches, any cached renderer has been destroyed
        /// (for example after a model reimport that reduced the drawable count), or a cached renderer is no
        /// longer bound to the draw object it is stored for.
        /// </summary>
        /// <remarks>
        /// The full <see cref="Renderers"/> cache (<c>_renderers</c>, serialized) and the
        /// <see cref="DrawableRenderers"/> / <see cref="OffscreenRenderers"/> caches (non-serialized, lazily
        /// rebuilt) have different lifetimes and can go stale independently, so all three are validated here.
        /// The lazy caches are read through their backing fields to avoid triggering a rebuild during the check.
        /// </remarks>
        private bool AreCachedRenderersValid()
        {
            var model = Model;

            if (_renderers == null || model == null || model.Drawables == null)
            {
                return false;
            }

            var drawables = model.Drawables;
            var offscreens = model.Offscreens;
            var offscreenCount = (offscreens != null) ? offscreens.Length : 0;

            if (_renderers.Length != drawables.Length + offscreenCount)
            {
                return false;
            }

            // Unity's overloaded equality reports destroyed objects as null, so the null check also covers a
            // renderer that a reimport destroyed. The short-circuit keeps a destroyed entry from being accessed.
            for (var i = 0; i < drawables.Length; ++i)
            {
                if (_renderers[i] == null || _renderers[i].Drawable != drawables[i])
                {
                    return false;
                }
            }

            // The offscreen renderers occupy the tail of the array. The total draw object count can stay the
            // same while offscreens are swapped or the drawable/offscreen split changes, so they are matched
            // against the model as well.
            for (var i = 0; i < offscreenCount; ++i)
            {
                var rendererIndex = drawables.Length + i;

                if (_renderers[rendererIndex] == null || _renderers[rendererIndex].Offscreen != offscreens[i])
                {
                    return false;
                }
            }

            // Only validate the lazy caches when they are already populated; a null cache is rebuilt on demand.
            // Both hold the very renderers the full cache holds, so comparing against it detects entries left
            // over from a previous generation, which a reimport produces because only the full cache is
            // serialized and therefore replaced along with the prefab.
            if (_drawableRenderers != null)
            {
                if (_drawableRenderers.Length != drawables.Length)
                {
                    return false;
                }

                for (var i = 0; i < _drawableRenderers.Length; ++i)
                {
                    if (_drawableRenderers[i] != _renderers[i])
                    {
                        return false;
                    }
                }
            }

            if (_offscreenRenderers != null)
            {
                if (_offscreenRenderers.Length != offscreenCount)
                {
                    return false;
                }

                for (var i = 0; i < _offscreenRenderers.Length; ++i)
                {
                    if (_offscreenRenderers[i] != _renderers[drawables.Length + i])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Called by Unity. Enables listening to render data updates.
        /// </summary>
        private void OnEnable()
        {
            // Fail silently.
            if (!Model)
            {
                return;
            }

            CurrentOffscreenUnmanagedIndex = -1;

            // Make sure renderers are available.
            // IsInitialized can survive a model reimport that does not trigger a domain reload, while the
            // cached renderers for removed drawables have already been destroyed. Re-initialize whenever the
            // cache no longer matches the model so the destroyed renderers are rebuilt before they are used.
            if (!IsInitialized || !AreCachedRenderersValid())
            {
                Model.Revive();
                TryInitialize();
            }


            // Register listener.
            Model.OnDynamicDrawableData += OnDynamicDrawableData;

#if UNITY_EDITOR
            // Only drive the editor mesh sync when the renderers were initialized and the model is consistent
            // with its native model. Running ForceUpdateNow while the model is stale would write mismatched
            // parameter/part/drawable data to the native model, and the renderer caches may not have been
            // rebuilt yet (leaving DrawableRenderers null), so the sync is skipped until initialization settles.
            if (!Application.isPlaying && IsInitialized && !Model.IsDrawableCacheStale())
            {
                Model.ForceUpdateNow();

                var drawableRenderers = DrawableRenderers;

                if (drawableRenderers != null)
                {
                    for (var drawableIndex = 0; drawableIndex < drawableRenderers.Length; drawableIndex++)
                    {
                        var drawableRenderer = drawableRenderers[drawableIndex];

                        // Skip any renderer that is missing or was destroyed, so a stale cache cannot throw here.
                        if (!drawableRenderer)
                        {
                            continue;
                        }

                        // Apply the initial visibility flags and render orders produced by ForceUpdateNow before the editor mesh sync advances SwapInfo.
                        drawableRenderer.UpdateVisibility();
                        drawableRenderer.UpdateRenderOrder();

                        drawableRenderer.SwapMeshes();
                    }
                }
            }
#endif

            if (GetComponent<ICubismRenderingInterceptor>() != null)
            {
                // Do not register at common controller when a rendering interceptor is attached.
                return;
            }

            // Register at common controller.
            CubismRenderControllerGroup.GetInstance().AddRenderController(this);
        }

        /// <summary>
        /// Called by Unity. Disables listening to render data updates.
        /// </summary>
        private void OnDisable()
        {
            // Fail silently.
            if (!Model)
            {
                return;
            }

            // Deregister listener.
            Model.OnDynamicDrawableData -= OnDynamicDrawableData;

            // Deregister at common controller.
            CubismRenderControllerGroup.GetInstance().RemoveRenderController(this);
        }

#endregion

        #region Cubism Event Handling

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void LateUpdate()
        {
            if (!HasUpdateController)
            {
                OnLateUpdate();
            }
        }

        /// <summary>
        /// Called whenever new render data is available.
        /// </summary>
        /// <param name="sender">Model with new render data.</param>
        /// <param name="data">New render data.</param>
        private void OnDynamicDrawableData(CubismModel sender, CubismDynamicDrawableData[] data)
        {
            if (data == null)
            {
                return;
            }

            var shouldInitialize = !IsInitialized;

#if UNITY_EDITOR
            // Neither IsInitialized nor the lazily rebuilt renderer caches are serialized, so both survive a model
            // reimport that replaces the serialized renderers and destroys the previous ones. Nothing guarantees an
            // enable between that reimport and the next model update, so the cache is revalidated here the way
            // OnEnable does it. The check walks every draw object, and only a reimport produces that state, so it is
            // confined to the editor outside of play mode.
            if (!shouldInitialize && !Application.isPlaying)
            {
                shouldInitialize = !AreCachedRenderersValid();
            }
#endif

            if (shouldInitialize)
            {
                // Initializing here can rebuild the backend model, which leaves the data of this call belonging
                // to the model that was just replaced. Its drawable and vertex counts do not have to match the
                // new model, so applying it would write mismatched vertex buffers and index past the current
                // drawables. The data is dropped instead, and the next update delivers data for the new model.
                var nativeModelRevision = sender.NativeModelRevision;

                TryInitialize();

                if (!IsInitialized || sender.NativeModelRevision != nativeModelRevision)
                {
                    return;
                }
            }

            // Get drawables.
            var drawables = sender.Drawables;
            var renderers = DrawableRenderers;

            // The lazy rebuild skips drawables that were destroyed and returns null for a model without drawables,
            // so the cache can be unusable even though initialization succeeded.
            if (renderers == null)
            {
                return;
            }

            var isMultiplyColorUpdated = false;
            var isScreenColorUpdated = false;

            // Grow the blend color buffers when they are smaller than the current drawable renderer count.
            // A model reimport can rebuild the renderers and the reverse mapping with a larger count while
            // these cached arrays still hold the previous (smaller) length, so a plain null-coalescing
            // allocation is not enough and would index out of bounds below.
            if ((_newMultiplyColors?.Length ?? 0) < renderers.Length)
            {
                _newMultiplyColors = new Color[renderers.Length];
            }

            if ((_newScreenColors?.Length ?? 0) < renderers.Length)
            {
                _newScreenColors = new Color[renderers.Length];
            }

            var newMultiplyColors = _newMultiplyColors;
            var newScreenColors = _newScreenColors;

            // Offscreen draw states are synchronized to the model update generation of Drawable draw states.
            var offscreenRenderers = OffscreenRenderers;
            for (var offscreenIndex = 0; offscreenIndex < OffscreenRenderers?.Length; offscreenIndex++ )
            {
                var offscreenRenderer = offscreenRenderers[offscreenIndex];

                // Skip an offscreen renderer the cache still references after it was destroyed, as the drawable
                // renderers are skipped below.
                if (!offscreenRenderer)
                {
                    continue;
                }

                offscreenRenderer.UpdateOffscreenDrawState();
            }

            // Rebuild the drawable->renderer reverse mapping only when the drawable count changes or the
            // renderer set was reinitialized (_rendererIndicesDirty). It is otherwise constant, so
            // rebuilding it every frame would be redundant.
            if ((_rendererIndicesByDrawableIndices?.Length ?? 0) != data.Length)
            {
                _rendererIndicesByDrawableIndices = new int[data.Length];
                _rendererIndicesDirty = true;
            }

            if (_rendererIndicesDirty)
            {
                // Fill mapping with -1 values.
                for (var i = 0; i < _rendererIndicesByDrawableIndices.Length; i++)
                {
                    _rendererIndicesByDrawableIndices[i] = -1;
                }

                // Fill mapping with actual indices.
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var mappedRenderer = renderers[rendererIndex];

                    // Leave a destroyed renderer unmapped so it cannot take the slot of the live renderer that
                    // holds the same drawable index. The drawable is assigned while the renderers are initialized,
                    // so a renderer the lazy rebuild picked up on its own has none and is left unmapped as well.
                    if (!mappedRenderer || !mappedRenderer.Drawable)
                    {
                        continue;
                    }

                    var unmanagedIndex = mappedRenderer.Drawable.UnmanagedIndex;

                    // Only set if not already set and within bounds.
                    if (unmanagedIndex >= 0 && unmanagedIndex < _rendererIndicesByDrawableIndices.Length && _rendererIndicesByDrawableIndices[unmanagedIndex] < 0)
                    {
                        _rendererIndicesByDrawableIndices[unmanagedIndex] = rendererIndex;
                    }
                }

                _rendererIndicesDirty = false;
            }

            // Handle render data changes.
            for (var dataIndex = 0; dataIndex < data.Length; ++dataIndex)
            {
                var rendererIndex = _rendererIndicesByDrawableIndices[dataIndex];

                // Skip if no renderer found.
                if (rendererIndex < 0) {
                    continue;
                }

                var renderer = renderers[rendererIndex];

                // Skip a renderer the cache still references after it was destroyed.
                if (!renderer)
                {
                    _rendererIndicesByDrawableIndices[dataIndex] = -1;
                    _rendererIndicesDirty = true;

                    continue;
                }

                // Controls whether mesh buffers are to be swapped.
                var swapMeshes = renderer.UpdatePendingBlendColors();

                // Update visibility if last SwapInfo flag is true.
                renderer.UpdateVisibility();

                // Update render order if last SwapInfo flags is true.
                renderer.UpdateRenderOrder();

                // Update draw state if last SwapInfo flags is true.
                renderer.UpdateDisplayedDrawState();

                // Skip completely non-dirty data.
                if (!data[dataIndex].IsAnyDirty && !swapMeshes)
                {
                    continue;
                }


                // Update visibility.
                if (data[dataIndex].IsVisibilityDirty)
                {
                    renderer.OnDrawableVisiblityDidChange(data[dataIndex].IsVisible);

                    swapMeshes = true;
                }


                // Update render order.
                if (data[dataIndex].IsRenderOrderDirty)
                {
                    renderer.OnDrawableRenderOrderDidChange(data[dataIndex].RenderOrder);
                    DidChangeDrawableRenderOrder = true;
                    swapMeshes = true;
                }


                // Update opacity.
                if (data[dataIndex].IsOpacityDirty)
                {
                    renderer.OnDrawableOpacityDidChange(data[dataIndex].Opacity);


                    swapMeshes = true;
                }


                // Update vertex positions.
                if (data[dataIndex].AreVertexPositionsDirty)
                {
                    renderer.OnDrawableVertexPositionsDidChange(data[dataIndex].VertexPositions);


                    swapMeshes = true;
                }

                var isUseModelMultiplyColor = !(renderer.DrawObjectMultiplyColorEnabled ||
                                                MultiplyColorEnabled);

                if (data[dataIndex].IsBlendColorDirty && isUseModelMultiplyColor)
                {
                    isMultiplyColorUpdated = true;
                }


                var isUseModelScreenColor = !(renderer.DrawObjectScreenColorEnabled ||
                                              ScreenColorEnabled);

                if (data[dataIndex].IsBlendColorDirty && isUseModelScreenColor)
                {
                    isScreenColorUpdated = true;
                }

                // Swap buffers if necessary.
                // [INV] Swapping only half of the meshes might improve performance even. Would that be visually feasible?
                if (swapMeshes)
                {
                    renderer.SwapMeshes();
                }
            }


            // Pass draw order changes to handler (if available).
            var drawOrderHandler = DrawOrderHandlerInterface;


            if (drawOrderHandler != null)
            {
                // The drawables are indexed with the data index here, so the shorter of the two bounds the loop.
                // The two lengths match while the model is consistent, and a model whose drawable components no
                // longer match its backend would otherwise be indexed out of bounds.
                var drawOrderCount = Mathf.Min(data.Length, drawables.Length);

                for (var i = 0; i < drawOrderCount; ++i)
                {
                    if (data[i].IsDrawOrderDirty)
                    {
                        drawOrderHandler.OnDrawOrderDidChange(this, drawables[i], data[i].DrawOrder);
                    }
                }
            }

            for (var dataIndex = 0; dataIndex < data.Length; ++dataIndex)
            {
                var rendererIndex = _rendererIndicesByDrawableIndices[dataIndex];

                // Skip if no renderer found.
                if (rendererIndex < 0)
                {
                    continue;
                }

                newMultiplyColors[rendererIndex] = renderers[rendererIndex].MultiplyColor;
            }

            for (var dataIndex = 0; dataIndex < data.Length; ++dataIndex)
            {
                var rendererIndex = _rendererIndicesByDrawableIndices[dataIndex];

                // Skip if no renderer found.
                if (rendererIndex < 0)
                {
                    continue;
                }

                newScreenColors[rendererIndex] = renderers[rendererIndex].ScreenColor;
            }

            // Pass blend color changes to handler (if available).
            var multiplyColorHandlerInterface = MultiplyColorHandlerInterface;
            var screenColorHandlerInterface = ScreenColorHandlerInterface;

            if (MultiplyColorHandler != null && isMultiplyColorUpdated)
            {
                multiplyColorHandlerInterface.OnBlendColorDidChange(this, newMultiplyColors);
            }

            if (ScreenColorHandler != null && isScreenColorUpdated)
            {
                screenColorHandlerInterface.OnBlendColorDidChange(this, newScreenColors);
            }
        }

        #endregion
    }
}
