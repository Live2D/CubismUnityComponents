/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Framework.Utils
{
    public class CubismMath
    {
        /// <summary>
        /// Minimum clipped-draw bounds size (model units). Below this the mask scale would diverge to
        /// Inf/NaN, so the clip group is skipped for the frame.
        /// </summary>
        public const float MinClippedDrawSize = 1e-4f;

        /// <summary>
        /// Returns the remainder of the division of two numbers.
        /// If invalid dividend or divisor is passed, it returns <see cref="float.NaN"/>.
        /// </summary>
        /// <param name="dividend"><see cref="Mathf.Infinity"/>, <see cref="Mathf.NegativeInfinity"/> and <see cref="float.NaN"/> are not allowed.</param>
        /// <param name="divisor"><see cref="float.NaN"/> and Zero is not allowed.</param>
        /// <returns></returns>
        public static float ModF(float dividend, float divisor)
        {
            if (!float.IsFinite(dividend) || Mathf.Approximately(divisor, 0)
                || float.IsNaN(dividend) || float.IsNaN(divisor))
            {
                Debug.LogWarning($"Invalid dividend or divisor. divided: {dividend}, divisor: {divisor} Mod() returns 'NaN'.");
                return float.NaN;
            }

            // Calculate the remainder of the division.
            return dividend % divisor;
        }


        /// <summary>
        /// Returns the value clamped within the specified range.
        /// </summary>
        /// <param name="val">Value to be checked within the range.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>Clamped value within the range.</returns>
        public static float ClampF(float val, float min, float max)
        {
            if (val < min)
            {
                return min;
            }
            else if (max < val)
            {
                return max;
            }

            return val;
        }

        /// <summary>
        /// Returns the CPU-side view-projection matching the camera's viewport coordinates
        /// (y-up, NDC in [-1, 1]).
        /// </summary>
        /// <param name="camera">Camera to read.</param>
        /// <returns>The combined view-projection matrix.</returns>
        /// <remarks>
        /// Using <see cref="Camera.projectionMatrix"/> avoids platform-specific GPU projection
        /// differences (Y-flip / reversed-Z). Both matrices come from native properties, so callers that
        /// project many meshes should hoist this out of their loop.
        /// </remarks>
        public static Matrix4x4 GetViewProjection(Camera camera)
        {
            return camera.projectionMatrix * camera.worldToCameraMatrix;
        }

        /// <summary>
        /// Returns the smallest rectangle containing both arguments.
        /// </summary>
        /// <param name="a">First rectangle.</param>
        /// <param name="b">Second rectangle.</param>
        /// <returns>The union.</returns>
        public static RectInt UnionRects(RectInt a, RectInt b)
        {
            var xMin = Mathf.Min(a.xMin, b.xMin);
            var yMin = Mathf.Min(a.yMin, b.yMin);
            var xMax = Mathf.Max(a.xMax, b.xMax);
            var yMax = Mathf.Max(a.yMax, b.yMax);

            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        /// <summary>
        /// Define a custom Epsilon
        /// </summary>
        public const float Epsilon = 1e-06f;
    }
}
