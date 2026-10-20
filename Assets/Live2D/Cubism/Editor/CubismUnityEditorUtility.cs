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
        /// Unity 6.6 makes <see cref="Object.GetInstanceID()"/> an obsolete error, so the id is derived from <see cref="EntityId"/> instead.
        /// Unity itself implements <see cref="Object.GetInstanceID()"/> as the lower 32 bits of the raw <see cref="EntityId"/> value returned by <see cref="EntityId.ToULong()"/>,
        /// so truncating it here keeps the id identical to the one already stored in assets.
        /// The implicit conversion from <see cref="EntityId"/> to <c>int</c> throws, and cannot be used instead.
        /// </summary>
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
