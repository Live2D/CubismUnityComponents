/*
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;
using Live2D.Cubism.Core;

namespace Live2D.Cubism.Framework.Pose
{
    /// <summary>
    /// Cubism pose controller.
    /// </summary>
    public sealed class CubismPoseController : MonoBehaviour, ICubismUpdatable
    {
        #region variable

        /// <summary>
        /// Cubism model cache.
        /// </summary> 
        private CubismModel _model;

        /// <summary>
        /// Cubism pose data.
        /// </summary>
        [SerializeField, HideInInspector] 
        public CubismPoseData PoseData;

        /// <summary>
        /// Model has update controller component.
        /// </summary>
        private bool _hasUpdateController = false;


        #endregion

        #region Function

        /// <summary>
        /// update hidden part opacity.
        /// </summary>
        /// <param name="groupIndex">Part group index.</param>
        private void DoFade(int groupIndex)
        {
            const float Phi = 0.5f;
            const float BackOpacityThreshold = 0.15f;

            var partIndex = -1;
            var newOpacity = 1.0f;

            for (var i = 0; i < PoseData.Groups[groupIndex].PoseDatas.Length; ++i)
            {
                var partData = PoseData.Groups[groupIndex].PoseDatas[i];
                var part = _model.Parts.FindById(partData.Id);

                if(part == null)
                {
                    continue;
                }

                if(part.Opacity > float.Epsilon)
                {
                    partIndex = i;
                    newOpacity = part.Opacity;
                    break;
                }
            }


            // Set the opacity of display parts and hidden parts
            for (var i = 0; i < PoseData.Groups[groupIndex].PoseDatas.Length; ++i)
            {
                // Fail silently...
                if (i == partIndex)
                {
                    continue;
                }

                var part = _model.Parts.FindById(PoseData.Groups[groupIndex].PoseDatas[i].Id);
                var opacity = part.Opacity;
                float a1;   // Opacity required by calculation

                if (newOpacity < Phi)
                {
                    a1 = newOpacity * (Phi - 1) / Phi + 1.0f; // Line equation passing through (0, 1), (phi, phi)
                }
                else
                {
                    a1 = (1 - newOpacity) * Phi / (1.0f - Phi); // Line equation passing through (1, 0), (phi, phi)
                }

                // When restricting the visible proportion of the background
                var backOpacity = (1.0f - a1) * (1.0f - newOpacity);

                if (backOpacity > BackOpacityThreshold)
                {
                    a1 = 1.0f - BackOpacityThreshold / (1.0f - newOpacity);
                }

                // Increase the opacity if it is greater than the opacity of the calculation.
                if (opacity > a1)
                {
                    opacity = a1;
                }

                part.Opacity = opacity;
            }
        }

        /// <summary>
        /// Copy opacity to linked parts.
        /// </summary>
        private void CopyPartOpacities()
        {
            for (var groupIndex = 0; groupIndex < PoseData.Groups.Length; ++groupIndex)
            {
                for (var partIndex = 0; partIndex < PoseData.Groups[groupIndex].PoseDatas.Length; ++partIndex)
                {
                    var partData = PoseData.Groups[groupIndex].PoseDatas[partIndex];
                    var part = _model.Parts.FindById(partData.Id);

                    // Fail silently...
                    if(part == null || partData.Link == null)
                    {
                        continue;
                    }

                    var opacity = _model.Parts.FindById(partData.Id).Opacity;

                    for (var linkIndex = 0; linkIndex < partData.Link.Length; ++linkIndex)
                    {
                        var linkPart = _model.Parts.FindById(partData.Link[linkIndex]);
                        if(linkPart != null)
                        {
                           linkPart.Opacity = opacity;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called by cubism update manager. Updates controller.
        /// </summary>
        public void OnLateUpdate()
        {
            // Fail silently...
            if (!enabled || _model == null || PoseData.Groups == null)
            {
               return;
            }

            for (var i = 0; i < PoseData.Groups.Length; i++)
            {
               DoFade(i);
            }

            CopyPartOpacities();
        }

        #endregion

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity. Makes sure cache is initialized.
        /// </summary>
        private void Start()
        {
            _model = this.FindCubismModel();

            // Get cubism update controller.
            _hasUpdateController = (GetComponent<CubismUpdateController>() != null);
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
