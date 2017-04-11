/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Framework
{
    /// <summary>
    /// <see cref="CubismEyeBlinkParameter"/> controller.
    /// </summary>
    public sealed class CubismEyeBlinkController : MonoBehaviour
    {
        /// <summary>
        /// Blend mode.
        /// </summary>
        [SerializeField]
        public CubismParameterBlendMode BlendMode = CubismParameterBlendMode.Multiply;


        /// <summary>
        /// Opening of the eyes.
        /// </summary>
        [SerializeField, Range(0f, 1f)]
        public float EyeOpening = 1f;


        /// <summary>
        /// Eye blink parameters cache.
        /// </summary>
        private CubismParameter[] Destinations { get; set; }


        /// <summary>
        /// Refreshes controller. Call this method after adding and/or removing <see cref="CubismEyeBlinkParameter"/>s.
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
                .GetComponentsMany<CubismEyeBlinkParameter>();


            Destinations = new CubismParameter[tags.Length];


            for (var i = 0; i < tags.Length; ++i)
            {
                Destinations[i] = tags[i].GetComponent<CubismParameter>();
            }
        }

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity. Makes sure cache is initialized.
        /// </summary>
        private void Start()
        {
            // Initialize cache.
            Refresh();
        }


        /// <summary>
        /// Called by Unity. Updates controller.
        /// </summary>
        private void LateUpdate()
        {
            // Fail silently.
            if (Destinations == null)
            {
                return;
            }


            // Apply value.
            Destinations.BlendToValue(BlendMode, EyeOpening);
        }

        #endregion
    }
}
