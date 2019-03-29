/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

using Live2D.Cubism.Framework.MotionFade;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Live2D.Cubism.Framework.Motion
{
    /// <summary>
    /// Cubism motion layer.
    /// </summary>
    public class CubismMotionLayer : ICubismFadeState
    {
        #region Action

        /// <summary>
        /// Action animation end handler.
        /// </summary>
        public Action<float> AnimationEndHandler;

        #endregion

        #region Variable

        /// <summary>
        /// Playable output.
        /// </summary>
        public AnimationMixerPlayable PlayableOutput { get; private set; }

        /// <summary>
        /// Playable output.
        /// </summary>
        private PlayableGraph _playableGraph;

        /// <summary>
        /// Cubism playing motions.
        /// </summary>
        private List<CubismFadePlayingMotion> _playingMotions;

        /// <summary>
        /// Cubism playing motions.
        /// </summary>
        private List<CubismMotionState> _motionStates;

        /// <summary>
        /// List of cubism fade motion.
        /// </summary>
        private CubismFadeMotionList _cubismFadeMotionList;

        /// <summary>
        /// Layer weight.
        /// </summary>
        private float _layerWeight;

        /// <summary>
        /// Animation is finished.
        /// </summary>
        private bool _isFinished;

        /// <summary>
        /// Is finished.
        /// </summary>
        /// <returns>True if the animation is finished, false otherwise.</returns>
        public bool IsFinished
        {
            get { return _isFinished; }
        }

        #endregion

        #region Fade State Interface

        /// <summary>
        /// Get cubism playing motion list.
        /// </summary>
        /// <returns>Cubism playing motion list.</returns>
        public List<CubismFadePlayingMotion> GetPlayingMotions()
        {
            return _playingMotions;
        }

        /// <summary>
        /// Is default state.
        /// </summary>
        /// <returns><see langword="true"/> State is default; <see langword="false"/> otherwise.</returns>
        public bool IsDefaultState()
        {
            return false;
        }

        /// <summary>
        /// Get layer weight.
        /// </summary>
        /// <returns>Layer weight.</returns>
        public float GetLayerWeight()
        {
            return _layerWeight;
        }

        /// <summary>
        /// Get state transition finished.
        /// </summary>
        /// <returns><see langword="true"/> State transition is finished; <see langword="false"/> otherwise.</returns>
        public bool GetStateTransitionFinished()
        {
            return true;
        }

        /// <summary>
        /// Set state transition finished.
        /// </summary>
        /// <param name="isFinished">State is finished.</param>
        public void SetStateTransitionFinished(bool isFinished) {}

        /// <summary>
        /// Stop animation.
        /// </summary>
        /// <param name="index">Playing motion index.</param>
        public void StopAnimation(int index)
        {
            if(index == 0 && _motionStates.Count == 1)
            {
                _isFinished = true;
#if UNITY_2017_3_OR_NEWER
                _motionStates[0].ClipMixer.GetInput(0).Pause();
#else
                _motionStates[0].ClipMixer.GetInput(0).SetPlayState(PlayState.Paused);
#endif
                return;
            }

            // Disconnect from previou state.
            var preMixer = (index == 0) ? PlayableOutput : _motionStates[index - 1].ClipMixer;
            var lastInput = (index == 0) ? 0 : _motionStates[index - 1].ClipMixer.GetInputCount() - 1;

#if UNITY_2018_2_OR_NEWER
            preMixer.DisconnectInput(lastInput);
#else
            preMixer.GetGraph().Disconnect(preMixer, lastInput);
#endif

            // Connect next state.
            if(index + 1 < _motionStates.Count)
            {
#if UNITY_2018_2_OR_NEWER
                _motionStates[index].ClipMixer.DisconnectInput(_motionStates[index].ClipMixer.GetInputCount() - 1);
#else
                _motionStates[index].ClipMixer.GetGraph().Disconnect(_motionStates[index].ClipMixer, _motionStates[index].ClipMixer.GetInputCount() - 1);
#endif

                preMixer.ConnectInput(lastInput, _motionStates[index + 1].ClipMixer, 0);
                preMixer.SetInputWeight(lastInput, 1.0f);
            }

            // Remove from motion state list.
            _motionStates.RemoveAt(index);

            // Remove from playing motion list.
            _playingMotions.RemoveAt(index);
        }

        #endregion

        #region Function

        /// <summary>
        /// Initialize motion layer.
        /// </summary>
        /// <param name="playableGraph">.</param>
        /// <param name="fadeMotionList">.</param>
        /// <param name="layerWeight">.</param>
        public static CubismMotionLayer CreateCubismMotionLayer(PlayableGraph playableGraph, CubismFadeMotionList fadeMotionList, float layerWeight = 1.0f)
        {
            var ret = new CubismMotionLayer();

            ret._playableGraph = playableGraph;
            ret._cubismFadeMotionList = fadeMotionList;
            ret._layerWeight = layerWeight;
            ret._isFinished = false;
            ret._motionStates = new List<CubismMotionState>();
            ret._playingMotions = new List<CubismFadePlayingMotion>();
            ret.PlayableOutput = AnimationMixerPlayable.Create(playableGraph, 1);

            return ret;
        }

        /// <summary>
        /// Create fade playing motion.
        /// </summary>
        /// <param name="clip">Animator clip.</param>
        /// <param name="speed">Animation speed.</param>
        private CubismFadePlayingMotion CreateFadePlayingMotion(AnimationClip clip, float speed = 1.0f)
        {
            var ret = new CubismFadePlayingMotion();

            var isNotFound = true;
            var instanceId = -1;
            var events = clip.events;
            for(var i = 0; i < events.Length; ++i)
            {
                if(events[i].functionName != "InstanceId")
                {
                    continue;
                }

                instanceId = events[i].intParameter;
            }

            for (int i = 0; i < _cubismFadeMotionList.MotionInstanceIds.Length; i++)
            {
                if(_cubismFadeMotionList.MotionInstanceIds[i] != instanceId)
                {
                    continue;
                }

                isNotFound = false;

                ret.Speed = speed;
                ret.StartTime = Time.time;
                ret.FadeInStartTime = Time.time;
                ret.Motion = _cubismFadeMotionList.CubismFadeMotionObjects[i];
                ret.EndTime = (ret.Motion.MotionLength <= 0)
                              ? -1
                              : ret.StartTime + ret.Motion.MotionLength / speed;

                break;
            }

            if(isNotFound)
            {
                Debug.LogError("CubismMotionController : Not found motion from CubismFadeMotionList.");
            }

            return ret;
        }

        /// <summary>
        /// Play animation.
        /// </summary>
        /// <param name="clip">Animation clip.</param>
        /// <param name="isLoop">Animation is loop.</param>
        /// <param name="speed">Animation speed.</param>
        public void PlayAnimation(AnimationClip clip, bool isLoop = true, float speed = 1.0f)
        {
            // Create cubism motion state.
            var state = CubismMotionState.CreateCubismMotionState(_playableGraph, clip, isLoop, speed);

            if(_motionStates.Count > 0)
            {
                _motionStates[_motionStates.Count - 1].ConnectClipMixer(state.ClipMixer);
            }
            else
            {
#if UNITY_2018_2_OR_NEWER
                PlayableOutput.DisconnectInput(0);
#else
                PlayableOutput.GetGraph().Disconnect(PlayableOutput, 0);
#endif
                PlayableOutput.ConnectInput(0, state.ClipMixer, 0);
                PlayableOutput.SetInputWeight(0, 1.0f);
            }

            _motionStates.Add(state);

            // Set last motion end time and fade in start time;
            if(_playingMotions.Count > 0)
            {
                var lastMotion = _playingMotions[_playingMotions.Count - 1];
                lastMotion.FadeInStartTime = Time.time;
                lastMotion.EndTime = Time.time + lastMotion.Motion.FadeOutTime;
                _playingMotions[_playingMotions.Count - 1] = lastMotion;
            }

            // Create fade playing motion.
            var playingMotion = CreateFadePlayingMotion(clip, speed);
            _playingMotions.Add(playingMotion);

            _isFinished = false;
        }

        /// <summary>
        /// Stop all animation.
        /// </summary>
        public void StopAllAnimation()
        {
            for(var i = _playingMotions.Count - 1; i >= 0; --i)
            {
                StopAnimation(i);
            }
        }

        /// <summary>
        /// Set layer weight.
        /// </summary>
        /// <param name="weight">Layer weight.</param>
        public void SetLayerWeight(float weight)
        {
            _layerWeight = weight;
        }

        /// <summary>
        /// Set state speed.
        /// </summary>
        /// <param name="index">index of playing motion list.</param>
        /// <param name="speed">Animation speed.</param>
        public void SetStateSpeed(int index, float speed)
        {
            // Fail silently...
            if(index < 0 || index > _motionStates.Count)
            {
                return;
            }

            var playingMotionData = _playingMotions[index]; 
            playingMotionData.Speed = speed;
            playingMotionData.EndTime = (playingMotionData.EndTime - Time.time) / speed;
            _playingMotions[index] = playingMotionData;

            _motionStates[index].ClipMixer.SetSpeed(speed);
            _motionStates[index].ClipPlayable.SetDuration(_motionStates[index].Clip.length / speed - 0.0001f);
        }

        /// <summary>
        /// Set state is loop.
        /// </summary>
        /// <param name="index">index of playing motion list.</param>
        /// <param name="isLoop">Animation is loop.</param>
        public void SetStateIsLoop(int index, bool isLoop)
        {
            // Fail silently...
            if(index < 0 || index > _motionStates.Count)
            {
                return;
            }

            if(isLoop)
            {
                _motionStates[index].ClipPlayable.SetDuration(double.MaxValue);
            }
            else
            {
                _motionStates[index].ClipPlayable.SetDuration(_motionStates[index].Clip.length - 0.0001f);
            }
        }

        #endregion

        public void Update()
        {
            // Fail silently...
            if (AnimationEndHandler == null || _playingMotions.Count != 1 || _isFinished
             || _motionStates[0].ClipPlayable.GetDuration() == double.MaxValue || Time.time <= _playingMotions[0].EndTime)
            {
                return;
            }

            _isFinished = true;
            var instanceId = -1;
            var events = _motionStates[0].Clip.events;
            for (var i = 0; i < events.Length; ++i)
            {
                if (events[i].functionName != "InstanceId")
                {
                    continue;
                }

                instanceId = events[i].intParameter;
            }

            AnimationEndHandler(instanceId);
        }
    }
}