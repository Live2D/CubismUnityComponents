/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Framework.MouthMovement
{
    /// <summary>
    /// Controls <see cref="CubismMouthParameter"/>s.
    /// </summary>
    public sealed class CubismMouthController : MonoBehaviour, ICubismUpdatable
    {
        /// <summary>
        /// The blend mode.
        /// </summary>
        [SerializeField]
        public CubismParameterBlendMode BlendMode = CubismParameterBlendMode.Multiply;


        /// <summary>
        /// The opening of the mouth.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        public float MouthOpening = 1f;

        /// <summary>
        /// It has voice input.
        /// </summary>
        public bool HasInput { get; set; }

        /// <summary>
        /// Apply to CubismMouthParameter or.
        /// </summary>
        [SerializeField]
        public bool CanOutput = true;

        /// <summary>
        /// Fade when switching between <see cref="HasInput"/> on / off
        /// </summary>
        [SerializeField]
        public bool HasFade = true;

        /// <summary>
        /// Mouth parameters.
        /// </summary>
        private CubismParameter[] Destinations { get; set; }

        /// <summary>
        /// Model has update controller component.
        /// </summary>
        [HideInInspector]
        public bool HasUpdateController { get; set; }



        /// <summary>
        /// Refreshes controller. Call this method after adding and/or removing <see cref="CubismMouthParameter"/>s.
        /// </summary>
        public void Refresh()
        {
            var model = this.FindCubismModel();


            // Fail silently...
            if (model == null)
            {
                return;
            }


            // Cache destinations.
            var tags = model
                .Parameters
                .GetComponentsMany<CubismMouthParameter>();


            Destinations = new CubismParameter[tags.Length];


            for (var i = 0; i < tags.Length; ++i)
            {
                Destinations[i] = tags[i].GetComponent<CubismParameter>();
            }

            // Get cubism update controller.
            HasUpdateController = (GetComponent<CubismUpdateController>() != null);
        }

        /// <summary>
        /// Called by cubism update controller. Order to invoke OnLateUpdate.
        /// </summary>
        public int ExecutionOrder
        {
            get { return CubismUpdateExecutionOrder.CubismMouthController; }
        }

        /// <summary>
        /// Called by cubism update controller. Needs to invoke OnLateUpdate on Editing.
        /// </summary>
        public bool NeedsUpdateOnEditing
        {
            get { return false; }
        }

        private float _fadeWeight = 0;

        /// <summary>
        /// Called by cubism update controller. Updates controller.
        /// </summary>
        /// <remarks>
        /// Make sure this method is called after any animations are evaluated.
        /// </remarks>
        public void OnLateUpdate()
        {
            if (!HasInput || !CanOutput)
            {
                if (0 < _fadeWeight)
                    _fadeWeight -= 0.1f;
                if (!HasFade || _fadeWeight < 0)
                    _fadeWeight = 0;
            }
            else
            {
                if (_fadeWeight < 1)
                    _fadeWeight += 0.05f;
                if (!HasFade || 1 < _fadeWeight)
                    _fadeWeight = 1;
            }

            // Fail silently.
            if (!enabled || Destinations == null || _fadeWeight <= 0)
                return;

            // Apply value.
            Destinations.BlendToValue(BlendMode, MouthOpening, _fadeWeight);
        }

        #region Unity Events Handling

        /// <summary>
        /// Called by Unity. Makes sure cache is initialized.
        /// </summary>
        private void Start()
        {
            // Initialize cache.
            Refresh();
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void LateUpdate()
        {
            if(!HasUpdateController)
            {
                OnLateUpdate();
            }
        }

        #endregion
    }
}
