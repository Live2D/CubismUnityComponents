/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using System;
using System.Linq;
using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Rendering
{
    [ExecuteInEditMode, RequireComponent(typeof(CubismPart))]
    public class CubismPartColorsEditor : MonoBehaviour
    {
        /// <summary>
        /// RenderController.
        /// </summary>
        private CubismRenderController _renderController;

        /// <summary>
        /// Renderer Array.
        /// </summary>
        private CubismRenderer[] _renderers;

        /// <summary>
        /// Part Array.
        /// </summary>
        private CubismPart _part;

        /// <summary>
        /// <see cref="ChildDrawableRenderers"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismRenderer[] _childDrawableRenderers;

        /// <summary>
        /// Array of art meshes with parts as parents.
        /// </summary>
        public CubismRenderer[] ChildDrawableRenderers
        {
            get { return _childDrawableRenderers; }
            set { _childDrawableRenderers = value; }
        }

        /// <summary>
        /// <see cref="ChildParts"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismPartColorsEditor[] _childParts;

        /// <summary>
        /// Array of own child parts.
        /// </summary>
        public CubismPartColorsEditor[] ChildParts
        {
            get { return _childParts; }
            set { _childParts = value; }
        }

        /// <summary>
        /// <see cref="OverrideColorForPartMultiplyColors"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _isOverriddenPartMultiplyColors;

        /// <summary>
        /// Whether to override with multiply color from the model.
        ///
        /// This property is deprecated due to a naming change. Use <see cref="OverrideColorForPartMultiplyColors"/> instead.
        /// </summary>
        public bool OverwriteColorForPartMultiplyColors
        {
            get { return OverrideColorForPartMultiplyColors; }
            set { OverrideColorForPartMultiplyColors = value; }
        }

        /// <summary>
        /// Whether to override with multiply color from the model.
        /// </summary>
        public bool OverrideColorForPartMultiplyColors
        {
            get { return _isOverriddenPartMultiplyColors; }
            set {
                _isOverriddenPartMultiplyColors = value;
                for (int i = 0; i < ChildDrawableRenderers.Length; i++)
                {
                    ChildDrawableRenderers[i].OverrideFlagForDrawableMultiplyColors = OverrideColorForPartMultiplyColors;
                    ChildDrawableRenderers[i].LastMultiplyColor = OverrideColorForPartMultiplyColors ? MultiplyColor : ChildDrawableRenderers[i].LastMultiplyColor;
                    ChildDrawableRenderers[i].MultiplyColor = OverrideColorForPartMultiplyColors ? MultiplyColor : ChildDrawableRenderers[i].MultiplyColor;
                    ChildDrawableRenderers[i].ApplyMultiplyColor();
                }
                for (int i = 0; i < ChildParts.Length; i++)
                {
                    ChildParts[i].OverrideColorForPartMultiplyColors = OverrideColorForPartMultiplyColors;
                    ChildParts[i].MultiplyColor = MultiplyColor;
                }
            }
        }

        /// <summary>
        /// <see cref="OverrideColorForPartScreenColors"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _isOverriddenPartScreenColors;

        /// <summary>
        /// Whether to override with screen color from the model.
        ///
        /// This property is deprecated due to a naming change. Use <see cref="OverrideColorForPartScreenColors"/> instead.
        /// </summary>
        public bool OverwriteColorForPartScreenColors
        {
            get { return OverrideColorForPartScreenColors; }
            set { OverrideColorForPartScreenColors = value;}
        }

        /// <summary>
        /// Whether to override with screen color from the model.
        /// </summary>
        public bool OverrideColorForPartScreenColors
        {
            get { return _isOverriddenPartScreenColors; }
            set {
                _isOverriddenPartScreenColors = value;
                for (int i = 0; i < ChildDrawableRenderers.Length; i++)
                {
                    ChildDrawableRenderers[i].OverrideFlagForDrawableScreenColors = OverrideColorForPartScreenColors;
                    ChildDrawableRenderers[i].LastScreenColor = OverrideColorForPartScreenColors ? ScreenColor : ChildDrawableRenderers[i].LastScreenColor;
                    ChildDrawableRenderers[i].ScreenColor = OverrideColorForPartScreenColors ? ScreenColor : ChildDrawableRenderers[i].ScreenColor;
                    ChildDrawableRenderers[i].ApplyScreenColor();
                }
                for (int i = 0; i < ChildParts.Length; i++)
                {
                    ChildParts[i].OverrideColorForPartScreenColors = OverrideColorForPartScreenColors;
                    ChildParts[i].ScreenColor = ScreenColor;
                }
            }
        }

        /// <summary>
        /// <see cref="MultiplyColor"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Color _multiplyColor = Color.white;

        /// <summary>
        /// Multiply color.
        /// </summary>
        public Color MultiplyColor
        {
            get
            {
                return _multiplyColor;
            }
            set
            {
                // 同じ値が与えられた場合、早めに返す
                if (value == _multiplyColor)
                {
                    return;
                }

                // 値を保存
                _multiplyColor = value;

                for (int i = 0; i < ChildDrawableRenderers.Length; i++)
                {
                    ChildDrawableRenderers[i].MultiplyColor = OverrideColorForPartMultiplyColors ? MultiplyColor : ChildDrawableRenderers[i].MultiplyColor;
                }
                for (int i = 0; i < ChildParts.Length; i++)
                {
                    ChildParts[i].MultiplyColor = OverrideColorForPartMultiplyColors ? MultiplyColor : ChildParts[i].MultiplyColor;
                }
            }
        }

        /// <summary>
        /// <see cref="ScreenColor"/>s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private Color _screenColor = Color.clear;

        /// <summary>
        /// Screen color.
        /// </summary>
        public Color ScreenColor
        {
            get
            {
                return _screenColor;
            }
            set
            {
                // 同じ値が与えられた場合、早めに返す
                if (value == _screenColor)
                {
                    return;
                }

                // 値を保存
                _screenColor = value;

                for (int i = 0; i < ChildDrawableRenderers.Length; i++)
                {
                    ChildDrawableRenderers[i].ScreenColor = OverrideColorForPartScreenColors ? ScreenColor : ChildDrawableRenderers[i].ScreenColor;
                }
                for (int i = 0; i < ChildParts.Length; i++)
                {
                    ChildParts[i].ScreenColor = OverrideColorForPartMultiplyColors ? ScreenColor : ChildParts[i].ScreenColor;
                }
            }
        }

        [Obsolete]
        public void TryInitialize(CubismRenderController cubismRenderController, CubismPart part, CubismDrawable[] drawables)
        {
            TryInitialize(gameObject.FindCubismModel(true));
        }

        public void TryInitialize(CubismModel model)
        {
            var drawables = model.Drawables;

            // Initialize.
            _renderController = model.GetComponent<CubismRenderController>();
            _renderers = _renderController.Renderers;
            _part = _part = GetComponent<CubismPart>();

            // Initialize elements.
            ChildDrawableRenderers = Array.Empty<CubismRenderer>();

            for (var i = 0; i < _renderers.Length; i++)
            {
                // When this object is the parent part.
                if (drawables[i].ParentPartIndex == _part.UnmanagedIndex)
                {
                    // Register the corresponding renderers in the dictionary.
                    Array.Resize(ref _childDrawableRenderers, _childDrawableRenderers.Length + 1);
                    ChildDrawableRenderers[ChildDrawableRenderers.Length - 1] = _renderers[i];
                }
            }

            _childParts = Array.Empty<CubismPartColorsEditor>();
            foreach (var part in model.Parts.Where((e) => e.UnmanagedParentIndex == _part.UnmanagedIndex))
            {
                Array.Resize(ref _childParts, _childParts.Length + 1);
                var colorsEditor = part.GetComponent<CubismPartColorsEditor>();
                _childParts[_childParts.Length - 1] = colorsEditor;
            }
        }

#region Unity Events

        private void OnEnable()
        {
            // Early return.
            if (ChildDrawableRenderers != null && !ChildDrawableRenderers.Contains(null))
            {
                return;
            }

            // Initialize.
            var model = gameObject.FindCubismModel(true);
            TryInitialize(model);
        }

#endregion
    }
}
