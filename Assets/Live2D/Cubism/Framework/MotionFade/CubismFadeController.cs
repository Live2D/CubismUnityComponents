/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Pose;
using UnityEngine;



namespace Live2D.Cubism.Framework.MotionFade
{
    /// <summary>
    /// 
    /// </summary>
    public class CubismFadeController : MonoBehaviour, ICubismUpdatable
    {
        /// <summary>
        /// 
        /// </summary>
        [SerializeField]
        public CubismFadeMotionList CubismFadeMotionList;

        /// <summary>
        /// 
        /// </summary>
        private CubismFadeStateObserver[] StateObservers { get; set; }


        /// <summary>
        /// Parameters cache.
        /// </summary>
        private CubismParameter[] DestinationParameters { get; set; }


        /// <summary>
        /// Parts cache.
        /// </summary>
        private CubismPart[] DestinationParts { get; set; }

        /// <summary>
        /// Model has update controller component.
        /// </summary>
        private bool _hasUpdateController = false;

        /// <summary>
        /// Refreshes the controller. Call this method after adding and/or removing <see cref="CubismFadeParameter"/>s.
        /// </summary>
        private void Refresh()
        {
            var animator = GetComponent<Animator>();

            // Faill silently...
            if(animator == null)
            {
                return;
            }

            StateObservers = animator.GetBehaviours<CubismFadeStateObserver>();
            DestinationParameters = this.FindCubismModel().Parameters;
            DestinationParts = this.FindCubismModel().Parts;

            // Get cubism update controller.
            _hasUpdateController = (GetComponent<CubismUpdateController>() != null);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stateObserver"></param>
        private void UpdateFade(CubismFadeStateObserver stateObserver)
        {
            var playingMotions = stateObserver.PlayingMotions;

            if (playingMotions.Count <= 1)
            {
                // 再生中のモーションが一つ＝切り替わらない場合は処理しない
                return;
            }


            // 処理中のレイヤーに設定されているWeight（最上段に配置されたレイヤーの場合は1に強制される）
            var layerWeight = stateObserver.LayerWeight;


            var time = Time.time;

            var isDoneAllFadeIn = true;

            // Calcurate MotionFade.
            for (var i = 0; i < playingMotions.Count; i++)
            {
                var playingMotion = playingMotions[i];
                
                var fadeMotion = playingMotion.Motion;
                if (fadeMotion == null)
                {
                    continue;
                }

                var erapsedTime = time - playingMotion.FadeInStartTime;


                var fadeInTime = fadeMotion.FadeInTime;
                var fadeOutTime = fadeMotion.FadeOutTime;


                var fadeInWeight = (fadeInTime <= 0.0f)
                    ? 1.0f
                    : CubismFadeMath.GetEasingSine(erapsedTime / fadeInTime);
                var fadeOutWeight = (fadeOutTime <= 0.0f)
                    ? 1.0f
                    : CubismFadeMath.GetEasingSine((playingMotion.EndTime - Time.time) / fadeOutTime);
                var motionWeight = (i == 0)
                    ? fadeOutWeight * layerWeight
                    : (fadeInWeight * fadeOutWeight * layerWeight);

                // Apply to parameter values
                for (var j = 0; j < DestinationParameters.Length; ++j)
                {
                    var index = -1;
                    for (var k = 0; k < fadeMotion.ParameterIds.Length; ++k)
                    {
                        if (fadeMotion.ParameterIds[k] != DestinationParameters[j].Id)
                        {
                            continue;
                        }

                        index = k;
                        break;
                    }

                    if (index < 0)
                    {
                        // モーションにそのIDのカーブが存在しない
                        continue;
                    }

                    DestinationParameters[j].Value = Evaluate(
                            fadeMotion.ParameterCurves[index], erapsedTime,
                            fadeInWeight, fadeOutWeight,
                            fadeMotion.ParameterFadeInTimes[index], fadeMotion.ParameterFadeOutTimes[index],
                            motionWeight, DestinationParameters[j].Value);
                }

                // Apply to part opacities
                for (var j = 0; j < DestinationParts.Length; ++j)
                {
                    var index = -1;
                    for (var k = 0; k < fadeMotion.ParameterIds.Length; ++k)
                    {
                        if (fadeMotion.ParameterIds[k] != DestinationParts[j].Id)
                        {
                            continue;
                        }

                        index = k;
                        break;
                    }

                    if (index < 0)
                    {
                        // モーションにそのIDのカーブが存在しない
                        continue;
                    }

                    DestinationParts[j].Opacity = Evaluate(
                            fadeMotion.ParameterCurves[index], erapsedTime,
                            fadeInWeight, fadeOutWeight,
                            fadeMotion.ParameterFadeInTimes[index], fadeMotion.ParameterFadeOutTimes[index],
                            motionWeight, DestinationParts[j].Opacity);
                }


                if (erapsedTime > fadeInTime)
                {
                    continue;
                }
                isDoneAllFadeIn = false;
            }


            if ((!isDoneAllFadeIn) || (!stateObserver.IsStateTransitionFinished))
            {
                // 一つでもフェードインが終了していないモーションがあれば処理しない
                return;
            }


            stateObserver.IsStateTransitionFinished = false;

            var playingMotionCount = playingMotions.Count - 1;

            for (var i = playingMotionCount; i >= 0; --i)
            {
                var playingMotion = playingMotions[i];
                

                var fadeMotion = playingMotion.Motion;
                if (fadeMotion == null)
                {
                    continue;
                }


                var elapsedTime = time - playingMotion.StartTime;

                if (elapsedTime <= fadeMotion.MotionLength)
                {
                    continue;
                }

                // 全てのモーションのフェードインが終了している場合、再生が終了しているモーションは削除する
                stateObserver.PlayingMotions.RemoveAt(i);
            }
        }


        public float Evaluate(
            AnimationCurve curve, float time,
            float fadeInTime, float fadeOutTime,
            float parameterFadeInTime, float parameterFadeOutTime,
            float motionWeight, float currentValue)
        {
            if (curve.length <= 0)
            {
                return currentValue;
            }



            // Motion fade.
            if (parameterFadeInTime < 0.0f &&
                parameterFadeOutTime < 0.0f)
            {
                return currentValue + (curve.Evaluate(time) - currentValue) * motionWeight;
            }



            // Parameter fade.
            float fadeInWeight, fadeOutWeight;
            if (parameterFadeInTime < 0.0f)
            {
                fadeInWeight = fadeInTime;
            }
            else
            {
                fadeInWeight = (parameterFadeInTime < float.Epsilon)
                    ? 1.0f
                    : CubismFadeMath.GetEasingSine(time / parameterFadeInTime);
            }

            if (parameterFadeOutTime < 0.0f)
            {
                fadeOutWeight = fadeOutTime;
            }
            else
            {
                fadeOutWeight = (parameterFadeOutTime < float.Epsilon)
                    ? 1.0f
                    : CubismFadeMath.GetEasingSine((curve[curve.length-1].time - Time.time) / parameterFadeOutTime);
            }

            var parameterWeight = fadeInWeight * fadeOutWeight;


            return currentValue + (curve.Evaluate(time) - currentValue) * parameterWeight;
        }

        /// <summary>
        /// Called by cubism update controller. Updates controller.
        /// </summary>
        /// <remarks>
        /// Make sure this method is called after any animations are evaluated.
        /// </remarks>
        public void OnLateUpdate()
        {
            // Fail silently.
            if (!enabled || DestinationParameters == null || StateObservers == null)
            {
                return;
            }


            // Update sources and destinations.
            for (var i = 0; i < StateObservers.Length; ++i)
            {
                if (StateObservers[i].IsDefaultState)
                {
                    // Entryから遷移してきたステートは処理しない
                    continue;
                }


                UpdateFade(StateObservers[i]);
            }
        }


        #region Unity Events Handling

        /// <summary>
        /// Initializes instance.
        /// </summary>
        private void OnEnable()
        {
            // Initialize cache.
            Refresh();
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

        #endregion
    }
}
 