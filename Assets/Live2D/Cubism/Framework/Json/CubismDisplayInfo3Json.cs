/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Collections.Generic;
using UnityEngine;


namespace Live2D.Cubism.Framework.Json
{
    /// <summary>
    /// Handles user data from cdi3.json.
    /// </summary>
    [Serializable]
    public sealed class CubismDisplayInfo3Json
    {
        /// <summary>
        /// Loads a cdi3.json asset.
        /// </summary>
        /// <param name="displayInfo3Json">cdi3.json to deserialize.</param>
        /// <returns>Deserialized cdi3.json on success; <see langword="null"/> otherwise.</returns>
        public static CubismDisplayInfo3Json LoadFrom(string displayInfo3Json)
        {
            return (string.IsNullOrEmpty(displayInfo3Json))
                ? null
                : JsonUtility.FromJson<CubismDisplayInfo3Json>(displayInfo3Json);
        }

        /// <summary>
        /// Loads a cdi3.json asset.
        /// </summary>
        /// <param name="displayInfo3JsonAsset">cdi3.json to deserialize.</param>
        /// <returns>Deserialized cdi3.json on success; <see langword="null"/> otherwise.</returns>
        public static CubismDisplayInfo3Json LoadFrom(TextAsset displayInfo3JsonAsset)
        {
            return (displayInfo3JsonAsset == null)
                ? null
                : LoadFrom(displayInfo3JsonAsset.text);
        }

        #region Json Data

        /// <summary>
        /// Json file format version.
        /// </summary>
        [SerializeField]
        public int Version;

        /// <summary>
        /// Array of parameters data.
        /// </summary>
        [SerializeField]
        public SerializableDisplayInfo[] Parameters;

        /// <summary>
        /// Array of parameters groups data.
        /// </summary>
        [SerializeField]
        public SerializableDisplayInfo[] ParameterGroups;

        /// <summary>
        /// Array of parts data.
        /// </summary>
        [SerializeField]
        public SerializablePartInfo[] Parts;

        #endregion


        #region Json Helpers

        /// <summary>
        /// Display info.
        /// </summary>
        [Serializable]
        public struct SerializableDisplayInfo
        {
            /// <summary>
            /// Id of target object.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// GroupId of target object.
            /// </summary>
            [SerializeField]
            public string GroupId;

            /// <summary>
            /// Name of target object.
            /// </summary>
            [SerializeField]
            public string Name;
        }

        /// <summary>
        /// Parts info.
        /// </summary>
        [Serializable]
        public struct SerializablePartInfo
        {
            /// <summary>
            /// Id of target object.
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// Name of target object.
            /// </summary>
            [SerializeField]
            public string Name;
        }

        #endregion
    }
}
