/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using Live2D.Cubism.Framework.MouthMovement;
using Live2D.Cubism.Framework.Pose;
using Live2D.Cubism.Framework.HarmonicMotion;
using Live2D.Cubism.Framework.LookAt;
using Live2D.Cubism.Rendering;
using Live2D.Cubism.Rendering.Masking;
using System;
using UnityEngine;


namespace Live2D.Cubism.Framework
{
    [ExecuteInEditMode]
    public class CubismUpdateController : MonoBehaviour
    {
        /// <summary>
        /// The action of cubism component late update.
        /// </summary>
        private Action OnLateUpdate;

        /// <summary>
        /// The paremeter store cache.
        /// </summary>
        private CubismParameterStore _parameterStore;

        /// <summary>
        /// Refresh delegate manager.
        /// </summary>
        public void Refresh()
        {
            var model = this.FindCubismModel();

            // Fail silently...
            if (model == null)
            {
                return;
            }

            // Clear delegate.
            Delegate.RemoveAll(OnLateUpdate, null);

            ICubismUpdatable renderController = null;
            ICubismUpdatable maskController = null;
            ICubismUpdatable fadeController = null;
            ICubismUpdatable poseController = null;
            ICubismUpdatable expressionController = null;
            ICubismUpdatable eyeBlinkController = null;
            ICubismUpdatable mouthController = null;
            ICubismUpdatable harmonicMotionController = null;
            ICubismUpdatable lookController = null;

            // Find cubism components.
            var components = model.GetComponents<ICubismUpdatable>();
            foreach(var component in components)
            {
                if (component.GetType() == typeof(CubismRenderController))
                {
                    renderController = component;
                }
                else if (component.GetType() == typeof(CubismMaskController))
                {
                    maskController = component;
                }
#if UNITY_EDITOR
                else if (!Application.isPlaying)
                {
                    continue;
                }
#endif
                else if (component.GetType() == typeof(CubismFadeController))
                {
                    fadeController = component;
                }
                else if (component.GetType() == typeof(CubismPoseController))
                {
                    poseController = component;
                }
                else if (component.GetType() == typeof(CubismExpressionController))
                {
                    expressionController = component;
                }
                else if (component.GetType() == typeof(CubismEyeBlinkController))
                {
                    eyeBlinkController = component;
                }
                else if (component.GetType() == typeof(CubismMouthController))
                {
                    mouthController = component;
                }
                else if (component.GetType() == typeof(CubismHarmonicMotionController))
                {
                    harmonicMotionController = component;
                }
                else if(component.GetType() == typeof(CubismLookController))
                {
                    lookController = component;
                }
            }

#if UNITY_EDITOR
            // Application is playing.
            if(Application.isPlaying)
            {
#endif
                // Cache parameter save restore.
                _parameterStore = model.GetComponent<CubismParameterStore>();

                // Add fade controller late update.
                if (fadeController != null)
                {
                    OnLateUpdate += fadeController.OnLateUpdate;
                }

                // Add pose controller late update.
                if (poseController != null)
                {
                    OnLateUpdate += poseController.OnLateUpdate;
                }

                // Add expression controller late update.
                if (expressionController != null)
                {
                    OnLateUpdate += expressionController.OnLateUpdate;
                }

                // Add eye blink controller late update.
                if (eyeBlinkController != null)
                {
                    OnLateUpdate += eyeBlinkController.OnLateUpdate;
                }

                // Add mouth controller late update.
                if (mouthController != null)
                {
                    OnLateUpdate += mouthController.OnLateUpdate;
                }

                // Add harmonic motion controller late update.
                if (harmonicMotionController != null)
                {
                    OnLateUpdate += harmonicMotionController.OnLateUpdate;
                }

                // Add look controller late update.
                if (lookController != null)
                {
                    OnLateUpdate += lookController.OnLateUpdate;
                }
#if UNITY_EDITOR
            }
#endif

            // Add render late update.
            if (renderController != null)
            {
                OnLateUpdate += renderController.OnLateUpdate;
            }

            // Add mask controller late update.
            if (maskController != null)
            {
                OnLateUpdate += maskController.OnLateUpdate;
            }
        }


        #region Unity Event Handling

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void Start()
        {
            Refresh();
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void LateUpdate()
        {
            // Save model parameters value and parts opacity
            if (_parameterStore != null)
            {
                _parameterStore.SaveParameters();
            }

            // Cubism late update.
            if(OnLateUpdate != null)
            {
                OnLateUpdate();
            }
        }

        #endregion
    }
}

