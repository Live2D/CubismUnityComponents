/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using System;

namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Extensions for <see cref="CubismPhysics"/>s.
    /// </summary>
    public static class CubismPhysicsExtensionMethods
    {
        /// <summary>
        /// Get <see cref="CubismPhysicsSubRig"/> by name.
        /// </summary>
        /// <param name="cubismModel"></param>
        /// <param name="name">sub rig name</param>
        public static CubismPhysicsSubRig GetPhysicsSubRig(this CubismModel cubismModel, string name)
        {
            var controller = cubismModel.GetComponent<CubismPhysicsController>();
            if (controller == null)
                return null;

            return controller.Rig.GetSubRig(name);
        }
    }
}
