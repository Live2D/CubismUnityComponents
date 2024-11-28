﻿/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using UnityEngine;

namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Handles display info from cdi3.json.
    /// </summary>
    [Serializable]
    public sealed class CubismDisplayInfo3Json
    {
        /// <summary>
        /// Loads a cdi3.json.
        /// </summary>
        /// <param name="cdi3Json">cdi3.json to deserialize.</param>
        /// <returns>Deserialized cdi3.json on success; <see langword="null"/> otherwise.</returns>
        public static CubismDisplayInfo3Json LoadFrom(string cdi3Json)
        {
            if (string.IsNullOrEmpty(cdi3Json))
            {
                return null;
            }

            var ret = JsonUtility.FromJson<CubismDisplayInfo3Json>(cdi3Json);

            // Parse CombinedParameters.
            // Unity is unable to parse the double array.
            var value = CubismJsonParser.ParseFromString(cdi3Json);

            // Return early if there is no references.
            if (value.Get("CombinedParameters") == null)
            {
                return ret;
            }

            // Get CombinedParameters from json data.
            var rawCombinedParameters = value.Get("CombinedParameters").GetVector(null);
            ret.CombinedParameters = new SerializableCombinedParameterIds[rawCombinedParameters.Count];

            for (var combinedParameterIndex = 0; combinedParameterIndex < rawCombinedParameters.Count; combinedParameterIndex++)
            {
                var combinedParameter = rawCombinedParameters[combinedParameterIndex];
                var ids = new string[combinedParameter.GetVector(null).Count];

                // Set ids.
                for (var idIndex = 0; idIndex < ids.Length; idIndex++)
                {
                    ids.SetValue(combinedParameter.GetVector(null)[idIndex].toString(), idIndex);
                }

                // Set combined parameters.
                ret.CombinedParameters[combinedParameterIndex] = new SerializableCombinedParameterIds
                {
                    Ids = ids
                };
            }

            return ret;
        }


        #region Json Data

        /// <summary>
        /// Json file format version.
        /// </summary>
        [SerializeField]
        public int Version;

        /// <summary>
        /// Array of model parameters.
        /// </summary>
        [SerializeField]
        public SerializableParameters[] Parameters;

        /// <summary>
        /// Array of ParameterGroups.
        /// </summary>
        [SerializeField]
        public SerializableParameterGroups[] ParameterGroups;

        /// <summary>
        /// Array of Parts.
        /// </summary>
        [SerializeField]
        public SerializableParts[] Parts;

        [SerializeField]
        public SerializableCombinedParameterIds[] CombinedParameters;

        #endregion

        #region Json Helpers

        [Serializable]
        public struct SerializableParameters
        {
            /// <summary>
            /// The ID of the parameter.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// The Group ID of the parameter.
            /// </summary>
            [SerializeField]
            public string GroupId;

            /// <summary>
            /// The Name of the parameter.
            /// </summary>
            [SerializeField]
            public string Name;
        }

        [Serializable]
        public struct SerializableParameterGroups
        {
            /// <summary>
            /// The ID of the parameter.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// The Group ID of the parameter.
            /// </summary>
            [SerializeField]
            public string GroupId;

            /// <summary>
            /// The Name of the parameter.
            /// </summary>
            [SerializeField]
            public string Name;
        }

        [Serializable]
        public struct SerializableParts
        {
            /// <summary>
            /// The ID of the part.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// The Name of the part.
            /// </summary>
            [SerializeField]
            public string Name;
        }

        [Serializable]
        public struct SerializableCombinedParameterIds
        {
            /// <summary>
            /// The ID of the part.
            /// </summary>
            [SerializeField]
            public string[] Ids;
        }

        public struct CombinedParameter
        {
            /// <summary>
            /// The Id of the parameter that becomes the X-axis when combined.
            /// </summary>
            [SerializeField]
            public string HorizontalParameterId;

            /// <summary>
            /// The Id of the parameter that becomes the Y-axis when combined.
            /// </summary>
            [SerializeField]
            public string VerticalParameterId;
        }

        #endregion
    }
}
