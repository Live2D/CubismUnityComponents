/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;


namespace Live2D.Cubism.Framework
{
    /// <summary>
    /// Extensions for <see cref="CubismParameter"/>s.
    /// </summary>
    public static class CubismParameterExtensionMethods
    {
        /// <summary>
        /// Additively blends a value in.
        /// </summary>
        /// <param name="parameter"><see langword="this"/>.</param>
        /// <param name="value">Value to blend in.</param>
        /// <param name="weight">Blend weight.</param>
        public static void AddToValue(this CubismParameter parameter, float value, float weight = 1f)
        {
            parameter.Value += (value * weight);
        }


        /// <summary>
        /// Multiply blends a value in.
        /// </summary>
        /// <param name="parameter"><see langword="this"/>.</param>
        /// <param name="value">Value to blend in.</param>
        /// <param name="weight">Blend weight.</param>
        public static void MultiplyValueBy(this CubismParameter parameter, float value, float weight = 1f)
        {
            parameter.Value *= (1f + ((value - 1f) * weight));
        }


        /// <summary>
        /// Blends a value in.
        /// </summary>
        /// <param name="self"><see langword="this"/>.</param>
        /// <param name="value">Value to blend in.</param>
        /// <param name="mode">Blend mode to use.</param>
        public static void BlendToValue(this CubismParameter self, CubismParameterBlendMode mode, float value)
        {
            if (mode == CubismParameterBlendMode.Additive)
            {
                self.AddToValue(value);


                return;
            }


            if (mode == CubismParameterBlendMode.Multiply)
            {
                self.MultiplyValueBy(value);


                return;
            }


            self.Value = value;
        }

        /// <summary>
        /// Blends the same value into multiple <see cref="CubismParameter"/>s.
        /// </summary>
        /// <param name="self"><see langword="this"/>.</param>
        /// <param name="value">Value to blend in.</param>
        /// <param name="mode">Blend mode to use.</param>
        public static void BlendToValue(this CubismParameter[] self, CubismParameterBlendMode mode, float value)
        {
            if (mode == CubismParameterBlendMode.Additive)
            {
                for (var i = 0; i < self.Length; ++i)
                {
                    self[i].AddToValue(value);
                }


                return;
            }


            if (mode == CubismParameterBlendMode.Multiply)
            {
                for (var i = 0; i < self.Length; ++i)
                {
                    self[i].MultiplyValueBy(value);
                }


                return;
            }


            for (var i = 0; i < self.Length; ++i)
            {
                self[i].Value = value;
            }
        }
    }
}
