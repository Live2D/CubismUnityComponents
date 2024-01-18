using Live2D.Cubism.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="CubismRenderer"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismPart)), CanEditMultipleObjects]
    internal sealed class CubismPartInspector : UnityEditor.Editor
    {
        /// <summary>
        /// Parent <see cref="CubismPart"/> cache.
        /// </summary>
        private CubismPart _parent;

        /// <summary>
        /// Child <see cref="CubismPart"/>s foldout state.
        /// </summary>
        private static bool ChildPartsFoldout { get; set; } = true;

        /// <summary>
        /// Child <see cref="CubismPart"/>s ReorderableList.
        /// </summary>
        private ReorderableList ChildParts { get; set; }

        /// <summary>
        /// Child <see cref="CubismPart"/>'s child <see cref="CubismDrawable"/>s.
        /// </summary>
        private InternalTreeInfo ChildPartDrawables { get; set; }

        private int DrawablesTotalCount { get; set; }

        #region Editor

        /// <summary>
        /// Draws inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (!IsInitialized)
            {
                Initialize();
            }

            EditorGUILayout.ObjectField("Parent Part", _parent, typeof(CubismPart), false);

            {
                var rect = EditorGUILayout.GetControlRect();
                var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                var fieldRect = new Rect(rect.x + rect.width - 50f, rect.y, 50f, rect.height);

                ChildPartsFoldout = EditorGUI.Foldout(foldoutRect, ChildPartsFoldout, "Child Parts");
                EditorGUI.IntField(fieldRect, ChildParts.count);
            }
            if (ChildPartsFoldout)
            {
                ChildParts.DoLayoutList();
            }

            {
                var rect = EditorGUILayout.GetControlRect();
                var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                var fieldRect = new Rect(rect.x + rect.width - 50f, rect.y, 50f, rect.height);

                ChildPartDrawables.Foldout = EditorGUI.Foldout(foldoutRect, ChildPartDrawables.Foldout, "Child Drawables");
                EditorGUI.IntField(fieldRect, DrawablesTotalCount);
            }
            if (ChildPartDrawables.Foldout)
            {
                for (var i = 0; i < ChildPartDrawables.Drawables.Length; i++)
                {
                    EditorGUILayout.ObjectField(ChildPartDrawables.Drawables[i], typeof(CubismDrawable), true);
                }
                EditorGUI.indentLevel++;
                for (var i = 0; i < ChildPartDrawables.Childs.Length; i++)
                {
                    DrawTree(ChildPartDrawables.Childs[i]);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawTree(InternalTreeInfo item)
        {
            item.Foldout = EditorGUILayout.Foldout(item.Foldout, item.Part.Id);
            if (item.Foldout)
            {
                for (var i = 0; i < item.Drawables.Length; i++)
                {
                    EditorGUILayout.ObjectField(item.Drawables[i], typeof(CubismDrawable), true);
                }
                for (var i = 0; i < item.Childs.Length; i++)
                {
                    DrawTree(item.Childs[i]);
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets whether <see langword="this"/> is initialized.
        /// </summary>
        private bool IsInitialized
        {
            get
            {
                return ChildParts != null && ChildPartDrawables != null;
            }
        }

        /// <summary>
        /// Initializes <see langword="this"/>.
        /// </summary>
        private void Initialize()
        {
            var part = target as CubismPart;

            var model = part.FindCubismModel(true);
            var parts = model.Parts;
            var drawables = model.Drawables;

            _parent = part.UnmanagedParentIndex < 0 ? null : parts[part.UnmanagedParentIndex];

            var childParts = new CubismPart[0];
            for (var i = 0; i < parts.Length; i++)
            {
                if (parts[i].UnmanagedParentIndex == part.UnmanagedIndex)
                {
                    System.Array.Resize(ref childParts, childParts.Length + 1);
                    childParts[childParts.Length - 1] = parts[i];
                }
            }

            ChildParts = new ReorderableList(childParts, typeof(CubismPart), false, false, false, false)
            {
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var element = childParts[index];
                    rect.height = EditorGUIUtility.singleLineHeight;
                    var labelRect = new Rect(rect.x, rect.y, rect.width * 0.2f, rect.height);
                    var objRect = new Rect(rect.x + labelRect.width, rect.y, rect.width * 0.8f, rect.height);
                    EditorGUI.LabelField(labelRect, $"{index}");
                    EditorGUI.ObjectField(objRect, childParts[index], typeof(CubismPart), true);
                }
            };

            int count = 0;
            ChildPartDrawables = new InternalTreeInfo(part, drawables, parts, ref count);
            DrawablesTotalCount = count;
        }

        private class InternalTreeInfo
        {
            public bool Foldout = true;
            public readonly CubismPart Part;
            public readonly CubismDrawable[] Drawables;

            public readonly InternalTreeInfo[] Childs;


            public InternalTreeInfo(CubismPart part, CubismDrawable[] srcDrawables, CubismPart[] srcParts, ref int drawableCount)
            {
                this.Part = part;
                Drawables = new CubismDrawable[0];
                for (var i = 0; i < srcDrawables.Length; i++)
                {
                    if (srcDrawables[i].UnmanagedParentIndex == part.UnmanagedIndex)
                    {
                        System.Array.Resize(ref Drawables, Drawables.Length + 1);
                        Drawables[Drawables.Length - 1] = srcDrawables[i];
                        drawableCount++;
                    }
                }

                Childs = new InternalTreeInfo[0];
                for (var i = 0; i < srcParts.Length; i++)
                {
                    if (srcParts[i].UnmanagedParentIndex == part.UnmanagedIndex)
                    {
                        System.Array.Resize(ref Childs, Childs.Length + 1);
                        Childs[Childs.Length - 1] = new InternalTreeInfo(srcParts[i], srcDrawables, srcParts, ref drawableCount);
                    }
                }
            }
        }
    }
}
