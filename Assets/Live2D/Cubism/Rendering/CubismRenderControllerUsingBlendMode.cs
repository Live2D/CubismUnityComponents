/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Utils;
using Live2D.Cubism.Rendering.Masking;
using Live2D.Cubism.Rendering.URP;
using System;
using System.Collections.Generic;
using Live2D.Cubism.Rendering.URP.RenderingInterceptor;
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
        /// Whether the model has a root part offscreen.
        /// </summary>
        internal bool HasRootPartOffscreen = true;

        /// <summary>
        /// <see cref="HasMask"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _hasMask;

        /// <summary>
        /// Is the model using masks?
        /// </summary>
        public bool HasMask
        {
            get
            {
                return _hasMask;
            }
            set
            {
                _hasMask = value;
            }
        }

        /// <summary>
        /// Reuses the high-precision mask texture across consecutive draw objects that share the same mask
        /// set, skipping redundant per-draw redraws. Only affects the high-precision path
        /// (<see cref="UseHighPrecisionMask"/>); the atlas path renders once per frame in the mask pre-pass
        /// and needs no cache.
        /// </summary>
        [SerializeField]
        public bool EnableMaskCache = true;

        /// <summary>
        /// Whether the custom-blend backdrop copy in <see cref="CubismRenderer.DrawDrawable"/>
        /// is limited to the region the drawable covers. When off, the whole frame buffer is
        /// blitted as before.
        /// </summary>
        /// <remarks>
        /// When on, a hardware scissor rectangle restricts the blit's fragments to that region,
        /// reducing transfer cost (notably on TBDR GPUs). Exposed as a fallback to the full-buffer
        /// blit in case the region calculation misbehaves. Like <see cref="EnableMaskCache"/>, this
        /// is not drawn in the normal inspector; change it from script or the inspector's Debug mode.
        /// </remarks>
        [SerializeField]
        public bool EnableScissorBlit = true;

        /// <summary>
        /// Whether a custom-blend offscreen composite writes straight into a freshly acquired
        /// offscreen render texture that then replaces the parent's buffer, instead of compositing
        /// into the shared temporary texture and blitting that result back over the parent.
        /// </summary>
        /// <remarks>
        /// Both paths produce the same pixels; ping-ponging just drops one full-screen copy, and the
        /// render target switch it needs, per composite. That copy is pure overhead on TBDR GPUs
        /// because it forces a tile store followed by a tile load.
        ///
        /// It only applies when the destination is a pooled offscreen texture owned by a renderer,
        /// since the swap works by reassigning that renderer's
        /// <see cref="CubismRenderer.OffscreenFrameBuffer"/>. Composites that write to the common
        /// rendering texture keep using the temporary texture and the blit.
        ///
        /// Exposed as a fallback in case the buffer swap misbehaves. Like
        /// <see cref="EnableMaskCache"/>, this is not drawn in the normal inspector; change it from
        /// script or the inspector's Debug mode.
        /// </remarks>
        [SerializeField]
        public bool EnableOffscreenPingPong = true;

        /// <summary>
        /// Whether offscreens that hold no content this frame are skipped entirely, together with
        /// everything below them. When off, they are cleared and composited as before.
        /// </summary>
        /// <remarks>
        /// An offscreen whose own opacity is above zero is composited even when every drawable inside
        /// it is invisible, which costs a render texture, a clear, a composite quad and the render
        /// target switches around them for a result that is fully transparent. Whether that happens is
        /// a property of the current pose, not of the model, so the test runs per frame.
        ///
        /// Exposed as a fallback in case the emptiness test misbehaves. Like
        /// <see cref="EnableMaskCache"/>, this is not drawn in the normal inspector; change it from
        /// script or the inspector's Debug mode.
        /// </remarks>
        [SerializeField]
        public bool EnableEmptyOffscreenSkip = true;

        /// <summary>
        /// Whether an offscreen's clear and composite are limited to the region that actually holds
        /// content. When off, both cover the whole buffer as before.
        /// </summary>
        /// <remarks>
        /// Every offscreen costs a full-screen clear and a full-screen composite quad however small the
        /// part inside it is, while the composite only changes pixels where that part has coverage. A
        /// hardware scissor rectangle limits both to the union of the footprints of the drawables that
        /// render into the offscreen.
        ///
        /// The clear and the composite are limited together or not at all: a limited clear leaves an
        /// older frame's content outside the region, which the composite must then never sample. The
        /// custom blend paths keep their full-buffer behaviour, see
        /// <see cref="CubismRenderer.OffscreenScreenRect"/>.
        ///
        /// Exposed as a fallback in case the region calculation misbehaves. Like
        /// <see cref="EnableMaskCache"/>, this is not drawn in the normal inspector; change it from
        /// script or the inspector's Debug mode.
        /// </remarks>
        [SerializeField]
        public bool EnableOffscreenScissor = true;

        /// <summary>
        /// <see cref="MaskTextureController"/>'s backing field.
        /// </summary>
        private CubismMaskTextureController _maskTextureController;

        /// <summary>
        /// Mask texture controller for this model. Lazily created for masked models.
        /// </summary>
        public CubismMaskTextureController MaskTextureController
        {
            get
            {
                if (!_maskTextureController && HasMask)
                {
                    _maskTextureController = GetComponent<CubismMaskTextureController>();

                    if (!_maskTextureController)
                    {
                        _maskTextureController = gameObject.AddComponent<CubismMaskTextureController>();
                    }
                }
                return _maskTextureController;
            }
            set
            {
                _maskTextureController = value;
            }
        }


        /// <summary>
        /// Clipping manager for drawable draw objects. Lazily created for masked models.
        /// </summary>
        private CubismClippingManager _drawableClippingManager;

        /// <summary>
        /// Frame index the clipping contexts were last computed for. Guards <see cref="SetupClippingContexts"/>
        /// against redundant recomputation when it is invoked more than once per frame.
        /// </summary>
        [NonSerialized]
        private int _clippingContextsSetupFrame = -1;

        /// <summary>
        /// Frame index the mask atlas was last rendered for. Guards <see cref="PrepareMaskAtlas"/> against
        /// re-rendering the atlas more than once per frame.
        /// </summary>
        [NonSerialized]
        private int _maskAtlasFrame = -1;

        /// <summary>
        /// Pixel size of the high-precision mask render texture (the full-screen buffer the mask is drawn into).
        /// Supplied by the render pass so the high-precision scale fit uses the real buffer resolution.
        /// </summary>
        [NonSerialized]
        private Vector2 _highPrecisionMaskBufferSize = new Vector2(256.0f, 256.0f);

        /// <summary>
        /// High-precision mask buffer size the clipping contexts were last computed for. Lets the per-frame
        /// guard recompute when the size changes within a frame (e.g. a second camera of a different resolution).
        /// </summary>
        [NonSerialized]
        private Vector2 _clippingContextsSetupBufferSize = new Vector2(-1.0f, -1.0f);

        /// <summary>
        /// Current frame buffer used for rendering.
        /// Offscreen rendering uses this to render the model.
        /// </summary>
        public RenderTexture CurrentFrameBuffer { get; set; }

        /// <summary>
        /// Index of the current offscreen owner in the unmanaged array.
        /// </summary>
        public int CurrentOffscreenUnmanagedIndex { get; set; }

        /// <summary>
        /// <see cref="CanRenderDirectly"/>'s evaluation guard. True when the property has been evaluated and cached.
        /// </summary>
        [NonSerialized]
        private bool _hasEvaluatedCanRenderDirectly;

        /// <summary>
        /// <see cref="CanRenderDirectly"/>'s backing field.
        /// </summary>
        [NonSerialized]
        private bool _canRenderDirectly;

        /// <summary>
        /// Whether this model can be rendered directly to the camera color target without an
        /// intermediate RenderTexture. True when the model has no offscreen draw objects and every
        /// drawable uses a 5.2-compatible (non background-reading) blend mode
        /// (<see cref="CubismRenderer.IsDirectDrawBlendMode"/>). Masks are allowed (the mask texture is
        /// still required). Evaluated once after initialization; the blend/offscreen composition is
        /// model-data driven and treated as immutable.
        /// </summary>
        public bool CanRenderDirectly
        {
            get
            {
                if (!_hasEvaluatedCanRenderDirectly)
                {
                    EvaluateCanRenderDirectly();
                }
                return _canRenderDirectly;
            }
        }

        #region Sorting

        /// <summary>
        /// <see cref="GroupedSortingIndex"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _groupedSortingIndex;

        /// <summary>
        /// Sorting index for grouped rendering.
        /// </summary>
        public int GroupedSortingIndex
        {
            get
            {
                return _groupedSortingIndex;
            }

            set
            {
                // Return early in case same value given.
                // The inspector assigns this on every repaint, which would otherwise rebuild the groups
                // and flag a sorting change every frame it is shown.
                if (value == _groupedSortingIndex)
                {
                    return;
                }

                var renderControllerGroup = CubismRenderControllerGroup.GetInstance();

                // A model the common rendering controller does not manage - one with a rendering
                // interceptor attached, or one whose component is disabled - must stay out of its groups:
                // the group draw would be a second draw on top of the path the model is actually drawn
                // through, using skip flags that are only refreshed for managed models.
                // The stored value is picked up when the model registers itself.
                if (!renderControllerGroup.ContainsRenderController(this))
                {
                    _groupedSortingIndex = value;

                    return;
                }

                // Remove from the common rendering controller's groups.
                renderControllerGroup.RemoveRenderControllerFromGroups(this, true);

                _groupedSortingIndex = value;

                // Re-add to the common rendering controller's groups.
                renderControllerGroup.AddRenderControllerGroups(this);
            }
        }

        /// <summary>
        /// Whether the render order of the <see cref="CubismDrawable"/>s did change.
        /// </summary>
        internal bool DidChangeDrawableRenderOrder;

        /// <summary>
        /// Whether the sorting order of the <see cref="CubismRenderer"/>s did change.
        /// </summary>
        internal bool DidChangeSorting;

        [NonSerialized]
        private CubismRenderer[] _sortedRenderers;

        /// <summary>
        /// Sorted <see cref="Renderers"/>s for using <see cref="ICubismRenderingInterceptor"/>.
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

        /// <summary>
        /// Sorts the given renderers by their sorting order.
        /// </summary>
        /// <param name="renderers"> Renderers to sort. </param>
        private void SortBySortingOrder(List<CubismRenderer> renderers)
        {
            renderers.Sort(CompareBySortingOrder);
        }

        /// <summary>
        /// Compares two <see cref="CubismRenderer"/>s by their sorting order.
        /// </summary>
        /// <param name="a"> First renderer. </param>
        /// <param name="b"> Second renderer. </param>
        /// <returns></returns>
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
        /// Returns the renderer of <paramref name="drawable"/>, or null when this model has none.
        /// </summary>
        /// <param name="drawable">Drawable to look up.</param>
        /// <returns>The drawable's renderer.</returns>
        /// <remarks>
        /// Uses the drawable-to-renderer mapping <see cref="OnDynamicDrawableData"/> maintains, so a
        /// caller that walks a part's drawables does not have to scan every renderer per drawable. Falls
        /// back to a scan while that mapping is unbuilt or stale, which is only the case before the first
        /// dynamic data arrives.
        /// </remarks>
        internal CubismRenderer FindDrawableRenderer(CubismDrawable drawable)
        {
            var renderers = DrawableRenderers;

            if (!drawable
                || renderers == null)
            {
                return null;
            }

            var unmanagedIndex = drawable.UnmanagedIndex;

            if (!_rendererIndicesDirty
                && unmanagedIndex >= 0
                && unmanagedIndex < (_rendererIndicesByDrawableIndices?.Length ?? 0))
            {
                var rendererIndex = _rendererIndicesByDrawableIndices[unmanagedIndex];

                return rendererIndex >= 0 && rendererIndex < renderers.Length
                    ? renderers[rendererIndex]
                    : null;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                if (renderer
                    && renderer.Drawable
                    && renderer.Drawable.UnmanagedIndex == unmanagedIndex)
                {
                    return renderer;
                }
            }

            return null;
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
                if (_offscreenRenderers == null && Model?.Offscreens != null)
                {
                    _offscreenRenderers = Model.Offscreens.GetComponentsMany<CubismRenderer>();
                }
                return _offscreenRenderers;
            }
            private set { _offscreenRenderers = value; }
        }

        /// <summary>
        /// Region this whole model covers on screen this frame, in target pixels (bottom-left origin).
        /// Only meaningful together with <see cref="HasModelScreenRect"/>.
        /// </summary>
        /// <remarks>
        /// Contains the region of every offscreen of this model, which is what makes it usable as a
        /// conservative bound wherever an offscreen's own region is not enough. See
        /// <see cref="EnableOffscreenScissor"/>.
        /// </remarks>
        internal RectInt ModelScreenRect { get; private set; }

        /// <summary>
        /// Whether <see cref="ModelScreenRect"/> holds a usable region for this frame.
        /// </summary>
        internal bool HasModelScreenRect { get; private set; }

        /// <summary>
        /// Drops the region accumulated for the previous frame.
        /// </summary>
        internal void ResetModelScreenRect()
        {
            ModelScreenRect = new RectInt(0, 0, 0, 0);
            HasModelScreenRect = false;
        }

        /// <summary>
        /// Grows this model's region to also contain <paramref name="rect"/>.
        /// </summary>
        /// <param name="rect">Region of one drawable of this model.</param>
        internal void UnionModelScreenRect(RectInt rect)
        {
            if (rect.width <= 0
                || rect.height <= 0)
            {
                return;
            }

            if (!HasModelScreenRect)
            {
                ModelScreenRect = rect;
                HasModelScreenRect = true;

                return;
            }

            ModelScreenRect = CubismMath.UnionRects(ModelScreenRect, rect);
        }

        /// <summary>
        /// Returns the renderer of the offscreen with the given unmanaged index, or null when this
        /// model has none.
        /// </summary>
        /// <param name="offscreenUnmanagedIndex">Unmanaged index of the offscreen to look up.</param>
        /// <returns>The offscreen's renderer.</returns>
        /// <remarks>
        /// A scan, unlike <see cref="FindDrawableRenderer"/>: there are far fewer offscreens than
        /// drawables, and callers walk a part chain that only reaches a couple of them.
        /// </remarks>
        internal CubismRenderer FindOffscreenRenderer(int offscreenUnmanagedIndex)
        {
            var renderers = OffscreenRenderers;

            if (renderers == null
                || offscreenUnmanagedIndex < 0)
            {
                return null;
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                if (renderer
                    && renderer.Offscreen
                    && renderer.Offscreen.UnmanagedIndex == offscreenUnmanagedIndex)
                {
                    return renderer;
                }
            }

            return null;
        }

        /// <summary>
        /// <see cref="FindPart"/>'s lookup: this model's parts indexed by their unmanaged index.
        /// </summary>
        [NonSerialized]
        private CubismPart[] _partsByUnmanagedIndex;

        /// <summary>
        /// The <see cref="CubismModel.Parts"/> array <see cref="_partsByUnmanagedIndex"/> was built
        /// from, kept to detect that the model has rebuilt its proxies.
        /// </summary>
        [NonSerialized]
        private CubismPart[] _partsOfLookup;

        /// <summary>
        /// Returns the part with the given unmanaged index, or null when this model has none.
        /// </summary>
        /// <param name="partUnmanagedIndex">Unmanaged index of the part to look up.</param>
        /// <returns>The part.</returns>
        /// <remarks>
        /// A position in <see cref="CubismModel.Parts"/> is not the same thing as a part's unmanaged
        /// index: the array is gathered with <c>GetComponentsInChildren</c> and can be reordered by the
        /// hierarchy or filtered when it no longer matches the moc, so an unmanaged index - a drawable's
        /// <see cref="CubismDrawable.ParentPartIndex"/>, a part's
        /// <see cref="CubismPart.UnmanagedParentIndex"/> - has to be resolved through this lookup rather
        /// than used as a subscript.
        ///
        /// The lookup is built once per parts array and reused afterwards, so a caller that walks a part
        /// chain per drawable does not pay a scan per step.
        /// </remarks>
        internal CubismPart FindPart(int partUnmanagedIndex)
        {
            if (partUnmanagedIndex < 0)
            {
                return null;
            }

            var parts = Model?.Parts;

            if (parts == null)
            {
                return null;
            }

            // The model replaces the array whenever it rebuilds its proxies, so reference equality is
            // enough to notice a stale lookup.
            if (!ReferenceEquals(parts, _partsOfLookup))
            {
                BuildPartLookup(parts);
            }

            return partUnmanagedIndex < _partsByUnmanagedIndex.Length
                ? _partsByUnmanagedIndex[partUnmanagedIndex]
                : null;
        }

        /// <summary>
        /// Builds <see cref="_partsByUnmanagedIndex"/> for <paramref name="parts"/>.
        /// </summary>
        /// <param name="parts">The model's parts.</param>
        private void BuildPartLookup(CubismPart[] parts)
        {
            var length = 0;

            for (var partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                var part = parts[partIndex];

                if (part
                    && part.UnmanagedIndex >= length)
                {
                    length = part.UnmanagedIndex + 1;
                }
            }

            _partsByUnmanagedIndex = new CubismPart[length];

            for (var partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                var part = parts[partIndex];

                if (!part
                    || part.UnmanagedIndex < 0)
                {
                    continue;
                }

                _partsByUnmanagedIndex[part.UnmanagedIndex] = part;
            }

            _partsOfLookup = parts;
        }

        #endregion

        /// <summary>
        /// Initializes the render controller.
        /// </summary>
        /// <param name="renderers">Array of renderers to initialize.</param>
        private void TryInitializeRenderers(CubismRenderer[] renderers)
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

            var drawableRenderers = drawables.GetOrAddComponentEach<CubismRenderer>();
            Array.Resize(ref renderers, drawableRenderers.Length);
            Array.Copy(drawableRenderers, renderers, drawableRenderers.Length);

            for (var index = 0; index < renderers.Length; index++)
            {
                var targetRenderer = renderers[index];
                targetRenderer.DrawObjectType = CubismModelTypes.DrawObjectType.Drawable;
                targetRenderer.Drawable = drawables[index];

                if (!HasRootPartOffscreen)
                {
                    continue;
                }
                HasRootPartOffscreen = CheckHasRootPartOffscreen(targetRenderer);
            }

            // Store drawable renderers.
            DrawableRenderers = drawableRenderers;

            // Renderer set changed: the drawable->renderer reverse mapping must be rebuilt.
            _rendererIndicesDirty = true;

            var offscreens = Model.Offscreens;

            if (offscreens != null)
            {
                var offscreenRenderers = offscreens.GetOrAddComponentEach<CubismRenderer>();
                Array.Resize(ref renderers, renderers.Length + offscreenRenderers.Length);
                Array.Copy(offscreenRenderers, 0, renderers, renderers.Length - offscreenRenderers.Length, offscreenRenderers.Length);

                for (var index = drawableRenderers.Length; index < renderers.Length; ++index)
                {
                    var targetRenderer = renderers[index];
                    targetRenderer.DrawObjectType = CubismModelTypes.DrawObjectType.Offscreen;
                    targetRenderer.Offscreen = offscreens[index - drawableRenderers.Length];

                    if (!HasRootPartOffscreen)
                    {
                        continue;
                    }
                    HasRootPartOffscreen = CheckHasRootPartOffscreen(targetRenderer);
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

            // Cache whether this model can be drawn directly to the camera target.
            EvaluateCanRenderDirectly();
        }

        /// <summary>
        /// Evaluates and caches <see cref="CanRenderDirectly"/>. A model qualifies when it has no
        /// offscreen draw objects and every drawable uses a 5.2-compatible (non background-reading)
        /// blend mode. Masks do not disqualify the model (the mask texture is still required).
        /// </summary>
        private void EvaluateCanRenderDirectly()
        {
            _canRenderDirectly = false;

            // Defer while the renderers are not built yet: a controller can be registered before its
            // initialization finishes (OnEnable reaches AddRenderController even when TryInitialize returned
            // early). Leaving the guard unset lets a later access - or OnAfterRenderersInitialize - re-evaluate
            // once they exist, and keeps this property from reviving the model or rebuilding renderer arrays
            // from inside the render pass. Until then CanRenderDirectly reports the conservative default
            // (false), so the common RT stays allocated. Read the backing field directly to avoid the
            // Renderers getter's lazy TryInitialize.
            if (_renderers == null)
            {
                return;
            }

            // DrawableRenderers is null for a model whose drawables are unavailable; defer that too.
            var drawableRenderers = DrawableRenderers;
            if (drawableRenderers == null)
            {
                return;
            }

            _hasEvaluatedCanRenderDirectly = true;

            // Offscreen drawing always needs an intermediate RenderTexture.
            if (OffscreenRenderers != null && OffscreenRenderers.Length > 0)
            {
                return;
            }

            // Any background-reading blend mode (5.3 special blends / AlphaBlend other than Over)
            // requires the intermediate RenderTexture, so disqualify the whole model.
            for (var i = 0; i < drawableRenderers.Length; i++)
            {
                var drawableRenderer = drawableRenderers[i];
                if (drawableRenderer && !drawableRenderer.IsDirectDrawBlendMode)
                {
                    return;
                }
            }

            _canRenderDirectly = true;
        }

        /// <summary>
        /// Does the model have an offscreen for the root part?
        /// </summary>
        /// <param name="targetRenderer">The <see cref="CubismRenderer"/> to check.</param>
        /// <returns>True if it has one, false if it does not.</returns>
        private bool CheckHasRootPartOffscreen(CubismRenderer targetRenderer)
        {
            var parentPartIndex = -1;
            switch (targetRenderer.DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    parentPartIndex = targetRenderer.Drawable.ParentPartIndex;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    parentPartIndex = Model.Parts[targetRenderer.Offscreen.OwnerIndex].UnmanagedParentIndex;
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
                    if (Model.Parts[partIndex].UnmanagedIndex != parentPartIndex)
                    {
                        continue;
                    }

                    // Found the part.
                    part = Model.Parts[partIndex];
                    break;
                }

                parentPartIndex = part?.UnmanagedParentIndex ?? -1;
            }

            return parentPartIndex == 0 && Model.Parts[parentPartIndex].OffscreenIndex > -1;
        }

        /// <summary>
        /// Computes clipping-mask matrices for masked drawables and assigns each drawable renderer its
        /// clipping context for the current frame. Runs at most once per frame; the render pass invokes
        /// this before drawing objects.
        /// </summary>
        public void SetupClippingContexts()
        {
            if (!HasMask)
            {
                return;
            }

            // Run at most once per frame.
            // The high-precision fit depends on the mask buffer resolution, so also recompute
            // when that size changes within a frame (e.g. a second camera of a different resolution rendering the same model).
            var useHighPrecision = MaskTextureController && MaskTextureController.UseHighPrecisionMask;
            if (_clippingContextsSetupFrame == Time.frameCount
                && (!useHighPrecision || _clippingContextsSetupBufferSize == _highPrecisionMaskBufferSize))
            {
                return;
            }
            _clippingContextsSetupFrame = Time.frameCount;
            _clippingContextsSetupBufferSize = _highPrecisionMaskBufferSize;

            var model = Model;
            if (!model)
            {
                return;
            }

            // Number of mask atlas buffers is defined by the shared mask texture settings.
            var maskBufferCount = MaskTextureController.RenderTextureCount;

            // Lazily create the drawable clipping manager, and rebuild it if the buffer count changed.
            if (_drawableClippingManager == null)
            {
                _drawableClippingManager = CubismClippingManager.Create(this);
                _drawableClippingManager?.Initialize(this, maskBufferCount, CubismModelTypes.DrawObjectType.Drawable);
            }
            else if (_drawableClippingManager.GetRenderTextureCount() != maskBufferCount)
            {
                _drawableClippingManager.Initialize(this, maskBufferCount, CubismModelTypes.DrawObjectType.Drawable);
            }

            if (_drawableClippingManager == null)
            {
                return;
            }

            // Compute mask/draw matrices for the current frame.
            if (MaskTextureController && MaskTextureController.UseHighPrecisionMask)
            {
                // High-precision: every mask fills the full region (channel R); redrawn per draw object.
                // The mask is drawn into the full-screen buffer, so the fit uses that buffer's resolution.
                _drawableClippingManager.SetClippingMaskBufferSize(_highPrecisionMaskBufferSize.x, _highPrecisionMaskBufferSize.y);
                _drawableClippingManager.SetupMatrixForHighPrecision(model, CubismModelTypes.DrawObjectType.Drawable);
            }
            else
            {
                // Atlas: channel / tile packing. A single-mask model resolves to channel R with a full tile.
                _drawableClippingManager.SetupClippingContext(model, CubismModelTypes.DrawObjectType.Drawable);
            }

            // Assign each drawable renderer its clipping context (null when the drawable is unmasked).
            var contexts = _drawableClippingManager.GetClippingContextListForDraw();
            var drawableRenderers = DrawableRenderers;
            if (contexts == null || drawableRenderers == null)
            {
                return;
            }

            var count = Mathf.Min(contexts.Count, drawableRenderers.Length);
            for (var i = 0; i < count; i++)
            {
                var renderer = drawableRenderers[i];
                if (!renderer)
                {
                    continue;
                }

                renderer.ClippingContext = contexts[i];
            }
        }

        /// <summary>
        /// Computes this frame's clipping contexts and renders this controller's whole mask atlas into its own buffers,
        /// once per frame, before the object draw loop.
        /// </summary>
        /// <param name="commandBuffer">Command buffer to record the mask draws into.</param>
        /// <param name="highPrecisionMaskBufferSize">Pixel size of the high-precision mask render texture.</param>
        internal void PrepareMaskAtlas(CommandBuffer commandBuffer, Vector2 highPrecisionMaskBufferSize)
        {
            if (!HasMask)
            {
                return;
            }

            // Record the actual high-precision buffer resolution before the clipping contexts are computed.
            if (highPrecisionMaskBufferSize.x > 0.0f && highPrecisionMaskBufferSize.y > 0.0f)
            {
                _highPrecisionMaskBufferSize = highPrecisionMaskBufferSize;
            }

            // Compute matrices / assign contexts for the frame (frame-guarded internally).
            SetupClippingContexts();

            // High-precision and offscreen masks are redrawn per draw object into the shared full-screen
            // texture; they do not use the per-controller atlas.
            if ((MaskTextureController && MaskTextureController.UseHighPrecisionMask) || _drawableClippingManager == null)
            {
                return;
            }

            // Render the atlas at most once per frame.
            if (_maskAtlasFrame == Time.frameCount)
            {
                return;
            }
            _maskAtlasFrame = Time.frameCount;

            // Mask atlas buffers are created and held by the per-model CubismMaskTextureController.
            var atlasBuffers = MaskTextureController ? MaskTextureController.RenderTextures : null;
            if (atlasBuffers == null || atlasBuffers.Length == 0)
            {
                return;
            }

            var clipContexts = _drawableClippingManager.GetClippingContextListForMask();

            // Clear each buffer once (white = uncovered, subtractive convention), then draw the groups
            // assigned to that buffer.
            for (var bufferIndex = 0; bufferIndex < atlasBuffers.Length; bufferIndex++)
            {
                commandBuffer.SetRenderTarget(atlasBuffers[bufferIndex]);
                commandBuffer.ClearRenderTarget(true, true, Color.white);

                if (clipContexts == null)
                {
                    continue;
                }

                for (var contextIndex = 0; contextIndex < clipContexts.Count; contextIndex++)
                {
                    var clipContext = clipContexts[contextIndex];
                    if (clipContext == null || !clipContext.IsUsing || clipContext.BufferIndex != bufferIndex)
                    {
                        continue;
                    }

                    var masks = clipContext.Masks;
                    for (var maskIndex = 0; maskIndex < masks.Length; maskIndex++)
                    {
                        if (masks[maskIndex])
                        {
                            masks[maskIndex].DrawAsMask(commandBuffer, clipContext);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns this model's mask atlas buffer for the given buffer index, or null when unavailable.
        /// The buffers are created and held by <see cref="MaskTextureController"/>.
        /// </summary>
        internal RenderTexture GetMaskAtlasBuffer(int bufferIndex)
        {
            return MaskTextureController ? MaskTextureController.GetRenderTexture(bufferIndex) : null;
        }

        /// <summary>
        /// Submits drawing offscreen for all offscreen renderers.
        /// </summary>
        /// <param name="commandBuffer">Command buffer to record draw commands.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        internal void SubmitDrawOffscreen(CommandBuffer commandBuffer, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (!IsInitialized
                || !Model
                || OffscreenRenderers == null
                || passData == null)
            {
                return;
            }

            // Destination the offscreen stack unwinds to. The camera color target stands in for the common
            // rendering texture when it is unavailable; with neither there is nothing to composite into,
            // and the loop below would have no buffer to stop at.
            var commonRenderingTexture = passData.CommonRenderingTexture;
            if (!commonRenderingTexture)
            {
                // The loop below is what gives the pooled offscreen textures back, so skipping it outright
                // would leak them. Release them here instead; the next frame re-points this controller at
                // its group's render target before anything is drawn.
                ReleaseOffscreenFrameBuffers();

                return;
            }

            while (CurrentFrameBuffer != commonRenderingTexture)
            {
                CubismRenderer offscreenRenderer = null;
                if (CurrentOffscreenUnmanagedIndex == -1 && HasRootPartOffscreen)
                {
                    for (var offscreenIndex = 0; offscreenIndex < OffscreenRenderers.Length; offscreenIndex++)
                    {
                        if (OffscreenRenderers[offscreenIndex].Offscreen.UnmanagedIndex != 0)
                        {
                            continue;
                        }

                        offscreenRenderer = OffscreenRenderers[offscreenIndex];

                        break;
                    }

                    if (!offscreenRenderer)
                    {
                        break;
                    }

                    // No owner: the destination is the common rendering texture, so this composite always
                    // takes the blit path and gives the common texture back.
                    offscreenRenderer.DrawOffscreen(commandBuffer, commonRenderingTexture, null, offscreenRenderer, passData);
                    offscreenRenderer.OffscreenFrameBuffer = null;
                    CurrentFrameBuffer = commonRenderingTexture;
                    CurrentOffscreenUnmanagedIndex = -1;
                    break;
                }

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

                // Renderer that owns previousOffscreen, set only together with it. Needed so a custom
                // blend composite can swap that renderer's buffer instead of blitting into it. Stays
                // null when the destination is the common rendering texture, which no renderer owns.
                CubismRenderer previousOffscreenOwner = null;

                var currentOwnerIndex = offscreenRenderer?.Offscreen?.OwnerIndex ?? -1;

                var parentIndex = -1;
                if (currentOwnerIndex != -1)
                {
                    parentIndex = Model.Parts[currentOwnerIndex]?.UnmanagedParentIndex ?? -1;
                }

                var previousIndex = -1;
                // Find the offscreen renderer with the previous offscreen unmanaged index.
                while (parentIndex != -1)
                {
                    var part = Model.Parts[parentIndex];
                    if (part && part.OffscreenIndex != -1)
                    {
                        for (var offscreenRendererIndex = 0; offscreenRendererIndex < OffscreenRenderers.Length; offscreenRendererIndex++)
                        {
                            var element = OffscreenRenderers[offscreenRendererIndex];

                            if (!element.isActiveAndEnabled
                                || !element.MeshRenderer.enabled)
                            {
                                continue;
                            }

                            if (element.Offscreen.UnmanagedIndex != part?.OffscreenIndex)
                            {
                                continue;
                            }

                            // Only adopt this ancestor when it actually owns a buffer. Recording the
                            // index for a renderer with no buffer would leave previousIndex pointing at
                            // an offscreen that never becomes the render target, and the loop would then
                            // try to composite it again on the next iteration. Read the owned buffer
                            // rather than OffscreenFrameBuffer, whose getter would acquire a blank
                            // texture from the pool and make the test pass by allocating what it asked about.
                            var parentOffscreenFrameBuffer = element.OwnedOffscreenFrameBuffer;

                            if (parentOffscreenFrameBuffer)
                            {
                                previousOffscreen = parentOffscreenFrameBuffer;
                                previousIndex = part.OffscreenIndex;
                                previousOffscreenOwner = element;
                            }

                            break;
                        }
                    }
                    parentIndex = Model.Parts[parentIndex]?.UnmanagedParentIndex ?? -1;

                    if (!previousOffscreen)
                    {
                        continue;
                    }

                    break;
                }

                // Try to get the root part offscreen if there is no parent offscreen.
                if (!previousOffscreen && HasRootPartOffscreen)
                {
                    for (var offscreenIndex = 0; offscreenIndex < OffscreenRenderers.Length; offscreenIndex++)
                    {
                        if (OffscreenRenderers[offscreenIndex].Offscreen.UnmanagedIndex != 0)
                        {
                            continue;
                        }

                        // Same buffer / owner pairing as the ancestor search above, and the same reason
                        // for reading the owned buffer instead of the allocating property.
                        var rootOffscreenFrameBuffer = OffscreenRenderers[offscreenIndex].OwnedOffscreenFrameBuffer;

                        if (rootOffscreenFrameBuffer)
                        {
                            previousOffscreen = rootOffscreenFrameBuffer;
                            previousOffscreenOwner = OffscreenRenderers[offscreenIndex];
                        }

                        break;
                    }
                }

                // If there is no previous offscreen, or it is the same as the current offscreen, use the common rendering texture.
                if (!previousOffscreen
                    || previousOffscreen == offscreenRenderer?.OffscreenFrameBuffer)
                {
                    previousOffscreen = commonRenderingTexture;

                    // Drop any owner found above: the destination is no longer that renderer's buffer.
                    previousOffscreenOwner = null;
                }

                if (offscreenRenderer)
                {
                    // If It can copy the parent offscreen, copy it to the current offscreen renderer.
                    // A ping-ponged composite returns a different texture than it was given, and that one
                    // is now the owner's buffer, so continue from the returned texture.
                    previousOffscreen = offscreenRenderer.DrawOffscreen(commandBuffer, previousOffscreen, previousOffscreenOwner, offscreenRenderer, passData);

                    offscreenRenderer.OffscreenFrameBuffer = null;
                }

                CurrentFrameBuffer = previousOffscreen;
                CurrentOffscreenUnmanagedIndex = previousIndex;
            }
        }

        /// <summary>
        /// Returns every offscreen render texture this model's renderers hold to the pool and unwinds the
        /// offscreen stack. Used when the frame cannot be composited at all, where the normal unwind in
        /// <see cref="SubmitDrawOffscreen"/> - the only thing that gives those textures back - cannot run.
        /// </summary>
        private void ReleaseOffscreenFrameBuffers()
        {
            for (var offscreenIndex = 0; offscreenIndex < OffscreenRenderers.Length; offscreenIndex++)
            {
                var offscreenRenderer = OffscreenRenderers[offscreenIndex];

                if (!offscreenRenderer
                    || !offscreenRenderer.OwnedOffscreenFrameBuffer)
                {
                    continue;
                }

                // Assigning null is what releases the texture back to the pool.
                offscreenRenderer.OffscreenFrameBuffer = null;
            }

            CurrentFrameBuffer = null;
            CurrentOffscreenUnmanagedIndex = -1;
        }
    }
}
