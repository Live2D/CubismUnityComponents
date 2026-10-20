/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using UnityEngine;

namespace Live2D.Cubism.Framework.Breath
{
    public class CubismBreathParameter : MonoBehaviour
    {
        /// <summary>
        /// The offset of the sine wave.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Offset;

        /// <summary>
        /// The peak value of the sine wave.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Peak;

        /// <summary>
        /// The cycle of the sine wave.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Cycle;

        /// <summary>
        /// The weight of the sine wave.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Weight;
    }
}
