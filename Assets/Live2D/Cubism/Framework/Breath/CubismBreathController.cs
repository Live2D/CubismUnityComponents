/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Utils;
using UnityEngine;

namespace Live2D.Cubism.Framework.Breath
{
    /// <summary>
    /// Controller for <see cref="CubismBreathParameter"/>s.
    /// </summary>
    public class CubismBreathController : MonoBehaviour, ICubismUpdatable
    {
        [SerializeField, HideInInspector]
        private CubismModel _model;

        public CubismModel Model
        {
            get
            {
                if (!_model)
                {
                    _model = this.FindCubismModel();
                }

                return _model;
            }
        }

        /// <summary>
        /// Sources of the breath controller.
        /// </summary>
        public CubismBreathParameter[] Sources { get; set; }

        /// <summary>
        /// Destinations of the breath controller.
        /// </summary>
        public CubismParameter[] Destinations { get; set; }

        public int ExecutionOrder
        {
            get
            {
                return CubismUpdateExecutionOrder.CubismBreathController;
            }
        }

        public bool NeedsUpdateOnEditing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Model has <see cref="CubismUpdateController"/> component.
        /// </summary>
        public bool HasUpdateController { get; set; }

        /// <summary>
        /// Current time of the breath controller.
        /// </summary>
        private float _currentTime;

        /// <summary>
        /// Refreshes the sources and destinations of the breath controller.
        /// </summary>
        public void Refresh()
        {
            if (!Model
                || Model.Parameters == null)
            {
                return;
            }

            HasUpdateController = Model.GetComponent<CubismUpdateController>() != null;

            Sources = Model
                .Parameters
                .GetComponentsMany<CubismBreathParameter>();

            if (Sources == null)
            {
                return;
            }

            Destinations = new CubismParameter[Sources.Length];

            for (var i = 0; i < Sources.Length; ++i)
            {
                Destinations[i] = Sources[i].GetComponent<CubismParameter>();
            }

            _currentTime = 0.0f;
        }

        /// <summary>
        /// Updates and applies the values of the breath controller.
        /// </summary>
        private void UpdateParameters()
        {
            if (!Model || Sources == null || Destinations == null)
            {
                return;
            }

            _currentTime += CubismTime.DeltaTime;

            var time = _currentTime * 2.0f * Mathf.PI;

            for (var i = 0; i < Sources.Length; ++i)
            {
                var source = Sources[i];
                var destination = Destinations[i];
                if (!source || !destination || source.Cycle <= 0.0f)
                {
                    // Fail silently if the source or destination is missing or the cycle is invalid.
                    continue;
                }

                // Calculate the value.
                var value = source.Offset + (source.Peak * Mathf.Sin(time / source.Cycle));

                // Add the value to the destination parameter.
                destination.AddToValue(value, source.Weight);
            }
        }

        public void OnLateUpdate()
        {
            if (!enabled)
            {
                // Do not update if the component is disabled.
                return;
            }

            UpdateParameters();
        }

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity when the script instance is being loaded.
        /// </summary>
        private void Start()
        {
            Refresh();
        }

        /// <summary>
        /// Called every frame, if the component is enabled.
        /// </summary>
        private void LateUpdate()
        {
            if (HasUpdateController)
            {
                return;
            }

            OnLateUpdate();
        }

        #endregion
    }
}
