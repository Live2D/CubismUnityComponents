/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Singleton buffer for Cubism mask related draw commands.
    /// </summary>
    [ExecuteInEditMode]
    public sealed class CubismMaskCommandBuffer : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static CubismMaskCommandBuffer Singleton { get; set; }

        /// <summary>
        /// Draw command sources.
        /// </summary>
        private static List<ICubismMaskCommandSource> Sources { get; set; }

        /// <summary>
        /// Command buffer.
        /// </summary>
        private static CommandBuffer Buffer { get; set; }


        /// <summary>
        /// True if singleton is initialized.
        /// </summary>
        private static bool IsInitialized
        {
            get { return Singleton != null; }
        }

        /// <summary>
        /// True if <see cref="Sources"/> are empty.
        /// </summary>
        private static bool ContainsSources
        {
            get { return Sources != null && Sources.Count > 0; }
        }


        /// <summary>
        /// Makes sure class is initialized for static usage.
        /// </summary>
        private static void Initialize()
        {
            // Return early if already initialized.
            if (IsInitialized)
            {
                return;
            }


            // Create singleton.
            var proxy = new GameObject("cubism_MaskCommandBuffer")
            {
                hideFlags = HideFlags.HideAndDontSave
            };


            if (!Application.isEditor || Application.isPlaying)
            {
                DontDestroyOnLoad(proxy);
            }


            Singleton = proxy.AddComponent<CubismMaskCommandBuffer>();


            // Initialize containers.
            Sources = new List<ICubismMaskCommandSource>();
            Buffer = new CommandBuffer {
                name = "cubism_MaskCommandBuffer"
            };
        }


        /// <summary>
        /// Registers a new draw command source.
        /// </summary>
        /// <param name="source">Source to add.</param>
        internal static void AddSource(ICubismMaskCommandSource source)
        {
			// Make sure singleton is initialized.
            Initialize();


            // Prevent same source from being added twice.
            if (Sources.Contains(source))
            {
                return;
            }


            // Add source and force refresh.
            Sources.Add(source);
        }

        /// <summary>
        /// Deregisters a draw command source.
        /// </summary>
        /// <param name="source">Source to remove.</param>
        internal static void RemoveSource(ICubismMaskCommandSource source)
        {
			// Make sure singleton is initialized.
            Initialize();


            // Remove source and force refresh.
            Sources.RemoveAll(s => s == source);
        }


        /// <summary>
        /// Forces the command buffer to be refreshed.
        /// </summary>
        private static void RefreshCommandBuffer()
        {
            // Clear buffer.
            Buffer.Clear();


            // Enqueue sources.
            for (var i = 0; i < Sources.Count; ++i)
            {
                Sources[i].AddToCommandBuffer(Buffer);
            }
        }

        #region Unity Event Handling

        /// <summary>
        /// Executes <see cref="Buffer"/>.
        /// </summary>
        private void Update()
        {
            // Return if sources are empty empty or self isn't the singleton.
            if (this != Singleton)
            {
                return;
            }


			if (!ContainsSources)
			{
				return;
			}


            // Refresh and execute buffer.
			RefreshCommandBuffer();
            Graphics.ExecuteCommandBuffer(Buffer);
        }

        #endregion
    }
}
