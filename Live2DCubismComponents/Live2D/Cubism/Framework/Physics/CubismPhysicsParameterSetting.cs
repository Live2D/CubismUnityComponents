/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using Live2D.Cubism.Core;
using UnityEngine;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Setting of paramters syncronized to Points
    /// </summary>
    [Serializable]
    public class CubismPhysicsParameterSetting
    {
        /// <summary>
        /// List of parameters for physics
        /// </summary>
        [SerializeField]
        public string[] ParameterIds;

        /// <summary>
        /// 物理演算の入出力に使用するパラメータ群
        /// </summary>
        [NonSerialized, HideInInspector]
        public CubismParameter[] Parameters;

        /// <summary>
        /// Type which parameters are syncronized
        /// </summary>
        [SerializeField]
        public ParameterType ParameterType;

        /// <summary>
        /// Create the setting of parameter
        /// </summary>
        /// <param name="parameterIds">Parameters for physics</param>
        /// <param name="parameterType">Type of syncronized parameter</param>
        public CubismPhysicsParameterSetting(string[] parameterIds, string parameterType)
        {
            ParameterIds = parameterIds;
            
            if (parameterType == ParameterType.X.ToString())
            {
                ParameterType = ParameterType.X;
            }
            else if (parameterType == ParameterType.Y.ToString())
            {
                ParameterType = ParameterType.Y;
            }
            else if (parameterType == ParameterType.Z.ToString())
            {
                ParameterType = ParameterType.Z;
            }
            else if (parameterType == ParameterType.RotateX.ToString())
            {
                ParameterType = ParameterType.RotateX;
            }
            else if (parameterType == ParameterType.RotateY.ToString())
            {
                ParameterType = ParameterType.RotateY;
            }
            else if (parameterType == ParameterType.RotateZ.ToString())
            {
                ParameterType = ParameterType.RotateZ;
            }
        }
    }

    /// <summary>
    /// Parameters syncronized to Point movement
    /// </summary>
    public enum ParameterType
    {
        X=0,
        Y,
        Z,
        RotateX,
        RotateY,
        RotateZ
    }
}
