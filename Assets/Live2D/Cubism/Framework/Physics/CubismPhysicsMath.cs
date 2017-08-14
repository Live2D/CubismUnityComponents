/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Math utilities for physics.
    /// </summary>
    internal static class CubismPhysicsMath
    {
        /// <summary>
        /// Gets radian from degrees.
        /// </summary>
        /// <param name="degrees">Degrees.</param>
        /// <returns>Radian.</returns>
        public static float DegreesToRadian(float degrees)
        {
            return (degrees / 180.0f) * Mathf.PI;
        }

        /// <summary>
        /// Gets degrees from radian.
        /// </summary>
        /// <param name="radian">Radian.</param>
        /// <returns>Degrees.</returns>
        public static float RadianToDegrees(float radian)
        {
            return (radian * 180.0f) / Mathf.PI;
        }
        

        /// <summary>
        /// Gets angle from both vector direction.
        /// </summary>
        /// <param name="from">From vector.</param>
        /// <param name="to">To vector.</param>
        /// <returns>Angle of radian.</returns>
        public static float DirectionToRadian(Vector2 from, Vector2 to)
        {
            var dotProduct = Vector2.Dot(from, to);
            var magnitude = from.magnitude * to.magnitude;


            if (magnitude == 0.0f)
            {
                return 0.0f;
            }

            
            var cosTheta = (dotProduct / magnitude);

            if (Mathf.Abs(cosTheta) > 1.0)
            {
                return 0.0f;
            }

            
            var theta = (float)Mathf.Acos(cosTheta);

            return theta;
        }
        
        /// <summary>
        /// Gets angle from both vector direction.
        /// </summary>
        /// <param name="from">From vector.</param>
        /// <param name="to">To vector.</param>
        /// <returns>Angle of degrees.</returns>
        public static float DirectionToDegrees(Vector2 from, Vector2 to)
        {
            var radian = DirectionToRadian(from, to);
            var degree = (float)RadianToDegrees(radian);


            if ((to.x - from.x) > 0.0f)
            {
                degree = -degree;
            }


            return degree;
        }
        
        /// <summary>
        /// Gets vector direction from angle.
        /// </summary>
        /// <param name="totalAngle">Radian.</param>
        /// <returns>Direction of vector.</returns>
        public static Vector2 RadianToDirection(float totalAngle)
        {
            var ret = Vector2.zero;


            ret.x = Mathf.Sin(totalAngle);
            ret.y = (float)Mathf.Cos(totalAngle);


            return ret;
        }


        /// <summary>
        /// Normalize parameter value.
        /// </summary>
        /// <param name="parameter">Target parameter.</param>
        /// <param name="NormalizedMinimum">Value of normalized minimum.</param>
        /// <param name="NormalizedMaximum">Value of normalized maximum.</param>
        /// <param name="NormalizedDefault">Value of normalized default.</param>
        /// <param name="isInverted">True if input is inverted; otherwise.</param>
        /// <returns></returns>
        public static float Normalize(CubismParameter parameter,
                                                float NormalizedMinimum,
                                                float NormalizedMaximum,
                                                float NormalizedDefault,
                                                bool isInverted = false)
        {

            var result = 0.0f;
            var maximumValue = Mathf.Max(parameter.MaximumValue, parameter.MinimumValue);
            var minimumValue = Mathf.Min(parameter.MaximumValue, parameter.MinimumValue);
            var defaultValue = parameter.DefaultValue;
            var parameterValue = parameter.Value - defaultValue;


            switch ((int)Mathf.Sign(parameterValue))
            {
                case 1:
                    {
                        var parameterRange = maximumValue - defaultValue;
                        
                        if (parameterRange == 0.0f)
                        {
                            return NormalizedDefault;
                        }


                        var normalizedRange = NormalizedMaximum - NormalizedDefault;

                        if (normalizedRange == 0.0f)
                        {
                            return NormalizedMaximum;
                        }


                        result = parameter.Value * Mathf.Abs(normalizedRange / parameterRange);
                    }
                    break;
                case -1:
                    {
                        var parameterRange = defaultValue - minimumValue;

                        if (parameterRange == 0.0f)
                        {
                            return NormalizedDefault;
                        }


                        var normalizedRange = NormalizedDefault - NormalizedMinimum;

                        if (normalizedRange == 0.0f)
                        {
                            return NormalizedMinimum;
                        }


                        result = parameter.Value * Mathf.Abs(normalizedRange / parameterRange);
                    }
                    break;
                case 0:
                    {
                        result = NormalizedDefault;
                    }
                    break;
            }


            return (isInverted)
                ? result
                : (result * -1.0f);
        }



    }
}


