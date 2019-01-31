/*
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using System;
using UnityEngine;
using Live2D.Cubism.Framework.Json;

namespace Live2D.Cubism.Framework.Pose
{
    /// <summary>
    /// Cubism pose data.
    /// </summary>
    [Serializable]
    public struct CubismPoseData
    {
        /// <summary>
        /// Cubism pose type.
        /// </summary>
        [SerializeField]
        public string Type;

        /// <summary>
        /// Cubism pose fade in time.
        /// </summary>
        [SerializeField]
        public float FadeInTime;

        /// <summary>
        /// Cubism pose groups.
        /// </summary>
        [SerializeField]
        public PoseGroup[] Groups;

        /// <summary>
        /// Cubism pose group.
        /// </summary>
        [Serializable]
        public struct PoseGroup
        {
            /// <summary>
            /// Cubism pose datas.
            /// </summary>
            [SerializeField]
            public PoseData[] PoseDatas;
        }

        /// <summary>
        /// Cubism pose data.
        /// </summary>
        [Serializable]
        public struct PoseData
        {
            /// <summary>
            /// Cubism pose part id.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// Cubism pose link part id.
            /// </summary>
            [SerializeField]
            public string[] Link;
        }

        /// <summary>
        /// Initialize cubism pose data form <see cref="CubismPose3Json"/>.
        /// </summary>
        public void Initialize(CubismPose3Json pose3Json)
        {
            // Fail silently...
            if(pose3Json == null)
            {
                return;
            }

            Type = pose3Json.Type;
            FadeInTime = pose3Json.FadeInTime;

            var groupCount = pose3Json.Groups.Length;
            Array.Resize(ref Groups, groupCount);

            for(var groupIndex = 0; groupIndex < groupCount; ++groupIndex)
            {
                var partCount = pose3Json.Groups[groupIndex].Length;
                Array.Resize(ref Groups[groupIndex].PoseDatas, partCount);

                for(var patrIndex = 0; patrIndex < partCount; ++patrIndex)
                {
                    Groups[groupIndex].PoseDatas[patrIndex].Id = pose3Json.Groups[groupIndex][patrIndex].Id;
                    if(pose3Json.Groups[groupIndex][patrIndex].Link != null)
                    {
                        Groups[groupIndex].PoseDatas[patrIndex].Link = pose3Json.Groups[groupIndex][patrIndex].Link;
                    }
                }
            }
        }
    }
}