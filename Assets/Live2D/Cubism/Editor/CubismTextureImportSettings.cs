/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor.OriginalWorkflow
{
    public class CubismTextureImportSettings : ScriptableObject
    {
        /// <summary>
        /// Default mipmap setting for Cubism model's texture import settings.
        /// </summary>
        public static readonly bool DefaultShouldImportWithMipmaps = true;

        /// <summary>
        /// Default texture atlas filter mode for Cubism model's texture import settings.
        /// </summary>
        public static readonly FilterMode DefaultTextureAtlasFilterMode = FilterMode.Trilinear;

        /// <summary>
        /// Should import with mipmaps for Cubism model's texture import settings.
        /// </summary>
        [SerializeField]
        public bool ShouldImportWithMipmaps = DefaultShouldImportWithMipmaps;

        /// <summary>
        /// Texture atlas filter mode for Cubism model's texture import settings.
        /// </summary>
        [SerializeField]
        public FilterMode TextureAtlasFilterMode = DefaultTextureAtlasFilterMode;

        /// <summary>
        /// Cached Cubism model's texture import settings.
        /// </summary>
        private static CubismTextureImportSettings _cachedTextureImportSettings;

        /// <summary>
        /// Cubism model's texture import settings.
        /// </summary>
        /// <returns>Cubism model's texture import settings asset. </returns>
        public static CubismTextureImportSettings TextureImportSettings
        {
            get
            {
                if (_cachedTextureImportSettings)
                {
                    return _cachedTextureImportSettings;
                }

                var setting = Resources.Load<CubismTextureImportSettings>("Live2D/Cubism/CubismTextureImportSettings");

                if (!setting)
                {
                    setting = CreateInstance<CubismTextureImportSettings>();
                    var directory = "Assets/Live2D/Cubism/Editor/Resources/Live2D/Cubism/";
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    AssetDatabase.CreateAsset(setting, "Assets/Live2D/Cubism/Editor/Resources/Live2D/Cubism/CubismTextureImportSettings.asset");
                }

                _cachedTextureImportSettings = setting;
                return setting;
            }
        }
    }
}

