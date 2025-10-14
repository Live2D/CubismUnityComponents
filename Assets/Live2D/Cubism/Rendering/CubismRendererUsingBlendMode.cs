/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering.Util;
using UnityEngine;
using UnityEngine.Rendering;


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
        private static Vector3[] OffscreenVertices = new Vector3[]
        {
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0)
        };

        /// <summary>
        /// UVs for offscreen rendering.
        /// </summary>
        private static Vector2[] OffscreenUVs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        /// <summary>
        /// Triangle indices for offscreen rendering.
        /// </summary>
        private static int[] OffscreenTriangle = new int[] { 0, 1, 2, 0, 2, 3 };

        /// <summary>
        /// Masks used by this renderer.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismRenderer[] _masks;

        /// <summary>
        /// <see cref="UnityEngine.MeshRenderer"/>.
        /// </summary>
        public MeshRenderer ModelCanvasRenderer
        {
            get
            {
                return RenderController.ModelCanvasRenderer;
            }
        }

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

        private RenderTexture _offscreenFrameBuffer;

        /// <summary>
        /// Offscreen render texture used for rendering.
        /// </summary>
        public RenderTexture OffscreenFrameBuffer
        {
            get
            {
                if (_offscreenFrameBuffer == null)
                {
                    _offscreenFrameBuffer = CubismOffscreenRenderTextureManager.GetInstance().GetOffscreenRenderTexture(
                        RenderController.CurrentFrameBuffer.width,
                        RenderController.CurrentFrameBuffer.height);
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
        /// Offscreen mesh used for rendering.
        /// </summary>
        private Mesh _offscreenMesh;

        /// <summary>
        /// Previous offscreen unmanaged index.
        /// </summary>
        private int _previousOffscreenUnmanagedIndex;

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
        /// Apply <see cref="CubismCommonRenderFrameBuffer.CommonFrameBuffer"/> to shader.
        /// </summary>
        private void ApplyCommonFrameBuffer()
        {
            var property = MeshFilter ? SharedPropertyBlock : PropertyBlock;

            MeshRenderer.GetPropertyBlock(property);

            // Write property.
            property.SetTexture("_RenderTexture", CubismCommonRenderFrameBuffer.GetInstance().CommonFrameBuffer);

            MeshRenderer.SetPropertyBlock(property);
        }

        /// <summary>
        /// Adds this renderer to the command buffer for rendering.
        /// </summary>
        public void DrawObject(CommandBuffer buffer, RenderTexture frameBuffer)
        {
            if (MeshRenderer == null)
            {
                return;
            }

            switch (DrawObjectType)
            {
                case CubismModelTypes.DrawObjectType.Offscreen:
                    // Set offscreen frame buffer.
                    SetOffscreen(buffer, frameBuffer);
                    break;
                case CubismModelTypes.DrawObjectType.Drawable:
                    // Draw a drawable.
                    DrawDrawable(buffer, frameBuffer);
                    break;
            }
        }

        /// <summary>
        /// Sets the offscreen frame buffer for rendering.
        /// </summary>
        /// <param name="frameBuffer">Rendering frame buffer</param>
        private void SetOffscreen(CommandBuffer buffer, RenderTexture frameBuffer)
        {
            var currentOffscreenUnmanagedIndex = RenderController.CurrentOffscreenUnmanagedIndex;
            SubmitDrawToParentOffscreen(ref frameBuffer, ref currentOffscreenUnmanagedIndex,
                buffer, this, RenderController.RootFrameBuffer);
            RenderController.CurrentOffscreenUnmanagedIndex = currentOffscreenUnmanagedIndex;

            // Ready the render target to the offscreen frame buffer.
            OffscreenFrameBuffer = CubismOffscreenRenderTextureManager.GetInstance().GetOffscreenRenderTexture(frameBuffer.width, frameBuffer.height);
            RenderController.CurrentFrameBuffer = OffscreenFrameBuffer;
            RenderController.CurrentOffscreenUnmanagedIndex = Offscreen.UnmanagedIndex;
        }

        /// <summary>
        /// Draws the mask.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="frameBuffer"></param>
        internal void DrawMasks(CommandBuffer buffer, RenderTexture frameBuffer)
        {
            if (RenderController.MaskController != null
                && _masks?.Length > 0
                && RenderController.MaskController.MaskTexture?.HighPrecisionRenderTexture != null)
            {
                var maskTexture = RenderController.MaskController.MaskTexture.HighPrecisionRenderTexture;

                // Check if the mask texture size matches the frame buffer size.
                if (maskTexture.width != frameBuffer.width
                    || maskTexture.height != frameBuffer.height)
                {
                    maskTexture.Release();
                    maskTexture.width = frameBuffer.width;
                    maskTexture.height = frameBuffer.height;
                    maskTexture.Create();
                }

                buffer.SetRenderTarget(maskTexture);
                buffer.ClearRenderTarget(true, true, Color.clear);

                // Set VP matrix
                var cavWidth = RenderController.Model.CanvasInformation.CanvasWidth;
                var cavHeight = RenderController.Model.CanvasInformation.CanvasHeight;
                var projHalfWidth = cavWidth / RenderController.Model.CanvasInformation.PixelsPerUnit * 0.5f;
                var projHalfHeight = cavHeight / RenderController.Model.CanvasInformation.PixelsPerUnit * 0.5f;
                var projMatrix = Matrix4x4.Ortho(-projHalfWidth, projHalfWidth, -projHalfHeight, projHalfHeight, -1.0f, 1.0f);
                var viewMatrix = Matrix4x4.identity;
                buffer.SetProjectionMatrix(projMatrix);
                buffer.SetViewMatrix(viewMatrix);

                // Draw the masks.
                for (var maskIndex = 0; maskIndex < _masks.Length; maskIndex++)
                {
                    buffer.DrawMesh(_masks[maskIndex].Mesh, Matrix4x4.identity, CubismBuiltinMaterials.BlendMask, 0, 0, _masks[maskIndex].PropertyBlock);
                }

                ApplyMaskTexture();
            }
        }

        /// <summary>
        /// Submits the offscreen frame buffer and draws to the parent offscreen if necessary.
        /// </summary>
        /// <param name="frameBuffer">Current RenderTexture</param>
        /// <param name="currentOffscreenUnmanagedIndex">Current Offscreen's UnmanagedIndex</param>
        /// <param name="buffer">Command buffer</param>
        /// <param name="targetRenderer">Current target rendering object</param>
        /// <param name="rootFrameBuffer">Current root RenderTexture</param>
        private void SubmitDrawToParentOffscreen(ref RenderTexture frameBuffer, ref int currentOffscreenUnmanagedIndex,
            CommandBuffer buffer, CubismRenderer targetRenderer, RenderTexture rootFrameBuffer)
        {
            if (frameBuffer == null
                || frameBuffer == RenderController.ModelFrameBuffer
                || currentOffscreenUnmanagedIndex == -1)
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

            var parentIndex = -1;
            var targetParentIndex = -1;
            RenderTexture previousOffscreen = null;
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
            parentIndex = RenderController.Model.Parts[currentOwnerIndex].UnmanagedParentIndex;

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

                        previousOffscreen = RenderController.OffscreenRenderers[offscreenRendererIndex].OffscreenFrameBuffer;
                        _previousOffscreenUnmanagedIndex = part.OffscreenIndex;
                        break;
                    }
                }

                parentIndex = RenderController.Model.Parts[parentIndex].UnmanagedParentIndex;

                if (previousOffscreen == null)
                {
                    continue;
                }

                break;
            }

            if (previousOffscreen == null)
            {
                previousOffscreen = rootFrameBuffer;
            }

            if (previousOffscreen == offscreenRenderer?.OffscreenFrameBuffer)
            {
                return;
            }

            // If It can copy the parent offscreen, copy it to the current offscreen renderer.
            DrawOffscreen(buffer, previousOffscreen, offscreenRenderer);

            offscreenRenderer.OffscreenFrameBuffer = null;
            frameBuffer = previousOffscreen;
            RenderController.CurrentFrameBuffer = previousOffscreen;
            currentOffscreenUnmanagedIndex = _previousOffscreenUnmanagedIndex;

            // If the current offscreen is the parent of the target renderer, draw to the parent offscreen.
            SubmitDrawToParentOffscreen(ref frameBuffer, ref currentOffscreenUnmanagedIndex,
                buffer, targetRenderer, rootFrameBuffer);
        }

        /// <summary>
        /// Renders a drawable.
        /// </summary>
        /// <param name="buffer">Command buffer</param>
        /// <param name="frameBuffer">Rendering frame buffer</param>
        private void DrawDrawable(CommandBuffer buffer, RenderTexture frameBuffer)
        {
            if (RenderController.CurrentOffscreenUnmanagedIndex != -1)
            {
                var currentOffscreenOwnerUnmanagedIndex = RenderController.CurrentOffscreenUnmanagedIndex;
                SubmitDrawToParentOffscreen(ref frameBuffer, ref currentOffscreenOwnerUnmanagedIndex,
                    buffer, this, RenderController.RootFrameBuffer);

                RenderController.CurrentOffscreenUnmanagedIndex = currentOffscreenOwnerUnmanagedIndex;
            }

            // Mask rendering.
            DrawMasks(buffer, frameBuffer);

            // Set property block.
            ApplyMainTexture();
            ApplyCommonFrameBuffer();
            ApplyScreenColor();
            ApplyMultiplyColor();
            ApplyVertexColors();

            // Set VP matrix
            var cavWidth = RenderController.Model.CanvasInformation.CanvasWidth;
            var cavHeight = RenderController.Model.CanvasInformation.CanvasHeight;
            var projHalfWidth = cavWidth / RenderController.Model.CanvasInformation.PixelsPerUnit * 0.5f;
            var projHalfHeight = cavHeight / RenderController.Model.CanvasInformation.PixelsPerUnit * 0.5f;
            var projMatrix = Matrix4x4.Ortho(-projHalfWidth, projHalfWidth, -projHalfHeight, projHalfHeight, -1.0f, 1.0f);
            var viewMatrix = Matrix4x4.identity;
            buffer.SetProjectionMatrix(projMatrix);
            buffer.SetViewMatrix(viewMatrix);

            // In the case of color blending before Cubism 5.2.
            if ((ColorBlendType == BlendTypes.ColorBlend.Normal
                && AlphaBlendType == BlendTypes.AlphaBlend.Over)
                || ColorBlendType == BlendTypes.ColorBlend.Add
                || ColorBlendType == BlendTypes.ColorBlend.Multiply)
            {
                buffer.SetRenderTarget(frameBuffer);

                // Draw the mesh with the material.
                buffer.DrawMesh(Mesh, Matrix4x4.identity, Material, 0, 0, PropertyBlock);

                return;
            }

            // Copy the current frame buffer to the common render frame buffer.
            buffer.Blit(frameBuffer, CubismCommonRenderFrameBuffer.GetInstance().CommonFrameBuffer, Vector2.one, Vector2.zero, 0, 0);

            buffer.SetRenderTarget(frameBuffer);

            // Draw the mesh with the material.
            buffer.DrawMesh(Mesh, Matrix4x4.identity, Material, 0, 0, PropertyBlock);
        }

        /// <summary>
        /// Copies the current drawable to the parent offscreen.
        /// </summary>
        public void DrawOffscreen(CommandBuffer buffer, RenderTexture previousOffscreen, CubismRenderer currentOffscreenRenderer)
        {
            if (previousOffscreen == null
                || currentOffscreenRenderer == null)
            {
                return;
            }

            // Draw the mesh with the material.
            currentOffscreenRenderer.DrawOffscreenMesh(buffer, previousOffscreen);
        }

        /// <summary>
        /// Draws the mesh for offscreen rendering.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="previousOffscreen"></param>
        internal void DrawOffscreenMesh(CommandBuffer buffer, RenderTexture previousOffscreen)
        {
            // Mask rendering.
            DrawMasks(buffer, OffscreenFrameBuffer);

            ApplyPropertyForOffscreen(previousOffscreen);

            // In the case of color blending before Cubism 5.2.
            if ((ColorBlendType == BlendTypes.ColorBlend.Normal
                && AlphaBlendType == BlendTypes.AlphaBlend.Over)
                || ColorBlendType == BlendTypes.ColorBlend.Add
                || ColorBlendType == BlendTypes.ColorBlend.Multiply)
            {
                buffer.SetRenderTarget(previousOffscreen);

                // Draw the mesh with the material.
                buffer.DrawMesh(Mesh, Matrix4x4.identity, Material, 0, 0, PropertyBlock);

                return;
            }

            buffer.SetRenderTarget(RenderController.OffscreenRenderingFrameBuffer);
            buffer.ClearRenderTarget(true, true, Color.clear);

            // Draw the mesh with the material.
            buffer.DrawMesh(Mesh, Matrix4x4.identity, Material, 0, 0, PropertyBlock);

            // Copy the current frame buffer to the previous offscreen frame buffer.
            buffer.Blit(RenderController.OffscreenRenderingFrameBuffer, previousOffscreen);
        }

        private void ApplyPropertyForOffscreen(RenderTexture previousOffscreen)
        {
            var property = MeshFilter ? SharedPropertyBlock : PropertyBlock;
            MeshRenderer.GetPropertyBlock(property);

            // Write property.
            property.SetTexture(CubismShaderVariables.MainTexture, OffscreenFrameBuffer);
            property.SetTexture("_RenderTexture", previousOffscreen);
            property.SetColor(CubismShaderVariables.MultiplyColor, MultiplyColor);
            property.SetColor(CubismShaderVariables.ScreenColor, ScreenColor);
            property.SetFloat("_OffscreenOpacity", Offscreen.Opacity);

            MeshRenderer.SetPropertyBlock(property);
        }

        /// <summary>
        /// Applies mask texture for rendering.
        /// </summary>
        private void ApplyMaskTexture()
        {
            if (RenderController.MaskController.MaskTexture?.HighPrecisionRenderTexture == null)
            {
                return;
            }

            MeshRenderer.GetPropertyBlock(PropertyBlock);

            // Write property.
            PropertyBlock.SetTexture(CubismShaderVariables.MaskTexture, RenderController.MaskController.MaskTexture.HighPrecisionRenderTexture);

            MeshRenderer.SetPropertyBlock(PropertyBlock);
        }

        /// <summary>
        /// Initialize screen frame buffer.
        /// </summary>
        private void TryInitializeFrameBuffer()
        {
            ApplyCommonFrameBuffer();
        }

        /// <summary>
        /// Initializes masks if possible.
        /// </summary>
        private void TryInitializeMasks()
        {
            if (RenderController.MaskController == null)
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

            for (var i = 0; i < maskDrawables.Length; i++)
            {
                for (var j = 0; j < RenderController.Renderers.Length; j++)
                {
                    if (RenderController.Renderers[j].Drawable.UnmanagedIndex != maskDrawables[i].UnmanagedIndex)
                    {
                        continue;
                    }

                    _masks[i] = RenderController.Renderers[j];
                    break;
                }
            }
        }

        /// <summary>
        /// Initializes the draw object based on its type.
        /// </summary>
        private void InitializeDrawObject()
        {
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
                    var frameBuffer = RenderController.CurrentFrameBuffer;

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
        }
    }
}
