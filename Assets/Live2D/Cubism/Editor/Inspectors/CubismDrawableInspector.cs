using Live2D.Cubism.Core;
using UnityEditor;

namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismRenderer"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismDrawable)), CanEditMultipleObjects]
    internal sealed class CubismDrawableInspector : UnityEditor.Editor
    {
        /// <summary>
        /// <see cref="CubismPart"/>s cache.
        /// </summary>
        private CubismPart[] Parts { get; set; }

        #region Editor

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var drawable = target as CubismDrawable;

            if (Parts == null)
            {
                Parts = drawable.FindCubismModel(true).Parts;
            }

            EditorGUILayout.ObjectField("Parent Part", Parts[drawable.ParentPartIndex], typeof(CubismPart), false);
        }

        #endregion
    }
}
