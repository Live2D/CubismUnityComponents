﻿/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.MotionFade;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


namespace Live2D.Cubism.Framework.Motion
{
    /// <summary>
    /// Cubism motion controller.
    /// </summary>
    [RequireComponent(typeof(CubismFadeController))]
    public class CubismMotionController : MonoBehaviour, ICubismUpdatable
    {
        #region Action

        /// <summary>
        /// Action animation end handler.
        /// </summary>
        public Action<float> AnimationEndHandler;

        /// <summary>
        /// Action OnAnimationEnd.
        /// </summary>
        private void OnAnimationEnd(float instanceId)
        {
            if(AnimationEndHandler != null)
            {
                AnimationEndHandler(instanceId);
            }
        }

        #endregion

        #region Variable

        /// <summary>
        /// Layer count.
        /// </summary>
        public int LayerCount = 1;

        /// <summary>
        /// List of cubism fade motion.
        /// </summary>
        private CubismFadeMotionList _cubismFadeMotionList;

        /// <summary>
        /// Motion controller is active.
        /// </summary>
        private bool _isActive = false;

        /// <summary>
        /// Playable graph controller.
        /// </summary>
        private PlayableGraph _playableGrap;

        /// <summary>
        /// Playable output.
        /// </summary>
        private AnimationPlayableOutput _playableOutput;

        /// <summary>
        /// Animation layer mixer.
        /// </summary>
        private AnimationLayerMixerPlayable _layerMixer;

        /// <summary>
        /// Cubism motion layers.
        /// </summary>
        private CubismMotionLayer[] _motionLayers;

        /// <summary>
        /// Cubism model has CubismUpdateController.
        /// </summary>
        private bool _hasUpdateController;

        #endregion Variable

        #region Function

        /// <summary>
        /// Play animations.
        /// </summary>
        /// <param name="clip">Animator clip.</param>
        /// <param name="layerIndex">layer index.</param>
        /// <param name="isLoop">Animation is loop.</param>
        /// <param name="speed">Animation speed.</param>
        public void PlayAnimations(AnimationClip clip, int layerIndex = 0, bool isLoop = true, float speed = 1.0f)
        {
            // Fail silently...
            if(!enabled || !_isActive || _cubismFadeMotionList == null || clip == null
               || layerIndex < 0 || layerIndex >= LayerCount)
            {
                return;
            }

            _motionLayers[layerIndex].PlayAnimations(clip, isLoop, speed);

            // Play Playable Graph
            if(!_playableGrap.IsPlaying())
            {
                _playableGrap.Play();
            }
        }

        /// <summary>
        /// Stop animation.
        /// </summary>
        /// <param name="animationIndex">Animator index.</param>
        /// <param name="layerIndex">layer index.</param>
        public void StopAnimation(int animationIndex, int layerIndex = 0)
        {
            // Fail silently...
            if(layerIndex < 0 || layerIndex >= LayerCount)
            {
                return;
            }

            _motionLayers[layerIndex].StopAnimation(animationIndex);
        }

        /// <summary>
        /// Stop all animation.
        /// </summary>
        public void StopAllAnimation()
        {
            for(var i = 0; i < LayerCount; ++i)
            {
                _motionLayers[i].StopAllAnimation();
            }
        }

        /// <summary>
        /// Set layer weight.
        /// </summary>
        /// <param name="layerIndex">layer index.</param>
        /// <param name="weight">Layer weight.</param>
        public void SetLayerWeight(int layerIndex, float weight)
        {
            // Fail silently...
            if(layerIndex < 0 || layerIndex >= LayerCount)
            {
                return;
            }

            _motionLayers[layerIndex].SetLayerWeight(weight);
            _layerMixer.SetInputWeight(layerIndex, weight);
        }

        /// <summary>
        /// Set layer blend type is additive.
        /// </summary>
        /// <param name="layerIndex">layer index.</param>
        /// <param name="isAdditive">Blend type is additive.</param>
        public void SetLayerAdditive(int layerIndex, bool isAdditive)
        {
            // Fail silently...
            if(layerIndex < 0 || layerIndex >= LayerCount)
            {
                return;
            }

            _layerMixer.SetLayerAdditive((uint)layerIndex, isAdditive);
        }

        /// <summary>
        /// Set state is loop.
        /// </summary>
        /// <param name="layerIndex">layer index.</param>
        /// <param name="index">Index of playing motion list.</param>
        /// <param name="isLoop">State is loop.</param>
        public void SetIsLoop(int layerIndex, int index, bool isLoop)
        {
            // Fail silently...
            if(layerIndex < 0 || layerIndex >= LayerCount)
            {
                return;
            }

            _motionLayers[layerIndex].SetStateIsLoop(index, isLoop);
        }

        /// <summary>
        /// Get cubism fade states.
        /// </summary>
        public ICubismFadeState[] GetFadeStates()
        {
            return _motionLayers;
        }

        /// <summary>
        /// Called by CubismUpdateController.
        /// </summary>
        public void OnLateUpdate()
        {
            for(var i = 0; i < _motionLayers.Length; ++i)
            {
                _motionLayers[i].OnLateUpdate();
            }
        }

        #endregion Function

        #region Unity Events Handling

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void OnEnable()
        {
            _hasUpdateController = (GetComponent<CubismUpdateController>() != null);
            _cubismFadeMotionList = GetComponent<CubismFadeController>().CubismFadeMotionList;

            // Fail silently...
            if(_cubismFadeMotionList == null)
            {
                Debug.LogError("CubismMotionController : CubismFadeMotionList doesn't set in CubismFadeController.");
                return;
            }

            // Get Animator.
            var animator = GetComponent<Animator>();

            if (animator.runtimeAnimatorController != null)
            {
                Debug.LogWarning("Animator Controller was set in Animator component.");
                return;
            }

            _isActive = true;

            // Disabble animator's playablegrap.
            var graph = animator.playableGraph;

            if(graph.IsValid())
            {
                graph.GetOutput(0).SetWeight(0);
            }
            
            // Create Playable Graph.
            _playableGrap = PlayableGraph.Create("Playable Graph : " + this.FindCubismModel().name);
            _playableGrap.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            // Create Playable Output.
            _playableOutput = AnimationPlayableOutput.Create(_playableGrap, "Animation", animator);
            _playableOutput.SetWeight(1);

            // Create animation layer mixer.
            _layerMixer = AnimationLayerMixerPlayable.Create(_playableGrap, LayerCount);

            // Create cubism motion layers.
            _motionLayers = new CubismMotionLayer[LayerCount];
            for(var i = 0; i < LayerCount; ++i)
            {
                _motionLayers[i] = CubismMotionLayer.CreateCubismMotionLayer(_playableGrap, _cubismFadeMotionList);
                _motionLayers[i].AnimationEndHandler += OnAnimationEnd;
                _layerMixer.ConnectInput(i, _motionLayers[i].PlayableOutput, 0);
                _layerMixer.SetInputWeight(i, 1.0f);
            }

            // Set Playable Output.
            _playableOutput.SetSourcePlayable(_layerMixer);

        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void OnDisable()
        {
            // Destroy _playableGrap.
            if(_playableGrap.IsValid())
            {
                _playableGrap.Destroy();
            }
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        private void LateUpdate()
        {
            if(!_hasUpdateController)
            {
                OnLateUpdate();
            }
        }

        #endregion Unity Events Handling
    }
}