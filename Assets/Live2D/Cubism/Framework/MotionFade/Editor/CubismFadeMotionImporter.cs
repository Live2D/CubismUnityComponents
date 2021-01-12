/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


namespace Live2D.Cubism.Framework.MotionFade
{
    internal static class CubismFadeMotionImporter
    {
        #region Unity Event Handling

        /// <summary>
        /// Register fadeMotion importer.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RegisterMotionImporter()
        {
            CubismImporter.OnDidImportModel += OnModelImport;
            CubismImporter.OnDidImportMotion += OnFadeMotionImport;
        }

        #endregion

        #region Cubism Import Event Handling

        /// <summary>
        /// Create animator controller for MotionFade.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="model">Imported model.</param>
        private static void OnModelImport(CubismModel3JsonImporter importer, CubismModel model)
        {
            var dataPath = Directory.GetParent(Application.dataPath).FullName + "/";
            var assetPath = importer.AssetPath.Replace(".model3.json", ".controller");

            if (File.Exists(dataPath + assetPath))
            {
                return;
            }

            CreateAnimatorController(assetPath);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Create oldFadeMotion.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="animationClip">Imported motion.</param>
        private static void OnFadeMotionImport(CubismMotion3JsonImporter importer, AnimationClip animationClip)
        {
            var oldFadeMotion = AssetDatabase.LoadAssetAtPath<CubismFadeMotionData>(importer.AssetPath.Replace(".motion3.json", ".fade.asset"));

            // Create fade motion.
            CubismFadeMotionData fadeMotion;
            if (oldFadeMotion == null)
            {
                // Create fade motion instance.
                fadeMotion = CubismFadeMotionData.CreateInstance(
                    importer.Motion3Json,
                    importer.AssetPath,
                    animationClip.length,
                    CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow,
                    CubismUnityEditorMenu.ShouldClearAnimationCurves);

                AssetDatabase.CreateAsset(
                    fadeMotion,
                    importer.AssetPath.Replace(".motion3.json", ".fade.asset"));
            }
            else
            {
                fadeMotion = CubismFadeMotionData.CreateInstance(
                    oldFadeMotion,
                    importer.Motion3Json,
                    importer.AssetPath,
                    animationClip.length,
                    CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow,
                    CubismUnityEditorMenu.ShouldClearAnimationCurves);

                EditorUtility.CopySerialized(fadeMotion, oldFadeMotion);
            }

            EditorUtility.SetDirty(fadeMotion);


            // Add reference of motion for Fade to list.
            var directoryName = Path.GetDirectoryName(importer.AssetPath);
            var modelDir = Path.GetDirectoryName(directoryName);
            var modelName = Path.GetFileName(modelDir);
            var fadeMotionListPath = modelDir + "/" + modelName + ".fadeMotionList.asset";
            var fadeMotions = AssetDatabase.LoadAssetAtPath<CubismFadeMotionList>(fadeMotionListPath);

            // Create reference list.
            if (fadeMotions == null)
            {
                fadeMotions = ScriptableObject.CreateInstance<CubismFadeMotionList>();
                fadeMotions.MotionInstanceIds = new int[0];
                fadeMotions.CubismFadeMotionObjects = new CubismFadeMotionData[0];
                AssetDatabase.CreateAsset(fadeMotions, fadeMotionListPath);
            }


            var instanceId = 0;
            var isExistInstanceId = false;
            var events = animationClip.events;
            for (var k = 0; k < events.Length; ++k)
            {
                if (events[k].functionName != "InstanceId")
                {
                    continue;
                }

                instanceId = events[k].intParameter;
                isExistInstanceId = true;
                break;
            }

            if (!isExistInstanceId)
            {
                instanceId = animationClip.GetInstanceID();
            }


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

            // Add animation event
            {
                var sourceAnimationEvents = AnimationUtility.GetAnimationEvents(animationClip);
                var index = -1;

                for(var i = 0; i < sourceAnimationEvents.Length; ++i)
                {
                    if(sourceAnimationEvents[i].functionName != "InstanceId")
                    {
                        continue;
                    }

                    index = i;
                    break;
                }

                if(index == -1)
                {
                    index = sourceAnimationEvents.Length;
                    Array.Resize(ref sourceAnimationEvents, sourceAnimationEvents.Length + 1);
                    sourceAnimationEvents[sourceAnimationEvents.Length - 1] = new AnimationEvent();
                }

                sourceAnimationEvents[index].time = 0;
                sourceAnimationEvents[index].functionName = "InstanceId";
                sourceAnimationEvents[index].intParameter = instanceId;
                sourceAnimationEvents[index].messageOptions = SendMessageOptions.DontRequireReceiver;

                AnimationUtility.SetAnimationEvents(animationClip, sourceAnimationEvents);
            }
        }

        #endregion


        #region Functions

        /// <summary>
        /// Create animator controller for MotionFade.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns>Animator controller attached CubismFadeStateObserver.</returns>
        public static AnimatorController CreateAnimatorController(string assetPath)
        {
            var animatorController = AnimatorController.CreateAnimatorControllerAtPath(assetPath);
            animatorController.layers[0].stateMachine.AddStateMachineBehaviour<CubismFadeStateObserver>();

            return animatorController;
        }

        #endregion
    }
}
