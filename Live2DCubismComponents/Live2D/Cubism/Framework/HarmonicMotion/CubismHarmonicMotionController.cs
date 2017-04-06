/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Framework.HarmonicMotion
{
    /// <summary>
    /// Controller for <see cref="CubismHarmonicMotionParameter"/>s.
    /// </summary>
    public sealed class CubismHarmonicMotionController : MonoBehaviour
    {
        /// <summary>
        /// Default number of channels.
        /// </summary>
        private const int DefaultChannelCount = 1;


        /// <summary>
        /// Blend mode.
        /// </summary>
        [SerializeField]
        public CubismParameterBlendMode BlendMode = CubismParameterBlendMode.Additive;


        /// <summary>
        /// The timescales for each channel.
        /// </summary>
        [SerializeField]
        public float[] ChannelTimescales;


        /// <summary>
        /// Sources.
        /// </summary>
        private CubismHarmonicMotionParameter[] Sources { get; set; }

        /// <summary>
        /// Destinations.
        /// </summary>
        private CubismParameter[] Destinations { get; set; }


        /// <summary>
        /// Refreshes the controller. Call this method after adding and/or removing <see cref="CubismHarmonicMotionParameter"/>.
        /// </summary>
        public void Refresh()
        {
            var model = this.FindCubismModel();


            // Catch sources and destinations.
            Sources = model
                .Parameters
                .GetComponentsMany<CubismHarmonicMotionParameter>();
            Destinations = new CubismParameter[Sources.Length];


            for (var i = 0; i < Sources.Length; ++i)
            {
                Destinations[i] = Sources[i].GetComponent<CubismParameter>();
            }
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
        /// Called by Unity. Updates controller.
        /// </summary>
        private void LateUpdate()
        {
            // Return early in case there's nothing to update.
            if (Sources == null)
            {
                return;
            }


            // Update sources and destinations.
            for (var i = 0; i < Sources.Length; ++i)
            {
                Sources[i].Play(ChannelTimescales);


                Destinations[i].BlendToValue(BlendMode, Sources[i].Evaluate());
            }
        }


        /// <summary>
        /// Called by Unity. Resets channels.
        /// </summary>
        private void Reset()
        {
            // Reset/Initialize channel timescales.
            ChannelTimescales = new float[DefaultChannelCount];


            for (var s = 0; s < DefaultChannelCount; ++s)
            {
                ChannelTimescales[s] = 1f;
            }
        }

        #endregion
    }
}
