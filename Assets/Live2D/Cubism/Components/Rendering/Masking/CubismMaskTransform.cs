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
                w = 1f
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
