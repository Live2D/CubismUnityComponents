/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Manages the mask render textures used by <see cref="CubismRenderController"/>s.
    /// </summary>
    public sealed class CubismMaskTextureController : MonoBehaviour
    {
        /// <summary>
        /// Default power-of-two mask texture resolution.
        /// </summary>
        private const int DefaultSize = 1024;

        /// <summary>
        /// Minimum power-of-two mask texture resolution.
        /// </summary>
        private const int MinimumSize = 256;

        /// <summary>
        /// Default number of mask render textures.
        /// </summary>
        private const int DefaultRenderTextureCount = 1;

        /// <summary>
        /// <see cref="UseHighPrecisionMask"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private bool _useHighPrecisionMask = false;

        /// <summary>
        /// When enabled, this model's drawable masks are redrawn per draw object into a dedicated
        /// full-screen mask texture (high precision) instead of being packed into the shared atlas.
        /// Offscreen masks always use this path.
        /// </summary>
        public bool UseHighPrecisionMask
        {
            get
            {
                return _useHighPrecisionMask;
            }
            set
            {
                if (_useHighPrecisionMask == value)
                {
                    return;
                }

                _useHighPrecisionMask = value;

                // Release the render textures when switching to high precision, since they are no longer used.
                if (_useHighPrecisionMask)
                {
                    ReleaseRenderTextures();
                }
            }
        }

        /// <summary>
        /// <see cref="Size"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _size = DefaultSize;

        /// <summary>
        /// Mask texture resolution in pixels. Only power-of-two values are accepted.
        /// </summary>
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                // Fail silently if not power-of-two.
                if (Mathf.ClosestPowerOfTwo(value) != value
                    || value < MinimumSize)
                {
                    return;
                }

                _size = value;
            }
        }

        /// <summary>
        /// <see cref="RenderTextureCount"/>'s backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _renderTextureCount = DefaultRenderTextureCount;

        /// <summary>
        /// Number of mask render textures. Values below one are clamped to one.
        /// </summary>
        public int RenderTextureCount
        {
            get
            {
                return Mathf.Max(1, _renderTextureCount);
            }
            set
            {
                _renderTextureCount = Mathf.Max(1, value);
            }
        }

        /// <summary>
        /// <see cref="RenderTextures"/>'s backing field.
        /// </summary>
        [System.NonSerialized]
        private RenderTexture[] _renderTextures;

        /// <summary>
        /// Mask render textures (power-of-two square). Lazily created; recreated when size or count changes.
        /// </summary>
        public RenderTexture[] RenderTextures
        {
            get
            {
                TryCreateRenderTextures();

                return _renderTextures;
            }
        }

        /// <summary>
        /// Returns the mask render texture at the given buffer index (clamped), or null when none exist.
        /// </summary>
        /// <param name="index">Buffer index (0 to <see cref="RenderTextureCount"/>-1).</param>
        public RenderTexture GetRenderTexture(int index)
        {
            var textures = RenderTextures;
            if (textures == null || textures.Length == 0)
            {
                return null;
            }

            return textures[Mathf.Clamp(index, 0, textures.Length - 1)];
        }

        /// <summary>
        /// Creates the mask render textures if they are missing or no longer match the current size / count.
        /// </summary>
        private void TryCreateRenderTextures()
        {
            var count = RenderTextureCount;

            if (_renderTextures != null
                && _renderTextures.Length == count
                && _renderTextures[0]
                && _renderTextures[0].width == _size)
            {
                return;
            }

            ReleaseRenderTextures();

            _renderTextures = new RenderTexture[count];
            for (var i = 0; i < count; i++)
            {
                var renderTexture = new RenderTexture(_size, _size, 0, RenderTextureFormat.ARGB32)
                {
                    name = $"{name}_Mask_{i}",
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Point
                };
                renderTexture.Create();

                _renderTextures[i] = renderTexture;
            }
        }

        /// <summary>
        /// Releases the mask render textures.
        /// </summary>
        private void ReleaseRenderTextures()
        {
            if (_renderTextures == null)
            {
                return;
            }

            for (var i = 0; i < _renderTextures.Length; i++)
            {
                if (!_renderTextures[i])
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(_renderTextures[i]);
                }
                else
                {
                    DestroyImmediate(_renderTextures[i]);
                }
                _renderTextures[i] = null;
            }

            _renderTextures = null;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDisable()
        {
            ReleaseRenderTextures();
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDestroy()
        {
            ReleaseRenderTextures();
        }
    }
}
