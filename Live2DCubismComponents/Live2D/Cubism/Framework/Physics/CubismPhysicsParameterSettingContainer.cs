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
    /// Settings of parameters syncronized to Point
    /// </summary>
    [Serializable]
    public class CubismPhysicsParameterSettingContainer
    {
        /// <summary>
        /// Array parameters syncronized to Point
        /// It is able to set parameters to each <see cref="ParameterType"/>
        /// </summary>
        [SerializeField]
        public CubismPhysicsParameterSetting[] CubismPhysicsParameterSettings;
    }
}