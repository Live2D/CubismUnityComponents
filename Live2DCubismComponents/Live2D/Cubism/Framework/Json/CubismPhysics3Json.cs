/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;
using Live2D.Cubism.Framework.Physics;
using System;
using System.Collections.Generic;


namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Contains Cubism physics3.json data.
    /// </summary>
    // ONO Document.
    public sealed class CubismPhysics3Json
    {
        #region Load Methods

        /// <summary>
        /// Loads a physics3.json asset.
        /// </summary>
        /// <param name="physics3Json">Source JSON string.</param>
        /// <returns>Deserialized physics3.json on success; <see langword="null"/> otherwise.</returns>
        public static CubismPhysics3Json LoadFrom(string physics3Json)
        {
            return (string.IsNullOrEmpty(physics3Json))
                ? null
                : JsonUtility.FromJson<CubismPhysics3Json>(physics3Json);
        }

        /// <summary>
        /// Loads a motion.json asset.
        /// </summary>
        /// <param name="physics3JsonAsset">Source JSON asset.</param>
        /// <returns>Deserialized physics3.json on success; <see langword="null"/> otherwise.</returns>
        public static CubismPhysics3Json LoadFrom(TextAsset physics3JsonAsset)
        {
            return (physics3JsonAsset == null)
                ? null
                : LoadFrom(physics3JsonAsset.text);
        }

        #endregion

        #region Json Data

        [SerializeField]
        public float Version;

        [SerializeField]
        public SerializablePhysicsSetting[] Settings;

        #endregion

        /// <summary>
        /// Creates a rig from the instance.
        /// </summary>
        /// <param name="model">Cubism model</param>
        /// <returns>The rig.</returns>
        public GameObject ToRig()
        {
            var physicsRoot = new GameObject("PhysicsRig");
            

            var rootPointNames = new List<string>();
            var pointNames = new List<string>();
            var rootParameterSettingContainer = new List<CubismPhysicsParameterSettingContainer>();
            var physicalParameterSettingContainer = new List<CubismPhysicsParameterSettingContainer>();
            var linkInfos = new List<CubismPhysicsLinkInfo>();


            for (var i = 0; i < Settings.Length; i++)
            {
                var physicsSetting = Settings[i];
                
                // 物理点を設定
                for (var j = 0; j < physicsSetting.Points.Length; j++)
                {
                    var point = physicsSetting.Points[j];
                    var pp = InitializeParameterPoint(
                        point,
                        physicsRoot.transform,
                        physicalParameterSettingContainer);

                    if (point.RigidBody != null)
                    {
                        // RigidBody
                        pp.InitializeRigidBody(
                            point.RigidBody.Mass,
                            point.RigidBody.MomentOfInertia,
                            point.RigidBody.Friction,
                            point.RigidBody.AngularFriction,
                            point.RigidBody.UseGravity
                            );
                    }

                    pointNames.Add(pp.transform.name);
                }

                // ノードの情報を設定

                var linkInfosTemporary = new List<CubismPhysicsLinkInfo>();
                for (var j=0; j < physicsSetting.Links.Length; j++)
                {
                    linkInfosTemporary.Add(new CubismPhysicsLinkInfo()
                    {
                        LinkType = physicsSetting.Links[j].LinkType,
                        Parent = physicsSetting.Links[j].Parent,
                        Child = physicsSetting.Links[j].Child
                    });
                }

                // 予めControlLinkを取得
                for (var j = 0; j < linkInfosTemporary.Count; j++)
                {
                    if(linkInfosTemporary[j].LinkType != "Control") continue;
                    linkInfos.Add(linkInfosTemporary[j]);

                }

                // ControlLinkから順に親子関係の参照順に並び替える
                for (var j = 0; j < linkInfos.Count; j++)
                {
                    for (var k = 0; k < linkInfosTemporary.Count; k++)
                    {
                        var linkInfo = linkInfosTemporary[k];
                        if (linkInfos[j].Child != linkInfo.Parent) continue;
                        if (linkInfos.Contains(linkInfo)) continue;
                        linkInfos.Add(linkInfo);
                    }
                }

                for (var j = 0; j < linkInfos.Count; j++)
                {
                    if (linkInfos[j].LinkType != "Control") continue;

                    // LinkTypeがControlであるLinkのParent、つまりRootのPointは別のListで扱う
                    if (!rootPointNames.Contains(linkInfos[j].Parent))
                    {
                        rootPointNames.Add(linkInfos[j].Parent);
                    }

                    for (var k = 0; k < pointNames.Count; ++k)
                    {
                        if (pointNames[k] != linkInfos[j].Parent) continue;
                        rootParameterSettingContainer.Add(physicalParameterSettingContainer[k]);

                        // 元のListから移動したRootの情報を削除
                        pointNames.Remove(linkInfos[j].Parent);
                        physicalParameterSettingContainer.Remove(physicalParameterSettingContainer[k]);
                        break;
                    }
                }
            }


            var physicsRig = physicsRoot.AddComponent<CubismPhysicsRig>();

            physicsRig.InitializeRig(
                rootPointNames.ToArray(),
                pointNames.ToArray(),
                linkInfos.ToArray(),
                rootParameterSettingContainer.ToArray(),
                physicalParameterSettingContainer.ToArray());

            return physicsRoot;
        }

        #region Helper Methods

        private static CubismPhysicsPoint InitializeParameterPoint(
            SerializablePoint point,
            Transform parentTransform,
            List<CubismPhysicsParameterSettingContainer> settingContainers
            )
        {
            var go = new GameObject(point.Guid);

            go.transform.position = point.Position;
            go.transform.parent = parentTransform;
            var ret = go.AddComponent<CubismPhysicsPoint>();


            ret.PositionFactor = point.PositionFactor;
            ret.RotationFactor = point.RotationFactor;

            // パラメータIDの取得
            var parameterSettings = new List<CubismPhysicsParameterSetting>();
            for (var i = 0; i < point.ParameterId.Length; i++)
            {
                var parameterId = point.ParameterId[i];
                var parameterList = new List<string>();
                for (var j = 0; j < parameterId.Id.Length; j++)
                {
                    parameterList.Add(parameterId.Id[j]);
                }
                parameterSettings.Add(
                    new CubismPhysicsParameterSetting(
                        ((parameterList.ToArray().Length == 0)
                            ? null
                            : parameterList.ToArray()),
                        parameterId.ParameterType));
            }
            settingContainers.Add(
                new CubismPhysicsParameterSettingContainer
                {
                    CubismPhysicsParameterSettings = 
                        parameterSettings.ToArray()
                });

            return ret;
        }

        #endregion

        #region Json Object Types

        [Serializable]
        public class SerializablePhysicsSetting
        {
            [SerializeField]
            public SerializablePoint[] Points;
            [SerializeField]
            public SerializableLink[] Links;
        }

        [Serializable]
        public class SerializablePoint
        {
            [SerializeField]
            public string Guid = "";
            [SerializeField]
            public string Name = "";
            [SerializeField]
            public Vector3 Position;
            [SerializeField]
            public SerializableParameterIdInfo[] ParameterId;
            [SerializeField]
            public Vector3 PositionFactor;
            [SerializeField]
            public Vector3 RotationFactor;

            [SerializeField]
            public SerializableRigidBody RigidBody;
            [SerializeField]
            public Vector3 Gravity;
        }

        [Serializable]
        public class SerializableParameterIdInfo
        {
            [SerializeField]
            public string ParameterType;
            [SerializeField]
            public string[] Id;
        }


        [Serializable]
        public class SerializableRigidBody
        {
            [SerializeField]
            public float Mass;
            [SerializeField]
            public float MomentOfInertia;
            [SerializeField]
            public float Friction;
            [SerializeField]
            public float AngularFriction;
            [SerializeField]
            public bool UseGravity;
        }

        [Serializable]
        public class SerializableLink
        {
            [SerializeField]
            public string LinkType = "";
            [SerializeField]
            public string Parent = "";
            [SerializeField]
            public string Child = "";
        }

        #endregion
    }
}
