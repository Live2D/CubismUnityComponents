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
    /// Link parent Point and child Point
    /// </summary>
    public class CubismPhysicsControlLink : CubismPhysicsLink
    {
        /// <summary>
        /// Initial position of parent Point
        /// </summary>
        public Vector3 InitialParentPosition { get; private set; }

        /// <summary>
        /// Initial angular of parent Point
        /// </summary>
        public Quaternion InitialParentRotation { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent"> Parent Point </param>
        /// <param name="child"> Child Point </param>
        /// <param name="hasParent"> Whether the point has a parent </param>
        public CubismPhysicsControlLink(CubismPhysicsPoint parent, CubismPhysicsPoint child) : base(parent, child)
        {
            Child.Link = this;

            InitialRelativeRotation = Child.transform.rotation * Quaternion.Inverse(Parent.transform.rotation);
            InitialRelativePosition = Child.transform.position - Parent.transform.position;

            InitialParentPosition = Parent.transform.position;
            InitialParentRotation = Parent.transform.rotation;

            child.HasParent = false;
        }
    }
}