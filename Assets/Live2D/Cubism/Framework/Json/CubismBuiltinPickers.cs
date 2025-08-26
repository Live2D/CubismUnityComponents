﻿/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering;
using UnityEngine;


namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Default pickers.
    /// </summary>
    public static class CubismBuiltinPickers
    {
        /// <summary>
        /// Builtin <see cref="Material"/> picker.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to map to.</param>
        /// <param name="isUsingBlendMode">Use blend mode flag.</param>
        /// <returns>Mapped texture.</returns>
        public static Material DrawableMaterialPicker(CubismModel3Json sender, CubismDrawable drawable, bool isUsingBlendMode)
        {
            if (isUsingBlendMode)
            {
                return CubismBuiltinMaterials.GetBlendModeMaterial("UnlitBlendMode", drawable.ColorBlend, drawable.AlphaBlend, drawable.IsMasked, drawable.IsInverted, drawable.IsDoubleSided);
            }

            if (drawable.IsDoubleSided)
            {
                if (drawable.BlendAdditive)
                {
                    return (drawable.IsMasked)
                        ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitAdditiveMaskedInverted :
                            CubismBuiltinMaterials.UnlitAdditiveMasked
                        : CubismBuiltinMaterials.UnlitAdditive;
                }


                if (drawable.MultiplyBlend)
                {
                    return (drawable.IsMasked)
                        ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitMultiplyMaskedInverted :
                            CubismBuiltinMaterials.UnlitMultiplyMasked
                        : CubismBuiltinMaterials.UnlitMultiply;
                }


                return (drawable.IsMasked)
                    ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitMaskedInverted :
                        CubismBuiltinMaterials.UnlitMasked
                    : CubismBuiltinMaterials.Unlit;
            }

            if (drawable.BlendAdditive)
            {
                return (drawable.IsMasked)
                    ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitAdditiveMaskedInvertedCulling :
                        CubismBuiltinMaterials.UnlitAdditiveMaskedCulling
                    : CubismBuiltinMaterials.UnlitAdditiveCulling;
            }


            if (drawable.MultiplyBlend)
            {
                return (drawable.IsMasked)
                    ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitMultiplyMaskedInvertedCulling :
                        CubismBuiltinMaterials.UnlitMultiplyMaskedCulling
                    : CubismBuiltinMaterials.UnlitMultiplyCulling;
            }


            return (drawable.IsMasked)
                ? (drawable.IsInverted) ? CubismBuiltinMaterials.UnlitMaskedInvertedCulling :
                    CubismBuiltinMaterials.UnlitMaskedCulling
                : CubismBuiltinMaterials.UnlitCulling;
        }

        /// <summary>
        /// Pick material for <see cref="CubismOffscreen"/>s.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="offscreen">Offscreen to map to.</param>
        /// <returns></returns>
        public static Material OffscreenMaterialPicker(CubismModel3Json sender, CubismOffscreen offscreen)
        {
            return CubismBuiltinMaterials.GetBlendModeMaterial("UnlitOffscreen", offscreen.ColorBlend, offscreen.AlphaBlend, offscreen.IsMasked, offscreen.IsInverted, offscreen.IsDoubleSided);
        }


        /// <summary>
        /// Builtin <see cref="Texture2D"/> picker.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to map to.</param>
        /// <returns>Mapped texture.</returns>
        public static Texture2D TexturePicker(CubismModel3Json sender, CubismDrawable drawable)
        {
            return sender.Textures[drawable.TextureIndex];
        }
    }
}
