/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Live2D.Cubism.Samples.OriginalWorkflow.Motion
{
    [RequireComponent(typeof(CubismMotionController))]
    [RequireComponent(typeof(CubismFadeController))]
    public class CubismMotionPreview : MonoBehaviour
    {
        /// <summary>
        /// MotionController to be operated.
        /// </summary>
        public Text MotionList;

        private CubismMotionController _motionController;
        private CubismFadeMotionList _cubismFadeMotionList;

        private List<Tuple<string, bool, int, int>> _playingMotions = new List<Tuple<string, bool, int, int>>();
        private int _motionId;
        private List<float> _fadeOutTimeList = new List<float>();

        /// <summary>
        /// Get motion controller.
        /// </summary>
        private void Start()
        {
            var model = this.FindCubismModel();

            _motionController = model.GetComponent<CubismMotionController>();
            _cubismFadeMotionList = GetComponent<CubismFadeController>().CubismFadeMotionList;
            _motionController.AnimationBeginHandler += OnAnimationBegin;
            _motionController.AnimationEndHandler += OnAnimationEnd;
        }

        private void OnDestroy()
        {
            if (_motionController != null)
            {
                _motionController.AnimationBeginHandler -= OnAnimationBegin;
                _motionController.AnimationEndHandler -= OnAnimationEnd;
            }
        }
        /// <summary>
        /// Play specified animation.
        /// </summary>
        /// <param name="animation">Animation clip to play.</param>
        public void PlayAnimation(AnimationClip animation, bool isLoop, int priority, float fadeOutTime)
        {
            _fadeOutTimeList.Add(fadeOutTime);
            if (_playingMotions.Count != 0 && _playingMotions[_playingMotions.Count - 1].Item3 >= priority && priority != CubismMotionPriority.PriorityForce)
            {
                Debug.Log($"can't start motion.\n{animation.name}, Priority: {GetPriorityName(priority)}({priority})");
                return;
            }
            else
            {
                if (_motionId > 0)
                {
                    StartCoroutine(EndMotion(_fadeOutTimeList[_motionId - 1], _motionId - 1));
                }
            }
            _playingMotions.Add(new Tuple<string, bool, int, int>(animation.name, isLoop, priority, _motionId));
            UpdateMotionList();
            if (isLoop == false)
            {
                StartCoroutine(EndMotion(animation.length, _motionId));
            }
            _motionId++;
            _motionController.PlayAnimation(animation, isLoop: isLoop, priority: priority);
        }

        IEnumerator EndMotion(float delay, int ID)
        {
            yield return new WaitForSeconds(delay);
            _playingMotions.RemoveAll(item => item.Item4 == ID);
            UpdateMotionList();
        }

        private void UpdateMotionList()
        {
            string text = "";
            foreach (var item in _playingMotions)
            {
                text += $"{item.Item1}, Priority: {GetPriorityName(item.Item3)}({item.Item3}), isLoop: {item.Item2}\n";
            }

            var lines = text.Trim().Split('\n');
            var resultLines = new List<string>();

            for (var i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    resultLines.Add($"{i}: {lines[i]}");
                }
            }

            string result = string.Join("\n", resultLines);
            MotionList.text = result;
        }

        public void StopAllMotion()
        {
            _motionController.StopAllAnimation();
            StopAllCoroutines();
            _playingMotions.Clear();
            UpdateMotionList();
        }

        private string GetPriorityName(int priority)
        {
            string priorityName = "";
            if (priority == 0)
            {
                priorityName = "PriorityNone";
            }
            else if (priority == 1)
            {
                priorityName = "PriorityIdle";
            }
            else if (priority == 2)
            {
                priorityName = "PriorityNormal";
            }
            else if (priority == 3)
            {
                priorityName = "PriorityForce";
            }
            return priorityName;
        }

        private void OnAnimationBegin(int instanceId)
        {
            if (!_cubismFadeMotionList)
            {
                return;
            }
            for (var i = 0; i < _cubismFadeMotionList.MotionInstanceIds.Length; i++)
            {
                if (_cubismFadeMotionList.MotionInstanceIds[i] != instanceId)
                {
                    continue;
                }
                Debug.Log("StartedMotion: " + _cubismFadeMotionList.CubismFadeMotionObjects[i].MotionName);
                break;
            }
        }

        void OnAnimationEnd(int instanceId)
        {
            if (!_cubismFadeMotionList)
            {
                return;
            }
            for (var i = 0; i < _cubismFadeMotionList.MotionInstanceIds.Length; i++)
            {
                if (_cubismFadeMotionList.MotionInstanceIds[i] != instanceId)
                {
                    continue;
                }
                Debug.Log("EndedMotion: " + _cubismFadeMotionList.CubismFadeMotionObjects[i].MotionName);
                break;
            }
        }
    }
}
