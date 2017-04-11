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
    /// ONO Document.
    /// </summary>
    public class CubismPhysicsRigidBody
    {
        /// <summary>
        /// Mass of Point
        /// </summary>
        public float Mass;

        /// <summary>
        /// Moment of Inertia
        /// </summary>
        public float MomentOfInertia;

        /// <summary>
        /// Friction
        /// </summary>
        public float Friction;

        /// <summary>
        /// Friction of rotation
        /// </summary>
        public float AngularFriction;

        /// <summary>
        /// Whether the gravity is used
        /// </summary>
        public bool UseGravity;

        public static readonly Vector3 Gravity = new Vector3(0, -9.80665f, 0);

        /// <summary>
        /// Transform of Point
        /// </summary>
        public Transform Transform;

        /// <summary>
        /// Velocity of Point
        /// </summary>
        public Vector3 Velocity ;

        /// <summary>
        /// Acceleration of Point
        /// </summary>
        public Vector3 Acceleration ;

        /// <summary>
        /// Angular Velocity of Point
        /// </summary>
        public Quaternion AngularVelocity ;

        /// <summary>
        /// Angular acceleration of Point
        /// </summary>
        public Quaternion AngularAcceleration;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="mass"></param>
        /// <param name="moment"></param>
        /// <param name="friction"></param>
        /// <param name="angularFriction"></param>
        /// <param name="useGravity"></param>
        public CubismPhysicsRigidBody(Transform transform, float mass, float moment, float friction, float angularFriction, bool useGravity)
        {
            Transform = transform;
            Mass = mass;
            MomentOfInertia = moment;
            Friction = friction;
            AngularFriction = angularFriction;
            UseGravity = useGravity;

            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;

            AngularVelocity = Quaternion.identity;
            AngularAcceleration = Quaternion.identity;
        }


        /// <summary>
        /// Add force for acceleration
        /// </summary>
        /// <param name="f"></param>
        public void AddForce(Vector3 f)
        {
            Acceleration += ((Mass==0.0000f)
                ? Vector3.zero
                : (f / Mass));
        }

        /// <summary>
        /// Add torque for angular acceleration
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="t"></param>
        public void AddTorque(Vector3 axis, float t)
        {
            AngularAcceleration = AngularAcceleration * (Quaternion.AngleAxis(t / MomentOfInertia, axis));
        }

        /// <summary>
        /// Add torque for angular acceleration (using quaternion)
        /// </summary>
        /// <param name="torque"></param>
        public void AddTorque(Quaternion torque)
        {
            AngularAcceleration = AngularAcceleration * (Quaternion.Slerp(Quaternion.identity, torque, 1f / MomentOfInertia));
        }
    }
}