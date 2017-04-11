/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


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
        /// Default mask material.
        /// </summary>
        public static Material Mask
        {
            get { return LoadMaskMaterial(); }
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
            var material = Resources.Load<Material>(ResourcesDirectory + "/" + name);


            // Make sure shader is attached.
            if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
            {
                material.shader = CubismBuiltinShaders.Unlit;


#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(material);
#endif
            }


            return material;
        }

        /// <summary>
        /// Loads an unlit material.
        /// </summary>
        /// <returns>The material.</returns>
        private static Material LoadMaskMaterial()
        {
            var material = Resources.Load<Material>(ResourcesDirectory + "/Mask");


            // Make sure shader is attached.
            if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
            {
                material.shader = CubismBuiltinShaders.Mask;


#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(material);
#endif
            }


            return material;
        }

        #endregion
    }
}
