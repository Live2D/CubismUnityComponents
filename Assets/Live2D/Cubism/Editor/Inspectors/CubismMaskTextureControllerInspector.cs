/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Rendering.Masking;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismMaskTextureController"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismMaskTextureController))]
    internal sealed class CubismMaskTextureControllerInspector : UnityEditor.Editor
    {
        #region Editor

        private bool _foldoutStatus = true;

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var texture = target as CubismMaskTextureController;


            // Fail silently.
            if (!texture)
            {
                return;
            }

            // Show settings.
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Use High Precision Mask", EditorStyles.boldLabel);
            texture.UseHighPrecisionMask = EditorGUILayout.Toggle("Use High Precision Mask", texture.UseHighPrecisionMask);

            if (texture.UseHighPrecisionMask)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("High Precision Mask is enabled. Hide other settings.", EditorStyles.boldLabel);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("Mask Texture Size", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("NOTE: Size need Power of Two and Minimum 256.", EditorStyles.boldLabel);
                texture.Size = EditorGUILayout.IntField("Size (In Pixels)", texture.Size);
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Multiple RenderTexture", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                texture.RenderTextureCount = EditorGUILayout.IntField("RenderTextureCount", texture.RenderTextureCount);
                EditorGUILayout.Space();

                _foldoutStatus = EditorGUILayout.Foldout(_foldoutStatus, "Render Textures (Read-only)");
                if (_foldoutStatus)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);

                    // Make it practically ReadOnly.
                    GUI.enabled = false;
                    for (var renderTextureIndex = 0; renderTextureIndex < texture.RenderTextures.Length; renderTextureIndex++)
                    {
                        EditorGUILayout.ObjectField($"element {renderTextureIndex} (Read-only)", texture.RenderTextures[renderTextureIndex], typeof(RenderTexture), false);
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;
            }

            // Save any changes.
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(texture);
            }
        }

        #endregion
    }
}
