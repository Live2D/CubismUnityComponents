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
    /// Physics controller
    /// </summary>
    // ONO Provide custom inspector if necessary.
    public sealed class CubismPhysicsController : MonoBehaviour
    {
        /// <summary>
        /// Rig to perform physics.
        /// </summary>
        [SerializeField, HideInInspector]
        public CubismPhysicsRig PhysicsRig;

        #region Unity Event Handling

        /// <summary>
        /// Revives controller.
        /// </summary>
        private void Start()
        {
            if (PhysicsRig == null)
            {
                return;
            }

            // Initialize cache.
            PhysicsRig.Refresh(this);
        }


        /// <summary>
        /// Updates rig.
        /// </summary>
        private void FixedUpdate()
        {
            int i;

            
            // Calculate the transform of Root from parameters
            for (i = 0; i < PhysicsRig.RootPoints.Length; i++)
            {
                PhysicsRig.ResetRootPointTransform(
                    PhysicsRig.RootPoints[i],
                    PhysicsRig.BaseParameterSettings[i].CubismPhysicsParameterSettings);
            }

            
            // Initialize the position of Point linked to Root
            for (i = 0; i < PhysicsRig.PhysicalPointRoots.Length; i++)
            {
                PhysicsRig.ResetPointRootTransform(PhysicsRig.PhysicalPointRoots[i]);
            }

            
            // Calculate relative transform of child Point from transform of parent Point
            for (i = 0; i < PhysicsRig.ControlLinks.Length; i++)
            {
                PhysicsRig.UpdateChildTransform(PhysicsRig.ControlLinks[i]);
            }


            // Calculate the force to Point
            for (i = 0; i < PhysicsRig.Links.Length; i++)
            {
                PhysicsRig.UpdateAddedForceToChild(PhysicsRig.Links[i]);
            }


            // Calculate the resist to Point
            for (i = 0; i < PhysicsRig.Points.Length; i++)
            {
                PhysicsRig.UpdateChildFrictions(PhysicsRig.Points[i].RigidBody);
            }


            // Restrict the force of Point
            for (i = 0; i < PhysicsRig.Links.Length; i++)
            {
                PhysicsRig.RestrictChildForce(PhysicsRig.Links[i]);
            }


            // Calculate accerelation, velocity, position
            var deltaTime = Time.deltaTime;


            for (i = 0; i < PhysicsRig.Points.Length; i++)
            {
                PhysicsRig.UpdateTransformFromDeltaTime(
                    PhysicsRig.Points[i].RigidBody,
                    deltaTime);
            }
        }


        /// <summary>
        /// Updates actual parameters.
        /// </summary>
        private void LateUpdate()
        {
            // Apply the result of physics to parameters
            for (var i = 0; i < PhysicsRig.Points.Length; i++)
            {
                PhysicsRig.SetValueToParameter(
                    PhysicsRig.Points[i],
                    PhysicsRig.PhysicalParameterSettings[i].CubismPhysicsParameterSettings);
            }
        }
        #endregion
    }
}
