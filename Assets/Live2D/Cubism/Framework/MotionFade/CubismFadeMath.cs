using System;

namespace Live2D.Cubism.Framework.MotionFade
{
    public static class CubismFadeMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float GetEasingSine(float value)
        {
            if (value < 0.0f) return 0.0f;
            if (value > 1.0f) return 1.0f;

            return (float)(0.5f - 0.5f * Math.Cos(value * (float)Math.PI));
        }

    }
}
