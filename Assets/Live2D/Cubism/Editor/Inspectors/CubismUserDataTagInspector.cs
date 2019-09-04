/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.UserData;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismUserDataTag"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismUserDataTag))]
    internal sealed class CubismUserDataTagInspector : UnityEditor.Editor
    {
        #region Editor

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var tag = target as CubismUserDataTag;


            // Fail silently.
            if (tag == null)
            {
                return;
            }


            EditorGUILayout.BeginHorizontal();


            // Display user data as readonly.
            EditorGUILayout.LabelField("Value", GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.TextArea(tag.Value, EditorStyles.textArea, null);


            EditorGUILayout.EndHorizontal();

        }

        #endregion
    }
}
