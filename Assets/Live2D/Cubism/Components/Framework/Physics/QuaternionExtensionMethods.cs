/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;
using System;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Extensions for <see cref="Quaternion"/>s.
    /// </summary>
    public static class QuaternionExtensionMethods
    {
        /// <summary>
        /// Normalize quaternion.
        /// </summary>
        /// <param name="self">Quaternion to normalize.</param>
        /// <returns>Normalized quaternion.</returns>
        public static Quaternion Normalize(this Quaternion self)
        {
            var length = (float)Math.Sqrt(self.x * self.x + self.y * self.y + self.z * self.z + self.w * self.w);
            self.x /= length;
            self.y /= length;
            self.z /= length;
            self.w /= length;

            return self;
        }
    }
}
