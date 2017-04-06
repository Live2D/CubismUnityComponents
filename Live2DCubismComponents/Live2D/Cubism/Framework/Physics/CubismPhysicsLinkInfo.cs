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
    /// Setting of <see cref="CubismPhysicsLink"/>
    /// </summary>
    [Serializable]
    public class CubismPhysicsLinkInfo
    {
        [SerializeField] public string LinkType = "";
        [SerializeField] public string Parent = "";
        [SerializeField] public string Child = "";
    }
}