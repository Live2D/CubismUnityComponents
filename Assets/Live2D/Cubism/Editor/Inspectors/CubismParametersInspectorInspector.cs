/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Allows inspecting <see cref="CubismParameter"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismParametersInspector))]
    internal sealed class CubismParametersInspectorInspector : UnityEditor.Editor
    {
        #region Editor

        /// <summary>
        /// Draws the inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Lazily initialize.
            if (!IsInitialized)
            {
                Initialize();
            }


            // Show parameters.
            var didParametersChange = false;


            foreach (var parameter in Parameters)
            {
                EditorGUI.BeginChangeCheck();


                parameter.Value = EditorGUILayout.Slider(
                    parameter.Id,
                    parameter.Value,
                    parameter.MinimumValue,
                    parameter.MaximumValue
                    );


                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(parameter);


                    didParametersChange = true;
                }
            }


            // Show reset button.
            var resetPosition = EditorGUILayout.GetControlRect();


            resetPosition.width *= 0.25f;
            resetPosition.x += (resetPosition.width*3f);


            if (GUI.Button(resetPosition, "Reset"))
            {
                foreach (var parameter in Parameters)
                {
                    parameter.Value = parameter.DefaultValue;


                    EditorUtility.SetDirty(parameter);
                }


                didParametersChange = true;
            }

            
            if (didParametersChange)
            {
                (target as Component)
                    .FindCubismModel()
                    .ForceUpdateNow();
            }
        }

        #endregion

        /// <summary>
        /// <see cref="CubismParameter"/>s cache.
        /// </summary>
        private CubismParameter[] Parameters { get; set; }


        /// <summary>
        /// Gets whether <see langword="this"/> is initialized.
        /// </summary>
        private bool IsInitialized
        {
            get
            {
                return Parameters != null;
            }
        }


        /// <summary>
        /// Initializes <see langword="this"/>.
        /// </summary>
        private void Initialize()
        {
            Parameters = (target as Component)
                .FindCubismModel(true)
                .Parameters;
        }
    }
}
