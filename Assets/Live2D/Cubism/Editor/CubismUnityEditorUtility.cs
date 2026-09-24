using System.IO;
using UnityEditor;
using UnityEngine;

namespace Live2D.Cubism.Editor
{
    public class CubismUnityEditorUtility
    {
        /// <summary>
        /// Projectウィンドウで現在選択しているディレクトリのパスを取得。
        /// Projectウィンドウ以外が選択されていたり、何も選択されていない場合、返す値はAssets直下。
        /// </summary>
        /// <returns>Projectウィンドウで現在のディレクトリのパス</returns>
        public static string GetCurrentDirectoryPath()
        {
            var activeObject = Selection.activeObject;
            var currentDirectoryPath = ((activeObject == null)
                ? "Assets"
                : AssetDatabase.GetAssetPath(activeObject));

            if (string.IsNullOrEmpty(currentDirectoryPath))
            {
                currentDirectoryPath = "Assets";
            }
            else if (!Directory.Exists(currentDirectoryPath))
            {
                currentDirectoryPath = currentDirectoryPath.Replace("/" + Path.GetFileName(currentDirectoryPath), "");
            }

            return currentDirectoryPath;
        }

        /// <summary>
        /// Get the id that links an animation clip to its entry in <c>CubismFadeMotionList.MotionInstanceIds</c>.
        /// </summary>
        /// <remarks>
        /// Unity 6.6 makes <c>Object.GetInstanceID()</c> an obsolete error and offers no public way to get an int instance id from an <c>EntityId</c>.
        /// The lower 32 bits of <c>EntityId.ToULong()</c> hold the entity index and equal the value <c>GetInstanceID()</c> used to return,
        /// so ids stay unique among live objects and stay compatible with ids already stored in assets.
        /// </remarks>
        /// <param name="animationClip">Target animation clip.</param>
        /// <returns>Id to store in the animation event and the fade motion list.</returns>
        internal static int GetMotionInstanceId(AnimationClip animationClip)
        {
#if UNITY_6000_6_OR_NEWER
            return unchecked((int)EntityId.ToULong(animationClip.GetEntityId()));
#else
            return animationClip.GetInstanceID();
#endif
        }
    }
}
