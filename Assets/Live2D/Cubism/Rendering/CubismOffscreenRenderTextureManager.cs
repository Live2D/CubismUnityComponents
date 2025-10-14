/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using UnityEngine;
using UnityEngine.Rendering;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Class for managing offscreen render textures.
    /// </summary>
    public class CubismOffscreenRenderTextureManager
    {
        /// <summary>
        /// Tag of the GameObject that holds <see cref="CubismCommonRenderTextureController"/> script.
        /// </summary>
        private static readonly string RenderTextureControllerName = "CubismRenderTextureController";

        /// <summary>
        /// Default number of offscreen render textures.
        /// </summary>
        private static readonly int OffscreenRenderTextureDefaultCount = 0;

        /// <summary>
        /// instance of the <see cref="CubismOffscreenRenderTextureManager"/>.
        /// </summary>
        private static CubismOffscreenRenderTextureManager Instance;

        public static CubismOffscreenRenderTextureManager GetInstance()
        {
            if (Instance == null)
            {
                // Initialize the singleton instance.
                Instance = new CubismOffscreenRenderTextureManager();

                if (Application.isPlaying)
                {
                    // Check if the CubismRenderTextureController is already instantiated.
                    Instance._isRenderTextureControllerInstantiated = GameObject.Find(RenderTextureControllerName) != null;
                }
            }

            return Instance;
        }

        /// <summary>
        /// Container for render texture and its usage status.
        /// </summary>
        private struct RenderTextureContainer
        {
            /// <summary>
            /// Render texture.
            /// </summary>
            public RenderTexture RenderTexture;

            /// <summary>
            /// Is in use.
            /// </summary>
            public bool InUse;
        }

        /// <summary>
        /// Render texture containers.
        /// </summary>
        private RenderTextureContainer[] _offscreenRenderTextureContainers;

        /// <summary>
        /// Previous frame active render texture count.
        /// </summary>
        private int _previousActiveRenderTextureCount;

        /// <summary>
        /// Current active render texture count.
        /// </summary>
        private int _currentActiveRenderTextureCount;

        /// <summary>
        /// CubismRenderTextureController already instantiated?
        /// </summary>
        private bool _isRenderTextureControllerInstantiated;

        /// <summary>
        /// <see cref="HasResetThisFrame"/>'s backing field.
        /// </summary>
        private bool _hasResetThisFrame;

        /// <summary>
        /// Did reset _previousActiveRenderTextureCount this frame?
        /// </summary>
        public bool HasResetThisFrame
        {
            get
            {
                return _hasResetThisFrame;
            }

            set
            {
                _hasResetThisFrame = value;
            }
        }

        /// <summary>
        /// Initialize the offscreen render texture manager.
        /// </summary>
        /// <param name="width">Initialize width.</param>
        /// <param name="height">Initialize height</param>
        private void Initialize(int width, int height)
        {
            if (Application.isPlaying && !_isRenderTextureControllerInstantiated)
            {
                var prefab = Resources.Load<GameObject>($"Live2D/Cubism/Prefabs/{RenderTextureControllerName}");

                if (prefab != null)
                {
                    // Instantiate the controller GameObject from the prefab.
                    var instance = GameObject.Instantiate(prefab);
                    if (instance != null)
                    {
                        instance.name = RenderTextureControllerName;
                    }

                    GameObject.DontDestroyOnLoad(instance);
                }

                _isRenderTextureControllerInstantiated = true;
            }

            if (width < 1 || height < 1)
            {
                width = Screen.width;
                height = Screen.height;
            }

            // Release existing render textures.
            if (_offscreenRenderTextureContainers != null)
            {
                for (var i = 0; i < _offscreenRenderTextureContainers.Length; ++i)
                {
                    _offscreenRenderTextureContainers[i].RenderTexture.Release();
                }
            }

            // Create default number of render textures.
            _offscreenRenderTextureContainers = new RenderTextureContainer[OffscreenRenderTextureDefaultCount];
            for (var i = 0; i < OffscreenRenderTextureDefaultCount; ++i)
            {
                _offscreenRenderTextureContainers[i] = new RenderTextureContainer
                {
                    RenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32),
                    InUse = false
                };
                _offscreenRenderTextureContainers[i].RenderTexture.Create();
            }

            _previousActiveRenderTextureCount = 0;
            _currentActiveRenderTextureCount = 0;
            _hasResetThisFrame = false;
        }

        /// <summary>
        /// Reset the previous active render texture count at the beginning of each frame.
        /// This should be called once per frame before any CubismRenderController OnLateUpdate.
        /// </summary>
        public void ResetPreviousActiveCount()
        {
            if (!_hasResetThisFrame)
            {
                _previousActiveRenderTextureCount = 0;
                _hasResetThisFrame = true;
            }
        }

        /// <summary>
        /// Clear all offscreen render textures.
        /// </summary>
        /// <param name="commandBuffer">Command buffer.</param>
        public void ClearRenderTextures(CommandBuffer commandBuffer)
        {
            for (var i = 0; i < _offscreenRenderTextureContainers?.Length; i++)
            {
                if (!_offscreenRenderTextureContainers[i].RenderTexture)
                {
                    continue;
                }

                commandBuffer.SetRenderTarget(_offscreenRenderTextureContainers[i].RenderTexture);
                commandBuffer.ClearRenderTarget(true, true, Color.clear);
            }
        }

        /// <summary>
        /// Get an offscreen render texture.
        /// </summary>
        /// <param name="width">Require width.</param>
        /// <param name="height">Require height.</param>
        /// <returns></returns>
        public RenderTexture GetOffscreenRenderTexture(int width, int height)
        {
            // If width or height is less than 1, use screen size.
            if (width < 1 || height < 1)
            {
                width = CubismCommonRenderFrameBuffer.GetInstance().Size.Width;
                height = CubismCommonRenderFrameBuffer.GetInstance().Size.Height;
            }

            // Initialize if not yet.
            if (_offscreenRenderTextureContainers == null)
            {
                Initialize(width, height);
            }

            _currentActiveRenderTextureCount++;

            // Update the maximum count for this frame
            _previousActiveRenderTextureCount = _currentActiveRenderTextureCount > _previousActiveRenderTextureCount
            ? _currentActiveRenderTextureCount
            : _previousActiveRenderTextureCount;

            // Search for an unused render texture.
            for (var i = 0; i < _offscreenRenderTextureContainers?.Length; ++i)
            {
                if (_offscreenRenderTextureContainers[i].InUse
                    || !_offscreenRenderTextureContainers[i].RenderTexture)
                {
                    continue;
                }

                // Resize if the size is different.
                if (_offscreenRenderTextureContainers[i].RenderTexture.width != width ||
                    _offscreenRenderTextureContainers[i].RenderTexture.height != height)
                {
                    _offscreenRenderTextureContainers[i].RenderTexture.Release();
                    _offscreenRenderTextureContainers[i].RenderTexture.width = width;
                    _offscreenRenderTextureContainers[i].RenderTexture.height = height;
                    _offscreenRenderTextureContainers[i].RenderTexture.Create();
                }
                _offscreenRenderTextureContainers[i].InUse = true;

                // Return the found render texture.
                return _offscreenRenderTextureContainers[i].RenderTexture;
            }

            // If no unused render texture is found, create a new one.
            return CreateContainer(width, height).RenderTexture;
        }

        /// <summary>
        /// Create a new render texture container.
        /// </summary>
        /// <param name="width">Width of the render texture.</param>
        /// <param name="height">Height of the render texture.</param>
        /// <returns>The new render texture container.</returns>
        private RenderTextureContainer CreateContainer(int width, int height)
        {
            Array.Resize(ref _offscreenRenderTextureContainers, _offscreenRenderTextureContainers.Length + 1);
            _offscreenRenderTextureContainers[^1] = new RenderTextureContainer
            {
                RenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32),
                InUse = true
            };

            _offscreenRenderTextureContainers[^1].RenderTexture.Create();

            return _offscreenRenderTextureContainers[^1];
        }

        /// <summary>
        /// End use the specified render texture.
        /// </summary>
        /// <param name="renderController">CubismRenderController.</param>
        /// <param name="renderTexture">Use RenderTexture.</param>
        public void StopUsingRenderTexture(CubismRenderController renderController, RenderTexture renderTexture)
        {
            // If not initialized, do nothing.
            if (_offscreenRenderTextureContainers == null
                || !renderController)
            {
                return;
            }

            // Search for the specified render texture.
            for (var i = 0; i < _offscreenRenderTextureContainers.Length; ++i)
            {
                if (_offscreenRenderTextureContainers[i].RenderTexture != renderTexture
                    || !_offscreenRenderTextureContainers[i].RenderTexture)
                {
                    continue;
                }

                // Mark as not in use.
                _offscreenRenderTextureContainers[i].InUse = false;

                // Clear the render texture.
                renderController.CommandBuffer.SetRenderTarget(_offscreenRenderTextureContainers[i].RenderTexture);
                renderController.CommandBuffer.ClearRenderTarget(true, true, Color.clear);

                // Back to the current frame buffer.
                renderController.CommandBuffer.SetRenderTarget(renderController.CurrentFrameBuffer);

                _currentActiveRenderTextureCount--;
                break;
            }
        }

        /// <summary>
        /// End use all render textures.
        /// </summary>
        public void StopUsingAllRenderTextures()
        {
            // If not initialized, do nothing.
            if (_offscreenRenderTextureContainers == null)
            {
                return;
            }

            // Mark all render textures as not in use.
            for (var i = 0; i < _offscreenRenderTextureContainers.Length; ++i)
            {
                _offscreenRenderTextureContainers[i].InUse = false;
            }

            _currentActiveRenderTextureCount = 0;
            _hasResetThisFrame = false;
        }

        /// <summary>
        /// Release stale render textures that are not used in the previous frame.
        /// </summary>
        public void ReleaseStaleRenderTextures()
        {
            // If not initialized, do nothing.
            if (_offscreenRenderTextureContainers == null
                || _offscreenRenderTextureContainers.Length <= _previousActiveRenderTextureCount)
            {
                return;
            }

            var newSize = Math.Max(OffscreenRenderTextureDefaultCount, _previousActiveRenderTextureCount);

            // Release unused render textures beyond the _previousActiveRenderTextureCount.
            for (var i = newSize; i < _offscreenRenderTextureContainers.Length; i++)
            {
                _offscreenRenderTextureContainers[i].RenderTexture.Release();
                _offscreenRenderTextureContainers[i].RenderTexture = null;
            }

            // Resize the array to keep only the active render textures.
            Array.Resize(ref _offscreenRenderTextureContainers, newSize);
        }

        /// <summary>
        /// Release all offscreen render textures.
        /// </summary>
        public void Release()
        {
            // If not initialized, do nothing.
            if (_offscreenRenderTextureContainers == null)
            {
                return;
            }

            // Release all render textures.
            for (var i = 0; i < _offscreenRenderTextureContainers.Length; ++i)
            {
                if (_offscreenRenderTextureContainers[i].RenderTexture != null)
                {
                    _offscreenRenderTextureContainers[i].RenderTexture.Release();
                    _offscreenRenderTextureContainers[i].RenderTexture = null;
                }
                _offscreenRenderTextureContainers[i].InUse = false;
            }

            _offscreenRenderTextureContainers = null;
            _previousActiveRenderTextureCount = 0;
            _currentActiveRenderTextureCount = 0;
            _hasResetThisFrame = false;
        }
    }
}
