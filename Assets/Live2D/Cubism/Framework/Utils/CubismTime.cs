/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;

namespace Live2D.Cubism.Framework.Utils
{
    /// <summary>
    /// Manages delta time for Cubism operations to allow simulation with fixed frame rates.
    /// </summary>
    public static class CubismTime
    {
        /// <summary>
        /// Target fixed frame rate. If set to -1, standard <see cref="Time.deltaTime"/> is used.
        /// </summary>
        public static int FixFrameRate = -1;

        /// <summary>
        /// Gets the delta time based on the current configuration.
        /// </summary>
        public static float DeltaTime
        {
            get
            {
                if (FixFrameRate > 0)
                {
                    return (1.0f / FixFrameRate) * Time.timeScale;
                }

                return Time.deltaTime;
            }
        }
    }
}
