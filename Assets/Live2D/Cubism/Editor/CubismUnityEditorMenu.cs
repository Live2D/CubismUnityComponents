/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Editor.OriginalWorkflow;
using UnityEditor;


namespace Live2D.Cubism.Editor
{
    /// <summary>
    /// Cubism unity editor menu.
    /// </summary>
    public class CubismUnityEditorMenu
    {
        /// <summary>
        /// Should import as original workflow.
        /// </summary>
        public static bool ShouldImportAsOriginalWorkflow
        {
            get
            {
                return CubismOriginalWorkflowSettings.OriginalWorkflowSettings.ShouldImportAsOriginalWorkflow;
            }
            set
            {
                CubismOriginalWorkflowSettings.OriginalWorkflowSettings.ShouldImportAsOriginalWorkflow = value;
                EditorUtility.SetDirty(CubismOriginalWorkflowSettings.OriginalWorkflowSettings);
            }
        }

        /// <summary>
        /// Should clear animation clip curves.
        /// </summary>
        public static bool ShouldClearAnimationCurves
        {
            get
            {
                return CubismOriginalWorkflowSettings.OriginalWorkflowSettings.ShouldClearAnimationCurves;
            }
            set
            {
                CubismOriginalWorkflowSettings.OriginalWorkflowSettings.ShouldClearAnimationCurves = value;
                EditorUtility.SetDirty(CubismOriginalWorkflowSettings.OriginalWorkflowSettings);
            }
        }


        /// <summary>
        /// Unity editor menu should import as original workflow.
        /// </summary>
        [MenuItem ("Live2D/Cubism/OriginalWorkflow/Should Import As Original Workflow")]
        private static void ImportAsOriginalWorkflow()
        {
            SetImportAsOriginalWorkflow(!ShouldImportAsOriginalWorkflow);

            // Disable clear animation curves.
            if(!ShouldImportAsOriginalWorkflow)
            {
                SetClearAnimationCurves(false);
            }
        }

        /// <summary>
        /// Unity editor menu clear animation curves.
        /// </summary>
        [MenuItem ("Live2D/Cubism/OriginalWorkflow/Should Clear Animation Curves")]
        private static void ClearAnimationCurves()
        {
            SetClearAnimationCurves(!ShouldClearAnimationCurves);
        }

        /// <summary>
        /// Set import as original workflow.
        /// </summary>
        public static void SetImportAsOriginalWorkflow(bool isEnable)
        {
            ShouldImportAsOriginalWorkflow= isEnable;
            Menu.SetChecked ("Live2D/Cubism/OriginalWorkflow/Should Import As Original Workflow", ShouldImportAsOriginalWorkflow);
        }

        /// <summary>
        /// Set clear animation curves.
        /// </summary>
        public static void SetClearAnimationCurves(bool isEnable)
        {
            ShouldClearAnimationCurves= (ShouldImportAsOriginalWorkflow && isEnable);
            Menu.SetChecked ("Live2D/Cubism/OriginalWorkflow/Should Clear Animation Curves", ShouldClearAnimationCurves);
        }

        /// <summary>
        /// Initialize cubism menu.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () => Menu.SetChecked ("Live2D/Cubism/OriginalWorkflow/Should Import As Original Workflow", ShouldImportAsOriginalWorkflow);
            EditorApplication.delayCall += () => Menu.SetChecked ("Live2D/Cubism/OriginalWorkflow/Should Clear Animation Curves", ShouldClearAnimationCurves);
        }

    }
}