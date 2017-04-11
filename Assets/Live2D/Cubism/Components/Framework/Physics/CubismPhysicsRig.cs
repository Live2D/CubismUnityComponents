/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;
using Live2D.Cubism.Core;
using System;
using System.Collections.Generic;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Perform physics.
    /// </summary>
    // ONO Provide custom inspector if necessary.
    public sealed class CubismPhysicsRig : MonoBehaviour
    {
        /// <summary>
        /// Array of roots' name
        /// </summary>
        [SerializeField, HideInInspector]
        private string[] _rootPointNames;

        /// <summary>
        /// Array of Points' name
        /// </summary>
        [SerializeField, HideInInspector]
        private string[] _physicalPointNames;

        /// <summary>
        /// Settings of parameter which performs as an input of root Point
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismPhysicsParameterSettingContainer[] _rootParameterSettings;

        /// <summary>
        /// Settings of parameter which performs as an input of root Point
        /// </summary>
        public CubismPhysicsParameterSettingContainer[] BaseParameterSettings { get { return _rootParameterSettings; } }

        /// <summary>
        /// Settings of parameter which displays the output from <see cref="CubismPhysicsPoint"/>
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismPhysicsParameterSettingContainer[] _physicalParameterSettings;

        /// <summary>
        /// Settings of parameter which displays the output from <see cref="CubismPhysicsPoint"/>
        /// </summary>
        public CubismPhysicsParameterSettingContainer[] PhysicalParameterSettings { get { return _physicalParameterSettings; } }

        /// <summary>
        /// Settings of <see cref="CubismPhysicsLink"/>
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismPhysicsLinkInfo[] _linkInfos;

        /// <summary>
        /// Points linked to root Point
        /// </summary>
        public CubismPhysicsPoint[] PhysicalPointRoots { get; private set; }

        /// <summary>
        /// Live2D models
        /// </summary>
        private CubismModel Model { get; set; }

        /// <summary>
        /// <see cref="CubismPhysicsControlLink"/> from root
        /// </summary>
        public CubismPhysicsControlLink[] ControlLinks { get; private set; }

        /// <summary>
        /// ONO English...
        /// <see cref="CubismPhysicsLink"/>s linking two Points
        /// </summary>
        public CubismPhysicsLink[] Links { get; private set; }

        /// <summary>
        /// Roots
        /// </summary>
        public CubismPhysicsPoint[] RootPoints { get; private set; }

        /// <summary>
        /// Points
        /// </summary>
        public CubismPhysicsPoint[] Points { get; private set; }

        /// <summary>
        /// Dimensions
        /// </summary>
        private const int Xyz = 3;

        /// <summary>
        /// Dimensions
        /// </summary>
        private const int OffsetRotate = 3;


        /// <summary>
        /// Initialize <see cref="CubismPhysicsRig"/>
        /// </summary>
        /// <param name="rootPointNames"> Names of root Point </param>
        /// <param name="physicalPointNames"> Names of Point </param>
        /// <param name="linkInfos"> Settings of <see cref="CubismPhysicsLink"/> </param>
        /// <param name="rootParameterSettings"> Settings of parameter which performs as an input of root Point </param>
        /// <param name="physicalParameterSettings"> Settings of parameter which displays the output from <see cref="CubismPhysicsPoint"/> </param>
        public void InitializeRig(
            string[] rootPointNames, 
            string[] physicalPointNames, 
            CubismPhysicsLinkInfo[] linkInfos,
            CubismPhysicsParameterSettingContainer[] rootParameterSettings, 
            CubismPhysicsParameterSettingContainer[] physicalParameterSettings
            )
        {
            _rootPointNames = rootPointNames;
            _physicalPointNames = physicalPointNames;
            _linkInfos = linkInfos;
            _rootParameterSettings = rootParameterSettings;
            _physicalParameterSettings = physicalParameterSettings;
        }

        /// <summary>
        /// Refreshes the rig.
        /// </summary>
        public void Refresh(CubismPhysicsController controller)
        {
            Model = controller.GetComponent<CubismModel>();

            Debug.Assert(Model != null);

            if(Model==null) return;



            // Initialize
            var basePoints = new List<CubismPhysicsPoint>();
            var physicalPoints = new List<CubismPhysicsPoint>();
            var pointsForLink = new Dictionary<string, CubismPhysicsPoint>();

            var parameterPoints = controller.GetComponentsInChildren<CubismPhysicsPoint>(false);

            
            CubismPhysicsPoint point;
            int i, j;
            for (i = 0; i < parameterPoints.Length; i++)
            {
                point = parameterPoints[i];



                // Get root Points
                for (j = 0; j < _rootPointNames.Length; j++)
                {
                    var basePoint = _rootPointNames[j];
                    if (point.transform.name != basePoint) continue;

                    basePoints.Add(point);
                }
                RootPoints = basePoints.ToArray();

                // Get Points
                for (j = 0; j < _physicalPointNames.Length; j++)
                {
                    var physicalPoint = _physicalPointNames[j];
                    if (point.transform.name != physicalPoint) continue;

                    physicalPoints.Add((CubismPhysicsPoint)point);
                }
                Points = physicalPoints.ToArray();
                
                // Get parameters for the settings of Link
                for (j = 0; j < _linkInfos.Length; j++)
                {
                    var link = _linkInfos[j];
                    if (point.name == link.Parent && !pointsForLink.ContainsKey(link.Parent))
                    {
                        pointsForLink[link.Parent] = point;
                    }
                    else if (point.name == link.Child && !pointsForLink.ContainsKey(link.Child))
                    {
                        pointsForLink[link.Child] = point;
                    }
                }
            }

            // Initialize Links
            var controlLinks = new List<CubismPhysicsControlLink>();
            var links = new List<CubismPhysicsLink>();
            for (i = 0; i < _linkInfos.Length; i++)
            {
                var link = _linkInfos[i];

                switch (link.LinkType)
                {
                    case "Control":
                        controlLinks.Add(new CubismPhysicsControlLink(pointsForLink[link.Parent],
                            pointsForLink[link.Child]));
                        break;
                    case "Fix":
                        links.Add(new CubismPhysicsLink(pointsForLink[link.Parent],
                            pointsForLink[link.Child]));
                        break;
                    case "Sphere":
                        links.Add(new CubismPhysicsSphereLink(pointsForLink[link.Parent],
                            pointsForLink[link.Child]));
                        break;
                    case "SpringSphere":
                        links.Add(new CubismPhysicsSpringSphereLink(pointsForLink[link.Parent],
                            pointsForLink[link.Child]));
                        break;
                    default:
                        break;
                }
            }
            ControlLinks = controlLinks.ToArray();
            Links = links.ToArray();


            // Get points linked to root Point
            var physicalPointRoots = new List<CubismPhysicsPoint>();
            for (i = 0; i < Points.Length; i++)
            {
                if (Points[i].HasParent) continue;
                physicalPointRoots.Add(Points[i]);
            }
            PhysicalPointRoots = physicalPointRoots.ToArray();
            

            // Get settings of parameter related to input/output
            LoadParameter(_rootParameterSettings);
            LoadParameter(_physicalParameterSettings);
        }

        /// <summary>
        /// Get settings of parameter related to input/output
        /// </summary>
        /// <param name="parameterSettings"></param>
        private void LoadParameter(CubismPhysicsParameterSettingContainer[] parameterSettings)
        {
            var parameters = Model.Parameters;
            for (var i = 0; i < parameterSettings.Length; i++)
            { 
                var rootParameterSettings = parameterSettings[i].CubismPhysicsParameterSettings;

                for (var j = 0; j < rootParameterSettings.Length; j++)
                {
                    var parameterList = new List<CubismParameter>();
                    var parameterId = rootParameterSettings[j].ParameterIds;

                    for (var k = 0; k < parameterId.Length; k++)
                    {
                        for (var l = 0; l < parameters.Length; l++)
                        {
                            if (parameters[l].name != parameterId[k]) continue;

                            parameterList.Add(parameters[l]);
                            break;
                        }
                    }
                    rootParameterSettings[j].Parameters = parameterList.ToArray();
                }
                parameterSettings[i].CubismPhysicsParameterSettings = rootParameterSettings;
            }
        }


        /// <summary>
        /// Reset transform of root Point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="parameterSetting"></param>
        public void ResetRootPointTransform(CubismPhysicsPoint point, CubismPhysicsParameterSetting[] parameterSetting)
        {
            var position = point.InitialPosition;
            var rotation = point.InitialRotation;

            for (var i = 0; i < Xyz; i++)
            {
                // Position
                if ((parameterSetting[i].Parameters.Length > 0))
                {
                    position[i] = parameterSetting[i].Parameters[0].Value;
                }

                // Rotation
                if ((parameterSetting[i + OffsetRotate].ParameterIds.Length > 0))
                {
                    rotation[i] = (parameterSetting[i + OffsetRotate].Parameters[0].Value * point.RotationFactor[i]);
                }
            }

            point.transform.localPosition = position;

            point.transform.localRotation =
                Quaternion.AngleAxis(rotation.x, Vector3.right) *
                Quaternion.AngleAxis(rotation.y, Vector3.up) *
                Quaternion.AngleAxis(rotation.z, Vector3.forward);
        }

        /// <summary>
        /// Reset transform of Points linked to root
        /// </summary>
        /// <param name="parameterPoint"></param>
        public void ResetPointRootTransform(CubismPhysicsPoint parameterPoint)
        {
            parameterPoint.transform.localPosition = parameterPoint.InitialPosition;

            parameterPoint.transform.localRotation =
                Quaternion.AngleAxis(parameterPoint.InitialRotation.x, Vector3.right)
                * Quaternion.AngleAxis(parameterPoint.InitialRotation.y, Vector3.up)
                * Quaternion.AngleAxis(parameterPoint.InitialRotation.z, Vector3.forward);
        }


        /// <summary>
        /// Update the transform of child Point
        /// </summary>
        /// <param name="controlLink"></param>
        public void UpdateChildTransform(CubismPhysicsControlLink controlLink)
        {
            var localRotation =
                controlLink.Parent.transform.rotation *
                Quaternion.Inverse(controlLink.InitialParentRotation);

            controlLink.Child.RigidBody.Transform.rotation *= localRotation;
            controlLink.Child.RigidBody.Transform.rotation = controlLink.Child.RigidBody.Transform.rotation.Normalize();

            controlLink.Child.RigidBody.Transform.position +=
                (controlLink.Parent.transform.position - controlLink.InitialParentPosition)
                + (localRotation * controlLink.InitialRelativePosition)
                - controlLink.InitialRelativePosition;
        }

        /// <summary>
        /// Calculate the restrict to control the force to child
        /// </summary>
        /// <param name="link"></param>
        public void RestrictChildForce(CubismPhysicsLink link)
        {
            if (link.GetType() != typeof(CubismPhysicsSphereLink) &&
                link.GetType() != typeof(CubismPhysicsSpringSphereLink)) return;

            var relativePosition = link.Parent.transform.position - link.Child.transform.position;

            link.Child.RigidBody.Velocity =
                 link.Child.RigidBody.Velocity - ((Vector3.Dot(relativePosition, link.Child.RigidBody.Velocity) / relativePosition.sqrMagnitude) * relativePosition);

            link.Child.RigidBody.Acceleration =
                link.Child.RigidBody.Acceleration - ((Vector3.Dot(relativePosition, link.Child.RigidBody.Acceleration) / (relativePosition.sqrMagnitude)) * relativePosition);
        }


        /// <summary>
        /// Update the force to child Point
        /// </summary>
        /// <param name="link"></param>
        public void UpdateAddedForceToChild(CubismPhysicsLink link)
        {
            link.RelativePosition = link.Child.transform.position - link.Parent.transform.position;


            if ((link as CubismPhysicsSphereLink) != null)
            {
                // SphereLink
                UpdateAddedForceToChild((CubismPhysicsSphereLink)link);
                return;
            }


            if ((link as CubismPhysicsSpringSphereLink) != null)
            {
                // SpringSohereLink
                UpdateAddedForceToChild((CubismPhysicsSpringSphereLink)link);
                return;
            }


            // FixedLink
            link.Child.RigidBody.Transform.position +=
                (link.Parent.RigidBody.Transform.rotation * link.InitialRelativePosition)
                - link.RelativePosition;

            link.RelativeRotation =
                link.Child.RigidBody.Transform.rotation
                * Quaternion.Inverse(link.Parent.RigidBody.Transform.rotation);


            link.Child.RigidBody.Transform.rotation *=
                Quaternion.Inverse(Quaternion.Inverse(link.InitialRelativeRotation)
                * link.RelativeRotation);

            link.Child.RigidBody.Transform.rotation = link.Child.RigidBody.Transform.rotation.Normalize();
        }

        /// <summary>
        /// Update the force to child Point
        /// </summary>
        /// <param name="link"></param>
        private void UpdateAddedForceToChild(CubismPhysicsSphereLink link)
        {
            if (link.InitialRelativePosition == link.RelativePosition) return;

            // Turn child Point to gravity vector
            link.Child.transform.rotation =
                (Quaternion.FromToRotation(
                        link.InitialRelativePosition,
                        link.RelativePosition));
            link.Child.transform.rotation = link.Child.transform.rotation.Normalize();



            link.RelativePositionAdjusted = (link.InitialRelativePosition.sqrMagnitude > 0)
                ? (link.RelativePosition.normalized * link.InitialRelativePosition.magnitude)
                : Vector3.zero;
            link.Child.transform.position =
                link.RelativePositionAdjusted
                + link.Parent.transform.position;

            link.Child.RigidBody.AddForce(
                (link.InitialRelativePosition - link.RelativePositionAdjusted)
                * link.GravityEffect * link.Parent.RigidBody.Mass);
        }

        /// <summary>
        /// Calculate the force to Point : Sphere Link
        /// </summary>
        /// <param name="link"></param>
        private void UpdateAddedForceToChild(CubismPhysicsSpringSphereLink link)
        {
            link.Child.transform.rotation *=
                Quaternion.FromToRotation(
                    link.InitialRelativePosition,
                    link.RelativePosition)
                * Quaternion.Inverse(link.Child.transform.rotation);
            link.Child.transform.rotation = link.Child.transform.rotation.Normalize();

            link.RelativePositionAdjusted = link.InitialRelativePosition.sqrMagnitude > 0
                ? link.RelativePosition.normalized * link.InitialRelativePosition.magnitude
                : Vector3.zero;
            link.Child.transform.position = link.RelativePositionAdjusted + link.Parent.transform.position;

            link.Child.RigidBody.AddForce(
                   ((link.Parent.RigidBody.Transform.rotation * link.InitialRelativePosition) - link.RelativePositionAdjusted)
                  * link.LinkSpring * link.Parent.RigidBody.Mass);
        }

        /// <summary>
        /// Update attenuation of transform or rotation (friction) of child Point
        /// </summary>
        public void UpdateChildFrictions(CubismPhysicsRigidBody rigidBody)
        {
            if (rigidBody.UseGravity)
            {
                AddForce(rigidBody, (CubismPhysicsRigidBody.Gravity * rigidBody.Mass));
            }

            AddForce(rigidBody, (rigidBody.Velocity * -1f * rigidBody.Friction));
            AddTorque(rigidBody,
                (Quaternion.Lerp(
                    Quaternion.identity,
                    rigidBody.AngularVelocity,
                    -1f * rigidBody.AngularFriction)));
        }

        /// <summary>
        /// Add force
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="f"></param>
        private void AddForce(CubismPhysicsRigidBody rigidBody, Vector3 f)
        {
            rigidBody.Acceleration += ((rigidBody.Mass == 0f)
                ? Vector3.zero
                : f / rigidBody.Mass);
        }

        /// <summary>
        /// Add torque
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="axis"></param>
        /// <param name="t"></param>
        private void AddTorque(CubismPhysicsRigidBody rigidBody, Vector3 axis, float t)
        {
            rigidBody.AngularAcceleration = rigidBody.AngularAcceleration * (Quaternion.AngleAxis(t / rigidBody.MomentOfInertia, axis));
        }

        /// <summary>
        /// Add torque (using quaternion)
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="torque"></param>
        private void AddTorque(CubismPhysicsRigidBody rigidBody, Quaternion torque)
        {
            rigidBody.AngularAcceleration = rigidBody.AngularAcceleration * (Quaternion.Slerp(Quaternion.identity, torque, 1f / rigidBody.MomentOfInertia));
        }


        /// <summary>
        /// Update the transform
        /// </summary>
        /// <param name="rigidBody"></param>
        /// <param name="fixedDeltaTime"></param>
        public void UpdateTransformFromDeltaTime(CubismPhysicsRigidBody rigidBody, float fixedDeltaTime)
        {
            rigidBody.Velocity += rigidBody.Acceleration * fixedDeltaTime;

            rigidBody.Transform.position +=
                rigidBody.Velocity *
                fixedDeltaTime;
            rigidBody.Acceleration = Vector3.zero;

            rigidBody.AngularVelocity *= Quaternion.Slerp(Quaternion.identity,
                rigidBody.AngularAcceleration, fixedDeltaTime);
            rigidBody.Transform.rotation *= Quaternion.Slerp(Quaternion.identity,
                rigidBody.AngularVelocity, fixedDeltaTime);


            rigidBody.AngularVelocity = rigidBody.AngularVelocity.Normalize();

            rigidBody.AngularAcceleration = Quaternion.identity;
        }


        /// <summary>
        /// Set the results of physics to parameter
        /// </summary>
        /// <param name="parameterPoint"> Parameter to set the result </param>
        /// <param name="parameterSetting"> Settings of parameter </param>
        public void SetValueToParameter(CubismPhysicsPoint parameterPoint, CubismPhysicsParameterSetting[] parameterSetting)
        {
            // Set the result of physics to parameter
            var position = parameterPoint.RigidBody.Transform.position - parameterPoint.Link.Parent.transform.position;
            var localPosition = parameterPoint.Link.Parent.transform.rotation * position;
            var initialPosition = parameterPoint.Link.InitialRelativePosition;

            if (parameterPoint.Link.GetType() == typeof(CubismPhysicsSphereLink))
            {
                // Set the current gravity vector as initial Position
                initialPosition = CubismPhysicsRigidBody.Gravity;
                localPosition = position;
            }

            Vector3 globalEulerRot;

            globalEulerRot.x = (float)Math.Atan2(localPosition.y, localPosition.z) - (float)Math.Atan2(initialPosition.y, initialPosition.z);
            globalEulerRot.y = (float)Math.Atan2(localPosition.z, localPosition.x) - (float)Math.Atan2(initialPosition.z, initialPosition.x);
            globalEulerRot.z = (float)Math.Atan2(localPosition.x, localPosition.y) - (float)Math.Atan2(initialPosition.x, initialPosition.y);

            for (var i = 0; i < 3; i++)
            {
                if (globalEulerRot[i] > Math.PI) { globalEulerRot[i] -= (float)(2f * Math.PI); }
                if (globalEulerRot[i] < -Math.PI) { globalEulerRot[i] += (float)(2f * Math.PI); }
            }

            for (var i = 0; i < Xyz; i++)
            {
                // Translate
                for (var j = 0; j < parameterSetting[i].ParameterIds.Length; j++)
                {
                    if (parameterSetting[i].ParameterIds[j] != null)
                    {
                        parameterSetting[i].Parameters[j].Value =
                            (localPosition.x - parameterPoint.InitialPosition[i]) / parameterPoint.PositionFactor[i];
                    }
                }

                // Rotate
                for (var j = 0; j < parameterSetting[i + OffsetRotate].ParameterIds.Length; j++)
                {
                    if (parameterSetting[i + OffsetRotate].ParameterIds[j] != null)
                    {
                        parameterSetting[i + OffsetRotate].Parameters[j].Value =
                            globalEulerRot[i] * parameterPoint.RotationFactor[i];
                    }
                }
            }
        }
    }
}
