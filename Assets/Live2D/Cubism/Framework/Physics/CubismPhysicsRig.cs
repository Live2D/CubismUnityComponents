/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using Live2D.Cubism.Core;
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

        [SerializeField]
        public float Fps = 0.0f;


        private float _currentRemainTime; // Time not processed by physics.

        public float[] ParametersCache
        {
            get { return _parametersCache; }
            set { _parametersCache = value; }
        }

        [NonSerialized]
        private float[] _parametersCache; // Cache parameters used by Evaluate.

        /// <summary>
        /// Reference of controller to refer from children rig.
        /// </summary>
        public CubismPhysicsController Controller { get; set; }

        /// <summary>
        /// Initializes rigs.
        /// </summary>
        public void Initialize()
        {
            _currentRemainTime = 0.0f;

            Controller.gameObject.FindCubismModel();

            _parametersCache = new float[Controller.Parameters.Length];

            for (var i = 0; i < SubRigs.Length; ++i)
            {
                SubRigs[i].Initialize();
            }
        }

        /// <summary>
        /// Evaluate rigs.
        ///
        /// Pendulum interpolation weights
        ///
        /// The result of the pendulum calculation is saved and
        /// the output to the parameters is interpolated with the saved previous result of the pendulum calculation.
        ///
        /// The figure shows the interpolation between [1] and [2].
        ///
        /// The weight of the interpolation are determined by the current time seen between
        /// the latest pendulum calculation timing and the next timing.
        ///
        /// Figure shows the weight of position (3) as seen between [2] and [4].
        ///
        /// As an interpretation, the pendulum calculation and weights are misaligned.
        ///
        /// If there is no FPS information in physics3.json, it is always set in the previous pendulum state.
        ///
        /// The purpose of this specification is to avoid the quivering appearance caused by deviations from the interpolation range.
        ///
        /// ------------ time -------------->
        ///
        ///    　　　　　　　　|+++++|------| <- weight
        /// ==[1]====#=====[2]---(3)----(4)
        ///          ^ output contents
        ///
        /// 1: _previousRigOutput
        /// 2: _currentRigOutput
        /// 3: _currentRemainTime (now rendering)
        /// 4: next particles timing
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Evaluate(float deltaTime)
        {
            if (0.0f >= deltaTime)
            {
                return;
            }

            _currentRemainTime += deltaTime;
            if (_currentRemainTime > CubismPhysics.MaxDeltaTime)
            {
                _currentRemainTime = 0.0f;
            }

            var physicsDeltaTime = 0.0f;

            if (Fps > 0.0f)
            {
                physicsDeltaTime = 1.0f / Fps;
            }
            else
            {
                physicsDeltaTime = deltaTime;
            }

            if (_parametersCache.Length < Controller.Parameters.Length)
            {
                _parametersCache = new float[Controller.Parameters.Length];
            }

            while (_currentRemainTime >= physicsDeltaTime)
            {
                // copy parameter model to cache
                for (var i = 0; i < Controller.Parameters.Length; i++)
                {
                    _parametersCache[i] = Controller.Parameters[i].Value;
                }

                for (var i = 0; i < SubRigs.Length; ++i)
                {
                    SubRigs[i].Evaluate(physicsDeltaTime);
                }

                _currentRemainTime -= physicsDeltaTime;
            }

            float alpha = _currentRemainTime / physicsDeltaTime;
            for (var i = 0; i < SubRigs.Length; ++i)
            {
                SubRigs[i].Interpolate(alpha);
            }
        }
    }
}
