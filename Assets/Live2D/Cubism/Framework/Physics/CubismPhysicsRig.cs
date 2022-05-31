/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Collections.Generic;
using UnityEngine;


namespace Live2D.Cubism.Framework.Physics
{
    /// <summary>
    /// Physics rig.
    /// </summary>
    [Serializable]
    public class CubismPhysicsRig
    {
        /// <summary>
        /// Children of rig.
        /// </summary>
        [SerializeField]
        public CubismPhysicsSubRig[] SubRigs;


        [SerializeField]
        public Vector2 Gravity = CubismPhysics.Gravity;


        [SerializeField]
        public Vector2 Wind = CubismPhysics.Wind;


        /// <summary>
        /// Reference of controller to refer from children rig.
        /// </summary>
        public CubismPhysicsController Controller { get; set; }

        private Dictionary<string, CubismPhysicsSubRig> _subRigNameTable;

        /// <summary>
        /// Get <see cref="CubismPhysicsSubRig"/> by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CubismPhysicsSubRig GetSubRig(string name) => _subRigNameTable.TryGetValue(name, out var subRig) ? subRig : null;

        /// <summary>
        /// Initializes rigs.
        /// </summary>
        public void Initialize()
        {
            if (_subRigNameTable == null)
                _subRigNameTable = new Dictionary<string,CubismPhysicsSubRig>();
            else
                _subRigNameTable.Clear();

            for (var i = 0; i < SubRigs.Length; ++i)
            {
                var subRig = SubRigs[i];
                subRig.Initialize();
                _subRigNameTable[subRig.Name] = subRig;
            }
        }

        /// <summary>
        /// Evaluate rigs.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Evaluate(float deltaTime)
        {
            for (var i = 0; i < SubRigs.Length; ++i)
            {
                SubRigs[i].Evaluate(deltaTime);
            }
        }
    }
}
