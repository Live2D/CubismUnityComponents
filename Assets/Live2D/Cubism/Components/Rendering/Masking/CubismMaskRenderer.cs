/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Renders out Cubism masks.
    /// </summary>
    public sealed class CubismMaskRenderer
    {
        /// <summary>
        /// Shared buffer for <see cref="CubismMaskProperties"/>s.
        /// </summary>
        private static CubismMaskProperties SharedMaskProperties { get; set; }
        
        /// <summary>
        /// Material for drawing masks.
        /// </summary>
        private static Material SharedMaskMaterial { get; set; }


        /// <summary>
        /// Shader property for mask textures. 
        /// </summary>
        private static int MainTexturePropertyId { get; set; }

        /// <summary>
        /// Shader property for <see cref="CubismMaskTile"/>s. 
        /// </summary>
        private static int CubismMaskTilePropertyId { get; set; }

        /// <summary>
        /// Shader property for <see cref="CubismMaskTransform"/>s. 
        /// </summary>
        private static int CubismMaskTransformPropertyId { get; set; }


        /// <summary>
        /// Masks.
        /// </summary>
        private CubismRenderer[] Masks { get; set; }

        /// <summary>
        /// Masked drawables .
        /// </summary>
        private CubismRenderer[] Masked { get; set; }


        /// <summary>
        /// Texture to draw onto.
        /// </summary>
        private CubismMaskTexture MaskTexture { get; set; }

        /// <summary>
        /// MaskTile to draw on.
        /// </summary>
        private CubismMaskTile MaskTile { get; set; }

        /// <summary>
        /// Transform info for drawing masks.
        /// </summary>
        private CubismMaskTransform MaskTransform { get; set; }

        #region Ctors

        /// <summary>
        /// Initializes instance.
        /// </summary>
        /// <param name="masks">Masks.</param>
        /// <param name="masked">Masked drawables. </param>
        public CubismMaskRenderer(CubismRenderer[] masks, CubismRenderer[] masked)
        {
            // Initialize fields.
            Masks = masks;
            Masked = masked;


            // Initialize statics. (On each construction)...
            SharedMaskProperties = new CubismMaskProperties();

            SharedMaskMaterial = CubismBuiltinMaterials.Mask;

            MainTexturePropertyId = Shader.PropertyToID("_MainTex");
            CubismMaskTilePropertyId = Shader.PropertyToID("cubism_MaskTile");
            CubismMaskTransformPropertyId = Shader.PropertyToID("cubism_MaskTransform");
        }

        #endregion

        /// <summary>
        /// Set <see cref="CubismMaskTexture"/>. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CubismMaskRenderer SetMaskTexture(CubismMaskTexture value)
        {
            MaskTexture = value;


            ResetMaskTransform();


            return this;
        }

        /// <summary>
        /// Set <see cref="CubismMaskTile"/>. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public CubismMaskRenderer SetMaskTile(CubismMaskTile value)
        {
            MaskTile = value;


            ResetMaskTransform();


            return this;
        }


        /// <summary>
        /// Draw masks on <see cref="CubismMaskTexture"/>. 
        /// </summary>
        public void DrawMasksNow()
        {
            // Recompute mask transform (updating masked renderers as necessary).
            RecalculateMaskTransform();


            // Initialize material.
            var material = SharedMaskMaterial;
            var activeTexture = Masks[0].MainTexture;


            material.SetTexture(MainTexturePropertyId, activeTexture);
            material.SetVector(CubismMaskTilePropertyId, MaskTile);
            material.SetVector(CubismMaskTransformPropertyId, MaskTransform);


            // Activate material.
            material.SetPass(0);


            // Draw masks.
            for (var i = 0; i < Masks.Length; i++)
            {
                // Switch textue if necessary.
                if (Masks[i].MainTexture != activeTexture)
                {
                    activeTexture = Masks[i].MainTexture;


                    material.SetTexture(MainTexturePropertyId, activeTexture);
                }

                // Draw mesh.
                Graphics.DrawMeshNow(Masks[i].Mesh, Matrix4x4.identity);
            }
        }


        /// <summary>
        /// Resets <see cref="MaskTransform"/>.
        /// </summary>
        private void ResetMaskTransform()
        {
            MaskTransform = new CubismMaskTransform
            {
                Offset = Vector2.zero,
                Scale = 0f
            };
        }

        /// <summary>
        /// Updates <see cref="MaskTransform"/> and <see cref="Masked"/>s.
        /// </summary>
        private void RecalculateMaskTransform()
        {
            // Compute bounds and scale.
            var bounds = Masks.GetBounds();
            var scale = (bounds.size.x > bounds.size.y)
                ? bounds.size.x
                : bounds.size.y;


            // Compute mask transform.
            MaskTransform = new CubismMaskTransform
            {
                Offset = bounds.center,
                Scale = 1f / scale
            };


            // Apply mask properties to masked.
            var maskProperties = SharedMaskProperties;


            maskProperties.Texture = MaskTexture;
            maskProperties.Tile = MaskTile;
            maskProperties.Transform = MaskTransform;


            for (var i = 0; i < Masked.Length; ++i)
            {
                Masked[i].OnMaskPropertiesDidChange(maskProperties);
            }
        }
    }
}
