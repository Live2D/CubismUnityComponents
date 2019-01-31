
using Live2D.Cubism.Editor;
using Live2D.Cubism.Editor.Importers;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Live2D.Cubism.Framework.MotionFade
{
    internal static class CubismFadeMotionImporter
    {
        #region Unity Event Handling

        /// <summary>
        /// 
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

            // Fade用モーションを作成
            CubismFadeMotionData fadeMotion;
            if(oldFadeMotion == null)
            {
                // Fade用モーションを作成
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


            // Fade用モーションの参照をリストに追加
            var directoryName = Path.GetDirectoryName(importer.AssetPath).ToString();
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


            var instanceId = AssetDatabase.LoadAssetAtPath<AnimationClip>(importer.AssetPath.Replace(".motion3.json", ".anim")).GetInstanceID();
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

        #endregion
    }
}