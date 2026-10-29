/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Rendering;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace Live2D.Cubism.Editor.Inspectors
{
    /// <summary>
    /// Inspector for <see cref="Live2D.Cubism.Rendering.CubismRenderController"/>s.
    /// </summary>
    [CustomEditor(typeof(CubismRenderController))]
    internal sealed class CubismRenderControllerInspector : UnityEditor.Editor
    {
        private bool ShowSorting { get; set; }

        private bool ShowAdvanced { get; set; }

        private bool ShowDrawOrder { get; set; }

        /// <summary>
        /// Cached <see cref="SerializedObject"/>s of the renderers, used to read each draw object's
        /// sorting fields without exposing the renderer's private backing fields.
        /// </summary>
        private SerializedObject[] _rendererSerializedObjects;

        /// <summary>
        /// Reusable buffer of draw order entries so the display list is not reallocated every repaint.
        /// </summary>
        private readonly List<DrawOrderEntry> _drawOrderEntries = new List<DrawOrderEntry>();

        /// <summary>
        /// Scroll position of the draw order list.
        /// </summary>
        private Vector2 _drawOrderScrollPosition;

        /// <summary>
        /// Height in pixels of the scrollable draw order list.
        /// </summary>
        private const float DrawOrderListHeight = 200.0f;

        /// <summary>
        /// A single draw object's current draw order information shown in the inspector.
        /// </summary>
        private struct DrawOrderEntry
        {
            /// <summary>
            /// The draw object's name.
            /// </summary>
            public string Name;

            /// <summary>
            /// The draw object's type.
            /// </summary>
            public CubismModelTypes.DrawObjectType Type;

            /// <summary>
            /// Contribution written to <see cref="MeshRenderer.sortingOrder"/>
            /// (relative to the controller's base sorting order).
            /// Order modes: <c>RenderOrder + LocalSortingOrder + ParentLocalSortingOrder</c>.
            /// Depth modes: <c>LocalSortingOrder + ParentLocalSortingOrder</c> only.
            /// </summary>
            public int OrderKey;

            /// <summary>
            /// Model render order. Used as the sole/combined order key in order modes,
            /// and as the Z-depth secondary key in depth modes.
            /// </summary>
            public int RenderOrder;

            /// <summary>
            /// The draw object's renderer.
            /// </summary>
            public CubismRenderer Renderer;
        }

        #region Editor

        /// <summary>
        /// Draws the inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var controller = target as CubismRenderController;


            // Fail silently.
            if (controller == null)
            {
                return;
            }


            // Show settings.
            EditorGUI.BeginChangeCheck();

            controller.Opacity = EditorGUILayout.Slider("Opacity", controller.Opacity, 0f, 1f);
            controller.MultiplyColorEnabled = EditorGUILayout.Toggle("MultiplyColorEnabled", controller.MultiplyColorEnabled);
            controller.ScreenColorEnabled = EditorGUILayout.Toggle("ScreenColorEnabled", controller.ScreenColorEnabled);


            ShowSorting = EditorGUILayout.Foldout(ShowSorting, "Sorting", EditorStyles.boldFont);

            if (ShowSorting)
            {
                controller.GroupedSortingIndex = EditorGUILayout.IntField("Grouped Sorting Index", controller.GroupedSortingIndex);
                controller.SortingLayer = EditorGUILayout.TextField("Layer", controller.SortingLayer);
                controller.SortingOrder = EditorGUILayout.IntField("Order In Group", controller.SortingOrder);
                controller.SortingMode = (CubismSortingMode)EditorGUILayout.EnumPopup("Mode", (Enum)controller.SortingMode);
            }


            ShowAdvanced = EditorGUILayout.Foldout(ShowAdvanced, "Advanced", EditorStyles.boldFont);

            if (ShowAdvanced)
            {
                controller.CameraToFace = EditorGUILayout.ObjectField("Camera To Face", controller.CameraToFace, typeof(Camera), true) as Camera;
                controller.OpacityHandler = EditorGUILayout.ObjectField("Opacity Handler", controller.OpacityHandler, typeof(object), true);
                controller.DrawOrderHandler = EditorGUILayout.ObjectField("Draw Order Handler", controller.DrawOrderHandler, typeof(object), true);
                controller.MultiplyColorHandler = EditorGUILayout.ObjectField("Multiply Color Handler", controller.MultiplyColorHandler, typeof(object), true);
                controller.ScreenColorHandler = EditorGUILayout.ObjectField("Screen Color Handler", controller.ScreenColorHandler, typeof(object), true);

                if (controller.SortingMode.SortByDepth())
                {
                    controller.DepthOffset = EditorGUILayout.FloatField("Depth Offset", controller.DepthOffset);
                }
            }


            // Save any changes.
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(controller);


                foreach (var renderer in controller.Renderers)
                {
                    // The model-wide override toggles change what every renderer's blend color resolves to.
                    // The render path reads the displayed draw state (only advanced when the model is stepped),
                    // so sync it here to reflect the change immediately in the editor.
                    if (renderer)
                    {
                        renderer.ApplyDisplayedBlendColorsImmediate();
                    }

                    EditorUtility.SetDirty(renderer);
                    // HACK Get mesh renderer directly.
                    EditorUtility.SetDirty(renderer.MeshRenderer);
                }

                SceneView.RepaintAll();
            }


            // Show the current draw order of each draw object.
            // This is a read-only display and is intentionally kept outside the change check above.
            ShowDrawOrder = EditorGUILayout.Foldout(ShowDrawOrder, "Draw Order", EditorStyles.boldFont);

            if (ShowDrawOrder)
            {
                DrawDrawOrder(controller);
            }
        }

        /// <summary>
        /// Keeps the draw order display up to date while the foldout is open,
        /// as render orders change during playback and editing.
        /// </summary>
        public override bool RequiresConstantRepaint()
        {
            return ShowDrawOrder;
        }

        /// <summary>
        /// Draws the current draw order of every draw object (<see cref="CubismModelTypes.DrawObjectType.Drawable"/>
        /// and <see cref="CubismModelTypes.DrawObjectType.Offscreen"/>) sorted from the one drawn first to the one drawn last.
        /// Matches <see cref="CubismRenderer"/>.<c>ApplySorting</c>: order modes use a single
        /// <c>RenderOrder + Local + Parent</c> key (negated for <see cref="CubismSortingMode.FrontToBackOrder"/>).
        /// Depth modes use Local+Parent for <see cref="MeshRenderer.sortingOrder"/> and RenderOrder for Z.
        /// </summary>
        /// <param name="controller">The inspected render controller.</param>
        private void DrawDrawOrder(CubismRenderController controller)
        {
            var renderers = controller.Renderers;


            if (renderers == null || renderers.Length == 0)
            {
                EditorGUILayout.HelpBox("No draw objects are available.", MessageType.Info);
                return;
            }


            // Grow/shrink the serialized object cache to match the renderer set.
            if (_rendererSerializedObjects == null || _rendererSerializedObjects.Length != renderers.Length)
            {
                _rendererSerializedObjects = new SerializedObject[renderers.Length];
            }


            var sortByOrder = controller.SortingMode.SortByOrder();


            // Collect sorting fields of each draw object (mirrors CubismRenderer.ApplySorting).
            _drawOrderEntries.Clear();

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                // (Re)create the cached serialized object when missing or pointing at a stale renderer.
                var serializedRenderer = _rendererSerializedObjects[i];

                if (serializedRenderer == null || serializedRenderer.targetObject != renderer)
                {
                    serializedRenderer = new SerializedObject(renderer);
                    _rendererSerializedObjects[i] = serializedRenderer;
                }

                // Pull the latest serialized values so runtime changes are reflected.
                serializedRenderer.Update();

                var renderOrder = serializedRenderer.FindProperty("_renderOrder")?.intValue ?? 0;
                var localSortingOrder = serializedRenderer.FindProperty("_localSortingOrder")?.intValue ?? 0;
                var parentLocalSortingOrder = serializedRenderer.FindProperty("_parentLocalSortingOrder")?.intValue ?? 0;
                var localOrderKey = localSortingOrder + parentLocalSortingOrder;

                _drawOrderEntries.Add(new DrawOrderEntry
                {
                    Name = renderer.name,
                    Type = renderer.DrawObjectType,
                    // Order modes fold RenderOrder into sortingOrder. Depth modes keep it on Z only.
                    OrderKey = sortByOrder
                        ? renderOrder + localOrderKey
                        : localOrderKey,
                    RenderOrder = renderOrder,
                    Renderer = renderer
                });
            }


            // Sort from first drawn (behind) to last drawn (front).
            // Order: MeshRenderer.sortingOrder uses ±OrderKey (FrontToBackOrder negates).
            // Depth: sortingOrder uses OrderKey (Local+Parent). RenderOrder is the Z secondary key
            // (BackToFrontZ: lower RenderOrder behind, FrontToBackZ: higher RenderOrder behind).
            var frontToBackOrder = controller.SortingMode == CubismSortingMode.FrontToBackOrder;
            var frontToBackZ = controller.SortingMode == CubismSortingMode.FrontToBackZ;

            _drawOrderEntries.Sort((a, b) =>
            {
                var orderComparison = frontToBackOrder
                    ? b.OrderKey.CompareTo(a.OrderKey)
                    : a.OrderKey.CompareTo(b.OrderKey);

                if (orderComparison != 0 || sortByOrder)
                {
                    return orderComparison;
                }

                return frontToBackZ
                    ? b.RenderOrder.CompareTo(a.RenderOrder)
                    : a.RenderOrder.CompareTo(b.RenderOrder);
            });


            EditorGUILayout.LabelField($"Draw Objects: {_drawOrderEntries.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Header row.
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Sorting Order", EditorStyles.miniBoldLabel, GUILayout.Width(80.0f));
                EditorGUILayout.LabelField("Type", EditorStyles.miniBoldLabel, GUILayout.Width(70.0f));
                EditorGUILayout.LabelField("Name", EditorStyles.miniBoldLabel, GUILayout.Width(200.0f));
                EditorGUILayout.LabelField("Component", EditorStyles.miniBoldLabel);
            }

            // Data rows.
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_drawOrderScrollPosition, GUILayout.Height(DrawOrderListHeight)))
            {
                _drawOrderScrollPosition = scrollView.scrollPosition;
                for (var i = 0; i < _drawOrderEntries.Count; i++)
                {
                    var entry = _drawOrderEntries[i];
                    var orderLabel = sortByOrder
                        ? entry.OrderKey.ToString()
                        : $"{entry.OrderKey} / {entry.RenderOrder}";

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(orderLabel, GUILayout.Width(80.0f));
                        EditorGUILayout.LabelField(entry.Type.ToString(), GUILayout.Width(70.0f));
                        EditorGUILayout.LabelField(entry.Name, GUILayout.Width(200.0f));
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.ObjectField(entry.Renderer, typeof(Component), true);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
