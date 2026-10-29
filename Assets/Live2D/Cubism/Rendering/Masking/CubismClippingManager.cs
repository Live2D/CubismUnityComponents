/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using System;
using System.Collections.Generic;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Utils;
using UnityEngine;

namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Manages clipping masks (layout, matrices, buffer metadata) for a <see cref="CubismRenderController"/>.
    /// </summary>
    public class CubismClippingManager
    {
        /// <summary>
        /// Number of color channels in the mask render texture(s).
        /// </summary>
        private const int ColorChannelCount = 4;

        /// <summary>
        /// Maximum number of clipping masks that can be packed into a single render texture when using a single render texture.
        /// </summary>
        private const int ClippingMaskMaxCountOnDefault = 36;

        /// <summary>
        /// Maximum number of clipping masks that can be packed into a single render texture when using multiple render textures.
        /// </summary>
        private const int ClippingMaskMaxCountOnMultiRenderTexture = 32;

        /// <summary>
        /// Margin added to the union bounds of clipped drawables when computing the mask transformation matrix.
        /// </summary>
        private const float MarginForHighPrecision = 0.05f;

        /// <summary>
        /// Color channels used for each clipping context in the mask render texture(s).
        /// </summary>
        public enum MaskChannel
        {
            Red = 0,
            Green = 1,
            Blue = 2,
            Alpha = 3
        }

        /// <summary>
        /// Logical rectangle in model space.
        /// </summary>
        public struct RectFloat
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;

            /// <summary>
            /// Right edge of the rectangle.
            /// </summary>
            public float GetRight()
            {
                return X + Width;
            }

            /// <summary>
            /// Bottom edge of the rectangle.
            /// </summary>
            public float GetBottom()
            {
                return Y + Height;
            }

            public void SetRect(RectFloat other)
            {
                X = other.X;
                Y = other.Y;
                Width = other.Width;
                Height = other.Height;
            }

            public void Expand(float horizontal, float vertical)
            {
                X -= horizontal;
                Y -= vertical;
                Width += horizontal * 2.0f;
                Height += vertical * 2.0f;
            }
        }

        /// <summary>
        /// Single clipping mask group (shared by drawables/offscreens that use the same mask set).
        /// </summary>
        public class CubismClippingContext
        {
            internal CubismClippingContext(CubismClippingManager manager, CubismRenderer maskOwnerRenderer)
            {
                ClippingManager = manager;
                _maskOwnerRenderer = maskOwnerRenderer;
                AllClippedDrawRect = new RectFloat();
                LayoutBounds = new RectFloat { X = 0.0f, Y = 0.0f, Width = 1.0f, Height = 1.0f };
            }

            /// <summary>
            /// Reference to the clipping manager that owns this context.
            /// </summary>
            internal CubismClippingManager ClippingManager { get; private set; }

            /// <summary>
            /// Reference to the <see cref="CubismRenderer"/> that owns this clipping context (the renderer that defines the mask set).
            /// </summary>
            private readonly CubismRenderer _maskOwnerRenderer;

            /// <summary>
            /// Mask <see cref="CubismRenderer"/>s for this clip group (same reference as <see cref="CubismRenderer.Masks"/> on the owning draw-object renderer).
            /// </summary>
            public CubismRenderer[] Masks
            {
                get
                {
                    return _maskOwnerRenderer?.Masks ?? Array.Empty<CubismRenderer>();
                }
            }

            /// <summary>
            /// List of drawable indices that are clipped by this mask group (indices into <see cref="CubismModel.Drawables"/>).
            /// </summary>
            internal readonly List<int> ClippedDrawableIndexList = new List<int>();

            /// <summary>
            /// List of offscreen indices that are clipped by this mask group (indices into <see cref="CubismModel.Offscreens"/>).
            /// </summary>
            internal readonly List<int> ClippedOffscreenIndexList = new List<int>();

            /// <summary>
            /// Union bounds of all clipped drawables (model coordinates).
            /// This is used to compute the mask transformation matrix.
            /// </summary>
            public RectFloat AllClippedDrawRect { get; internal set; }

            /// <summary>
            /// Layout bounds of this clipping context in mask UV space (0-1).
            /// This is used to compute the mask transformation matrix.
            /// </summary>
            public RectFloat LayoutBounds { get; internal set; }

            /// <summary>
            /// Index of the color channel (0-3) used by this clipping context in the mask render texture.
            /// </summary>
            public int LayoutChannelIndex { get; internal set; }

            /// <summary>
            /// Index of the mask render texture used by this clipping context (0 to <see cref="_renderTextureCount"/>-1).
            /// </summary>
            public int BufferIndex { get; internal set; }

            /// <summary>
            /// Matrix that transforms model-space coordinates to mask UV space (0-1) for this clipping context.
            /// </summary>
            public Matrix4x4 MatrixForMask { get; internal set; }

            /// <summary>
            /// Matrix that transforms model-space coordinates to screen space for this clipping context.
            /// </summary>
            public Matrix4x4 MatrixForDraw { get; internal set; }

            /// <summary>
            /// Is using this clipping context (true if any clipped drawables are visible this frame).
            /// </summary>
            public bool IsUsing { get; internal set; }

            /// <summary>
            /// Adds a drawable index to the list of drawables that are clipped by this clipping context.
            /// </summary>
            /// <param name="drawableIndex">The index of the drawable to add.</param>
            internal void AddClippedDrawable(int drawableIndex)
            {
                ClippedDrawableIndexList.Add(drawableIndex);
            }

            /// <summary>
            /// Adds an offscreen index to the list of offscreens that are clipped by this clipping context.
            /// </summary>
            /// <param name="offscreenIndex">The index of the offscreen to add.</param>
            internal void AddClippedOffscreen(int offscreenIndex)
            {
                ClippedOffscreenIndexList.Add(offscreenIndex);
            }
        }

        /// <summary>
        /// Creates a new <see cref="CubismClippingManager"/> for the specified render controller, or returns null if the controller does not use masks.
        /// </summary>
        /// <param name="renderController">The render controller for which to create the clipping manager.</param>
        /// <returns>A new <see cref="CubismClippingManager"/> instance, or null if the controller does not use masks.</returns>
        public static CubismClippingManager Create(CubismRenderController renderController)
        {
            if (!renderController || !renderController.HasMask)
            {
                return null;
            }

            return new CubismClippingManager(renderController);
        }

        /// <summary>
        /// Colors used for each channel in the mask render texture(s).
        /// </summary>
        private readonly Color[] _channelColors;

        /// <summary>
        /// Reference to the render controller that owns this clipping manager.
        /// </summary>
        private CubismRenderController _renderController;

        /// <summary>
        /// List of all clipping contexts (mask groups) used by the model.
        /// </summary>
        private CubismRenderer[] _clippedRenderers;

        /// <summary>
        /// List of unique clipping contexts (mask groups) used by the model.
        /// </summary>
        private readonly List<CubismClippingContext> _clippingContextListForMask = new List<CubismClippingContext>();

        /// <summary>
        /// Clipping contexts for each drawable (one entry per drawable, may be null if the drawable does not use a mask).
        /// </summary>
        private CubismClippingContext[] _clippingContextListForDraw;

        /// <summary>
        /// Clipping contexts for each offscreen (one entry per offscreen, may be null if the offscreen does not use a mask).
        /// </summary>
        private CubismClippingContext[] _clippingContextListForOffscreen;

        /// <summary>
        /// Temporary list used when computing the union bounds of clipped drawables under an offscreen (to avoid repeated allocations).
        /// </summary>
        private readonly List<int> _clippedOffscreenChildDrawableIndexList = new List<int>();

        /// <summary>
        /// Reused list of the clipping contexts that need a mask this frame (<see cref="CubismClippingContext.IsUsing"/>),
        /// so the layout is assigned only to them and skips unused groups.
        /// </summary>
        private readonly List<CubismClippingContext> _layoutTargetContexts = new List<CubismClippingContext>();

        /// <summary>
        /// Flags indicating whether each mask render texture has been cleared this frame (to avoid clearing the same buffer multiple times).
        /// </summary>
        private bool[] _clearedMaskBufferFlags;

        /// <summary>
        /// Size of the clipping mask buffer (in pixels). This can be adjusted to control the resolution of the masks.
        /// </summary>
        private Vector2 _clippingMaskBufferSize = new Vector2(256.0f, 256.0f);

        /// <summary>
        /// Number of mask render textures used by the clipping manager.
        /// This can be adjusted to control how many masks can be rendered simultaneously.
        /// </summary>
        private int _renderTextureCount = 1;

        /// <summary>
        /// Temporary matrix used for computing the mask transformation (model space to mask UV space).
        /// </summary>
        private Matrix4x4 _tmpMatrixForMask;

        /// <summary>
        /// Temporary matrix used for computing the draw transformation (model space to screen space).
        /// </summary>
        private Matrix4x4 _tmpMatrixForDraw;

        /// <summary>
        /// Temporary rectangle used for computing the union bounds of clipped drawables in model space.
        /// </summary>
        private RectFloat _tmpBoundsOnModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="CubismClippingManager"/> class for the specified render controller.
        /// </summary>
        /// <param name="renderController">The render controller that owns this clipping manager.</param>
        private CubismClippingManager(CubismRenderController renderController)
        {
            if (!renderController || !renderController.HasMask)
            {
                return;
            }

            _channelColors = new Color[ColorChannelCount]
            {
                new Color(1.0f, 0.0f, 0.0f, 0.0f),
                new Color(0.0f, 1.0f, 0.0f, 0.0f),
                new Color(0.0f, 0.0f, 1.0f, 0.0f),
                new Color(0.0f, 0.0f, 0.0f, 1.0f)
            };

            Initialize(renderController);
        }

        /// <summary>
        /// Rebuilds clipping data from the render controller (mask buffer count = 1, drawable objects only).
        /// </summary>
        internal void Initialize(CubismRenderController renderController)
        {
            Initialize(renderController, 1, CubismModelTypes.DrawObjectType.Drawable);
        }

        /// <summary>
        /// Registers clipping contexts for drawables or offscreens.
        /// </summary>
        /// <param name="renderController">Target controller.</param>
        /// <param name="maskBufferCount">Number of mask render textures.</param>
        /// <param name="drawableObjectType">Whether to walk drawables or offscreens.</param>
        public void Initialize(CubismRenderController renderController, int maskBufferCount, CubismModelTypes.DrawObjectType drawableObjectType)
        {
            if (!renderController || !renderController.HasMask)
            {
                return;
            }

            _renderController = renderController;
            _renderTextureCount = Mathf.Max(1, maskBufferCount);

            _clearedMaskBufferFlags = new bool[_renderTextureCount];
            _clippingContextListForMask.Clear();

            renderController.TryInitialize();

            switch (drawableObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    BuildDrawableClippingLists(renderController);
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    BuildOffscreenClippingLists(renderController);
                    break;
                default:
                    _clippingContextListForDraw = Array.Empty<CubismClippingContext>();
                    _clippingContextListForOffscreen = Array.Empty<CubismClippingContext>();
                    break;
            }

            RebuildClippedRenderersList(renderController);
        }

        /// <summary>
        /// Builds clipping contexts for drawable renderers.
        /// </summary>
        /// <param name="renderController">The render controller containing the drawable renderers.</param>
        private void BuildDrawableClippingLists(CubismRenderController renderController)
        {
            var drawableRenderers = renderController.DrawableRenderers;
            var count = drawableRenderers?.Length ?? 0;
            _clippingContextListForDraw = new CubismClippingContext[count];

            for (var i = 0; i < count; i++)
            {
                var renderer = drawableRenderers[i];
                var masks = renderer.Masks;
                var maskCount = masks?.Length ?? 0;

                if (maskCount <= 0)
                {
                    _clippingContextListForDraw[i] = null;
                    continue;
                }

                var cc = FindSameClip(masks);
                if (cc == null)
                {
                    cc = new CubismClippingContext(this, renderer);
                    _clippingContextListForMask.Add(cc);
                }

                cc.AddClippedDrawable(i);
                _clippingContextListForDraw[i] = cc;
            }

            _clippingContextListForOffscreen = Array.Empty<CubismClippingContext>();
        }

        /// <summary>
        /// Builds clipping contexts for offscreen renderers.
        /// </summary>
        /// <param name="renderController">The render controller containing the offscreen renderers.</param>
        private void BuildOffscreenClippingLists(CubismRenderController renderController)
        {
            var offscreenRenderers = renderController.OffscreenRenderers;
            var count = offscreenRenderers?.Length ?? 0;
            _clippingContextListForOffscreen = new CubismClippingContext[count];
            _clippingContextListForDraw = Array.Empty<CubismClippingContext>();

            for (var i = 0; i < count; i++)
            {
                var offscreenRenderer = offscreenRenderers[i];
                var masks = offscreenRenderer.Masks;
                if (masks == null || masks.Length < 1)
                {
                    _clippingContextListForOffscreen[i] = null;
                    continue;
                }

                var cc = FindSameClip(masks);
                if (cc == null)
                {
                    cc = new CubismClippingContext(this, offscreenRenderer);
                    _clippingContextListForMask.Add(cc);
                }

                cc.AddClippedOffscreen(i);
                _clippingContextListForOffscreen[i] = cc;
            }
        }

        /// <summary>
        /// Rebuilds the list of clipped renderers (those that use masks) from the render controller.
        /// </summary>
        /// <param name="renderController"></param>
        private void RebuildClippedRenderersList(CubismRenderController renderController)
        {
            _clippedRenderers = Array.Empty<CubismRenderer>();
            var renderers = renderController.Renderers;
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (!renderer || renderer.Masks?.Length < 1)
                {
                    continue;
                }

                Array.Resize(ref _clippedRenderers, _clippedRenderers.Length + 1);
                _clippedRenderers[^1] = renderer;
            }
        }

        /// <summary>
        /// Finds an existing clipping context that uses the same mask set (same count and same renderers).
        /// </summary>
        internal CubismClippingContext FindSameClip(CubismRenderer[] masks)
        {
            if (masks == null || masks.Length < 1 || _clippingContextListForMask.Count < 1)
            {
                return null;
            }

            var drawableMaskCounts = masks.Length;

            for (var i = 0; i < _clippingContextListForMask.Count; i++)
            {
                var clippingContext = _clippingContextListForMask[i];
                if (clippingContext == null || clippingContext.Masks?.Length != drawableMaskCounts)
                {
                    continue;
                }

                var sameCount = 0;
                for (var j = 0; j < drawableMaskCounts; j++)
                {
                    var clipRenderer = clippingContext.Masks[j];
                    for (var k = 0; k < drawableMaskCounts; k++)
                    {
                        if (masks[k] == clipRenderer)
                        {
                            sameCount++;
                            break;
                        }
                    }
                }

                if (sameCount == drawableMaskCounts)
                {
                    return clippingContext;
                }
            }

            return null;
        }

        /// <summary>
        /// Computes high-precision mask matrices for all clipping contexts.
        /// <param name="model">Target model.</param>
        /// <param name="drawableObjectType">Whether to compute matrices for drawables or offscreens.</param>
        /// <param name="mvp">Optional model-view-projection matrix for offscreen rendering.</param>
        /// </summary>
        public void SetupMatrixForHighPrecision(CubismModel model, CubismModelTypes.DrawObjectType drawableObjectType, Matrix4x4 mvp = default)
        {
            if (!model || _clippingContextListForMask.Count < 1)
            {
                return;
            }

            var usingClipCount = 0;
            for (var clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
            {
                var cc = _clippingContextListForMask[clipIndex];

                // Decide whether this mask is needed from the clipped (masked) drawables,
                // and set the clipped-drawable union bounds as the fallback rect.
                CalcClippedTotalBounds(model, cc, drawableObjectType);

                if (!cc.IsUsing)
                {
                    continue;
                }

                // High-precision has no layout partitioning, so base the mask rect on the mask drawables
                // to keep the mask resolution tied to the mask shape and suppress mask-edge flicker.
                // Leaves the clipped-drawable fallback rect in place when the mask drawables have no valid vertices this frame.
                CalcMaskDrawableTotalBounds(cc);

                usingClipCount++;
            }

            if (usingClipCount <= 0)
            {
                return;
            }

            // High-precision path always uses a single mask render texture.
            SetupLayoutBounds(0);

            if (_clearedMaskBufferFlags.Length != _renderTextureCount)
            {
                _clearedMaskBufferFlags = new bool[_renderTextureCount];
            }
            else
            {
                for (var i = 0; i < _renderTextureCount; i++)
                {
                    _clearedMaskBufferFlags[i] = false;
                }
            }

            var invertMvp = mvp == default ? Matrix4x4.identity : mvp.inverse;

            for (var clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
            {
                var clipContext = _clippingContextListForMask[clipIndex];
                var allClippedDrawRect = clipContext.AllClippedDrawRect;
                var layoutBoundsOnTex01 = clipContext.LayoutBounds;

                var ppu = model.CanvasInformation.PixelsPerUnit;
                var bufferSize = clipContext.ClippingManager.GetClippingMaskBufferSize();
                var maskPixelWidth = bufferSize.x;
                var maskPixelHeight = bufferSize.y;
                var physicalMaskWidth = layoutBoundsOnTex01.Width * maskPixelWidth;
                var physicalMaskHeight = layoutBoundsOnTex01.Height * maskPixelHeight;

                _tmpBoundsOnModel.SetRect(allClippedDrawRect);

                float scaleX;
                if (_tmpBoundsOnModel.Width * ppu > physicalMaskWidth)
                {
                    _tmpBoundsOnModel.Expand(allClippedDrawRect.Width * MarginForHighPrecision, 0.0f);
                    scaleX = layoutBoundsOnTex01.Width / _tmpBoundsOnModel.Width;
                }
                else
                {
                    scaleX = ppu / physicalMaskWidth;
                }

                float scaleY;
                if (_tmpBoundsOnModel.Height * ppu > physicalMaskHeight)
                {
                    _tmpBoundsOnModel.Expand(0.0f, allClippedDrawRect.Height * MarginForHighPrecision);
                    scaleY = layoutBoundsOnTex01.Height / _tmpBoundsOnModel.Height;
                }
                else
                {
                    scaleY = ppu / physicalMaskHeight;
                }

                CreateMatrixForMask(layoutBoundsOnTex01, scaleX, scaleY);

                clipContext.MatrixForMask = _tmpMatrixForMask;
                clipContext.MatrixForDraw = _tmpMatrixForDraw;

                if (drawableObjectType == CubismModelTypes.DrawObjectType.Offscreen)
                {
                    clipContext.MatrixForDraw *= invertMvp;
                }
            }
        }

        /// <summary>
        /// Computes the low-precision (atlas) mask layout and matrices for all clipping contexts.
        /// </summary>
        /// <returns>Number of clipping contexts that require a mask this frame.</returns>
        public int SetupClippingContext(CubismModel model, CubismModelTypes.DrawObjectType drawableObjectType, Matrix4x4 mvp = default)
        {
            if (!model || _clippingContextListForMask.Count < 1)
            {
                return 0;
            }

            // Prepare all clips. Compute the bounding rect of the draw objects that use each clip and count the clips that actually need a mask this frame.
            var usingClipCount = 0;
            for (var clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
            {
                var cc = _clippingContextListForMask[clipIndex];
                CalcClippedTotalBounds(model, cc, drawableObjectType);
                if (cc.IsUsing)
                {
                    usingClipCount++;
                }
            }

            if (usingClipCount <= 0)
            {
                return 0;
            }

            // Decide the layout of each mask (channel / buffer / tile).
            SetupLayoutBounds(usingClipCount);

            // Reset the per-buffer cleared flags for this frame.
            if (_clearedMaskBufferFlags.Length != _renderTextureCount)
            {
                _clearedMaskBufferFlags = new bool[_renderTextureCount];
            }
            else
            {
                for (var i = 0; i < _renderTextureCount; i++)
                {
                    _clearedMaskBufferFlags[i] = false;
                }
            }

            var invertMvp = mvp == default
                ? Matrix4x4.identity
                : mvp.inverse;

            for (var clipIndex = 0; clipIndex < _clippingContextListForMask.Count; clipIndex++)
            {
                var clipContext = _clippingContextListForMask[clipIndex];
                var allClippedDrawRect = clipContext.AllClippedDrawRect;
                var layoutBoundsOnTex01 = clipContext.LayoutBounds;

                // Use the model-space rect with a margin. Unlike the high-precision path the scale is derived directly from the layout bounds.
                // the mask is packed into its assigned tile.
                _tmpBoundsOnModel.SetRect(allClippedDrawRect);
                _tmpBoundsOnModel.Expand(allClippedDrawRect.Width * MarginForHighPrecision, allClippedDrawRect.Height * MarginForHighPrecision);

                // Guard against degenerate (near-zero) bounds, which would make the scale diverge to Inf/NaN.
                // The clip group's drawables are collapsed this frame, so skip its mask. The drawables sample
                // the unwritten (white) tile and are simply clipped out instead of overflowing.
                if (_tmpBoundsOnModel.Width < CubismMath.MinClippedDrawSize || _tmpBoundsOnModel.Height < CubismMath.MinClippedDrawSize)
                {
                    clipContext.IsUsing = false;

                    var safeScaleX = layoutBoundsOnTex01.Width / Mathf.Max(_tmpBoundsOnModel.Width, CubismMath.MinClippedDrawSize);
                    var safeScaleY = layoutBoundsOnTex01.Height / Mathf.Max(_tmpBoundsOnModel.Height, CubismMath.MinClippedDrawSize);
                    CreateMatrixForMask(layoutBoundsOnTex01, safeScaleX, safeScaleY);
                    clipContext.MatrixForMask = _tmpMatrixForMask;
                    clipContext.MatrixForDraw = _tmpMatrixForDraw;
                    continue;
                }

                var scaleX = layoutBoundsOnTex01.Width / _tmpBoundsOnModel.Width;
                var scaleY = layoutBoundsOnTex01.Height / _tmpBoundsOnModel.Height;

                CreateMatrixForMask(layoutBoundsOnTex01, scaleX, scaleY);

                clipContext.MatrixForMask = _tmpMatrixForMask;
                clipContext.MatrixForDraw = _tmpMatrixForDraw;

                if (drawableObjectType == CubismModelTypes.DrawObjectType.Offscreen)
                {
                    clipContext.MatrixForDraw *= invertMvp;
                }
            }

            return usingClipCount;
        }

        /// <summary>
        /// Computes the mask and draw matrices for a clipping context based on its layout bounds and the union bounds of its clipped drawables.
        /// </summary>
        /// <param name="layoutBoundsOnTex01">The layout bounds of the clipping context in texture space (0-1).</param>
        /// <param name="scaleX">The scale factor in the X direction.</param>
        /// <param name="scaleY">The scale factor in the Y direction.</param>
        private void CreateMatrixForMask(RectFloat layoutBoundsOnTex01, float scaleX, float scaleY)
        {
            var boundsX = _tmpBoundsOnModel.X;
            var boundsY = _tmpBoundsOnModel.Y;
            var layoutX = layoutBoundsOnTex01.X;
            var layoutY = layoutBoundsOnTex01.Y;

            // The transform is a pure scale + translation chain (no rotation), so the composed matrix is
            // affine with only the diagonal and translation column non-zero. Compute those components
            // directly instead of multiplying several Matrix4x4.Translate/Scale temporaries per clip each frame.
            //
            // Mask: T(-1,-1) * S(2,2) * T(layout) * S(scale) * T(-bounds)
            var matrixForMask = Matrix4x4.identity;
            matrixForMask.m00 = 2.0f * scaleX;
            matrixForMask.m11 = 2.0f * scaleY;
            matrixForMask.m03 = (2.0f * (layoutX - (scaleX * boundsX))) - 1.0f;
            matrixForMask.m13 = (2.0f * (layoutY - (scaleY * boundsY))) - 1.0f;
            _tmpMatrixForMask = matrixForMask;

            // The render-texture Y orientation is handled by _ProjectionParams.x in the mask shader, so the
            // draw (sample) matrix uses the Unity model-space convention directly.
            //
            // Draw: T(layout) * S(scale) * T(-bounds)
            var matrixForDraw = Matrix4x4.identity;
            matrixForDraw.m00 = scaleX;
            matrixForDraw.m11 = scaleY;
            matrixForDraw.m03 = layoutX - (scaleX * boundsX);
            matrixForDraw.m13 = layoutY - (scaleY * boundsY);
            _tmpMatrixForDraw = matrixForDraw;
        }

        /// <summary>
        /// Lays out each clipping context in mask UV space and assigns channel / buffer index.
        /// </summary>
        public void SetupLayoutBounds(int usingClipCount)
        {
            var useClippingMaskMaxCount = _renderTextureCount <= 1
                ? ClippingMaskMaxCountOnDefault
                : ClippingMaskMaxCountOnMultiRenderTexture * _renderTextureCount;

            if (usingClipCount <= 0 || usingClipCount > useClippingMaskMaxCount)
            {
                if (usingClipCount > useClippingMaskMaxCount)
                {
                    var over = usingClipCount - useClippingMaskMaxCount;
                    Debug.LogError(
                        $"not supported mask count : {over}\n[Details] render texture count: {_renderTextureCount}\n, mask count : {usingClipCount}");
                }

                for (var index = 0; index < _clippingContextListForMask.Count; index++)
                {
                    var clippingContext = _clippingContextListForMask[index];
                    clippingContext.LayoutChannelIndex = 0;
                    var layoutBounds = clippingContext.LayoutBounds;
                    layoutBounds.X = 0.0f;
                    layoutBounds.Y = 0.0f;
                    layoutBounds.Width = 1.0f;
                    layoutBounds.Height = 1.0f;
                    clippingContext.LayoutBounds = layoutBounds;
                    clippingContext.BufferIndex = 0;
                }

                return;
            }

            // Assign the layout only to the contexts that need a mask this frame. Skipping unused groups
            // keeps a later used group from being denied a slot and left with the previous frame's tile /
            // channel (which would drop its mask or collide with another group).
            _layoutTargetContexts.Clear();
            for (var index = 0; index < _clippingContextListForMask.Count; index++)
            {
                if (_clippingContextListForMask[index].IsUsing)
                {
                    _layoutTargetContexts.Add(_clippingContextListForMask[index]);
                }
            }

            var layoutCountMaxValue = _renderTextureCount <= 1 ? 9 : 8;
            var countPerSheetDiv = (usingClipCount + _renderTextureCount - 1) / _renderTextureCount;
            var reduceLayoutTextureCount = usingClipCount % _renderTextureCount;
            var divCount = countPerSheetDiv / ColorChannelCount;
            var modCount = countPerSheetDiv % ColorChannelCount;
            var curClipIndex = 0;

            for (var renderTextureIndex = 0; renderTextureIndex < _renderTextureCount; renderTextureIndex++)
            {
                for (var channelIndex = 0; channelIndex < ColorChannelCount; channelIndex++)
                {
                    var layoutCount = divCount + (channelIndex < modCount ? 1 : 0);
                    var checkChannelIndex = modCount + (divCount < 1 ? -1 : 0);

                    if (channelIndex == checkChannelIndex && reduceLayoutTextureCount > 0)
                    {
                        if (!(renderTextureIndex < reduceLayoutTextureCount))
                        {
                            layoutCount -= 1;
                        }
                    }

                    if (layoutCount == 0)
                    {
                        continue;
                    }

                    if (layoutCount == 1)
                    {
                        var clippingContext = _layoutTargetContexts[curClipIndex++];
                        clippingContext.LayoutChannelIndex = channelIndex;
                        var layoutBounds = clippingContext.LayoutBounds;
                        layoutBounds.X = 0.0f;
                        layoutBounds.Y = 0.0f;
                        layoutBounds.Width = 1.0f;
                        layoutBounds.Height = 1.0f;
                        clippingContext.LayoutBounds = layoutBounds;
                        clippingContext.BufferIndex = renderTextureIndex;
                    }
                    else if (layoutCount == 2)
                    {
                        for (var i = 0; i < layoutCount; i++)
                        {
                            var xpos = i % 2;
                            var clippingContext = _layoutTargetContexts[curClipIndex++];
                            clippingContext.LayoutChannelIndex = channelIndex;
                            var layoutBounds = clippingContext.LayoutBounds;
                            layoutBounds.X = xpos * 0.5f;
                            layoutBounds.Y = 0.0f;
                            layoutBounds.Width = 0.5f;
                            layoutBounds.Height = 1.0f;
                            clippingContext.LayoutBounds = layoutBounds;
                            clippingContext.BufferIndex = renderTextureIndex;
                        }
                    }
                    else if (layoutCount <= 4)
                    {
                        for (var i = 0; i < layoutCount; i++)
                        {
                            var xPos = i % 2;
                            var yPos = i / 2;
                            var clippingContext = _layoutTargetContexts[curClipIndex++];
                            clippingContext.LayoutChannelIndex = channelIndex;
                            var layoutBounds = clippingContext.LayoutBounds;
                            layoutBounds.X = xPos * 0.5f;
                            layoutBounds.Y = yPos * 0.5f;
                            layoutBounds.Width = 0.5f;
                            layoutBounds.Height = 0.5f;
                            clippingContext.LayoutBounds = layoutBounds;
                            clippingContext.BufferIndex = renderTextureIndex;
                        }
                    }
                    else if (layoutCount <= layoutCountMaxValue)
                    {
                        for (var i = 0; i < layoutCount; i++)
                        {
                            var xPos = i % 3;
                            var yPos = i / 3;
                            var cc = _layoutTargetContexts[curClipIndex++];
                            cc.LayoutChannelIndex = channelIndex;
                            var layoutBounds = cc.LayoutBounds;
                            layoutBounds.X = xPos / 3.0f;
                            layoutBounds.Y = yPos / 3.0f;
                            layoutBounds.Width = 1.0f / 3.0f;
                            layoutBounds.Height = 1.0f / 3.0f;
                            cc.LayoutBounds = layoutBounds;
                            cc.BufferIndex = renderTextureIndex;
                        }
                    }
                    else
                    {
                        var overClipCount = usingClipCount - useClippingMaskMaxCount;
                        Debug.LogError(
                            $"Not supported mask count : {overClipCount}\n[Details] RenderTexture Count: {_renderTextureCount}\n, MaskCount : {usingClipCount}");
                        for (var i = 0; i < layoutCount; i++)
                        {
                            var clippingContext = _layoutTargetContexts[curClipIndex++];
                            clippingContext.LayoutChannelIndex = 0;
                            var layoutBounds = clippingContext.LayoutBounds;
                            layoutBounds.X = 0.0f;
                            layoutBounds.Y = 0.0f;
                            layoutBounds.Width = 1.0f;
                            layoutBounds.Height = 1.0f;
                            clippingContext.LayoutBounds = layoutBounds;
                            clippingContext.BufferIndex = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Computes the union bounds of all clipped drawables (model coordinates).
        /// </summary>
        public void CalcClippedTotalBounds(CubismModel model, CubismClippingContext clippingContext, CubismModelTypes.DrawObjectType drawableObjectType)
        {
            var clippedDrawTotalMinX = float.MaxValue;
            var clippedDrawTotalMinY = float.MaxValue;
            var clippedDrawTotalMaxX = float.MinValue;
            var clippedDrawTotalMaxY = float.MinValue;

            _clippedOffscreenChildDrawableIndexList.Clear();

            var clippedDrawCount = 0;
            switch (drawableObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    clippedDrawCount = clippingContext.ClippedDrawableIndexList.Count;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    for (var oi = 0; oi < clippingContext.ClippedOffscreenIndexList.Count; oi++)
                    {
                        var offscreenIndex = clippingContext.ClippedOffscreenIndexList[oi];
                        CollectOffscreenChildDrawableIndexList(model, offscreenIndex, _clippedOffscreenChildDrawableIndexList);
                    }

                    clippedDrawCount = _clippedOffscreenChildDrawableIndexList.Count;
                    break;
                default:
                    clippedDrawCount = 0;
                    break;
            }

            for (var clippedObjectIndex = 0; clippedObjectIndex < clippedDrawCount; clippedObjectIndex++)
            {
                var drawableIndex = -1;
                switch (drawableObjectType)
                {
                    case CubismModelTypes.DrawObjectType.Drawable:
                        drawableIndex = clippingContext.ClippedDrawableIndexList[clippedObjectIndex];
                        break;
                    case CubismModelTypes.DrawObjectType.Offscreen:
                        drawableIndex = _clippedOffscreenChildDrawableIndexList[clippedObjectIndex];
                        break;
                }

                if (drawableIndex < 0)
                {
                    continue;
                }

                // Skip drawables hidden this frame.
                if (!IsClippedDrawableVisible(drawableIndex))
                {
                    continue;
                }

                // Use the bounds of the mesh that is actually rendered this frame.
                if (!TryGetRenderedDrawableBoundsXY(drawableIndex, out var minX, out var minY, out var maxX, out var maxY))
                {
                    continue;
                }

                if (minX < clippedDrawTotalMinX)
                {
                    clippedDrawTotalMinX = minX;
                }

                if (minY < clippedDrawTotalMinY)
                {
                    clippedDrawTotalMinY = minY;
                }

                if (maxX > clippedDrawTotalMaxX)
                {
                    clippedDrawTotalMaxX = maxX;
                }

                if (maxY > clippedDrawTotalMaxY)
                {
                    clippedDrawTotalMaxY = maxY;
                }
            }

            if (Mathf.Approximately(clippedDrawTotalMinX, float.MaxValue))
            {
                var empty = new RectFloat { X = 0.0f, Y = 0.0f, Width = 0.0f, Height = 0.0f };
                clippingContext.AllClippedDrawRect = empty;
                clippingContext.IsUsing = false;
            }
            else
            {
                clippingContext.IsUsing = true;
                var w = clippedDrawTotalMaxX - clippedDrawTotalMinX;
                var h = clippedDrawTotalMaxY - clippedDrawTotalMinY;
                clippingContext.AllClippedDrawRect = new RectFloat
                {
                    X = clippedDrawTotalMinX,
                    Y = clippedDrawTotalMinY,
                    Width = w,
                    Height = h
                };
            }
        }

        /// <summary>
        /// Computes the bounds (model coordinates) enclosing this clip group's mask drawables and overwrites
        /// <see cref="CubismClippingContext.AllClippedDrawRect"/> with it.
        /// </summary>
        /// <remarks>
        /// Used by the high-precision path, where no layout partitioning occurs. Basing the rect on the mask
        /// drawables keeps the mask resolution tied to the mask shape and suppresses flicker at the mask edges.
        /// Only the rect is updated. Deciding <see cref="CubismClippingContext.IsUsing"/> is the caller's responsibility.
        /// </remarks>
        /// <param name="clippingContext">The clipping context whose mask drawables are measured.</param>
        private void CalcMaskDrawableTotalBounds(CubismClippingContext clippingContext)
        {
            var maskDrawTotalMinX = float.MaxValue;
            var maskDrawTotalMinY = float.MaxValue;
            var maskDrawTotalMaxX = float.MinValue;
            var maskDrawTotalMaxY = float.MinValue;

            var masks = clippingContext.Masks;
            for (var maskIndex = 0; maskIndex < masks.Length; maskIndex++)
            {
                var mask = masks[maskIndex];
                if (!mask)
                {
                    continue;
                }

                // Use the bounds of the mesh actually rendered this frame (double-buffered, frame N-1),
                // matching how CalcClippedTotalBounds measures the clipped drawables.
                var mesh = mask.Mesh;
                if (!mesh || mesh.vertexCount < 1)
                {
                    continue;
                }

                var bounds = mesh.bounds;
                if (bounds.min.x < maskDrawTotalMinX)
                {
                    maskDrawTotalMinX = bounds.min.x;
                }

                if (bounds.min.y < maskDrawTotalMinY)
                {
                    maskDrawTotalMinY = bounds.min.y;
                }

                if (bounds.max.x > maskDrawTotalMaxX)
                {
                    maskDrawTotalMaxX = bounds.max.x;
                }

                if (bounds.max.y > maskDrawTotalMaxY)
                {
                    maskDrawTotalMaxY = bounds.max.y;
                }
            }

            // No valid mask vertices this frame: leave the rect untouched so the caller keeps its fallback.
            if (Mathf.Approximately(maskDrawTotalMinX, float.MaxValue))
            {
                return;
            }

            // Overwrite the clipped-drawable fallback rect with the mask-drawable rect. The caller is responsible for deciding IsUsing.
            clippingContext.AllClippedDrawRect = new RectFloat
            {
                X = maskDrawTotalMinX,
                Y = maskDrawTotalMinY,
                Width = maskDrawTotalMaxX - maskDrawTotalMinX,
                Height = maskDrawTotalMaxY - maskDrawTotalMinY
            };
        }

        /// <summary>
        /// Returns whether the drawable at the given model index is visible this frame.
        /// </summary>
        /// <param name="drawableIndex">Model index of the drawable (same order as the drawable renderers).</param>
        /// <returns><see langword="true"/> when the drawable is drawn this frame, or when visibility cannot be resolved.</returns>
        private bool IsClippedDrawableVisible(int drawableIndex)
        {
            if (!_renderController)
            {
                return true;
            }

            var drawableRenderers = _renderController.DrawableRenderers;
            if (drawableRenderers == null
                || drawableIndex < 0
                || drawableIndex >= drawableRenderers.Length)
            {
                return true;
            }

            var renderer = drawableRenderers[drawableIndex];
            if (!renderer)
            {
                return true;
            }
            var meshRenderer = renderer.MeshRenderer;

            // Resolve through the drawable renderers rather than the Core dynamic flags.
            return !meshRenderer || meshRenderer.enabled;
        }

        /// <summary>
        /// Gets the model-space XY bounds of the mesh actually rendered this frame for the given drawable.
        /// </summary>
        /// <param name="drawableIndex">Model index of the drawable (same order as the drawable renderers).</param>
        /// <param name="minX">Minimum X of the drawn mesh.</param>
        /// <param name="minY">Minimum Y of the drawn mesh.</param>
        /// <param name="maxX">Maximum X of the drawn mesh.</param>
        /// <param name="maxY">Maximum Y of the drawn mesh.</param>
        /// <returns><see langword="true"/> when valid bounds were obtained; otherwise <see langword="false"/>.</returns>
        private bool TryGetRenderedDrawableBoundsXY(int drawableIndex, out float minX, out float minY, out float maxX, out float maxY)
        {
            minX = minY = maxX = maxY = 0.0f;

            if (!_renderController)
            {
                return false;
            }

            var drawableRenderers = _renderController.DrawableRenderers;
            if (drawableRenderers == null
                || drawableIndex < 0
                || drawableIndex >= drawableRenderers.Length)
            {
                return false;
            }

            var renderer = drawableRenderers[drawableIndex];
            if (!renderer)
            {
                return false;
            }

            var mesh = renderer.Mesh;
            if (!mesh || mesh.vertexCount < 1)
            {
                return false;
            }

            // Mesh.bounds holds the tight XY AABB set from the Burst-baked vertex positions
            // (see CubismVertexPositionBaker), so its XY extent equals the per-vertex min/max in model space.
            var bounds = mesh.bounds;
            minX = bounds.min.x;
            minY = bounds.min.y;
            maxX = bounds.max.x;
            maxY = bounds.max.y;
            return true;
        }

        /// <summary>
        /// Collects drawable indices that belong to the part subtree of an offscreen.
        /// </summary>
        /// <param name="model">Target model.</param>
        /// <param name="offscreenArrayIndex">Index of the offscreen in the model's offscreen array.</param>
        /// <param name="childDrawableIndexList">List that collected drawable indices are appended to.</param>
        public static void CollectOffscreenChildDrawableIndexList(CubismModel model, int offscreenArrayIndex, List<int> childDrawableIndexList)
        {
            var offscreens = model.Offscreens;
            if (offscreenArrayIndex < 0 || offscreenArrayIndex >= offscreens.Length)
            {
                return;
            }

            var ownerIndex = offscreens[offscreenArrayIndex].OwnerIndex;
            CollectPartChildDrawableIndexList(model, ownerIndex, childDrawableIndexList);
        }

        /// <summary>
        /// Collects drawable indices under a part (recursive via child offscreens), appending to the list.
        /// </summary>
        public static void CollectPartChildDrawableIndexList(CubismModel model, int partUnmanagedIndex, List<int> childDrawableIndexList)
        {
            if (childDrawableIndexList == null)
            {
                return;
            }

            var part = FindPart(model, partUnmanagedIndex);
            if (!part)
            {
                return;
            }

            if (part.ChildDrawables != null)
            {
                for (var i = 0; i < part.ChildDrawables.Length; i++)
                {
                    childDrawableIndexList.Add(part.ChildDrawables[i].UnmanagedIndex);
                }
            }

            if (part.ChildOffscreens != null)
            {
                for (var i = 0; i < part.ChildOffscreens.Length; i++)
                {
                    var arrayIndex = GetOffscreenArrayIndex(model, part.ChildOffscreens[i]);
                    if (arrayIndex >= 0)
                    {
                        CollectOffscreenChildDrawableIndexList(model, arrayIndex, childDrawableIndexList);
                    }
                }
            }
        }

        /// <summary>
        /// Finds a part by its unmanaged index in the model.
        /// </summary>
        /// <param name="model">Target model.</param>
        /// <param name="partUnmanagedIndex">Unmanaged index of the part.</param>
        /// <returns>The part if found; otherwise, null.</returns>
        private static CubismPart FindPart(CubismModel model, int partUnmanagedIndex)
        {
            var parts = model.Parts;
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].UnmanagedIndex == partUnmanagedIndex)
                {
                    return parts[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the index of an offscreen in the model's offscreen array.
        /// </summary>
        /// <param name="model">Target model.</param>
        /// <param name="offscreen">Offscreen to find.</param>
        /// <returns>Index of the offscreen if found; otherwise, -1.</returns>
        private static int GetOffscreenArrayIndex(CubismModel model, CubismOffscreen offscreen)
        {
            if (offscreen == null)
            {
                return -1;
            }

            var offscreens = model.Offscreens;
            for (var i = 0; i < offscreens.Length; i++)
            {
                if (offscreens[i] == offscreen)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// List used when drawing to the screen (one entry per drawable, may be null).
        /// </summary>
        public IReadOnlyList<CubismClippingContext> GetClippingContextListForDraw()
        {
            return _clippingContextListForDraw;
        }

        /// <summary>
        /// List used when drawing offscreen (one entry per offscreen, may be null).
        /// </summary>
        public IReadOnlyList<CubismClippingContext> GetClippingContextListForOffscreen()
        {
            return _clippingContextListForOffscreen;
        }

        /// <summary>
        /// Unique clipping contexts (mask groups).
        /// </summary>
        public IReadOnlyList<CubismClippingContext> GetClippingContextListForMask()
        {
            return _clippingContextListForMask;
        }

        /// <summary>
        /// Returns the size of the clipping mask buffer (in pixels).
        /// </summary>
        /// <returns>The size of the clipping mask buffer.</returns>
        public Vector2 GetClippingMaskBufferSize()
        {
            return _clippingMaskBufferSize;
        }

        /// <summary>
        /// Returns the number of render textures used for clipping masks.
        /// </summary>
        /// <returns>The number of render textures used for clipping masks.</returns>
        public int GetRenderTextureCount()
        {
            return _renderTextureCount;
        }

        /// <summary>
        /// Returns the color associated with a given channel index (0-3).
        /// </summary>
        /// <param name="channelIndex">Index of the channel (0-3).</param>
        /// <returns>The color associated with the given channel index.</returns>
        public Color GetChannelFlagAsColor(int channelIndex)
        {
            return _channelColors[channelIndex];
        }

        /// <summary>
        /// Sets the size of the clipping mask buffer (in pixels).
        /// </summary>
        /// <param name="width">Width of the clipping mask buffer.</param>
        /// <param name="height">Height of the clipping mask buffer.</param>
        public void SetClippingMaskBufferSize(float width, float height)
        {
            _clippingMaskBufferSize = new Vector2(width, height);
        }

        /// <summary>
        /// Returns the cleared mask buffer flags for each render texture (true if cleared, false otherwise).
        /// </summary>
        internal bool[] ClearedMaskBufferFlags
        {
            get
            {
                return _clearedMaskBufferFlags;
            }
        }

        /// <summary>
        /// Draws the mask textures before rendering the model. This method should be called during the pre-rendering phase.
        /// </summary>
        internal void DrawMaskOnPreRendering()
        {
            if (!_renderController || _clippingContextListForMask.Count < 1)
            {
                return;
            }

            var model = _renderController.Model;
            if (!model)
            {
                return;
            }

            SetupMatrixForHighPrecision(model, CubismModelTypes.DrawObjectType.Drawable);
        }
    }
}
