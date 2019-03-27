/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Motion;
using UnityEngine;

namespace Live2D.Cubism.Samples.OriginalWorkflow.Motion
{
    [RequireComponent(typeof(CubismMotionController))]
    public class CubismMotionPreview : MonoBehaviour
    {
        /// <summary>
        /// MotionController to be operated.
        /// </summary>
        CubismMotionController _motionController;

        /// <summary>
        /// Get motion controller.
        /// </summary>
        private void Start()
        {
            var model = this.FindCubismModel();

            _motionController = model.GetComponent<CubismMotionController>();
        }

        /// <summary>
        /// Play specified animation.
        /// </summary>
        /// <param name="animation">Animation clip to play.</param>
        public void PlayAnimation(AnimationClip animation)
        {
            _motionController.PlayAnimation(animation, isLoop: false);
        }
    }
}