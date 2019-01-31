
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;


namespace Live2D.Cubism.Framework.MotionFade
{
    public class CubismFadeStateObserver : StateMachineBehaviour
    {
        public List<CubismFadePlayingMotion> PlayingMotions { private set; get; }

        public bool IsStateTransitionFinished { set; get; }

        /// <summary>
        /// 
        /// </summary>
        public int LayerIndex { private set; get; }

        /// <summary>
        /// Weight of layer that attatched this 
        /// </summary>
        public float LayerWeight { private set; get; }


        /// <summary>
        /// 
        /// </summary>
        public bool IsDefaultState { private set; get; }

        private CubismFadeMotionList _cubismFadeMotionList;

        private void OnEnable()
        {
            IsStateTransitionFinished = false;

            if (PlayingMotions == null)
            {
                PlayingMotions = new List<CubismFadePlayingMotion>();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="stateInfo"></param>
        /// <param name="layerIndex"></param>
        /// <param name="controller"></param>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex, AnimatorControllerPlayable controller)
        {
            var fadeController = animator.gameObject.GetComponent<CubismFadeController>();

            // Fail silently...
            if (fadeController == null)
            {
                return;
            }

            _cubismFadeMotionList = fadeController.CubismFadeMotionList;

            LayerIndex = layerIndex;
            LayerWeight = (LayerIndex == 0)
                ? 1.0f
                : animator.GetLayerWeight(LayerIndex);


            var animatorClipInfo = controller.GetNextAnimatorClipInfo(layerIndex);


            IsDefaultState = (animatorClipInfo.Length == 0);

            if (IsDefaultState)
            {
                // 初回のみDefault Stateのモーションを取得する
                animatorClipInfo = controller.GetCurrentAnimatorClipInfo(layerIndex);
            }

            // Set playing motions end time.
            for(var i = 0; i < PlayingMotions.Count; ++i)
            {
                var motion = PlayingMotions[i];

                if (motion.Motion == null)
                {
                    continue;
                }

                var newEndTime = Time.time + motion.Motion.FadeOutTime;

                if (motion.EndTime < 0.0f || newEndTime < motion.EndTime)
                {
                    motion.EndTime = newEndTime;
                }
            }

            for (var i = 0; i < animatorClipInfo.Length; ++i)
            {
                CubismFadePlayingMotion playingMotion;

                var motionIndex = -1;
                for (var j = 0; j < _cubismFadeMotionList.MotionInstanceIds.Length; ++j)
                {
                    if (_cubismFadeMotionList.MotionInstanceIds[j] != animatorClipInfo[i].clip.GetInstanceID())
                    {
                        continue;
                    }

                    motionIndex = j;
                    break;
                }

                playingMotion.Motion = (motionIndex == -1)
                    ? null
                    : _cubismFadeMotionList.CubismFadeMotionObjects[motionIndex];

                playingMotion.StartTime = Time.time;
                playingMotion.FadeInStartTime = Time.time;
                playingMotion.EndTime = (playingMotion.Motion.MotionLength <= 0)
                                        ? -1
                                        : playingMotion.StartTime + playingMotion.Motion.MotionLength + playingMotion.Motion.FadeOutTime;

                PlayingMotions.Add(playingMotion);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="stateInfo"></param>
        /// <param name="layerIndex"></param>
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            IsStateTransitionFinished = true;
        }
    }
}