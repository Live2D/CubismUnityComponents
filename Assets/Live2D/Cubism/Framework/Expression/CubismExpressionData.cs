/*
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.Json;
using System;
using UnityEngine;

namespace Live2D.Cubism.Framework.Expression
{
    public class CubismExpressionData : ScriptableObject
    {
        /// <summary>
        /// Expression type.
        /// </summary>
        [SerializeField]
        public string Type;

        /// <summary>
        /// Expression fade in time.
        /// </summary>
        [SerializeField]
        public float FadeInTime;

        /// <summary>
        /// Expression fade out time.
        /// </summary>
        [SerializeField]
        public float FadeOutTime;

        /// <summary>
        /// Exression Parameters
        /// </summary>
        [SerializeField]
        public SerializableExpressionParameter[] Parameters;

        /// <summary>
        /// ExpressionParameter
        /// </summary>
        [Serializable]
        public struct SerializableExpressionParameter
        {
            /// <summary>
            /// Expression Parameter Id
            /// </summary>
            [SerializeField]
            public string Id;

            /// <summary>
            /// Expression Parameter Value
            /// </summary>
            [SerializeField]
            public float Value;

            /// <summary>
            /// Expression Parameter Blend Mode
            /// </summary>
            [SerializeField]
            public string Blend;
        }

        public static CubismExpressionData CreateInstance(CubismExp3Json json)
        {
            var expressionData = CreateInstance<CubismExpressionData>();

            expressionData.Type = json.Type;
            expressionData.FadeInTime = json.FadeInTime;
            expressionData.FadeOutTime = json.FadeOutTime;
            expressionData.Parameters = new SerializableExpressionParameter[json.Parameters.Length];

            for(var i = 0; i < json.Parameters.Length; ++i)
            {
                expressionData.Parameters[i].Id = json.Parameters[i].Id;
                expressionData.Parameters[i].Value = json.Parameters[i].Value;
                expressionData.Parameters[i].Blend = json.Parameters[i].Blend;
            }

            return expressionData;
        }

    }

}