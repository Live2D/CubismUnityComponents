using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using Live2D.Cubism.Samples.OriginalWorkflow.Motion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CubismMotionPreviewButton : MonoBehaviour
{
    private CubismMotionPreview _motionPreview;
    public GameObject Model;
    public AnimationClip Animation;
    public CubismFadeMotionData FadeMotion;

    [SerializeField]
    private Toggle loopToggle;

    [SerializeField]
    private Dropdown priorityDropdown;

    private void Start()
    {
        _motionPreview = Model.GetComponent<CubismMotionPreview>();
    }

    public void PlayMotion()
    {
        _motionPreview.PlayAnimation(
            Animation,
            isLoop: loopToggle != null && loopToggle.isOn,
            priority: priorityDropdown != null ? priorityDropdown.value : 0,
            fadeOutTime: FadeMotion.FadeOutTime
        );
    }
}
