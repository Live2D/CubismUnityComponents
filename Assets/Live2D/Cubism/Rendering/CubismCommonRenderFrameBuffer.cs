/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using UnityEngine;

namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Common RenderTexture Manager.
    /// This class only manages a single RenderTexture that can be used across the application.
    /// </summary>
    public class CubismCommonRenderFrameBuffer
    {
        /// <summary>
        /// Size of the common render frame buffer.
        /// </summary>
        public struct FrameBufferSize
        {
            public int Width;
            public int Height;
        }

        /// <summary>
        /// Singleton instance of the CubismCommonRenderFrameBuffer.
        /// </summary>
        private static CubismCommonRenderFrameBuffer _instance;

        /// <summary>
        /// The common render frame buffer.
        /// </summary>
        private RenderTexture _frameBuffer;

        /// <summary>
        /// Size of the common render frame buffer.
        /// </summary>
        public FrameBufferSize Size;

        /// <summary>
        /// Sets the size of the common render frame buffer.
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetFrameBufferSize(int width, int height)
        {
            Size.Width = width;
            Size.Height = height;
        }

        /// <summary>
        /// the common render frame buffer.
        /// </summary>
        public RenderTexture CommonFrameBuffer
        {
            get
            {
                if (!_frameBuffer)
                {
                    var width = Size.Width;
                    var height = Size.Height;
                    if (width < 1 || height < 1)
                    {
                        // Dummy size for invalid screen size.
                        Size.Width = width = 1;
                        Size.Height = height = 1;
                    }

                    // Create a new RenderTexture.
                    _frameBuffer = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                        {
                            name = "CubismCommonRenderFrameBuffer",
                            filterMode = FilterMode.Point,
                            wrapMode = TextureWrapMode.Clamp
                        };

                    CommonFrameBuffer.Create();
                }

                return _frameBuffer;
            }

            private set
            {
                _frameBuffer = value;
            }
        }

        /// <summary>
        /// Singleton instance getter for the CubismCommonRenderFrameBuffer.
        /// </summary>
        /// <returns>Instance</returns>
        public static CubismCommonRenderFrameBuffer GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CubismCommonRenderFrameBuffer();

                _instance.Initialize();
            }

            return _instance;
        }

        /// <summary>
        /// Initializes the common render frame buffer with the specified width and height.
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void Initialize(int width = 0, int height = 0)
        {
            Size.Width = width < 1 ? Screen.width : width;
            Size.Height = height < 1 ? Screen.height : height;

            // If the size is invalid, do not create the frame buffer.
            if (Size.Width < 1 || Size.Height < 1)
            {
                return;
            }

            if (CommonFrameBuffer != null)
            {
                CommonFrameBuffer.Release();
            }

            // Initialize the common frame buffer with the specified size
            CommonFrameBuffer = new RenderTexture(Size.Width, Size.Height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            CommonFrameBuffer.Create();
        }

        /// <summary>
        /// Releases the common render frame buffer.
        /// NOTE: This function should be called when the application is closing or when the buffer is no longer needed.
        /// </summary>
        public void Release()
        {
            if (CommonFrameBuffer != null)
            {
                CommonFrameBuffer.Release();
                CommonFrameBuffer = null;
            }
        }
    }
}
