/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using System;
using System.IO;
using UnityEditor;
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
            CubismImporter.OnDidImportMotion += OnFadeMotionImport;
        }

        #endregion

        #region Cubism Import Event Handling

        /// <summary>
        /// Create oldFadeMotion.
        /// </summary>
        /// <param name="importer">Event source.</param>
        /// <param name="animationClip">Imported motion.</param>
        private static void OnFadeMotionImport(CubismMotion3JsonImporter importer, AnimationClip animationClip)
        {
            var directoryPath = Path.GetDirectoryName(importer.AssetPath) + "/";

            var oldFadeMotion = AssetDatabase.LoadAssetAtPath<CubismFadeMotionData>(importer.AssetPath.Replace(".motion3.json", ".fade.asset"));

            // Create fade motion.
            CubismFadeMotionData fadeMotion;
            if (oldFadeMotion == null)
            {
                // Create fade motion instance.
                fadeMotion = CubismFadeMotionData.CreateInstance(
                    importer.Motion3Json,
                    importer.AssetPath.Replace(directoryPath, ""),
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
                    importer.AssetPath.Replace(directoryPath, ""),
                    animationClip.length,
                    CubismUnityEditorMenu.ShouldImportAsOriginalWorkflow,
                    CubismUnityEditorMenu.ShouldClearAnimationCurves);

                EditorUtility.CopySerialized(fadeMotion, oldFadeMotion);
            }

            EditorUtility.SetDirty(fadeMotion);


            // Add reference of motion for Fade to list.
            var directoryName = Path.GetDirectoryName(importer.AssetPath).ToString();
            var modelDir = Path.GetDirectoryName(directoryName).ToString();
            var modelName = Path.GetFileName(modelDir).ToString();
            var fadeMotionListPath = Path.GetDirectoryName(directoryName).ToString() + "/" + modelName + ".fadeMotionList.asset";
            var fadeMotions = AssetDatabase.LoadAssetAtPath<CubismFadeMotionList>(fadeMotionListPath);

            // Create reference list.
            if (fadeMotions == null)
            {
                fadeMotions = ScriptableObject.CreateInstance<CubismFadeMotionList>();
                fadeMotions.MotionInstanceIds = new int[0];
                fadeMotions.CubismFadeMotionObjects = new CubismFadeMotionData[0];
                AssetDatabase.CreateAsset(fadeMotions, fadeMotionListPath);
            }


            var instanceId = animationClip.GetInstanceID();
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
                var sourceAnimEvents  = AnimationUtility.GetAnimationEvents(animationClip);
                AnimationEvent instanceIdEvent= null;
                for(var i = 0; i < sourceAnimEvents.Length; ++i)
                {
                    if(sourceAnimEvents[i].functionName != "InstanceId")
                    {
                        continue;
                    }

                    instanceIdEvent= sourceAnimEvents[i];
                    break;
                }

                if(instanceIdEvent== null)
                {
                    instanceIdEvent= new AnimationEvent();
                    Array.Resize(ref sourceAnimEvents, sourceAnimEvents.Length + 1);
                    sourceAnimEvents[sourceAnimEvents.Length - 1] = instanceIdEvent;
                }

                instanceIdEvent.time = 0;
                instanceIdEvent.functionName = "InstanceId";
                instanceIdEvent.intParameter = instanceId;
                instanceIdEvent.messageOptions = SendMessageOptions.DontRequireReceiver;

                AnimationUtility.SetAnimationEvents(animationClip, sourceAnimEvents);
            }
        }

        #endregion
    }
}