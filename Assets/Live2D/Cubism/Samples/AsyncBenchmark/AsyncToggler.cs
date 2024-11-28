/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.Tasking;
using UnityEngine;


namespace Live2D.Cubism.Samples.AsyncBenchmark
{
    /// <summary>
    /// Shows how to enable the <see cref="CubismBuiltinAsyncTaskHandler"/> from script.
    /// </summary>
    public sealed class AsyncToggler : MonoBehaviour
    {
#if !UNITY_WEBGL
        /// <summary>
        /// Controls async task handling.
        /// </summary>
        public bool EnableAsync = true;

        /// <summary>
        /// Last <see cref="EnableAsync"/> state.
        /// </summary>
        private bool LastEnableSync { get; set; }
#endif

        #region Unity Event Handling

#if UNITY_WEBGL
        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void Start()
        {
            // Deactivate Async.
            CubismBuiltinAsyncTaskHandler.Deactivate();
        }
#else
        /// <summary>
        /// Called by Unity. Enables/Disables async task handler.
        /// </summary>
        private void Update()
        {
            if (EnableAsync == LastEnableSync)
            {
                return;
            }


            if (EnableAsync)
            {
                CubismBuiltinAsyncTaskHandler.Activate();
            }
            else
            {
                CubismBuiltinAsyncTaskHandler.Deactivate();
            }


            LastEnableSync = EnableAsync;
        }


        /// <summary>
        /// Called by Unity. Disables async task handler.
        /// </summary>
        private void OnDestroy()
        {
            EnableAsync = false;


            Update();
        }
#endif

            #endregion
        }
    }
