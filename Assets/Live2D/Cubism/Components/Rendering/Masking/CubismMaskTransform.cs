/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Rendering.Masking
{
    /// <summary>
    /// Holds info used for masking.
    /// </summary>
    public struct CubismMaskTransform
    {
        #region Conversion

        /// <summary>
        /// HACK Prevents dynamic batching of <see cref="CubismRenderer"/>s that are masked.
        /// </summary>
        /// <remarks>
        /// As Unity transforms vertex positions into world space on dynamic batching, and masking relies on vertex positions to be in local space,
        /// masking isn't compatible with dynamic batching.
        /// 
        /// Unity exposes a shader tag for disabling dynamic batching ("DynamicBatching"), but this would make it necessary for creating separate shaders...
        /// </remarks>
        private static int UniqueId { get; set; }


        /// <summary>
        /// Converts a <see cref="CubismMaskTile"/> to a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        public static implicit operator Vector4(CubismMaskTransform value)
        {
            return new Vector4
            {
                x = value.Offset.x,
                y = value.Offset.y,
                z = value.Scale,
                w = (++UniqueId)
            };
        }

        #endregion

        /// <summary>
        /// Offset in model space.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Scale in model space.
        /// </summary>
        public float Scale;
    }
}
