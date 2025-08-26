/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Rendering.Util;
using UnityEngine;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Default materials.
    /// </summary>
    public static class CubismBuiltinMaterials
    {
        /// <summary>
        /// Default unlit material.
        /// </summary>
        public static Material Unlit
        {
            get { return LoadUnlitMaterial("Unlit"); }
        }

        /// <summary>
        /// Default unlit, additively blending material.
        /// </summary>
        public static Material UnlitAdditive
        {
            get { return LoadUnlitMaterial("UnlitAdditive"); }
        }

        /// <summary>
        /// Default unlit, multiply blending material.
        /// </summary>
        public static Material UnlitMultiply
        {
            get { return LoadUnlitMaterial("UnlitMultiply"); }
        }


        /// <summary>
        /// Default unlit masked material.
        /// </summary>
        public static Material UnlitMasked
        {
            get { return LoadUnlitMaterial("UnlitMasked"); }
        }

        /// <summary>
        /// Default unlit masked, additively blending material.
        /// </summary>
        public static Material UnlitAdditiveMasked
        {
            get { return LoadUnlitMaterial("UnlitAdditiveMasked"); }
        }

        /// <summary>
        /// Default unlit masked, multiply blending material.
        /// </summary>
        public static Material UnlitMultiplyMasked
        {
            get { return LoadUnlitMaterial("UnlitMultiplyMasked"); }
        }


        /// <summary>
        /// Default unlit masked inverted material.
        /// </summary>
        public static Material UnlitMaskedInverted
        {
            get { return LoadUnlitMaterial("UnlitMaskedInverted"); }
        }

        /// <summary>
        /// Default unlit masked inverted, additively blending material.
        /// </summary>
        public static Material UnlitAdditiveMaskedInverted
        {
            get { return LoadUnlitMaterial("UnlitAdditiveMaskedInverted"); }
        }

        /// <summary>
        /// Default unlit masked inverted, multiply blending material.
        /// </summary>
        public static Material UnlitMultiplyMaskedInverted
        {
            get { return LoadUnlitMaterial("UnlitMultiplyMaskedInverted"); }
        }


        /// <summary>
        /// Default unlit material.
        /// </summary>
        public static Material UnlitCulling
        {
            get { return LoadUnlitMaterial("UnlitCulling"); }
        }

        /// <summary>
        /// Default unlit, additively blending material.
        /// </summary>
        public static Material UnlitAdditiveCulling
        {
            get { return LoadUnlitMaterial("UnlitAdditiveCulling"); }
        }

        /// <summary>
        /// Default unlit, multiply blending material.
        /// </summary>
        public static Material UnlitMultiplyCulling
        {
            get { return LoadUnlitMaterial("UnlitMultiplyCulling"); }
        }


        /// <summary>
        /// Default unlit masked material.
        /// </summary>
        public static Material UnlitMaskedCulling
        {
            get { return LoadUnlitMaterial("UnlitMaskedCulling"); }
        }

        /// <summary>
        /// Default unlit masked, additively blending material.
        /// </summary>
        public static Material UnlitAdditiveMaskedCulling
        {
            get { return LoadUnlitMaterial("UnlitAdditiveMaskedCulling"); }
        }

        /// <summary>
        /// Default unlit masked, multiply blending material.
        /// </summary>
        public static Material UnlitMultiplyMaskedCulling
        {
            get { return LoadUnlitMaterial("UnlitMultiplyMaskedCulling"); }
        }


        /// <summary>
        /// Default unlit masked inverted material.
        /// </summary>
        public static Material UnlitMaskedInvertedCulling
        {
            get { return LoadUnlitMaterial("UnlitMaskedInvertedCulling"); }
        }

        /// <summary>
        /// Default unlit masked inverted, additively blending material.
        /// </summary>
        public static Material UnlitAdditiveMaskedInvertedCulling
        {
            get { return LoadUnlitMaterial("UnlitAdditiveMaskedInvertedCulling"); }
        }

        /// <summary>
        /// Default unlit masked inverted, multiply blending material.
        /// </summary>
        public static Material UnlitMultiplyMaskedInvertedCulling
        {
            get { return LoadUnlitMaterial("UnlitMultiplyMaskedInvertedCulling"); }
        }



        /// <summary>
        /// Default mask material.
        /// </summary>
        public static Material Mask
        {
            get { return LoadMaskMaterial(); }
        }

        /// <summary>
        /// Default culled mask material.
        /// </summary>
        public static Material MaskCulling
        {
            get { return LoadMaskCullingMaterial(); }
        }

        /// <summary>
        /// Unlit plane material for BlendMode.
        /// </summary>
        public static Material UnlitPlane
        {
            get
            {
                return LoadBlendModeMaterial("UnlitPlane");
            }
        }

        /// <summary>
        /// <see cref="BlendMask"/>'s backing field.
        /// </summary>
        private static Material _unlitBlendMask;

        /// <summary>
        /// Unlit mask material for BlendMode.
        /// </summary>
        public static Material BlendMask
        {
            get
            {
                if (_unlitBlendMask == null)
                {
                    _unlitBlendMask = LoadBlendModeMaterial("UnlitBlendMask");
                }

                return _unlitBlendMask;
            }
        }

        /// <summary>
        /// Returns a material for the given BlendMode.
        /// </summary>
        /// <param name="materialName">Name</param>
        /// <param name="colorBlend">ColorBlend type</param>
        /// <param name="alphaBlend">AlphaBlend type</param>
        /// <param name="masked">Is masked?</param>
        /// <param name="inverted">Is using invert mask?</param>
        /// <param name="isDoubleSided">Is double-sided rendering?</param>
        /// <returns></returns>
        public static Material GetBlendModeMaterial(string materialName, BlendTypes.ColorBlend colorBlend, BlendTypes.AlphaBlend alphaBlend, bool masked, bool inverted, bool isDoubleSided)
        {
            var maskedString = masked ? "Masked" : "";
            var invertedString = masked && inverted ? "Invert" : "";
            var cullingString = isDoubleSided ? "" : "Culling";

            switch (colorBlend)
            {
                case BlendTypes.ColorBlend.Add:
                case BlendTypes.ColorBlend.Multiply:
                    materialName += $"{invertedString}{maskedString}{colorBlend}{cullingString}";
                    break;
                default:
                    materialName += $"{invertedString}{maskedString}{colorBlend}{alphaBlend}{cullingString}";
                    break;
            }

            return LoadBlendModeMaterial(materialName);
        }


        #region Helper Methods

        /// <summary>
        /// Resource directory of builtin <see cref="Material"/>s.
        /// </summary>
        private const string ResourcesDirectory = "Live2D/Cubism/Materials";


        /// <summary>
        /// Loads an unlit material.
        /// </summary>
        /// <param name="name">Material name.</param>
        /// <returns>The material.</returns>
        private static Material LoadUnlitMaterial(string name)
        {
            return Resources.Load<Material>(ResourcesDirectory + "/" + name);
        }

        /// <summary>
        /// Loads an mask material.
        /// </summary>
        /// <returns>The material.</returns>
        private static Material LoadMaskMaterial()
        {
            return Resources.Load<Material>(ResourcesDirectory + "/Mask");
        }

        /// <summary>
        /// Loads an mask culling material.
        /// </summary>
        /// <returns>The material.</returns>
        private static Material LoadMaskCullingMaterial()
        {
            return Resources.Load<Material>(ResourcesDirectory + "/MaskCulling");
        }

        /// <summary>
        /// Loads a material for BlendMode.
        /// </summary>
        /// <param name="name">Material's name</param>
        /// <returns>Material</returns>
        private static Material LoadBlendModeMaterial(string name)
        {
            var material = Resources.Load<Material>(ResourcesDirectory + "/BlendMode/" + name);
            if (material == null)
            {
                Debug.LogError($"Could not load material '{name}'");
            }
            return material;
        }

        #endregion
    }
}
