/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Link two Points as one follows the other only the rotation.。
    /// </summary>
    public class CubismPhysicsSphereLink : CubismPhysicsLink
    {
        /// <summary>
        /// Strength of gravity
        /// </summary>
        public float GravityEffect { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent"> Parent Point </param>
        /// <param name="child"> Child Point </param>
        public CubismPhysicsSphereLink(CubismPhysicsPoint parent, CubismPhysicsPoint child) : base(parent, child)
        {
            GravityEffect = 40f;

            InitialRelativePosition = (InitialRelativePosition.sqrMagnitude > 0)
                ? (CubismPhysicsRigidBody.Gravity.normalized * InitialRelativePosition.magnitude)
                : Vector3.zero;
        }
    }
}