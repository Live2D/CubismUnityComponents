/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismRenderer"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismRenderer)), CanEditMultipleObjects]
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

            if (!renderer.isActiveAndEnabled)
            {
                var model = renderer.FindCubismModel(true);
                var ctrl = model.GetComponent<CubismRenderController>();
                if (!ctrl.IsInitialized)
                {
                    ctrl.TryInitializeRenderers();
                }
            }

            // Show settings.
            EditorGUILayout.ObjectField("Mesh", renderer.Mesh, typeof(Mesh), false);


            EditorGUI.BeginChangeCheck();

            // Display OverrideFlagForDrawableMultiplyColors.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var overrideFlagForDrawableMultiplyColors = EditorGUILayout.Toggle("OverrideFlagForDrawableMultiplyColors", renderer.OverrideFlagForDrawableMultiplyColors);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.OverrideFlagForDrawableMultiplyColors = overrideFlagForDrawableMultiplyColors;
                    }
                }
            }

            // Display OverrideFlagForDrawableScreenColors.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var overrideFlagForDrawableScreenColors = EditorGUILayout.Toggle("OverrideFlagForDrawableScreenColors", renderer.OverrideFlagForDrawableScreenColors);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.OverrideFlagForDrawableScreenColors = overrideFlagForDrawableScreenColors;
                    }
                }
            }

            // Display color.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var color = EditorGUILayout.ColorField("Color", renderer.Color);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.Color = color;
                    }
                }
            }

            // Display multiply color.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var multiplyColor = EditorGUILayout.ColorField("MultiplyColor", renderer.MultiplyColor);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.MultiplyColor = multiplyColor;
                    }
                }
            }

            // Display screen color.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var screenColor = EditorGUILayout.ColorField("ScreenColor", renderer.ScreenColor);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.ScreenColor = screenColor;
                    }
                }
            }

            // Display material.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var material = EditorGUILayout.ObjectField("Material", renderer.Material, typeof(Material), true) as Material;

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.Material = material;
                    }
                }
            }

            // Display main texture.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var mainTexture = EditorGUILayout.ObjectField("Main Texture", renderer.MainTexture, typeof(Texture2D), true) as Texture2D;

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.MainTexture = mainTexture;
                    }
                }
            }

            // Display local sorting order.
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var localSortingOrder = EditorGUILayout.IntField("Local Order", renderer.LocalSortingOrder);

                if (scope.changed)
                {
                    foreach (CubismRenderer cubismRenderer in targets)
                    {
                        cubismRenderer.LocalSortingOrder = localSortingOrder;
                    }
                }
            }


            // Save any changes.
            if (EditorGUI.EndChangeCheck())
            {
                foreach (CubismRenderer cubismRenderer in targets)
                {
                    EditorUtility.SetDirty(cubismRenderer);
                    EditorUtility.SetDirty(cubismRenderer.MeshFilter);
                    EditorUtility.SetDirty(cubismRenderer.MeshRenderer);
                }
            }


            // Show backend toggle.
            var showBackends = (renderer.MeshRenderer.hideFlags & HideFlags.HideInInspector) != HideFlags.HideInInspector;
            var toggle = EditorGUILayout.Toggle("Show Mesh Filter & Renderer", showBackends) != showBackends;


            if (toggle)
            {
                foreach (CubismRenderer cubismRenderer in targets)
                {
                    cubismRenderer.MeshFilter.hideFlags ^= HideFlags.HideInInspector;
                    cubismRenderer.MeshRenderer.hideFlags ^= HideFlags.HideInInspector;
                }
            }
        }

        #endregion
    }
}
