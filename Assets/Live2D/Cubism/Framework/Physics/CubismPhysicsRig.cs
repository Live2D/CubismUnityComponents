/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
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


        /// <summary>
        /// Initializes rigs.
        /// </summary>
        public void Initialize()
        {
            for (var i = 0; i < SubRigs.Length; ++i)
            {
                SubRigs[i].Initialize();
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

        /// <summary>
        /// Take over physics rig status.
        /// </summary>
        /// <remarks>
        /// The structure of the physics rig needs to be the same for the old and new models.
        /// </remarks>
        /// <param name="physicsRig"></param>
        public void TakeOverFrom(CubismPhysicsRig physicsRig)
        {
            for (var i = 0; i < SubRigs.Length; ++i)
            {
                SubRigs[i].TakeOverFrom(physicsRig.SubRigs[i]);
            }
        }
    }
}
