/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using UnityEngine;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Point for physics
    /// Calclulate the value for the parameter from this Point's movement 
    /// </summary>
    [Serializable]
    public class CubismPhysicsPoint : MonoBehaviour
    {
        /// <summary>
        /// Whether Point has parent
        /// </summary>
        [NonSerialized]
        public bool HasParent;

        /// <summary>
        /// Type of Linkage to parent
        /// </summary>
        [NonSerialized]
        public CubismPhysicsLink Link;

        /// <summary>
        /// Setting of RigidBody
        /// </summary>
        private CubismPhysicsRigidBody _rigidBody;

        /// <summary>
        /// Setting of RigidBody
        /// </summary>
        public CubismPhysicsRigidBody RigidBody
        {
            get
            {
                return _rigidBody ?? (_rigidBody = new CubismPhysicsRigidBody(
                    transform,
                    RigidBodySettings.Mass,
                    RigidBodySettings.Moment,
                    RigidBodySettings.Friction,
                    RigidBodySettings.AngularFriction,
                    RigidBodySettings.UseGravity));
            }
        }

        /// <summary>
        /// Setting of RigidBody in Point
        /// </summary>
        [SerializeField, HideInInspector]
        protected RigidBodySetting RigidBodySettings;

        [Serializable]
        protected struct RigidBodySetting
        {
            [SerializeField]
            internal float Mass;
            [SerializeField]
            internal float Moment;
            [SerializeField]
            internal float Friction;
            [SerializeField]
            internal float AngularFriction;
            [SerializeField]
            internal bool UseGravity;
        }

        [SerializeField]
        public Vector3 PositionFactor;
        [SerializeField]
        public Vector3 RotationFactor;

        /// <summary>
        ///  Initial position of Point
        /// </summary>
        public Vector3 InitialPosition;

        /// <summary>
        /// Initial rotation of Point
        /// </summary>
        public Vector3 InitialRotation;


        /// <summary>
        /// Initialize Rigidbody
        /// </summary>
        /// <param name="mass"> Mass </param>
        /// <param name="moment"> Inertia of Moment </param>
        /// <param name="friction"> Friction </param>
        /// <param name="angularFriction"> Angular friction </param>
        /// <param name="useGravity"> Whether gravity is used </param>
        public void InitializeRigidBody(float mass, float moment, float friction, float angularFriction, bool useGravity)
        {
            RigidBodySettings = new RigidBodySetting
            {
                Mass = mass,
                Moment = moment,
                Friction = friction,
                AngularFriction = angularFriction,
                UseGravity = useGravity
            };
        }
    }
}