/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Physics;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Extends <see cref="CubismModel"/>s.
    /// </summary>
    public static class CubismModelExtensionMethods
    {
        /// <summary>
        /// Transfer physics settings and blink settings from <paramref name="source"/> to <paramref name="destination"/>.
        /// </summary>
        /// <remarks>
        /// Helper method for smooth switching between models when changing clothes, etc.
        /// The structure of the physics rig needs to be the same for the old and new models.
        /// </remarks>
        /// <param name="source">Model to be taken over from</param>
        /// <param name="destination">Model to be taken over</param>
        public static void TakeOver(this CubismModel source, CubismModel destination)
        {
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));
            if (destination == null)
                throw new System.ArgumentNullException(nameof(destination));

            var physicsSource = source.GetComponent<CubismPhysicsController>();
            var physicsDestination = destination.GetComponent<CubismPhysicsController>();
            physicsDestination.Rig.TakeOverFrom(physicsSource.Rig);

            var eyeSource = source.GetComponent<CubismAutoEyeBlinkInput>();
            var eyeDestination = destination.GetComponent<CubismAutoEyeBlinkInput>();
            eyeDestination.TakeOverFrom(eyeSource);
        }
    }
}
