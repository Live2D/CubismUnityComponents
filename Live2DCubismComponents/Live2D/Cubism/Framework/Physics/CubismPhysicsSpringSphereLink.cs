/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Link two Points which perform as spring behaviour 
    /// </summary>
    public class CubismPhysicsSpringSphereLink : CubismPhysicsLink
    {
        /// <summary>
        /// Strength of linked spring (spring modulus)
        /// </summary>
        public float LinkSpring { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent"> Parent Point </param>
        /// <param name="child"> Child Point </param>
        public CubismPhysicsSpringSphereLink(CubismPhysicsPoint parent, CubismPhysicsPoint child) : base(parent, child)
        {
            LinkSpring = 40f;
        }
    }
}