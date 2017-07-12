/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Extensions for <see cref="Vector2"/>s.
    /// </summary>
    internal static class Vector2ExtensionMethods
    {
        /// <summary>
        /// Converts a 2D vector to vertex position.
        /// </summary>
        /// <param name="self">2D vector to convert.</param>
        /// <returns>Vertex position.</returns>
        public static Vector3 ToVertexPosition(this Vector2 self)
        {
            return new Vector3(self.x, self.y);
        }

        /// <summary>
        /// Converts a 2D vector to vector uv.
        /// </summary>
        /// <param name="self">2D vector to convert.</param>
        /// <returns>Vertex uv.</returns>
        public static Vector2 ToVertexUv(this Vector2 self)
        {
            return self;
        }
    }
}
