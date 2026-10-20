/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Editor.OriginalWorkflow;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor
{
    public class CubismTextureImportSettingWindow : EditorWindow
    {
        private CubismTextureImportSettings _textureImportSettings;

        [MenuItem("Live2D/Cubism/Texture Filter Setting Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<CubismTextureImportSettingWindow>();
            window.titleContent = new GUIContent("CubismTextureImportSettingWindow");
        }

        private void OnGUI()
        {
            _textureImportSettings = CubismTextureImportSettings.TextureImportSettings;

            GUILayout.Label("Cubism Texture Mipmap Setting", EditorStyles.boldLabel);

            _textureImportSettings.ShouldImportWithMipmaps = EditorGUILayout.ToggleLeft(
                "Should Import With Mipmaps",
                _textureImportSettings.ShouldImportWithMipmaps);

            GUILayout.Label("Cubism Texture Filter Setting", EditorStyles.boldLabel);

            _textureImportSettings.TextureAtlasFilterMode = (FilterMode)EditorGUILayout.EnumPopup("Texture Filter Mode", _textureImportSettings.TextureAtlasFilterMode);

            GUILayout.Space(20);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_textureImportSettings);
            }

            if (GUILayout.Button("Reset To Default", GUILayout.Height(30f)))
            {
                _textureImportSettings.ShouldImportWithMipmaps = CubismTextureImportSettings.DefaultShouldImportWithMipmaps;
                _textureImportSettings.TextureAtlasFilterMode = CubismTextureImportSettings.DefaultTextureAtlasFilterMode;
                EditorUtility.SetDirty(_textureImportSettings);
            }
        }
    }
}
