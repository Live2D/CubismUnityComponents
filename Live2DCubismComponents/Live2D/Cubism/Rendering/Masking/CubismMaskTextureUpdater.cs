/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Handles Cubism mask draw events.
    /// </summary>
    internal delegate void CubismDrawMasksNowHandler();


    /// <summary>
    /// Singleton that hooks into Unity events for triggering mask drawing events.
    /// </summary>
    [ExecuteInEditMode]
    internal class CubismMaskTextureUpdater : MonoBehaviour
    {
        #region Events

        /// <summary>
        /// Called on draw requested.
        /// </summary>
        public static event CubismDrawMasksNowHandler OnDrawNow
        {
            add
            {
                // Make sure singleton is spawned.
                SpawnSingleton();


                // Add delegate to list.
                if (Listeners == null)
                {
                    Listeners = new List<CubismDrawMasksNowHandler>();
                }


                // Make sure listener is unique.
                if (Listeners.Contains(value))
                {
                    return;
                }


                Listeners.Add(value);
            }
            remove { Listeners.Remove(value); }
        }

        #endregion

        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static CubismMaskTextureUpdater Singleton { get; set; }


        /// <summary>
        /// List of event subscribers.
        /// </summary>
        private static List<CubismDrawMasksNowHandler> Listeners { get; set; }


        /// <summary>
        /// Makes sure the singleton is available.
        /// </summary>
        private static void SpawnSingleton()
        {
            // Return early if singleton is already spawned.
            if (Singleton != null)
            {
                return;
            }


            // Spawn singleton.
            var proxy = new GameObject("__CubismMaskTextureUpdater")
            {
                hideFlags = HideFlags.HideAndDontSave
            };


            if (!Application.isEditor || Application.isPlaying)
            {
                DontDestroyOnLoad(proxy);
            }


            Singleton = proxy.AddComponent<CubismMaskTextureUpdater>();
        }

        #region Unity Event Handling

        /// <summary>
        /// Frame number last update was done.
        /// </summary>
        private int LastTick { get; set; }


        /// <summary>
        /// Triggers draw event.
        /// </summary>
        // INV  Is this the best timing for drawing masks?
        private void OnRenderObject()
        {
            // Return if noone listenes anyway...
            if (Listeners == null)
            {
                return;
            }


            // Return if already ticked this frame.
            if (LastTick == Time.frameCount && Application.isPlaying)
            {
                return;
            }


            LastTick = Time.frameCount;


            // Trigger event.
            for (var i = 0; i < Listeners.Count; ++i)
            {
                Listeners[i].Invoke();
            }
        }

        #endregion
    }
}
