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
    /// Manager of linkages among Points
    /// </summary>
    public class CubismPhysicsLink
    {
        /// <summary>
        /// Initial relative position from parent to child
        /// </summary>
        public Vector3 InitialRelativePosition { get; protected set; }

        /// <summary>
        /// Initial relative rotation from parent to child
        /// </summary>
        public Quaternion InitialRelativeRotation { get; protected set; }

        /// <summary>
        /// Parent Point
        /// </summary>
        public CubismPhysicsPoint Parent { get; private set; }

        /// <summary>
        /// Child Point
        /// </summary>
        public CubismPhysicsPoint Child { get; private set; }

        /// <summary>
        /// Current relative position from parent to child
        /// </summary>
        public Vector3 RelativePosition;

        /// <summary>
        /// Current adjusted relative position from parent to child (adjusted) 
        /// </summary>
        public Vector3 RelativePositionAdjusted;

        /// <summary>
        /// Current relative rotation from parent to child
        /// </summary>
        public Quaternion RelativeRotation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent"> Parent Point </param>
        /// <param name="child"> Child Point </param>
        public CubismPhysicsLink(CubismPhysicsPoint parent, CubismPhysicsPoint child)
        {
            Parent = parent;
            Child = child;
            
            Child.HasParent = true;
            Child.Link = this;

            InitialRelativePosition = Child.transform.position - Parent.gameObject.transform.position;
            InitialRelativeRotation = Child.transform.rotation * Quaternion.Inverse(Parent.gameObject.transform.rotation);
        }
    }

}