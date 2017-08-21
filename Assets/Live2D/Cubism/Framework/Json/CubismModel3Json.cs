/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using System;
using System.IO;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Physics;
using Live2D.Cubism.Rendering;
using Live2D.Cubism.Rendering.Masking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Exposes moc3.json asset data.
    /// </summary>
    [Serializable]
    // ReSharper disable once ClassCannotBeInstantiated
    public sealed class CubismModel3Json
    {
        #region Delegates

        /// <summary>
        /// Handles the loading of assets.
        /// </summary>
        /// <param name="assetType">The asset type to load.</param>
        /// <param name="assetPath">The path to the asset.</param>
        /// <returns></returns>
        public delegate object LoadAssetAtPathHandler(Type assetType, string assetPath);


        /// <summary>
        /// Picks a <see cref="Material"/> for a <see cref="CubismDrawable"/>.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to pick for.</param>
        /// <returns>Picked material.</returns>
        public delegate Material MaterialPicker(CubismModel3Json sender, CubismDrawable drawable);

        /// <summary>
        /// Picks a <see cref="Texture2D"/> for a <see cref="CubismDrawable"/>.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="drawable">Drawable to pick for.</param>
        /// <returns>Picked texture.</returns>
        public delegate Texture2D TexturePicker(CubismModel3Json sender, CubismDrawable drawable);

        #endregion

        #region Load Methods

        /// <summary>
        /// Loads a model.json asset.
        /// </summary>
        /// <param name="assetPath">The path to the asset.</param>
        /// <returns>The <see cref="CubismModel3Json"/> on success; <see langword="null"/> otherwise.</returns>
        public static CubismModel3Json LoadAtPath(string assetPath)
        {
            // Use default asset load handler.
            return LoadAtPath(assetPath, BuiltinLoadAssetAtPath);
        }

        /// <summary>
        /// Loads a model.json asset.
        /// </summary>
        /// <param name="assetPath">The path to the asset.</param>
        /// <param name="loadAssetAtPath">Handler for loading assets.</param>
        /// <returns>The <see cref="CubismModel3Json"/> on success; <see langword="null"/> otherwise.</returns>
        public static CubismModel3Json LoadAtPath(string assetPath, LoadAssetAtPathHandler loadAssetAtPath)
        {
            // Load Json asset.
            var modelJsonAsset = loadAssetAtPath(typeof(string), assetPath) as string;

            // Return early in case Json asset wasn't loaded.
            if (modelJsonAsset == null)
            {
                return null;
            }


            // Deserialize Json.
            var modelJson = JsonUtility.FromJson<CubismModel3Json>(modelJsonAsset);


            // Finalize deserialization.
            modelJson.AssetPath       = assetPath;
            modelJson.LoadAssetAtPath = loadAssetAtPath;


            return modelJson;
        }

        #endregion

        /// <summary>
        /// Path to <see langword="this"/>.
        /// </summary>
        public string AssetPath { get; private set; }


        /// <summary>
        /// Method for loading assets.
        /// </summary>
        private LoadAssetAtPathHandler LoadAssetAtPath { get; set; }

        #region Json Data

        /// <summary>
        /// The motion3.json format version.
        /// </summary>
        [SerializeField]
        public int Version;

        /// <summary>
        /// The file references.
        /// </summary>
        [SerializeField]
        public SerializableFileReferences FileReferences;

        /// <summary>
        /// Groups.
        /// </summary>
        [SerializeField]
        public SerializableGroup[] Groups;

        #endregion

        /// <summary>
        /// The contents of the referenced moc3 asset.
        /// </summary>
        /// <remarks>
        /// The contents isn't cached internally.
        /// </remarks>
        public byte[] Moc3
        {
            get
            {
                return LoadReferencedAsset<byte[]>(FileReferences.Moc);
            }
        }

        /// <summary>
        /// The contents of physics3.json asset.
        /// </summary>
        public string Physics3Json
        {
            get
            {
                return string.IsNullOrEmpty(FileReferences.Physics) ? null : LoadReferencedAsset<string>(FileReferences.Physics);
            }
        }


        /// <summary>
        /// <see cref="Textures"/> backing field.
        /// </summary>
        [NonSerialized]
        private Texture2D[] _textures;

        /// <summary>
        /// The referenced texture assets.
        /// </summary>
        /// <remarks>
        /// The references aren't chached internally.
        /// </remarks>
        public Texture2D[] Textures
        {
            get
            {
                // Load textures only if necessary.
                if (_textures == null)
                {
                    _textures = new Texture2D[FileReferences.Textures.Length];


                    for (var i = 0; i < _textures.Length; ++i)
                    {
                        _textures[i] = LoadReferencedAsset<Texture2D>(FileReferences.Textures[i]);
                    }
                }


                return _textures;
            }
        }

        #region Constructors

        /// <summary>
        /// Makes construction only possible through factories.
        /// </summary>
        private CubismModel3Json()
        {
        }

        #endregion

        /// <summary>
        /// Instantiates a <see cref="CubismMoc">model source</see> and a <see cref="CubismModel">model</see> with the default texture set.
        /// </summary>
        /// <returns>The instantiated <see cref="CubismModel">model</see> on success; <see langword="null"/> otherwise.</returns>
        public CubismModel ToModel()
        {
            return ToModel(CubismBuiltinPickers.MaterialPicker, CubismBuiltinPickers.TexturePicker);
        }

        /// <summary>
        /// Instantiates a <see cref="CubismMoc">model source</see> and a <see cref="CubismModel">model</see>.
        /// </summary>
        /// <param name="pickMaterial">The material mapper to use.</param>
        /// <param name="pickTexture">The texture mapper to use.</param>
        /// <returns>The instantiated <see cref="CubismModel">model</see> on success; <see langword="null"/> otherwise.</returns>
        public CubismModel ToModel(MaterialPicker pickMaterial, TexturePicker pickTexture)
        {
            // Initialize model source and instantiate it.
            var mocAsBytes = Moc3;


            if (mocAsBytes == null)
            {
                return null;
            }


            var moc = CubismMoc.CreateFrom(mocAsBytes);


            var model = CubismModel.InstantiateFrom(moc);


            model.name = Path.GetFileNameWithoutExtension(FileReferences.Moc);


#if UNITY_EDITOR
            // Add parameters and parts inspectors.
            model.gameObject.AddComponent<CubismParametersInspector>();
            model.gameObject.AddComponent<CubismPartsInspector>();
#endif


            // Create renderers.
            var rendererController = model.gameObject.AddComponent<CubismRenderController>();
            var renderers = rendererController.Renderers;

            var drawables = model.Drawables;


            // Initialize materials.
            for (var i = 0; i < renderers.Length; ++i)
            {
                renderers[i].Material = pickMaterial(this, drawables[i]);
            }


            // Initialize textures.
            for (var i = 0; i < renderers.Length; ++i)
            {
                renderers[i].MainTexture = pickTexture(this, drawables[i]);
            }


            // Initialize groups.
            var parameters = model.Parameters;


            for (var i = 0; i < parameters.Length; ++i)
            {
                if (IsParameterInGroup(parameters[i], "EyeBlink"))
                {
                    if (model.gameObject.GetComponent<CubismEyeBlinkController>() == null)
                    {
                        model.gameObject.AddComponent<CubismEyeBlinkController>();
                    }


                    parameters[i].gameObject.AddComponent<CubismEyeBlinkParameter>();
                }


                // Set up mouth parameters.
                if (IsParameterInGroup(parameters[i], "LipSync"))
                {
                    if (model.gameObject.GetComponent<CubismMouthController>() == null)
                    {
                        model.gameObject.AddComponent<CubismMouthController>();
                    }


                    parameters[i].gameObject.AddComponent<CubismMouthParameter>();
                }
            }


            // Add mask controller if required.
            for (var i = 0; i < drawables.Length; ++i)
            {
                if (!drawables[i].IsMasked)
                {
                    continue;
                }


                // Add controller exactly once...
                model.gameObject.AddComponent<CubismMaskController>();


                break;
            }


            // Initialize physics if JSON exists.
            var physics3JsonAsString = Physics3Json;

            
            if (!string.IsNullOrEmpty(physics3JsonAsString))
            {
                var physics3Json = CubismPhysics3Json.LoadFrom(physics3JsonAsString);
                var physicsController = model.gameObject.GetComponent<CubismPhysicsController>();

                if (physicsController == null)
                {
                    physicsController = model.gameObject.AddComponent<CubismPhysicsController>();
                    
                }

                physicsController.Initialize(physics3Json.ToRig());
            }


            // Make sure model is 'fresh'
            model.ForceUpdateNow();


            return model;
        }

        #region Helper Methods

        /// <summary>
        /// Type-safely loads an asset.
        /// </summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="referencedFile">Path to asset.</param>
        /// <returns>The asset on success; <see langword="null"/> otherwise.</returns>
        private T LoadReferencedAsset<T>(string referencedFile) where T : class
        {
            var assetPath = Path.GetDirectoryName(AssetPath) + "/" + referencedFile;


            return LoadAssetAtPath(typeof(T), assetPath) as T;
        }


        /// <summary>
        /// Builtin method for loading assets.
        /// </summary>
        /// <param name="assetType">Asset type.</param>
        /// <param name="assetPath">Path to asset.</param>
        /// <returns>The asset on success; <see langword="null"/> otherwise.</returns>
        private static object BuiltinLoadAssetAtPath(Type assetType, string assetPath)
        {
            // Explicitly deal with byte arrays.
            if (assetType == typeof(byte[]))
            {
#if UNITY_EDITOR
                return File.ReadAllBytes(assetPath);
#else
                var textAsset = Resources.Load(assetPath, typeof(TextAsset)) as TextAsset;


                return (textAsset != null)
                    ? textAsset.bytes
                    : null;
#endif
            }
            else if (assetType == typeof(string))
            {
#if UNITY_EDITOR
                return File.ReadAllText(assetPath);
#else
                var textAsset = Resources.Load(assetPath, typeof(TextAsset)) as TextAsset;


                return (textAsset != null)
                    ? textAsset.text
                    : null;
#endif
            }


#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath(assetPath, assetType);
#else
            return Resources.Load(assetPath, assetType);
#endif
        }


        /// <summary>
        /// Checks whether the parameter is an eye blink parameter.
        /// </summary>
        /// <param name="parameter">Parameter to check.</param>
        /// <param name="groupName">Name of group to query for.</param>
        /// <returns><see langword="true"/> if parameter is an eye blink parameter; <see langword="false"/> otherwise.</returns>
        private bool IsParameterInGroup(CubismParameter parameter, string groupName)
        {
            // Return early if groups aren't available...
            if (Groups == null || Groups.Length == 0)
            {
                return false;
            }


            for (var i = 0; i < Groups.Length; ++i)
            {
                if (Groups[i].Name != groupName)
                {
                    continue;
                }


                for (var j = 0; j < Groups[i].Ids.Length; ++j)
                {
                    if (Groups[i].Ids[j] == parameter.name)
                    {
                        return true;
                    }
                }
            }


            return false;
        }

        #endregion

        #region Json Helpers

        /// <summary>
        /// File references data.
        /// </summary>
        [Serializable]
        public struct SerializableFileReferences
        {
            /// <summary>
            /// Relative path to the moc3 asset.
            /// </summary>
            [SerializeField]
            public string Moc;

            /// <summary>
            /// Relative paths to texture assets.
            /// </summary>
            [SerializeField]
            public string[] Textures;

            /// <summary>
            /// Relative path to the physics asset.
            /// </summary>
            [SerializeField]
            public string Physics;
        }

        /// <summary>
        /// Group data.
        /// </summary>
        [Serializable]
        public struct SerializableGroup
        {
            /// <summary>
            /// Target type.
            /// </summary>
            [SerializeField]
            public string Target;

            /// <summary>
            /// Group name.
            /// </summary>
            [SerializeField]
            public string Name;

            /// <summary>
            /// Referenced IDs.
            /// </summary>
            [SerializeField]
            public string[] Ids;
        }

        #endregion
    }
}
