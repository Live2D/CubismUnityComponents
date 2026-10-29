/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Utils;
using Live2D.Cubism.Rendering.Masking;
using Live2D.Cubism.Rendering.URP;
using Live2D.Cubism.Rendering.Util;
using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Partial class that describes processing related to the rendering method added in Cubism 5.3 and later.
    /// </summary>
    public partial class CubismRenderer
    {
        #region Values

        /// <summary>
        /// Vertices for offscreen rendering.
        /// </summary>
        internal static Vector3[] OffscreenVertices = new Vector3[]
        {
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0)
        };

        /// <summary>
        /// UVs for offscreen rendering.
        /// </summary>
        internal static Vector2[] OffscreenUVs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        /// <summary>
        /// Triangle indices for offscreen rendering.
        /// </summary>
        internal static int[] OffscreenTriangle = new int[] { 0, 1, 2, 0, 2, 3 };

        /// <summary>
        /// <see cref="Masks"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismRenderer[] _masks;

        /// <summary>
        /// Masks used by this renderer.
        /// </summary>
        public CubismRenderer[] Masks
        {
            get
            {
                return _masks;
            }
        }

        /// <summary>
        /// Clipping context assigned to this renderer for the current frame by
        /// <see cref="CubismRenderController.SetupClippingContexts"/>. Null when this renderer has no mask.
        /// </summary>
        internal CubismClippingManager.CubismClippingContext ClippingContext { get; set; }

#if UNITY_EDITOR
        /// <summary>
        /// Whether <see cref="_masks"/> have null.
        /// </summary>
        private bool _haveMasksNull;
#endif

        /// <summary>
        /// Index of the draw object.
        /// </summary>
        public int DrawObjectUnmanagedIndex { get; set; }

        /// <summary>
        /// <see cref="CubismOffscreen"/>.
        /// </summary>
        public CubismOffscreen Offscreen { get; set; }


        /// <summary>
        /// Color blend types.
        /// </summary>
        [SerializeField]
        public BlendTypes.ColorBlend ColorBlendType;

        /// <summary>
        /// Alpha blend types.
        /// </summary>
        [SerializeField]
        public BlendTypes.AlphaBlend AlphaBlendType;

        /// <summary>
        /// Type of draw object this renderer is used for.
        /// </summary>
        [SerializeField]
        public CubismModelTypes.DrawObjectType DrawObjectType;

        /// <summary>
        /// Whether this is the last draw object in the model.
        /// </summary>
        internal bool IsLastDrawObjectInModel;

        /// <summary>
        /// <see cref="OffscreenFrameBuffer"/>'s backing field.
        /// </summary>
        private RenderTexture _offscreenFrameBuffer;

        /// <summary>
        /// Offscreen render texture used for rendering.
        /// </summary>
        public RenderTexture OffscreenFrameBuffer
        {
            get
            {
                if (!_offscreenFrameBuffer
                    && RenderController.CurrentFrameBuffer
                    && (RenderController.OffscreenRenderers?.Length ?? 0) > 0)
                {
                    _offscreenFrameBuffer = CubismOffscreenRenderTextureManager.GetInstance().GetOffscreenRenderTexture(
                        RenderController.CurrentFrameBuffer);
                }

                return _offscreenFrameBuffer;
            }

            set
            {
                if (_offscreenFrameBuffer != null)
                {
                    CubismOffscreenRenderTextureManager.GetInstance().StopUsingRenderTexture(RenderController, _offscreenFrameBuffer);
                    _offscreenFrameBuffer = null;
                }
                _offscreenFrameBuffer = value;
            }
        }

        /// <summary>
        /// Offscreen render texture this renderer currently owns, or <c>null</c> when it owns none.
        /// </summary>
        /// <remarks>
        /// Side-effect-free counterpart of <see cref="OffscreenFrameBuffer"/>, whose getter acquires a
        /// texture from the pool when the backing field is empty. Use this wherever the question is
        /// "does this renderer already have a buffer", so that asking cannot answer itself by allocating
        /// one.
        /// </remarks>
        internal RenderTexture OwnedOffscreenFrameBuffer
        {
            get { return _offscreenFrameBuffer; }
        }

        /// <summary>
        /// <see cref="OffsetScale"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Vector4 _offsetScale = new Vector4(0, 0, 1, 1);

        /// <summary>
        /// <see cref="Quaternion"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Vector4 _quaternion = Vector4.zero;

        /// <summary>
        /// <see cref="ZOffset"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private float _zOffset = 0.0f;

        /// <summary>
        /// Offscreen mesh used for rendering.
        /// </summary>
        private Mesh _offscreenMesh;

        /// <summary>
        /// Whether to skip rendering for this draw object.
        /// </summary>
        public bool SkipRendering
        {
            get;
            set;
        }

        #endregion

        #region Interface For CubismRenderController

        /// <summary>
        /// Sets draw object's render order.
        /// </summary>
        /// <param name="newRenderOrder"></param>
        internal void SetDrawObjectRenderOrder(int newRenderOrder)
        {
            if (RenderOrder == newRenderOrder) return;

            RenderOrder = newRenderOrder;

            ApplySorting();
        }

        #endregion

        /// <summary>
        /// Sorting direction at the last sorting.
        /// </summary>
        internal Vector3 LastDirection;

        /// <summary>
        /// Whether the direction has been updated since the last sorting.
        /// </summary>
        /// <returns>True if the direction has changed, false otherwise.</returns>
        internal bool DidUpdateDirectionFromLastSorted(Vector3 cameraPosition)
        {
            return LastDirection != (transform.position - cameraPosition);
        }

        /// <summary>
        /// Distance from the active camera.
        /// </summary>
        internal float DistanceToCamera;

        /// <summary>
        /// <see cref="MeshRenderer.sortingOrder"/> captured once per sort.
        /// </summary>
        /// <remarks>
        /// It is cached once per renderer at the start of the sort
        /// <see cref="CubismRenderPassFeature.SortBySortingOrder"/> and the comparator reads this field.
        /// </remarks>
        internal int CachedSortingOrder;

        /// <summary>
        /// Calculates the distance by projecting the vector from camera to renderer onto the camera's forward direction.
        /// </summary>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="cameraForward">Forward direction of the camera (normalized vector).</param>
        internal void CalculateDistanceToCamera(Vector3 cameraPosition, Vector3 cameraForward)
        {
            // Vector from cameraPosition to transform.position
            var directionToRenderer = transform.position - cameraPosition;

            LastDirection = directionToRenderer;

            // Project the vector onto the camera's forward direction
            var projection = Vector3.Project(directionToRenderer, cameraForward);

            // The magnitude of the projected vector is the distance along the camera's forward direction
            // Formula: projection = dot(directionToRenderer, cameraForward) * cameraForward
            // Distance = |projection| = |dot(directionToRenderer, cameraForward)|
            DistanceToCamera = projection.magnitude;
        }

        /// <summary>
        /// <see cref="PropertyBlock"/> backing field.
        /// </summary>
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// <see cref="MaterialPropertyBlock"/>
        /// </summary>
        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                // Lazily initialize.
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }


                return _propertyBlock;
            }
        }

        /// <summary>
        /// Fetches <see cref="PropertyBlock"/> pre-populated with the <see cref="MeshRenderer"/>'s current values.
        /// </summary>
        /// <returns>The renderer's shared <see cref="MaterialPropertyBlock"/>.</returns>
        private MaterialPropertyBlock FetchPropertyBlock()
        {
            var property = PropertyBlock;
            MeshRenderer.GetPropertyBlock(property);

            return property;
        }

        /// <summary>
        /// Commits <see cref="PropertyBlock"/> back to the <see cref="MeshRenderer"/>.
        /// </summary>
        private void CommitPropertyBlock()
        {
            MeshRenderer.SetPropertyBlock(PropertyBlock);
        }

        /// <summary>
        /// Writes the common rendering texture to the given property block without committing it.
        /// </summary>
        /// <param name="property">Property block to write to.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        private void WriteBlendedRenderTexture(MaterialPropertyBlock property, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (!RenderController?.CurrentFrameBuffer || (RenderController.CanRenderDirectly && passData.EnableDirectRendering))
            {
                return;
            }

            // The temporary texture holds the backdrop this drawable's blend samples. It is only allocated
            // when something composites through the common RT, so leave the property block untouched
            // when it does not resolve to a texture.
            var temporaryTexture = (RenderTexture)passData.CommonTemporaryTextureHandle;
            if (!temporaryTexture)
            {
                return;
            }

            property.SetTexture(CubismShaderVariables.RenderTexture, temporaryTexture);
        }

        /// <summary>
        /// Adds this renderer to the command buffer for rendering.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        public void DrawObject(CommandBuffer buffer, CubismRenderPassFeature.CubismRenderPass.PassData passData)//, RenderTexture frameBuffer)
        {
            if (!MeshRenderer)
            {
                return;
            }

            switch (DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Offscreen:
                    // Set offscreen frame buffer.
                    SetOffscreen(buffer, passData);
                    break;
                case CubismModelTypes.DrawObjectType.Drawable:
                    // Draw a drawable.
                    DrawDrawable(buffer, passData);
                    break;
            }
        }

        /// <summary>
        /// Applies transform properties to the material property block.
        /// </summary>
        private void ApplyTransform()
        {
            var property = FetchPropertyBlock();

            WriteTransform(property);

            CommitPropertyBlock();
        }

        /// <summary>
        /// Reads this draw object's local transform (position, scale and rotation) directly from the transform.
        /// </summary>
        /// <remarks>
        /// Read fresh every frame so runtime changes to this draw object's scale or rotation — including ones
        /// made by user code — are reflected. Shared by the transform-writing paths so the transform component
        /// is dereferenced once per draw object.
        /// </remarks>
        /// <param name="localPosition">The transform's local position.</param>
        /// <param name="localScale">The transform's local scale.</param>
        /// <param name="localRotation">The transform's local rotation.</param>
        private void GetLocalTransform(out Vector3 localPosition, out Vector3 localScale, out Quaternion localRotation)
        {
            var localTransform = transform;
            localPosition = localTransform.localPosition;
            localScale = localTransform.localScale;
            localRotation = localTransform.localRotation;
        }

        /// <summary>
        /// Writes transform properties (offset-scale, rotation and z offset) to the given property block without committing it.
        /// </summary>
        /// <param name="property">Property block to write to.</param>
        private void WriteTransform(MaterialPropertyBlock property)
        {
            // Combine the once-per-pass root snapshot with this drawable's own local transform, so the
            // model root's hierarchy walk (lossyScale) is not repeated for every drawable.
            var root = RenderController.RootTransform;
            GetLocalTransform(out var localPosition, out var localScale, out var localRotation);

            // Set offset and scale from transform.
            var offsetScale = _offsetScale;

            offsetScale.Set(root.Position.x + localPosition.x, root.Position.y + localPosition.y,
                root.LossyScale.x * localScale.x, root.LossyScale.y * localScale.y);
            _offsetScale = offsetScale;
            // Write property.
            property.SetVector(CubismShaderVariables.OffsetScale, _offsetScale);

            // Set rotation from transform.
            var combinedRotation = root.Rotation * localRotation;
            _quaternion.Set(combinedRotation.x, combinedRotation.y, combinedRotation.z, combinedRotation.w);

            // Write property.
            property.SetVector(CubismShaderVariables.RotationQuaternion, _quaternion);

            // Set z offset from transform.
            _zOffset = root.Position.z + localPosition.z;
            // Write property.
            property.SetFloat(CubismShaderVariables.ZOffset, _zOffset);
        }

        /// <summary>
        /// Sets the offscreen frame buffer for rendering.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        private void SetOffscreen(CommandBuffer buffer, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            var currentOffscreenUnmanagedIndex = RenderController.CurrentOffscreenUnmanagedIndex;
            SubmitDrawToParentOffscreen(ref passData, ref currentOffscreenUnmanagedIndex,
                buffer, this);

            // The pooled offscreen textures are created and resized from this texture, so without it there
            // is no buffer to draw into and this offscreen is not entered.
            var baseTexture = passData.CommonRenderingTexture;
            if (!baseTexture)
            {
                // The unwind above already moved the current frame buffer, so publish the stack index that
                // goes with it: the normal path below overwrites the index anyway, but bailing out here
                // would otherwise leave the model marked as being inside an offscreen it has just left.
                RenderController.CurrentOffscreenUnmanagedIndex = currentOffscreenUnmanagedIndex;

                return;
            }

            // Ready the render target to the offscreen frame buffer.
            OffscreenFrameBuffer = CubismOffscreenRenderTextureManager.GetInstance().GetOffscreenRenderTexture(baseTexture);

            // Set current frame buffer to offscreen frame buffer.
            buffer.SetRenderTarget(OffscreenFrameBuffer);

            // Clear the whole offscreen frame buffer, not just the region that receives content.
            // A scissor rectangle does not make the clear any cheaper: measured on Metal, a clear
            // issued inside the pass costs a full surface of fragment work whether or not one is set.
            // Clearing everything is therefore free of extra cost here, and it leaves no older frame's
            // content anywhere, which is what lets the composite be limited to its own region without
            // the two having to agree on a rectangle.
            //
            // The clear itself is not cheap - it is the single largest remaining GPU cost of this path -
            // but the way to cut it is a smaller render target, not a smaller clear.
            buffer.ClearRenderTarget(false, true, Color.clear);

            // Set up for drawing to offscreen.
            RenderController.CurrentFrameBuffer = OffscreenFrameBuffer;
            RenderController.CurrentOffscreenUnmanagedIndex = Offscreen.UnmanagedIndex;
        }

        /// <summary>
        /// Draws the mask and binds it for the draw that follows.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        /// <returns>
        /// True when the draw object can be drawn: it is unmasked, or its mask was rendered and bound.
        /// False when it is masked but the mask could not be resolved for this frame, in which case the
        /// caller has to skip the draw. Drawing it anyway would sample the mask bound on an earlier frame,
        /// whose texture the render graph may have recycled in the meantime.
        /// </returns>
        internal bool TryDrawMasks(CommandBuffer buffer, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            // If the model doesn't have masks, return.
            if (!RenderController.HasMask)
            {
                return true;
            }

#if UNITY_EDITOR
            // If the masks have null, try to initialize them.
            if (_haveMasksNull)
            {
                TryInitializeMasks();
                _haveMasksNull = false;
            }
#endif

            if (!(_masks?.Length > 0))
            {
                return true;
            }

            // Ensure this frame's clipping contexts are computed.
            // This is normally done once per frame by the render pass before drawing, but call it here as well (frame-guarded, so idempotent and cheap).
            RenderController.SetupClippingContexts();

            var isOffscreenDrawObject = DrawObjectType == CubismModelTypes.DrawObjectType.Offscreen;

            // Drawable mask path. The mask/draw matrices and channel come from the context. A missing
            // context means the mask cannot be placed. Offscreen masks are placed in screen space and need
            // no context.
            var clipContext = ClippingContext;
            if (!isOffscreenDrawObject
                && clipContext == null)
            {
                return false;
            }

            // Two paths share the dedicated full-screen high-precision texture. Offscreen masks are rendered
            // in screen space (transform / OffscreenMask path) and cannot be packed into the atlas, and the
            // high-precision drawable path redraws this drawable's masks (full region) just before drawing
            // rather than sampling the atlas.
            if (isOffscreenDrawObject
                || (RenderController.MaskTextureController
                    && RenderController.MaskTextureController.UseHighPrecisionMask))
            {
                // The high-precision mask buffer is a TextureHandle (Unity 6000 render graph) or an RTHandle (Unity 2022, Taunjie) both convert to RenderTexture,
                // so the mask methods work across versions.
                var highPrecisionMaskTexture = (RenderTexture)passData.HighPrecisionMaskTextureHandle;
                if (!highPrecisionMaskTexture)
                {
                    return false;
                }

                if (isOffscreenDrawObject)
                {
                    RenderOffscreenMasks(buffer, highPrecisionMaskTexture);
                    // Overwrites the shared high-precision texture, so no clip group is resident afterwards.
                    passData.ActiveHighPrecisionMaskContext = null;
                }
                else
                {
                    // The mask cache lets consecutive draw objects that share this clip group reuse the
                    // texture instead of re-rendering it.
                    var canReuse = RenderController.EnableMaskCache
                        && passData.ActiveHighPrecisionMaskContext == clipContext;
                    if (!canReuse)
                    {
                        RenderHighPrecisionMask(buffer, highPrecisionMaskTexture, clipContext);
                        passData.ActiveHighPrecisionMaskContext = clipContext;
                    }
                }

                ApplyMask(highPrecisionMaskTexture);

                return true;
            }

            // Atlas path: the controller's atlas was rendered once in the mask pre-pass
            // (CubismRenderController.PrepareMaskAtlas) before the object draw loop. This drawable simply
            // samples the buffer its clip group was packed into.
            var atlasBuffer = RenderController.GetMaskAtlasBuffer(clipContext.BufferIndex);
            if (!atlasBuffer)
            {
                return false;
            }

            ApplyMask(atlasBuffer);

            return true;
        }

        /// <summary>
        /// Draws this renderer's mesh into the mask atlas as a mask, packed into the given clip group's
        /// channel and layout tile. Invoked by the controller's mask pre-pass for each mask of each group.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="clipContext">Clip group whose channel / tile / matrix this mask is packed into.</param>
        internal void DrawAsMask(CommandBuffer buffer, CubismClippingManager.CubismClippingContext clipContext)
        {
            var property = PropertyBlock;
            property.SetTexture(CubismShaderVariables.MainTexture, MainTexture);
            property.SetVector(CubismShaderVariables.MaskUvTransform, MatrixToMaskUvTransform(clipContext.MatrixForMask));
            property.SetVector(CubismShaderVariables.ChannelFlag, GetMaskChannelFlag(clipContext));
            property.SetVector(CubismShaderVariables.MaskRegion, GetMaskRegion(clipContext));

            buffer.DrawMesh(
                Mesh,
                Matrix4x4.identity,
                Drawable.IsDoubleSided
                    ? CubismBuiltinMaterials.Mask
                    : CubismBuiltinMaterials.MaskCulling,
                0,
                0,
                property);
        }

        /// <summary>
        /// Renders this drawable's masks (full region) into the dedicated high-precision mask texture.
        /// Used by the per-draw-object high-precision path. Does not touch the atlas.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="maskTexture">High-precision mask render texture.</param>
        /// <param name="clipContext">Clipping context providing the mask matrices, channel and region.</param>
        private void RenderHighPrecisionMask(CommandBuffer buffer, RenderTexture maskTexture, CubismClippingManager.CubismClippingContext clipContext)
        {
            buffer.SetRenderTarget(maskTexture);
            // Color only: the mask texture is bound without a depth attachment, and the mask
            // shaders are `ZWrite Off`, so there is no depth to clear.
            buffer.ClearRenderTarget(false, true, Color.white);

            var maskUvTransform = MatrixToMaskUvTransform(clipContext.MatrixForMask);
            var channelFlag = GetMaskChannelFlag(clipContext);
            var maskRegion = GetMaskRegion(clipContext);

            for (var maskIndex = 0; maskIndex < _masks.Length; maskIndex++)
            {
                var mask = _masks[maskIndex];
                if (!mask)
                {
#if UNITY_EDITOR
                    _haveMasksNull = true;
#endif
                    continue;
                }

                mask.PropertyBlock.SetTexture(CubismShaderVariables.MainTexture, mask.MainTexture);
                mask.PropertyBlock.SetVector(CubismShaderVariables.MaskUvTransform, maskUvTransform);
                mask.PropertyBlock.SetVector(CubismShaderVariables.ChannelFlag, channelFlag);
                mask.PropertyBlock.SetVector(CubismShaderVariables.MaskRegion, maskRegion);

                buffer.DrawMesh(
                    mask.Mesh,
                    Matrix4x4.identity,
                    mask.Drawable.IsDoubleSided
                        ? CubismBuiltinMaterials.Mask
                        : CubismBuiltinMaterials.MaskCulling,
                    0,
                    0,
                    mask.PropertyBlock);
            }
        }

        /// <summary>
        /// Renders this offscreen's masks into the dedicated full-screen mask texture (transform / OffscreenMask path).
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="maskTexture">Mask render texture.</param>
        private void RenderOffscreenMasks(CommandBuffer buffer, RenderTexture maskTexture)
        {
            buffer.SetRenderTarget(maskTexture);
            buffer.ClearRenderTarget(true, true, Color.white);

            for (var maskIndex = 0; maskIndex < _masks.Length; maskIndex++)
            {
                var mask = _masks[maskIndex];
                if (!mask)
                {
#if UNITY_EDITOR
                    _haveMasksNull = true;
#endif
                    continue;
                }

                mask.PropertyBlock.SetTexture(CubismShaderVariables.MainTexture, mask.MainTexture);
                mask.ApplyTransform();

                buffer.DrawMesh(
                    mask.Mesh,
                    Matrix4x4.identity,
                    CubismBuiltinMaterials.OffscreenMask,
                    0,
                    0,
                    mask.PropertyBlock);
            }
        }

        /// <summary>
        /// Returns the RGBA channel selector for the given clipping context.
        /// </summary>
        private static Vector4 GetMaskChannelFlag(CubismClippingManager.CubismClippingContext clipContext)
        {
            return clipContext.ClippingManager.GetChannelFlagAsColor(clipContext.LayoutChannelIndex);
        }

        /// <summary>
        /// Returns the layout tile in clip space (X, Y, Right, Bottom) mapped from [0,1] layout bounds to [-1,1].
        /// </summary>
        private static Vector4 GetMaskRegion(CubismClippingManager.CubismClippingContext clipContext)
        {
            var lb = clipContext.LayoutBounds;
            return new Vector4(
                lb.X * 2.0f - 1.0f,
                lb.Y * 2.0f - 1.0f,
                lb.GetRight() * 2.0f - 1.0f,
                lb.GetBottom() * 2.0f - 1.0f);
        }

        /// <summary>
        /// Packs the diagonal (rotation-free) mask matrix into a <see cref="Vector4"/>
        /// (scaleX, scaleY, offsetX, offsetY) for the <c>cubism_MaskUvTransform</c> shader variable.
        /// </summary>
        private static Vector4 MatrixToMaskUvTransform(Matrix4x4 m)
        {
            return new Vector4(m.m00, m.m11, m.m03, m.m13);
        }

        /// <summary>
        /// Submits the offscreen frame buffer and draws to the parent offscreen if necessary.
        /// </summary>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        /// <param name="currentOffscreenUnmanagedIndex">Current offscreen's unmanaged index.</param>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="targetRenderer">Current target rendering object.</param>
        private void SubmitDrawToParentOffscreen(ref CubismRenderPassFeature.CubismRenderPass.PassData passData, ref int currentOffscreenUnmanagedIndex,
            CommandBuffer buffer, CubismRenderer targetRenderer)
        {
            if (passData == null
                || currentOffscreenUnmanagedIndex == -1)
            {
                return;
            }

            // Destination the offscreen stack unwinds to. The camera color target stands in for the common
            // rendering texture when it is unavailable, so the test below stays meaningful either way.
            var commonRenderingTexture = passData.CommonRenderingTexture;

            if (RenderController.CurrentFrameBuffer == commonRenderingTexture)
            {
                return;
            }

            // Get the current offscreen renderer.
            CubismRenderer offscreenRenderer = null;
            for (var offscreenRendererIndex = 0; offscreenRendererIndex < RenderController.OffscreenRenderers.Length; offscreenRendererIndex++)
            {
                if (RenderController.OffscreenRenderers[offscreenRendererIndex].Offscreen.UnmanagedIndex !=
                    currentOffscreenUnmanagedIndex)
                {
                    continue;
                }

                offscreenRenderer = RenderController.OffscreenRenderers[offscreenRendererIndex];
                break;
            }
            var currentOwnerIndex = offscreenRenderer?.Offscreen.OwnerIndex ?? -1;

            if (currentOwnerIndex == -1)
            {
                // Fail silently if the offscreen renderer is not found.
                return;
            }

            var targetParentIndex = -1;
            RenderTexture previousOffscreen = null;

            // Paired with previousOffscreen: only ever set together with it, so the index the caller
            // restores always names the offscreen whose buffer became the current render target.
            // -1 means "no ancestor offscreen buffer was found", which is also the state used for the
            // root part offscreen and the common rendering texture.
            var previousOffscreenUnmanagedIndex = -1;

            // Also paired with previousOffscreen: the renderer that owns it, needed so the composite can
            // swap that renderer's buffer instead of blitting. Stays null when the destination ends up
            // being the common rendering texture, which no renderer owns.
            CubismRenderer previousOffscreenOwner = null;

            switch (targetRenderer.DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    targetParentIndex = targetRenderer.Drawable.ParentPartIndex;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    // Check target renderer's offscreen owner type.
                    targetParentIndex = RenderController.Model.Parts[targetRenderer.Offscreen.OwnerIndex].UnmanagedParentIndex;
                    break;
                default:
                    // If the draw object type is not drawable or offscreen, return.
                    return;
            }

            // Descendant test: if targetRenderer is a descendant of the current offscreen owner, no pop
            // happens and this method returns as a no-op. WouldPopOffscreenStack() mirrors this exact test
            // to decide batchability, so keep the two in sync when changing either.
            // If targetRenderer isn't child of the current offscreen renderer.
            while (targetParentIndex != -1)
            {
                // If the target parent index is the same as the current owner index, return.
                if (targetParentIndex == RenderController.Model.Parts[currentOwnerIndex].UnmanagedIndex)
                {
                    return;
                }

                targetParentIndex = RenderController.Model.Parts[targetParentIndex].UnmanagedParentIndex;
            }
            var parentIndex = RenderController.Model.Parts[currentOwnerIndex].UnmanagedParentIndex;

            // Find the offscreen renderer with the previous offscreen unmanaged index.
            while (parentIndex != -1)
            {
                var part = RenderController.Model.Parts[parentIndex];
                if (part.OffscreenIndex != -1)
                {
                    for (var offscreenRendererIndex = 0; offscreenRendererIndex < RenderController.OffscreenRenderers.Length; offscreenRendererIndex++)
                    {
                        var element = RenderController.OffscreenRenderers[offscreenRendererIndex];

                        if (!element.isActiveAndEnabled
                            || !element.MeshRenderer.enabled
                            || element.Offscreen.UnmanagedIndex != part.OffscreenIndex)
                        {
                            continue;
                        }

                        // Only adopt this ancestor when it actually owns a buffer. An ancestor offscreen
                        // always runs SetOffscreen() before its descendants, and one that is skipped
                        // propagates SkipRendering to every descendant, so in normal flow the buffer is
                        // there. This guards the case where that invariant breaks. Read the owned buffer
                        // rather than OffscreenFrameBuffer, whose getter would acquire a blank texture
                        // from the pool and make the test pass by allocating what it asked about.
                        var parentOffscreenFrameBuffer = element.OwnedOffscreenFrameBuffer;

                        if (parentOffscreenFrameBuffer)
                        {
                            previousOffscreen = parentOffscreenFrameBuffer;
                            previousOffscreenUnmanagedIndex = part.OffscreenIndex;
                            previousOffscreenOwner = element;
                        }

                        break;
                    }
                }

                parentIndex = RenderController.Model.Parts[parentIndex].UnmanagedParentIndex;

                if (!previousOffscreen)
                {
                    continue;
                }

                break;
            }

            // Try to get the root part offscreen if there is no parent offscreen.
            if (!previousOffscreen && RenderController.HasRootPartOffscreen)
            {
                var offscreenRenderers = RenderController.OffscreenRenderers;

                for (var offscreenIndex = 0; offscreenIndex < offscreenRenderers.Length; offscreenIndex++)
                {
                    if (offscreenRenderers[offscreenIndex].Offscreen.UnmanagedIndex != 0)
                    {
                        continue;
                    }

                    // Same buffer / owner pairing as the ancestor search above, and the same reason for
                    // reading the owned buffer instead of the allocating property.
                    var rootOffscreenFrameBuffer = offscreenRenderers[offscreenIndex].OwnedOffscreenFrameBuffer;

                    if (rootOffscreenFrameBuffer)
                    {
                        previousOffscreen = rootOffscreenFrameBuffer;
                        previousOffscreenOwner = offscreenRenderers[offscreenIndex];
                    }

                    break;
                }
            }

            // If there is no parent offscreen, use the common rendering texture.
            if (!previousOffscreen)
            {
                previousOffscreen = commonRenderingTexture;
            }

            // Nothing to composite into, which only happens when no render target resolves at all. Give the
            // offscreen's buffer back to the pool anyway - assigning null is the only thing that returns it,
            // so bailing out here would leak it for the rest of the session - and unwind to the top level.
            if (!previousOffscreen)
            {
                offscreenRenderer.OffscreenFrameBuffer = null;
                RenderController.CurrentFrameBuffer = null;
                currentOffscreenUnmanagedIndex = -1;

                return;
            }

            // Read the owned buffer here too: asking whether the destination already is this renderer's
            // buffer must not acquire one from the pool to answer with.
            if (previousOffscreen == offscreenRenderer?.OwnedOffscreenFrameBuffer)
            {
                return;
            }

            // If It can copy the parent offscreen, copy it to the current offscreen renderer.
            // A ping-ponged composite returns a different texture than it was given, and that one is now
            // the owner's buffer, so everything below has to continue from the returned texture.
            previousOffscreen = DrawOffscreen(buffer, previousOffscreen, previousOffscreenOwner, offscreenRenderer, passData);

            offscreenRenderer.OffscreenFrameBuffer = null;
            RenderController.CurrentFrameBuffer = previousOffscreen;
            currentOffscreenUnmanagedIndex = previousOffscreenUnmanagedIndex;

            // If the current offscreen is the parent of the target renderer, draw to the parent offscreen.
            SubmitDrawToParentOffscreen(ref passData, ref currentOffscreenUnmanagedIndex,
                buffer, targetRenderer);
        }

        /// <summary>
        /// Renders a drawable.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        private void DrawDrawable(CommandBuffer buffer, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (!RenderController)
            {
                return;
            }

            if (RenderController.CurrentOffscreenUnmanagedIndex != -1)
            {
                var currentOffscreenOwnerUnmanagedIndex = RenderController.CurrentOffscreenUnmanagedIndex;
                SubmitDrawToParentOffscreen(ref passData, ref currentOffscreenOwnerUnmanagedIndex,
                    buffer, this);

                RenderController.CurrentOffscreenUnmanagedIndex = currentOffscreenOwnerUnmanagedIndex;
            }

            // Mask rendering. A masked drawable whose mask could not be rendered this frame is skipped
            // rather than drawn with the mask of an earlier frame.
            if (!TryDrawMasks(buffer, passData))
            {
                return;
            }

            // Reuse the persistent property block directly instead of reading it back with GetPropertyBlock.
            // Every property the block holds was written through this same instance (mask properties from
            // DrawMasks just above, cubism_ModelOpacity from OnModelOpacityDidChange, and the draw properties
            // overwritten below), so the read-back only re-read our own values and can be skipped.
            var property = PropertyBlock;

            // Write properties to the property block.
            WriteMainTexture(property);
            WriteBlendedRenderTexture(property, passData);
            WriteDisplayedScreenColor(property);
            WriteDisplayedMultiplyColor(property);
            WriteTransform(property);

            // Set the property block to the mesh renderer (single commit for the mask + draw properties).
            CommitPropertyBlock();


            // In the case of color blending before Cubism 5.2.
            if ((ColorBlendType == BlendTypes.ColorBlend.Normal
                && AlphaBlendType == BlendTypes.AlphaBlend.Over)
                || ColorBlendType == BlendTypes.ColorBlend.Add
                || ColorBlendType == BlendTypes.ColorBlend.Multiply)
            {
                // The depth surface does not resolve to a RenderTexture when the camera renders straight to
                // the backBuffer, so the size check is only made when there is something to compare against.
                var depthTexture = (RenderTexture)passData.CameraDepthTextureHandle;

                // If the current frame buffer is different from the common rendering texture, update it.
                // HACK: Assumes that the size has already been corrected in the `SetOffscreen()` function for cases where drawing occurs to the offscreen.
                if (!RenderController.CurrentFrameBuffer
                    || (depthTexture
                        && (RenderController.CurrentFrameBuffer.width != depthTexture.width
                            || RenderController.CurrentFrameBuffer.height != depthTexture.height)))
                {
                    // A direct group renders straight to the camera color target and never blits the common RT
                    // back, so diverting this draw to the common RT would drop it (or break sort order). Keep
                    // direct groups on the camera target. Only common-RT groups fall back to the common RT.
                    RenderController.CurrentFrameBuffer = passData.CurrentGroupRendersDirect
                        ? (RenderTexture)passData.CameraTextureHandle
                        : passData.CommonRenderingTexture;
                }

                // Set render target with depth buffer for proper depth testing.
                // Nothing resolves to a RenderTexture when the camera renders straight to the backBuffer,
                // so bind the camera handle itself, which stays a valid render target in that case.
                if (RenderController.CurrentFrameBuffer)
                {
                    buffer.SetRenderTarget(RenderController.CurrentFrameBuffer, passData.CameraDepthTextureHandle);
                }
                else
                {
                    buffer.SetRenderTarget(passData.CameraTextureHandle, passData.CameraDepthTextureHandle);
                }

                // Draw the mesh with the material.
                buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);

                return;
            }

            var frameBuffer = RenderController.CurrentFrameBuffer;

            // The blend shader samples the backdrop from the temporary texture, so the drawable cannot be
            // drawn without it. Skip the draw for this frame rather than blitting into an unknown target.
            var temporaryTexture = (RenderTexture)passData.CommonTemporaryTextureHandle;

            if (!frameBuffer
                || !temporaryTexture)
            {
                return;
            }

            // Copy only the region this drawable covers from the current frame buffer into the temporary
            // texture. The blend shader samples the backdrop only where the mesh draws, so copying the whole
            // buffer is wasteful. Pixels outside the region keep stale data but are never sampled.
            //
            // The copy goes through CopyRegionToTarget rather than CommandBuffer.Blit, because Blit binds the
            // destination itself and that render target change drops the scissor rectangle - the copy would
            // cover the whole destination no matter how small the region is. Scissor is bottom-left origin
            // pixel space, matching GetDrawableScreenRect's output.
            var copyRect = RenderController.EnableScissorBlit
                ? GetDrawableScreenRect(passData, frameBuffer.width, frameBuffer.height)
                : default;

            CubismRenderPassFeature.CubismRenderPass.CopyRegionToTarget(
                buffer, frameBuffer, temporaryTexture, copyRect, RenderController.EnableScissorBlit);

            // Set temporary render target.
            buffer.SetRenderTarget(RenderController.CurrentFrameBuffer, passData.CameraDepthTextureHandle);

            // Draw the mesh with the material.
            buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);
        }

        /// <summary>
        /// Calculates the minimal rectangle, in the target texture's pixel space, that the current
        /// drawable's mesh covers on screen. Used to limit the backdrop copy in
        /// <see cref="DrawDrawable"/> to only the region the blend shader actually samples.
        /// </summary>
        /// <param name="passData">Pass data containing the camera used for projection.</param>
        /// <param name="targetWidth">Width of the copy target texture in pixels.</param>
        /// <param name="targetHeight">Height of the copy target texture in pixels.</param>
        /// <returns>Pixel-space rectangle (bottom-left origin) clamped to the target size.</returns>
        private RectInt GetDrawableScreenRect(CubismRenderPassFeature.CubismRenderPass.PassData passData, int targetWidth, int targetHeight)
        {
            var camera = passData.CameraData.Camera;

            if (camera == null)
            {
                return new RectInt(0, 0, Mathf.Max(0, targetWidth), Mathf.Max(0, targetHeight));
            }

            // Same transform the vertex shader applies (see BlendImage.shader vert). _offsetScale
            // (.xy = offset, .zw = scale), _quaternion and _zOffset hold the values WriteTransform
            // cached for this drawable immediately before this draw, so they match what the shader got.
            return ComputeMeshScreenRect(
                Mesh,
                CubismMath.GetViewProjection(camera),
                new Vector2(_offsetScale.x, _offsetScale.y),
                new Vector2(_offsetScale.z, _offsetScale.w),
                new Quaternion(_quaternion.x, _quaternion.y, _quaternion.z, _quaternion.w),
                _zOffset,
                targetWidth,
                targetHeight);
        }

        /// <summary>
        /// Projects the XY bounds of <paramref name="mesh"/> using the same scale -> rotate -> translate
        /// transform the Cubism vertex shader applies, and returns the minimal pixel-space rectangle the
        /// mesh covers on screen.
        /// </summary>
        /// <param name="mesh">Mesh whose XY bounds are projected.</param>
        /// <param name="viewProjection">View-projection from <see cref="CubismMath.GetViewProjection"/>.</param>
        /// <param name="offset">World-space XY offset applied after scale and rotation.</param>
        /// <param name="scale">XY scale applied before rotation.</param>
        /// <param name="rotation">Rotation applied after scale.</param>
        /// <param name="zOffset">World-space Z offset applied after rotation.</param>
        /// <param name="targetWidth">Width of the target texture in pixels.</param>
        /// <param name="targetHeight">Height of the target texture in pixels.</param>
        /// <returns>
        /// Pixel-space rectangle (bottom-left origin) clamped to the target size. Returns a full-size
        /// rectangle when the projection cannot be trusted (missing mesh, or a corner on or behind the
        /// camera plane) so callers fall back to a whole-buffer operation.
        /// </returns>
        private static RectInt ComputeMeshScreenRect(Mesh mesh, Matrix4x4 viewProjection, Vector2 offset, Vector2 scale, Quaternion rotation, float zOffset, int targetWidth, int targetHeight)
        {
            // Fall back to a full-size copy if anything required for projection is missing.
            if (mesh == null || targetWidth <= 0 || targetHeight <= 0)
            {
                return new RectInt(0, 0, Mathf.Max(0, targetWidth), Mathf.Max(0, targetHeight));
            }

            var bounds = mesh.bounds;
            var min = bounds.min;
            var max = bounds.max;

            var minUv = new Vector2(float.MaxValue, float.MaxValue);
            var maxUv = new Vector2(float.MinValue, float.MinValue);

            // Project the 4 corners of the mesh's XY bounds. Cubism drawable meshes are planar
            // (constant z), so the 4 XY corners fully describe the on-screen footprint and there
            // is no need to project all 8 corners of the 3D bounds.
            var z = bounds.center.z;
            for (var i = 0; i < 4; ++i)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    z);

                // Scale -> rotate -> offset -> z offset. DrawMesh uses an identity matrix,
                // so the object space position is also the world space position.
                corner.x *= scale.x;
                corner.y *= scale.y;
                corner = rotation * corner;
                corner.x += offset.x;
                corner.y += offset.y;
                corner.z += zOffset;

                // World -> clip.
                var clip = viewProjection * new Vector4(corner.x, corner.y, corner.z, 1.0f);

                // Guard against division by (near) zero, i.e. points on or behind the camera plane.
                // In that case we cannot trust the projection, so copy the whole buffer.
                if (clip.w <= 0.0001f)
                {
                    return new RectInt(0, 0, targetWidth, targetHeight);
                }

                // Clip -> NDC -> viewport UV (bottom-left origin).
                var uv = new Vector2(
                    (clip.x / clip.w) * 0.5f + 0.5f,
                    (clip.y / clip.w) * 0.5f + 0.5f);

                minUv = Vector2.Min(minUv, uv);
                maxUv = Vector2.Max(maxUv, uv);
            }

            // Convert to pixel space with a small margin to cover bilinear filtering at the edges.
            const int margin = 2;
            var xMin = Mathf.FloorToInt(minUv.x * targetWidth) - margin;
            var yMin = Mathf.FloorToInt(minUv.y * targetHeight) - margin;
            var xMax = Mathf.CeilToInt(maxUv.x * targetWidth) + margin;
            var yMax = Mathf.CeilToInt(maxUv.y * targetHeight) + margin;

            // Clamp to the target texture bounds.
            xMin = Mathf.Clamp(xMin, 0, targetWidth);
            yMin = Mathf.Clamp(yMin, 0, targetHeight);
            xMax = Mathf.Clamp(xMax, 0, targetWidth);
            yMax = Mathf.Clamp(yMax, 0, targetHeight);

            return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
        }

        /// <summary>
        /// Computes the same transform values <see cref="WriteTransform"/> writes, without touching
        /// the cached fields.
        /// </summary>
        /// <param name="offset">World-space XY offset applied after scale and rotation.</param>
        /// <param name="scale">XY scale applied before rotation.</param>
        /// <param name="rotation">Rotation applied after scale.</param>
        /// <param name="zOffset">World-space Z offset applied after rotation.</param>
        /// <remarks>
        /// <see cref="WriteTransform"/> caches its results in <c>_offsetScale</c> / <c>_quaternion</c> /
        /// <c>_zOffset</c>, but those hold the values of the last draw this renderer recorded, which is
        /// a frame old while another renderer is being processed. Callers that need a renderer's
        /// transform out of turn have to recompute it.
        /// </remarks>
        private void ComputeTransformValues(out Vector2 offset, out Vector2 scale, out Quaternion rotation, out float zOffset)
        {
            var controllerTransform = RenderController.transform;
            var controllerPosition = controllerTransform.position;
            GetLocalTransform(out var localPosition, out var localScale, out var localRotation);

            offset = new Vector2(
                controllerPosition.x + localPosition.x,
                controllerPosition.y + localPosition.y);
            scale = new Vector2(
                controllerTransform.lossyScale.x * localScale.x,
                controllerTransform.lossyScale.y * localScale.y);
            rotation = controllerTransform.rotation * localRotation;
            zOffset = controllerPosition.z + localPosition.z;
        }

        /// <summary>
        /// Region of this offscreen's frame buffer that receives content this frame, in target pixels
        /// (bottom-left origin). Only meaningful together with <see cref="HasOffscreenScreenRect"/>.
        /// </summary>
        internal RectInt OffscreenScreenRect { get; private set; }

        /// <summary>
        /// Whether <see cref="OffscreenScreenRect"/> holds a usable region for this frame.
        /// </summary>
        /// <remarks>
        /// False when no drawable contributed, and while the pass has not run its rectangle update -
        /// which is also the case for every renderer of a model that opted out. Callers must fall back
        /// to whole-buffer operations then.
        /// </remarks>
        internal bool HasOffscreenScreenRect { get; private set; }

        /// <summary>
        /// Drops the region accumulated for the previous frame.
        /// </summary>
        internal void ResetOffscreenScreenRect()
        {
            OffscreenScreenRect = new RectInt(0, 0, 0, 0);
            HasOffscreenScreenRect = false;
        }

        /// <summary>
        /// Grows this offscreen's region to also contain <paramref name="rect"/>.
        /// </summary>
        /// <param name="rect">Region of one drawable that renders into this offscreen.</param>
        internal void UnionOffscreenScreenRect(RectInt rect)
        {
            if (rect.width <= 0
                || rect.height <= 0)
            {
                return;
            }

            if (!HasOffscreenScreenRect)
            {
                OffscreenScreenRect = rect;
                HasOffscreenScreenRect = true;

                return;
            }

            OffscreenScreenRect = CubismMath.UnionRects(OffscreenScreenRect, rect);
        }

        /// <summary>
        /// Returns the region this drawable covers on screen, computed from its current transform.
        /// </summary>
        /// <param name="viewProjection">View-projection from <see cref="CubismMath.GetViewProjection"/>.</param>
        /// <param name="targetWidth">Width of the target texture in pixels.</param>
        /// <param name="targetHeight">Height of the target texture in pixels.</param>
        /// <returns>Pixel-space rectangle (bottom-left origin) clamped to the target size.</returns>
        internal RectInt ComputeScreenRect(Matrix4x4 viewProjection, int targetWidth, int targetHeight)
        {
            ComputeTransformValues(out var offset, out var scale, out var rotation, out var zOffset);

            return ComputeMeshScreenRect(Mesh, viewProjection, offset, scale, rotation, zOffset, targetWidth, targetHeight);
        }

        /// <summary>
        /// Returns whether this offscreen's composite can be limited to <see cref="OffscreenScreenRect"/>,
        /// and gives that region.
        /// </summary>
        /// <param name="rect">The region to limit to. Only valid when this returns true.</param>
        /// <returns>True when the caller should scissor to <paramref name="rect"/>.</returns>
        /// <remarks>
        /// Only the composites ask this. The clears that precede them always cover the whole buffer, so
        /// a composite limited to a region leaves the cleared result everywhere else rather than an
        /// older frame's content, and the two no longer have to agree on a region.
        ///
        /// The custom blend path gets the whole model's region instead of this offscreen's own, because
        /// it writes with <c>Blend One Zero</c> and therefore replaces rather than blends into its
        /// destination: the composite has to run wherever the destination already holds content, which
        /// for a ping-pong composite is the region of an ancestor offscreen, and it samples this buffer
        /// there. The model's region contains every ancestor's, so using it keeps "a buffer is only ever
        /// sampled inside the region it was cleared" true without walking the chain.
        ///
        /// One case still opts out entirely: a model opacity below one makes an out-of-rect pixel of a
        /// custom blend composite write the backdrop darkened by <c>cubism_ModelOpacity</c> (the double
        /// multiply, see <see cref="CubismRenderController.Opacity"/>), which is visible.
        /// </remarks>
        private bool TryGetOffscreenScissorRect(out RectInt rect)
        {
            rect = OffscreenScreenRect;

            if (!RenderController
                || !RenderController.EnableOffscreenScissor)
            {
                return false;
            }

            if (!IsDirectDrawBlendMode)
            {
                if (RenderController.Opacity < 1.0f
                    || !RenderController.HasModelScreenRect)
                {
                    return false;
                }

                rect = RenderController.ModelScreenRect;
            }
            else if (!HasOffscreenScreenRect)
            {
                return false;
            }

            return rect.width > 0
                   && rect.height > 0;
        }

        /// <summary>
        /// Returns whether any drawable that renders into this offscreen is being drawn this frame.
        /// </summary>
        /// <returns>False only when this offscreen provably receives nothing.</returns>
        /// <remarks>
        /// The cheap emptiness test. It only reads <see cref="SkipRendering"/> over the owner part's
        /// drawables and stops at the first one that is being drawn, so a populated offscreen usually
        /// costs a single iteration.
        ///
        /// Requires <see cref="SkipRendering"/> to be final for every drawable, so callers must run after
        /// the per-renderer skip pass. That also means renderer and mesh renderer activity is already
        /// folded into the flag and must not be re-tested here.
        /// </remarks>
        internal bool HasVisibleDescendantDrawable()
        {
            var childDrawables = GetOffscreenOwnerPart()?.AllChildDrawables;

            // Without the part data the question cannot be answered. Claim content so that nothing is
            // dropped on a guess.
            if (childDrawables == null)
            {
                return true;
            }

            for (var i = 0; i < childDrawables.Length; i++)
            {
                var drawableRenderer = RenderController.FindDrawableRenderer(childDrawables[i]);

                if (drawableRenderer
                    && !drawableRenderer.SkipRendering)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the part that owns this offscreen, or null when it cannot be resolved.
        /// </summary>
        /// <returns>The owner part.</returns>
        /// <remarks>
        /// <see cref="CubismOffscreen.OwnerIndex"/> is an unmanaged part index, which is not necessarily
        /// a position in <see cref="CubismModel.Parts"/>, so it has to be matched rather than indexed.
        /// </remarks>
        private CubismPart GetOffscreenOwnerPart()
        {
            var parts = RenderController?.Model?.Parts;

            if (parts == null
                || DrawObjectType != CubismModelTypes.DrawObjectType.Offscreen
                || !Offscreen)
            {
                return null;
            }

            var ownerIndex = Offscreen.OwnerIndex;

            for (var partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                if (parts[partIndex].UnmanagedIndex == ownerIndex)
                {
                    return parts[partIndex];
                }
            }

            return null;
        }

        /// <summary>
        /// Marks every drawable and offscreen below this offscreen's owner part as skipped.
        /// </summary>
        /// <remarks>
        /// Call whenever this offscreen itself is skipped: its descendants must not be drawn either,
        /// because without the offscreen they would land in the parent's frame buffer uncomposited.
        /// </remarks>
        internal void SkipDescendantRenderers()
        {
            var ownerPart = GetOffscreenOwnerPart();
            var renderers = RenderController?.Renderers;

            if (!ownerPart
                || renderers == null)
            {
                return;
            }

            // Skip rendering for all child drawables of the offscreen part.
            var childDrawables = ownerPart.AllChildDrawables;
            for (var drawablesIndex = 0; drawablesIndex < childDrawables?.Length; drawablesIndex++)
            {
                var targetRenderer = RenderController.FindDrawableRenderer(childDrawables[drawablesIndex]);

                if (targetRenderer)
                {
                    targetRenderer.SkipRendering = true;
                }
            }

            // Skip rendering for all child offscreens of the offscreen part.
            var childOffscreens = ownerPart.AllChildOffscreens;
            for (var offscreensIndex = 0; offscreensIndex < childOffscreens?.Length; offscreensIndex++)
            {
                var childOffscreen = childOffscreens[offscreensIndex];

                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var targetRenderer = renderers[rendererIndex];

                    if (targetRenderer.DrawObjectType != CubismModelTypes.DrawObjectType.Offscreen
                        || targetRenderer.Offscreen.UnmanagedIndex != childOffscreen.UnmanagedIndex)
                    {
                        continue;
                    }

                    targetRenderer.SkipRendering = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Copies the current drawable to the parent offscreen.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="previousOffscreen">Previous offscreen render texture.</param>
        /// <param name="previousOffscreenOwner">
        /// Renderer that owns <paramref name="previousOffscreen"/>, or <c>null</c> when the texture is
        /// not a pooled offscreen texture (the common rendering texture).
        /// </param>
        /// <param name="currentOffscreenRenderer">Current offscreen renderer.</param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        /// <returns>
        /// The texture that holds the composited result. This is <paramref name="previousOffscreen"/>
        /// unless the composite ping-ponged, in which case it is the newly acquired texture that has
        /// become <paramref name="previousOffscreenOwner"/>'s buffer. Callers must use the returned
        /// texture from then on.
        /// </returns>
        internal RenderTexture DrawOffscreen(CommandBuffer buffer, RenderTexture previousOffscreen, CubismRenderer previousOffscreenOwner, CubismRenderer currentOffscreenRenderer, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (previousOffscreen == null
                || currentOffscreenRenderer == null)
            {
                return previousOffscreen;
            }

            // Draw the mesh with the material.
            return currentOffscreenRenderer.DrawOffscreenMesh(buffer, previousOffscreen, previousOffscreenOwner, passData);
        }

        /// <summary>
        /// Draws the mesh for offscreen rendering.
        /// </summary>
        /// <param name="buffer">Command buffer to record draw commands.</param>
        /// <param name="previousOffscreen">Previous offscreen render texture.</param>
        /// <param name="previousOffscreenOwner">
        /// Renderer that owns <paramref name="previousOffscreen"/>, or <c>null</c> when the texture is
        /// not a pooled offscreen texture. Required for the ping-pong path, which replaces that
        /// renderer's buffer with the texture it composited into.
        /// </param>
        /// <param name="passData">Pass data containing render controllers and camera data.</param>
        /// <returns>The texture that holds the composited result. See <see cref="DrawOffscreen"/>.</returns>
        internal RenderTexture DrawOffscreenMesh(CommandBuffer buffer, RenderTexture previousOffscreen, CubismRenderer previousOffscreenOwner, CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            // Mask rendering. An offscreen whose mask could not be rendered this frame is not composited
            // rather than composited with the mask of an earlier frame.
            if (!TryDrawMasks(buffer, passData))
            {
                return previousOffscreen;
            }

            ApplyPropertyForOffscreen(previousOffscreen);

            // In the case of color blending before Cubism 5.2.
            if ((ColorBlendType == BlendTypes.ColorBlend.Normal
                && AlphaBlendType == BlendTypes.AlphaBlend.Over)
                || ColorBlendType == BlendTypes.ColorBlend.Add
                || ColorBlendType == BlendTypes.ColorBlend.Multiply)
            {
                buffer.SetRenderTarget(previousOffscreen, passData.CameraDepthTextureHandle);

                // The quad is full-screen but only changes pixels this offscreen actually covers: where
                // its texture is transparent the blend leaves the destination as it was, for all three
                // direct blend modes. Limiting the rasterization to that region is therefore free of
                // visible effect, and it is what SetOffscreen cleared.
                if (TryGetOffscreenScissorRect(out var compositeRect))
                {
                    buffer.EnableScissorRect(new Rect(compositeRect.x, compositeRect.y, compositeRect.width, compositeRect.height));
                    buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);
                    buffer.DisableScissorRect();
                }
                else
                {
                    // Draw the mesh with the material.
                    buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);
                }

                return previousOffscreen;
            }

            // The custom blend shaders sample the backdrop (previousOffscreen) while writing the
            // composite, so the result cannot be written into that same texture and has to land in a
            // second one. Where that second texture is chosen decides whether a full-screen copy is
            // needed afterwards.
            // Ping-pong: composite into a texture taken from the pool and hand that to the owner as its
            // new buffer, so the backdrop's role passes to the result without copying anything. Acquire
            // before the swap below releases the old buffer, otherwise the pool could hand back the very
            // texture being sampled. Needs an owner to swap on, so a destination the renderers do not own
            // (the common rendering texture) falls through to the copy path below. The pooled texture is
            // created and resized from the texture below, so a destination that does not resolve falls
            // through to that same copy path.
            var pingPongBaseTexture = passData.CommonRenderingTexture;

            var nextOffscreen = RenderController
                                && RenderController.EnableOffscreenPingPong
                                && previousOffscreenOwner
                                && pingPongBaseTexture
                ? CubismOffscreenRenderTextureManager.GetInstance().GetOffscreenRenderTexture(pingPongBaseTexture)
                : null;

            if (nextOffscreen)
            {
                buffer.SetRenderTarget(nextOffscreen, passData.CameraDepthTextureHandle);

                // Clear the render target. Mandatory: a pooled texture holds an older frame's content,
                // and every pixel the composite quad fails to cover (it is depth tested against the
                // scene) would otherwise show up as that stale content in the parent.
                // Kept unscissored because this texture takes over as the owner's buffer and so has to
                // be valid everywhere that buffer is read. Scissoring it would not pay for itself in
                // any case - a clear issued inside the pass costs a full surface either way.
                buffer.ClearRenderTarget(false, true, Color.clear);

                // The composite is worth limiting even though the clear is not: it is ordinary fragment
                // work, which a scissor rectangle does reduce, and outside its region the clear above
                // already left the correct result.
                var pingPongScissored = TryGetOffscreenScissorRect(out var pingPongRect);

                if (pingPongScissored)
                {
                    buffer.EnableScissorRect(new Rect(pingPongRect.x, pingPongRect.y, pingPongRect.width, pingPongRect.height));
                }

                // Draw the mesh with the material.
                buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);

                if (pingPongScissored)
                {
                    buffer.DisableScissorRect();
                }

                // Swap: the setter returns the old buffer to the pool. Safe even though the draw above
                // samples it, because commands execute in the order they were recorded, so the sample
                // happens before anything that acquires the texture next can touch it.
                previousOffscreenOwner.OffscreenFrameBuffer = nextOffscreen;

                return nextOffscreen;
            }

            var temporaryTexture = (RenderTexture)passData.CommonTemporaryTextureHandle;

            // The composite has to be written into a second texture while the backdrop is sampled, and the
            // temporary texture is the only candidate left on this path. Leave the destination untouched
            // when it does not resolve.
            if (!temporaryTexture)
            {
                return previousOffscreen;
            }

            // Set temporary render target.
            buffer.SetRenderTarget(temporaryTexture, passData.CameraDepthTextureHandle);

            // Clear the render target, unscissored: a clear issued inside the pass costs a full surface
            // of fragment work whether or not a scissor rectangle is set, so limiting it buys nothing
            // and a full clear is the simpler thing to reason about.
            buffer.ClearRenderTarget(false, true, Color.clear);

            // The composite and the copy back are limited together. The copy is the one step a scissor
            // rectangle cannot limit: CommandBuffer.Blit binds the destination itself, and a
            // render-target change drops the scissor, so the blit would write the temporary texture's
            // pixels from outside the region over pixels the destination still needs (an ancestor
            // offscreen's own region, or the models already composited into the common rendering
            // texture). CopyTexture takes the region explicitly instead, so it cannot silently widen.
            // It is a transfer-path copy, which is why the per-drawable backdrop copy above avoids it,
            // but here it runs once per composite and replaces a whole-buffer blit.
            //
            // Without a usable region copy both fall back to whole-buffer work, rather than leaving a
            // limited composite with an unlimited copy.
            var scissored = TryGetOffscreenScissorRect(out var tempRect)
                            && CanCopyRegion(temporaryTexture, previousOffscreen);

            if (scissored)
            {
                buffer.EnableScissorRect(new Rect(tempRect.x, tempRect.y, tempRect.width, tempRect.height));
            }

            // Draw the mesh with the material.
            buffer.DrawMesh(Mesh, Matrix4x4.identity, DrawMaterial ?? Material, 0, 0, PropertyBlock);

            if (scissored)
            {
                buffer.DisableScissorRect();

                // Copy back exactly the region the clear and the composite covered. Region coordinates
                // are bottom-left origin pixels for both textures, the same space the scissor used, and
                // the copy is storage to storage, so it needs no orientation handling of its own.
                buffer.CopyTexture(
                    temporaryTexture, 0, 0, tempRect.x, tempRect.y, tempRect.width, tempRect.height,
                    previousOffscreen, 0, 0, tempRect.x, tempRect.y);
            }
            else
            {
                // Blit to previous offscreen.
                buffer.Blit(temporaryTexture, previousOffscreen);
            }

            return previousOffscreen;
        }

        /// <summary>
        /// Returns whether a region of <paramref name="source"/> can be copied into
        /// <paramref name="destination"/> with <see cref="CommandBuffer.CopyTexture(RenderTargetIdentifier, int, int, int, int, int, int, RenderTargetIdentifier, int, int, int, int)"/>.
        /// </summary>
        /// <param name="source">Texture to copy from.</param>
        /// <param name="destination">Texture to copy into.</param>
        /// <returns>True when the region copy is valid for this pair of textures on this platform.</returns>
        /// <remarks>
        /// CopyTexture needs matching formats and dimensions, rejects multisampled textures, and needs
        /// platform support at all. Both textures come from the common rendering texture's descriptor
        /// here, so the checks normally pass, but a copy that would fail has to be caught before the
        /// caller commits to a region-limited composite - a failed copy leaves the destination with the
        /// composite missing entirely.
        /// </remarks>
        private static bool CanCopyRegion(RenderTexture source, RenderTexture destination)
        {
            return source
                   && destination
                   && source.format == destination.format
                   && source.antiAliasing == 1
                   && destination.antiAliasing == 1
                   && source.width == destination.width
                   && source.height == destination.height
                   && (SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) != CopyTextureSupport.None;
        }

        /// <summary>
        /// Applies properties for offscreen rendering.
        /// </summary>
        /// <param name="previousOffscreen">Previous offscreen render texture.</param>
        private void ApplyPropertyForOffscreen(RenderTexture previousOffscreen)
        {
            var property = FetchPropertyBlock();

            // Write property.
            property.SetTexture(CubismShaderVariables.MainTexture, OffscreenFrameBuffer);
            property.SetTexture(CubismShaderVariables.RenderTexture, previousOffscreen);
            property.SetColor(CubismShaderVariables.MultiplyColor, GetDisplayedOffscreenMultiplyColor());
            property.SetColor(CubismShaderVariables.ScreenColor, GetDisplayedOffscreenScreenColor());
            property.SetFloat(CubismShaderVariables.OffscreenOpacity, DisplayedOffscreenOpacity);

            var reversedZ = SystemInfo.usesReversedZBuffer ? CubismRenderPassFeature.GEqual : CubismRenderPassFeature.LEqual;
            property.SetInt(CubismShaderVariables.ReversedZ, reversedZ);

            CommitPropertyBlock();
        }

        /// <summary>
        /// Applies mask texture for rendering.
        /// </summary>
        /// <param name="maskTexture">Mask render texture to sample.</param>
        private void ApplyMask(RenderTexture maskTexture)
        {
            // Write the mask properties straight into the persistent property block without a GetPropertyBlock
            // read-back (nothing external writes this block, so the read-back only re-read our own values).
            var property = PropertyBlock;

            // Write property.
            property.SetTexture(CubismShaderVariables.MaskTexture, maskTexture);
            if (DrawObjectType == CubismModelTypes.DrawObjectType.Drawable && ClippingContext != null)
            {
                // Sample the mask using the assigned channel.
                PropertyBlock.SetVector(CubismShaderVariables.MaskUvTransform, MatrixToMaskUvTransform(ClippingContext.MatrixForDraw));
                PropertyBlock.SetVector(CubismShaderVariables.ChannelFlag, GetMaskChannelFlag(ClippingContext));
            }

            // Commit the mask properties to the MeshRenderer. DrawMasks is called both from DrawDrawable (which
            // follows with its own CommitPropertyBlock — this commit is then redundant but harmless) and from
            // DrawOffscreenMesh, whose next step (ApplyPropertyForOffscreen) fetches the block back with
            // GetPropertyBlock: without this commit that fetch would drop the mask properties written above.
            CommitPropertyBlock();
        }

        /// <summary>
        /// Initializes masks if possible.
        /// </summary>
        /// <remarks>
        /// Always invalidates the per-controller mask cache because the mask
        /// reference list may have changed, which would make any prior snapshot
        /// stale.
        /// </remarks>
        private void TryInitializeMasks()
        {
            if (!RenderController.HasMask)
            {
                return;
            }

            CubismDrawable[] maskDrawables = null;
            switch (DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                    _masks = new CubismRenderer[Drawable.Masks.Length];
                    maskDrawables = Drawable.Masks;
                    break;
                case CubismModelTypes.DrawObjectType.Offscreen:
                    _masks = new CubismRenderer[Offscreen.Masks.Length];
                    maskDrawables = Offscreen.Masks;
                    break;
                default:
                    Debug.LogWarning($"{name} has unknown DrawObjectType.");
                    return;
            }

            for (var maskIndex = 0; maskIndex < maskDrawables.Length; maskIndex++)
            {
                for (var drawableRendererIndex = 0; drawableRendererIndex < RenderController.DrawableRenderers.Length; drawableRendererIndex++)
                {
                    if (RenderController.DrawableRenderers[drawableRendererIndex].Drawable.UnmanagedIndex != maskDrawables[maskIndex].UnmanagedIndex)
                    {
                        continue;
                    }

                    _masks[maskIndex] = RenderController.DrawableRenderers[drawableRendererIndex];
                    break;
                }
            }
        }

        /// <summary>
        /// Initializes the draw object based on its type.
        /// </summary>
        private void InitializeDrawObject()
        {
            // The draw object kind / drawable may have changed, and the mesh may be recreated with a different
            // topology right after this. Recompute batch eligibility, mesh counts and layout on next use.
            InvalidateBatchableCache();

            switch (DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Drawable:
                {
                    Drawable = GetComponent<CubismDrawable>();
                    DrawObjectUnmanagedIndex = Drawable.UnmanagedIndex;
                    break;
                }
                case CubismModelTypes.DrawObjectType.Offscreen:
                {
                    Offscreen = GetComponent<CubismOffscreen>();
                    DrawObjectUnmanagedIndex = Offscreen.UnmanagedIndex;
                    break;
                }
                default:
                    DrawObjectUnmanagedIndex = -1;
                    break;
            }
        }

        /// <summary>
        /// Called after all <see cref="CubismRenderer"/> are initialized.
        /// </summary>
        public void OnAfterAllRendererInitialize()
        {
            TryInitializeMasks();
            ApplySorting();
        }

        #region Batched Drawing

        /// <summary>
        /// Exposes this renderer's <see cref="MaterialPropertyBlock"/> to the batcher.
        /// </summary>
        internal MaterialPropertyBlock BatchPropertyBlock
        {
            get
            {
                return PropertyBlock;
            }
        }

        /// <summary>
        /// Whether <see cref="_batchMeshLayout"/> holds a computed value.
        /// </summary>
        [NonSerialized]
        private bool _hasBatchMeshLayout;

        /// <summary>
        /// Cached vertex layout for the batch combine job, validated per frame against the live stride.
        /// </summary>
        [NonSerialized]
        private BatchMeshLayout _batchMeshLayout;

        /// <summary>
        /// Returns the interleaved vertex/index layout of <paramref name="mesh"/> for the combine job,
        /// caching the attribute offsets to avoid re-querying the Mesh API for every batched drawable each frame.
        /// </summary>
        /// <remarks>
        /// The layout must describe the exact mesh the combine job reads, so it is computed from
        /// <paramref name="mesh"/> (this renderer's current front buffer) rather than from a fixed buffer:
        /// the two double-buffered meshes can hold different vertex layouts transiently (right after
        /// creation or a domain reload, before both have received a vertex upload), and applying one
        /// buffer's stride/offsets to the other scrambles the read.
        ///
        /// The cache is validated every frame against the live vertex stride. Stride is unique to the
        /// attribute set for these meshes, so an unchanged stride means the offsets are unchanged too and
        /// the cached layout can be reused for either double-buffer. A changed stride forces a recompute.
        /// The index format is always read fresh because it is tied to the exact buffer captured for combining.
        /// </remarks>
        /// <param name="mesh">The mesh being combined (the renderer's current front buffer).</param>
        /// <returns>The mesh's vertex/index buffer layout.</returns>
        internal BatchMeshLayout GetBatchMeshLayout(Mesh mesh)
        {
            var stride = mesh.GetVertexBufferStride(0);
            var index16 = mesh.indexFormat == IndexFormat.UInt16;

            if (_hasBatchMeshLayout && _batchMeshLayout.Stride == stride)
            {
                _batchMeshLayout.Index16 = index16;
                return _batchMeshLayout;
            }

            // Cubism drawable meshes use a single interleaved vertex stream (position + color + uv), so all
            // attribute offsets share stream 0's stride.
            var layout = new BatchMeshLayout
            {
                Stride = stride,
                PositionOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Position),
                HasUv = mesh.HasVertexAttribute(VertexAttribute.TexCoord0),
                HasColor = mesh.HasVertexAttribute(VertexAttribute.Color),
                Index16 = index16,
            };

            if (layout.HasUv)
            {
                // TexCoord0 is the only UV channel used by CubismRenderer.
                layout.UvOffset = mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord0);
            }

            if (layout.HasColor)
            {
                // Color is the only color channel used by CubismRenderer.
                layout.ColorOffset = mesh.GetVertexAttributeOffset(VertexAttribute.Color);
            }

            _batchMeshLayout = layout;
            _hasBatchMeshLayout = true;

            return _batchMeshLayout;
        }

        /// <summary>
        /// Whether <see cref="_batchMeshVertexCount"/> / <see cref="_batchMeshIndexCount"/> hold a computed value.
        /// </summary>
        [NonSerialized]
        private bool _hasBatchMeshCounts;

        /// <summary>
        /// Cached vertex count of the batched mesh.
        /// </summary>
        [NonSerialized]
        private int _batchMeshVertexCount;

        /// <summary>
        /// Cached triangle index count (submesh 0) of the batched mesh.
        /// </summary>
        [NonSerialized]
        private int _batchMeshIndexCount;

        /// <summary>
        /// Returns the vertex and index counts of <paramref name="mesh"/> for the combine job, caching them
        /// after the first successful read so the Mesh API is not queried for every batched drawable each frame.
        /// </summary>
        /// <remarks>
        /// Live2D drawable meshes have fixed topology at runtime (only vertex positions and colors animate,
        /// never the vertex/index counts), and the two double-buffered meshes are built from the same drawable,
        /// so the counts stay constant until the renderer is re-initialized and are identical for either buffer.
        /// Re-initialization can recreate the meshes with a different topology (for example when the model's moc
        /// is replaced), so <see cref="InvalidateBatchableCache"/> drops this cache there. A count of zero (a mesh
        /// whose vertices have not been uploaded yet) is never cached, so the cache only ever holds the true
        /// topology counts. The `_has` flag defaults to false, so a domain reload recomputes lazily.
        /// </remarks>
        /// <param name="mesh">The mesh being combined (the renderer's current front buffer).</param>
        /// <param name="vertexCount">The mesh's vertex count.</param>
        /// <param name="indexCount">The mesh's triangle index count (submesh 0).</param>
        /// <returns>True when both counts are non-zero and the mesh can be combined.</returns>
        internal bool TryGetBatchMeshCounts(Mesh mesh, out int vertexCount, out int indexCount)
        {
            if (_hasBatchMeshCounts)
            {
                vertexCount = _batchMeshVertexCount;
                indexCount = _batchMeshIndexCount;
                return true;
            }

            vertexCount = mesh.vertexCount;
            if (vertexCount == 0)
            {
                indexCount = 0;
                return false;
            }

            indexCount = (int)mesh.GetIndexCount(0);
            if (indexCount == 0)
            {
                return false;
            }

            _batchMeshVertexCount = vertexCount;
            _batchMeshIndexCount = indexCount;
            _hasBatchMeshCounts = true;

            return true;
        }

        /// <summary>
        /// Returns true when this renderer is eligible for batching.
        /// </summary>
        /// <remarks>
        /// Excludes offscreens, masked drawables, and drawables that require the blit-to-temp blend branch.
        /// When the model is currently rendering into an offscreen context, drawables that are not
        /// descendants of the current offscreen owner are also excluded — those drawables trigger
        /// <see cref="SubmitDrawToParentOffscreen"/> which mutates render-target state and must be
        /// handled by the per-drawable path.
        /// </remarks>
        /// <returns>True if the renderer can be merged into a batched DrawMesh.</returns>
        internal bool IsBatchable()
        {
            if (!RenderController)
            {
                return false;
            }
            // Model-lifetime-constant part (draw object kind + mask state), cached to avoid the per-frame
            // unmanaged mask-count read and drawable alive check.
            if (!GetIsDrawableBatchable())
            {
                return false;
            }
            // Only the direct-draw branch of DrawDrawable is batchable — the Blit branch is not.
            // Blend mode is a cheap enum comparison and can be changed at runtime, so it stays uncached.
            if (!IsDirectDrawBlendMode)
            {
                return false;
            }
            // Drawables that would trigger an offscreen pop inside DrawDrawable cannot be batched.
            // Pop has side effects on CurrentFrameBuffer and CurrentOffscreenUnmanagedIndex that
            // the per-drawable code path performs while drawing. This depends on the current offscreen
            // draw state, so it is evaluated every frame.
            if (WouldPopOffscreenStack())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// <see cref="GetIsDrawableBatchable"/>'s cached value.
        /// </summary>
        [NonSerialized]
        private bool _isDrawableBatchable;

        /// <summary>
        /// Whether <see cref="_isDrawableBatchable"/> has been computed for the current draw object.
        /// </summary>
        [NonSerialized]
        private bool _hasDrawableBatchable;

        /// <summary>
        /// Returns the model-lifetime-constant portion of batch eligibility: this is a drawable (not an
        /// offscreen) whose mesh is not masked. Cached because it depends only on the draw object kind and
        /// the Core mask count, which do not change while the model is alive.
        /// </summary>
        /// <returns>True when the draw object is an unmasked drawable.</returns>
        private bool GetIsDrawableBatchable()
        {
            if (_hasDrawableBatchable)
            {
                return _isDrawableBatchable;
            }

            _isDrawableBatchable = DrawObjectType == CubismModelTypes.DrawObjectType.Drawable
                                   && Drawable
                                   && !Drawable.IsMasked;
            _hasDrawableBatchable = true;

            return _isDrawableBatchable;
        }

        /// <summary>
        /// Drops the cached batch eligibility (<see cref="GetIsDrawableBatchable"/>), mesh counts
        /// (<see cref="TryGetBatchMeshCounts"/>) and mesh layout (<see cref="GetBatchMeshLayout"/>) so they are
        /// recomputed. Call whenever the draw object, its mask state or its mesh may have changed (for example on
        /// re-initialization, which can recreate the mesh with a different topology).
        /// </summary>
        internal void InvalidateBatchableCache()
        {
            _hasDrawableBatchable = false;
            _hasBatchMeshCounts = false;
            _hasBatchMeshLayout = false;
        }

        /// <summary>
        /// Predicts whether <see cref="SubmitDrawToParentOffscreen"/> would pop the offscreen stack
        /// if this drawable were drawn now. Returns false when the model is not currently inside an
        /// offscreen context, or when the drawable's part chain reaches the current offscreen's owner.
        /// </summary>
        /// <remarks>
        /// Mirrors the descendant test inside <see cref="SubmitDrawToParentOffscreen"/>: walk the
        /// drawable's parent-part chain looking for the current offscreen owner part. If found we
        /// are a descendant and no pop happens. Otherwise the per-drawable path will pop the stack
        /// and we must not batch.
        /// </remarks>
        private bool WouldPopOffscreenStack()
        {
            var currentOffscreenIndex = RenderController.CurrentOffscreenUnmanagedIndex;
            if (currentOffscreenIndex == -1)
            {
                return false;
            }

            var offscreens = RenderController.OffscreenRenderers;
            if (offscreens == null)
            {
                return false;
            }

            // Locate the current offscreen renderer's owner-part index.
            var currentOwnerIndex = -1;
            for (var i = 0; i < offscreens.Length; i++)
            {
                if (offscreens[i].Offscreen.UnmanagedIndex != currentOffscreenIndex)
                {
                    continue;
                }
                currentOwnerIndex = offscreens[i].Offscreen.OwnerIndex;
                break;
            }

            if (currentOwnerIndex == -1)
            {
                return false;
            }

            var parts = RenderController.Model.Parts;
            var currentOwnerUnmanagedIndex = parts[currentOwnerIndex].UnmanagedIndex;

            var parentIndex = Drawable.ParentPartIndex;
            while (parentIndex != -1)
            {
                if (parentIndex == currentOwnerUnmanagedIndex)
                {
                    return false;
                }
                parentIndex = parts[parentIndex].UnmanagedParentIndex;
            }
            return true;
        }

        /// <summary>
        /// Whether the current blend-mode combination follows the "SetRenderTarget + DrawMesh" branch.
        /// </summary>
        internal bool IsDirectDrawBlendMode
        {
            get
            {
                return (ColorBlendType == BlendTypes.ColorBlend.Normal && AlphaBlendType == BlendTypes.AlphaBlend.Over)
                    || ColorBlendType == BlendTypes.ColorBlend.Add
                    || ColorBlendType == BlendTypes.ColorBlend.Multiply;
            }
        }

        /// <summary>
        /// Populates this renderer's <see cref="MaterialPropertyBlock"/> for use as the shared property
        /// block of a batched DrawMesh call. All per-drawable non-transform properties are applied, then
        /// the transform uniforms are overwritten with identity values because the transform is baked
        /// into the combined mesh's vertex positions.
        /// </summary>
        /// <param name="passData">Render pass data.</param>
        /// <param name="identityOffsetScale">Offset-scale value corresponding to an identity transform.</param>
        /// <param name="identityRotationQuaternion">Quaternion value corresponding to an identity rotation.</param>
        /// <param name="identityZOffset">Z offset value corresponding to an identity transform.</param>
        internal void SetupBatchPropertyBlock(
            CubismRenderPassFeature.CubismRenderPass.PassData passData,
            Vector4 identityOffsetScale,
            Vector4 identityRotationQuaternion,
            float identityZOffset)
        {
            // Get the property block.
            var property = FetchPropertyBlock();

            // Write all per-drawable properties that would normally be applied in DrawDrawable().
            WriteMainTexture(property);
            WriteBlendedRenderTexture(property, passData);
            WriteDisplayedScreenColor(property);
            WriteDisplayedMultiplyColor(property);

            // Overwrite the transform uniforms with identity values.
            property.SetVector(CubismShaderVariables.OffsetScale, identityOffsetScale);
            property.SetVector(CubismShaderVariables.RotationQuaternion, identityRotationQuaternion);
            property.SetFloat(CubismShaderVariables.ZOffset, identityZOffset);

            // Set the property block.
            CommitPropertyBlock();
        }

        /// <summary>
        /// Composes this drawable's world transform — the transform the shader would otherwise apply
        /// per-drawable — so <see cref="CubismBatchCombineJob"/> can bake it into the vertices on a worker
        /// thread. Transform component reads must stay on the main thread, so they are gathered here.
        /// </summary>
        /// <returns>The baked transform for this drawable.</returns>
        internal BatchTransform GetBatchTransform()
        {
            // Combine the once-per-pass root snapshot with this drawable's own local transform, so the
            // model root's hierarchy walk (lossyScale) is not repeated for every batched drawable.
            var root = RenderController.RootTransform;
            GetLocalTransform(out var localPosition, out var localScale, out var localRotation);

            return new BatchTransform
            {
                ScaleX = root.LossyScale.x * localScale.x,
                ScaleY = root.LossyScale.y * localScale.y,
                Rotation = root.Rotation * localRotation,
                OffsetX = root.Position.x + localPosition.x,
                OffsetY = root.Position.y + localPosition.y,
                OffsetZ = root.Position.z + localPosition.z,
            };
        }

        #endregion
    }
}
