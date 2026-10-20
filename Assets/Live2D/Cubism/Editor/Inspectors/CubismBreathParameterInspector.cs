/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Framework.Breath;
using UnityEditor;

namespace Live2D.Cubism.Editor.Inspectors
{
    [CustomEditor(typeof(CubismBreathParameter)), CanEditMultipleObjects]
    public class CubismBreathParameterInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            // Display offset.
            var offset = serializedObject.FindProperty("Offset");
            EditorGUILayout.PropertyField(offset);

            // Display peak.
            var peak = serializedObject.FindProperty("Peak");
            EditorGUILayout.PropertyField(peak);

            // Display cycle.
            var cycle = serializedObject.FindProperty("Cycle");
            EditorGUILayout.PropertyField(cycle);

            // Display weight.
            var weight = serializedObject.FindProperty("Weight");
            EditorGUILayout.PropertyField(weight);

            // Save any changes.
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
