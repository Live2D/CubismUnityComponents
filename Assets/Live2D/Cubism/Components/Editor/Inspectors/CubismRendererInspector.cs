/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Rendering;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismRenderer"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismRenderer))]
    internal sealed class CubismRendererInspector : UnityEditor.Editor
    {
        #region Editor

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var renderer = target as CubismRenderer;


            // Fail silently.
            if (renderer == null)
            {
                return;
            }


            // Show settings.
            EditorGUILayout.ObjectField("Mesh", renderer.Mesh, typeof(Mesh), false);


            EditorGUI.BeginChangeCheck();

            renderer.Color = EditorGUILayout.ColorField("Color", renderer.Color);
            renderer.Material = EditorGUILayout.ObjectField("Material", renderer.Material, typeof(Material), true) as Material;
            renderer.MainTexture = EditorGUILayout.ObjectField("Main Texture", renderer.MainTexture, typeof(Texture2D), true) as Texture2D;
            renderer.LocalSortingOrder = EditorGUILayout.IntField("Local Order", renderer.LocalSortingOrder);


            // Save any changes.
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(renderer);
                EditorUtility.SetDirty(renderer.MeshFilter);
                EditorUtility.SetDirty(renderer.MeshRenderer);
            }


            // Show backend toggle.
            var showBackends = (renderer.MeshRenderer.hideFlags & HideFlags.HideInInspector) != HideFlags.HideInInspector;
            var toggle = EditorGUILayout.Toggle("Show Mesh Filter & Renderer", showBackends) != showBackends;


            if (toggle)
            {
                renderer.MeshFilter.hideFlags   ^= HideFlags.HideInInspector;
                renderer.MeshRenderer.hideFlags ^= HideFlags.HideInInspector;
            }
        }

        #endregion
    }
}
