/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using System;
using System.IO;
using System.Collections.Generic;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Physics;
using Live2D.Cubism.Framework.UserData;
using Live2D.Cubism.Framework.Pose;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.MotionFade;
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
            modelJson.AssetPath = assetPath;
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
        /// <see cref="CubismPose3Json"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismPose3Json _pose3Json;

        /// <summary>
        /// The contents of pose3.json asset.
        /// </summary>
        public CubismPose3Json Pose3Json
        {
            get
            {
                if(_pose3Json != null)
                {
                    return _pose3Json;
                }

                var jsonString = string.IsNullOrEmpty(FileReferences.Pose) ? null : LoadReferencedAsset<String>(FileReferences.Pose);
                _pose3Json = CubismPose3Json.LoadFrom(jsonString);
                return _pose3Json;
            }
        }

        /// <summary>
        /// <see cref="Expression3Jsons"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismExp3Json[] _expression3Jsons;

        /// <summary>
        /// The referenced expression assets.
        /// </summary>
        /// <remarks>
        /// The references aren't chached internally.
        /// </remarks>
        public CubismExp3Json[] Expression3Jsons
        {
            get
            {
                // Fail silently...
                if(FileReferences.Expressions == null)
                {
                    return null;
                }

                // Load expression only if necessary.
                if (_expression3Jsons == null)
                {
                    _expression3Jsons = new CubismExp3Json[FileReferences.Expressions.Length];

                    for (var i = 0; i < _expression3Jsons.Length; ++i)
                    {
                        var expressionJson = (string.IsNullOrEmpty(FileReferences.Expressions[i].File))
                                                ? null
                                                : LoadReferencedAsset<string>(FileReferences.Expressions[i].File);
                        _expression3Jsons[i] = CubismExp3Json.LoadFrom(expressionJson);
                    }
                }

                return _expression3Jsons;
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

        public string UserData3Json
        {
            get
            {
                return string.IsNullOrEmpty(FileReferences.UserData) ? null : LoadReferencedAsset<string>(FileReferences.UserData);
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
        /// <param name="shouldImportAsOriginalWorkflow">Should import as original workflow.</param>
        /// <returns>The instantiated <see cref="CubismModel">model</see> on success; <see langword="null"/> otherwise.</returns>
        public CubismModel ToModel(bool shouldImportAsOriginalWorkflow = false)
        {
            return ToModel(CubismBuiltinPickers.MaterialPicker, CubismBuiltinPickers.TexturePicker, shouldImportAsOriginalWorkflow);
        }

        /// <summary>
        /// Instantiates a <see cref="CubismMoc">model source</see> and a <see cref="CubismModel">model</see>.
        /// </summary>
        /// <param name="pickMaterial">The material mapper to use.</param>
        /// <param name="pickTexture">The texture mapper to use.</param>
        /// <param name="shouldImportAsOriginalWorkflow">Should import as original workflow.</param>
        /// <returns>The instantiated <see cref="CubismModel">model</see> on success; <see langword="null"/> otherwise.</returns>
        public CubismModel ToModel(MaterialPicker pickMaterial, TexturePicker pickTexture, bool shouldImportAsOriginalWorkflow = false)
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

            // Add original workflow component if is original workflow.
            if(shouldImportAsOriginalWorkflow)
            {
                // Add cubism update manager.
                var updateaManager = model.gameObject.GetComponent<CubismUpdateController>();

                if(updateaManager == null)
                {
                    model.gameObject.AddComponent<CubismUpdateController>();
                }

                // Add parameter store.
                var parameterStore = model.gameObject.GetComponent<CubismParameterStore>();

                if(parameterStore == null)
                {
                    parameterStore = model.gameObject.AddComponent<CubismParameterStore>();
                }

                // Add pose controller.
                var poseController = model.gameObject.GetComponent<CubismPoseController>();

                if(poseController == null)
                {
                    poseController = model.gameObject.AddComponent<CubismPoseController>();
                }

                poseController.PoseData.Initialize(Pose3Json);

#if UNITY_EDITOR
                // Create pose animation clip
                var motions = new List<SerializableMotion>();

                if(FileReferences.Motions.Idle != null)
                {
                    motions.AddRange(FileReferences.Motions.Idle);
                }

                if(FileReferences.Motions.TapBody != null)
                {
                    motions.AddRange(FileReferences.Motions.TapBody);
                }

                for(var i = 0; i < motions.Count; ++i)
                {
                    var jsonString = string.IsNullOrEmpty(motions[i].File)
                                        ? null
                                        : LoadReferencedAsset<string>(motions[i].File);

                    if(jsonString == null)
                    {
                        continue;
                    }

                    var assetsDirectoryPath = Application.dataPath.Replace("Assets", "");
                    var assetPath = AssetPath.Replace(assetsDirectoryPath, "");
                    var directoryPath = Path.GetDirectoryName(assetPath) + "/";
                    var motion3Json = CubismMotion3Json.LoadFrom(jsonString);

                    var animationClipPath = directoryPath + motions[i].File.Replace(".motion3.json", ".anim");
                    var newAnimationClip = motion3Json.ToAnimationClip(shouldImportAsOriginalWorkflow, false, true, Pose3Json);
                    var oldAnimationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationClipPath);

                    // Create animation clip.
                    if(oldAnimationClip == null)
                    {
                        AssetDatabase.CreateAsset(newAnimationClip, animationClipPath);
                        oldAnimationClip = newAnimationClip;
                    }
                    // Update animation clip.
                    else
                    {
                        EditorUtility.CopySerialized(newAnimationClip, oldAnimationClip);
                        EditorUtility.SetDirty(oldAnimationClip);
                    }

                    var fadeMotionPath = directoryPath + motions[i].File.Replace(".motion3.json", ".fade.asset");
                    var fadeMotion = AssetDatabase.LoadAssetAtPath<CubismFadeMotionData>(fadeMotionPath);

                    if(fadeMotion == null)
                    {
                        fadeMotion = CubismFadeMotionData.CreateInstance(motion3Json, fadeMotionPath.Replace(directoryPath, ""),
                                                                        newAnimationClip.length, shouldImportAsOriginalWorkflow, true);

                        AssetDatabase.CreateAsset(fadeMotion, fadeMotionPath); 

                        EditorUtility.SetDirty(fadeMotion);

                        // Fade用モーションの参照をリストに追加
                        var directoryName = Path.GetDirectoryName(fadeMotionPath).ToString();
                        var modelDir = Path.GetDirectoryName(directoryName).ToString();
                        var modelName = Path.GetFileName(modelDir).ToString();
                        var fadeMotionListPath = Path.GetDirectoryName(directoryName).ToString() + "/" + modelName + ".fadeMotionList.asset";
                        var fadeMotions = AssetDatabase.LoadAssetAtPath<CubismFadeMotionList>(fadeMotionListPath);

                        // 参照リスト作成
                        if(fadeMotions == null)
                        {
                            fadeMotions = ScriptableObject.CreateInstance<CubismFadeMotionList>();
                            fadeMotions.MotionInstanceIds = new int[0];
                            fadeMotions.CubismFadeMotionObjects = new CubismFadeMotionData[0];
                            AssetDatabase.CreateAsset(fadeMotions, fadeMotionListPath);
                        }

                        var instanceId = oldAnimationClip.GetInstanceID();
                        var motionIndex =  Array.IndexOf(fadeMotions.MotionInstanceIds, instanceId);
                        if (motionIndex != -1)
                        {
                            fadeMotions.CubismFadeMotionObjects[motionIndex] = fadeMotion;
                        }
                        else
                        {
                            motionIndex = fadeMotions.MotionInstanceIds.Length;

                            Array.Resize(ref fadeMotions.MotionInstanceIds, motionIndex+1);
                            fadeMotions.MotionInstanceIds[motionIndex] = instanceId;

                            Array.Resize(ref fadeMotions.CubismFadeMotionObjects, motionIndex+1);
                            fadeMotions.CubismFadeMotionObjects[motionIndex] = fadeMotion;
                        }

                        EditorUtility.SetDirty(fadeMotions);
                    
                    }

                    for (var curveIndex = 0; curveIndex < motion3Json.Curves.Length; ++curveIndex)
                    {
                        var curve = motion3Json.Curves[curveIndex];

                        if (curve.Target == "PartOpacity")
                        {
                            if(Pose3Json.FadeInTime == 0.0f)
                            {
                                fadeMotion.ParameterIds[curveIndex] = curve.Id;
                                fadeMotion.ParameterFadeInTimes[curveIndex] = Pose3Json.FadeInTime;
                                fadeMotion.ParameterFadeOutTimes[curveIndex] = (curve.FadeOutTime < 0.0f) ? -1.0f : curve.FadeOutTime;
                                fadeMotion.ParameterCurves[curveIndex] = new AnimationCurve(CubismMotion3Json.ConvertCurveSegmentsToKeyframes(curve.Segments));
                            }
                            else
                            {
                                var poseFadeInTIme = (Pose3Json.FadeInTime < 0.0f) ? 0.5f : Pose3Json.FadeInTime;
                                fadeMotion.ParameterIds[curveIndex] = curve.Id;
                                fadeMotion.ParameterFadeInTimes[curveIndex] = poseFadeInTIme;
                                fadeMotion.ParameterFadeOutTimes[curveIndex] = (curve.FadeOutTime < 0.0f) ? -1.0f : curve.FadeOutTime;

                                var segments = curve.Segments;
                                var segmentsCount = 2;
                                for(var index = 2; index < curve.Segments.Length; index += 3)
                                {
                                    // if current segment type is stepped and
                                    // next segment type is stepped or next segment is last segment
                                    // then convert segment type to liner.
                                    var currentSegmentTypeIsStepped = (curve.Segments[index] == 2);
                                    var currentSegmentIsLast = (index == (curve.Segments.Length - 3));
                                    var nextSegmentTypeIsStepped = (currentSegmentIsLast) ? false : (curve.Segments[index + 3] == 2);
                                    var nextSegmentIsLast = (currentSegmentIsLast) ? false : ((index + 3) == (curve.Segments.Length - 3));
                                    if ( currentSegmentTypeIsStepped && (nextSegmentTypeIsStepped || nextSegmentIsLast) )
                                    {
                                        Array.Resize(ref segments, segments.Length + 3);
                                        segments[segmentsCount + 0] = 0;
                                        segments[segmentsCount + 1] = curve.Segments[index + 1];
                                        segments[segmentsCount + 2] = curve.Segments[index - 1];
                                        segments[segmentsCount + 3] = 0;
                                        segments[segmentsCount + 4] = curve.Segments[index + 1] + poseFadeInTIme;
                                        segments[segmentsCount + 5] = curve.Segments[index + 2];
                                        segmentsCount += 6;
                                    }
                                    else if(curve.Segments[index] == 1)
                                    {
                                        segments[segmentsCount + 0] = curve.Segments[index + 0];
                                        segments[segmentsCount + 1] = curve.Segments[index + 1];
                                        segments[segmentsCount + 2] = curve.Segments[index + 2];
                                        segments[segmentsCount + 3] = curve.Segments[index + 3];
                                        segments[segmentsCount + 4] = curve.Segments[index + 4];
                                        segments[segmentsCount + 5] = curve.Segments[index + 5];
                                        segments[segmentsCount + 6] = curve.Segments[index + 6];
                                        index += 4;
                                        segmentsCount += 7;
                                    }
                                    else
                                    {
                                        segments[segmentsCount + 0] = curve.Segments[index + 0];
                                        segments[segmentsCount + 1] = curve.Segments[index + 1];
                                        segments[segmentsCount + 2] = curve.Segments[index + 2];
                                        segmentsCount += 3;
                                    }
                                }
                                fadeMotion.ParameterCurves[curveIndex] = new AnimationCurve(CubismMotion3Json.ConvertCurveSegmentsToKeyframes(segments));
                            }
                        }
                    }

                    EditorUtility.SetDirty(fadeMotion);

                }
#endif

                // Add expression controller.
                var expressionController = model.gameObject.GetComponent<CubismExpressionController>();

                if(expressionController == null)
                {
                    expressionController = model.gameObject.AddComponent<CubismExpressionController>();
                }


                // Add fade controller.
                var motionFadeController = model.gameObject.GetComponent<CubismFadeController>();

                if(motionFadeController == null)
                {
                    motionFadeController = model.gameObject.AddComponent<CubismFadeController>();
                }

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


            var userData3JsonAsString = UserData3Json;


            if (!string.IsNullOrEmpty(userData3JsonAsString))
            {
                var userData3Json = CubismUserData3Json.LoadFrom(userData3JsonAsString);


                var drawableBodies = userData3Json.ToBodyArray(CubismUserDataTargetType.ArtMesh);

                for (var i = 0; i < drawables.Length; ++i)
                {
                    var index = GetBodyIndexById(drawableBodies, drawables[i].Id);

                    if (index >= 0)
                    {
                        var tag = drawables[i].gameObject.GetComponent<CubismUserDataTag>();


                        if (tag == null)
                        {
                            tag = drawables[i].gameObject.AddComponent<CubismUserDataTag>();
                        }


                        tag.Initialize(drawableBodies[index]);
                    }
                }
            }

            if (model.gameObject.GetComponent<Animator>() == null)
            {
                model.gameObject.AddComponent<Animator>();
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

                if(Groups[i].Ids != null)
                {
                    for (var j = 0; j < Groups[i].Ids.Length; ++j)
                    {
                        if (Groups[i].Ids[j] == parameter.name)
                        {
                            return true;
                        }
                    }
                }
            }


            return false;
        }


        /// <summary>
        /// Get body index from body array by Id.
        /// </summary>
        /// <param name="bodies">Target body array.</param>
        /// <param name="id">Id for find.</param>
        /// <returns>Array index if Id found; -1 otherwise.</returns>
        private int GetBodyIndexById(CubismUserDataBody[] bodies, string id)
        {
            for (var i = 0; i < bodies.Length; ++i)
            {
                if (bodies[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
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
            /// Relative path to the pose3.json.
            /// </summary>
            [SerializeField]
            public string Pose;

            /// <summary>
            /// Relative path to the expression asset.
            /// </summary>
            [SerializeField]
            public SerializableExpression[] Expressions;

            /// <summary>
            /// Relative path to the pose motion3.json.
            /// </summary>
            [SerializeField]
            public SerializableMotions Motions;

            /// <summary>
            /// Relative path to the physics asset.
            /// </summary>
            [SerializeField]
            public string Physics;

            /// <summary>
            /// Relative path to the user data asset.
            /// </summary>
            [SerializeField]
            public string UserData;
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

        /// <summary>
        /// Expression data.
        /// </summary>
        [Serializable]
        public struct SerializableExpression
        {
            /// <summary>
            /// Expression Name.
            /// </summary>
            [SerializeField]
            public string Name;
            
            /// <summary>
            /// Expression File.
            /// </summary>
            [SerializeField]
            public string File;

            /// <summary>
            /// Expression FadeInTime.
            /// </summary>
            [SerializeField]
            public float FadeInTime;

            /// <summary>
            /// Expression FadeOutTime.
            /// </summary>
            [SerializeField]
            public float FadeOutTime;
        }

        /// <summary>
        /// Motion datas.
        /// </summary>
        [Serializable]
        public struct SerializableMotions
        {
            /// <summary>
            /// Motion group Idle.
            /// </summary>
            [SerializeField]
            public SerializableMotion[] Idle;

            /// <summary>
            /// Motion group TapBody.
            /// </summary>
            [SerializeField]
            public SerializableMotion[] TapBody;
        }

        /// <summary>
        /// Motion data.
        /// </summary>
        [Serializable]
        public struct SerializableMotion
        {
            /// <summary>
            /// File path.
            /// </summary>
            [SerializeField]
            public string File;

            /// <summary>
            /// Fade in time.
            /// </summary>
            [SerializeField]
            public float FadeInTime;

            /// <summary>
            /// Fade out time.
            /// </summary>
            [SerializeField]
            public float FadeOutTime;
        }

        #endregion
    }
}
