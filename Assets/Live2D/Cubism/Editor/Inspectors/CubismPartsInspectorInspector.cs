/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Allows inspecting <see cref="CubismPart"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismPartsInspector))]
    internal sealed class CubismPartsInspectorInspector : UnityEditor.Editor
    {
        private static class Internal
        {
            public sealed class InspectorViewData
            {
                public CubismPart part;
                public string name;
                public InspectorViewData[] children = new InspectorViewData[0];
                public bool foldout = true;
            }

            public static void OnInspectorGUI(ref bool didPartsChange, InspectorViewData[] src, int indent = 0)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    EditorGUI.BeginChangeCheck();

                    var rect = EditorGUILayout.GetControlRect();
                    //EditorGUI.indentLevel = indent;

                    var leftWidth = rect.width * 0.4f;
                    var indentSize = 12f * indent;

                    var left = new Rect(rect.x + indentSize, rect.y, leftWidth - indentSize, rect.height);
                    var right = new Rect(rect.x + leftWidth, rect.y, rect.width * 0.6f, rect.height);
                    if (src[i].children.Length > 0)
                    {
                        src[i].foldout = EditorGUI.Foldout(left, src[i].foldout, src[i].name);
                        src[i].part.Opacity = EditorGUI.Slider(right, src[i].part.Opacity, 0f, 1f);
                    }
                    else
                    {
                        EditorGUI.LabelField(left, src[i].name);
                        src[i].part.Opacity = EditorGUI.Slider(right, src[i].part.Opacity, 0f, 1f);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(src[i].part);

                        didPartsChange = true;
                    }

                    if (src[i].foldout)
                    {
                        OnInspectorGUI(ref didPartsChange, src[i].children, indent + 1);
                    }
                }
            }
        }

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

            // Show parts.
            var didPartsChange = false;
            Internal.OnInspectorGUI(ref didPartsChange, _data);

            // FIXME Force model update in case parameters have changed.
            if (didPartsChange)
            {
                (target as Component)
                    .FindCubismModel()
                    .ForceUpdateNow();
            }
        }

        #endregion

        /// <summary>
        /// <see cref="CubismPart"/>s cache.
        /// </summary>
        private CubismPart[] Parts { get; set; }

        private Internal.InspectorViewData[] _data = new Internal.InspectorViewData[0];

        /// <summary>
        /// Gets whether <see langword="this"/> is initialized.
        /// </summary>
        private bool IsInitialized
        {
            get
            {
                return Parts != null;
            }
        }


        /// <summary>
        /// Initializes <see langword="this"/>.
        /// </summary>
        private void Initialize()
        {
            Parts = (target as Component)
                .FindCubismModel(true)
                .Parts;

            var work = new Internal.InspectorViewData[Parts.Length];
            for (var i = 0; i < Parts.Length; i++)
            {
                var view = work[i] == null ? work[i] = new Internal.InspectorViewData() : work[i];
                view.part = Parts[i];

                var cdiPartName = view.part.GetComponent<CubismDisplayInfoPartName>();
                view.name = cdiPartName != null
                ? (string.IsNullOrEmpty(cdiPartName.DisplayName) ? cdiPartName.Name : cdiPartName.DisplayName)
                : string.Empty;

                var pi = view.part.UnmanagedParentIndex;
                if (pi < 0)
                {
                    System.Array.Resize(ref _data, _data.Length + 1);
                    _data[_data.Length - 1] = view;
                }
                else
                {
                    var parent = work[pi] == null
                        ? work[pi] = new Internal.InspectorViewData()
                        : work[pi];

                    System.Array.Resize(ref parent.children, parent.children.Length + 1);
                    parent.children[parent.children.Length - 1] = view;
                }
            }
        }
    }
}
