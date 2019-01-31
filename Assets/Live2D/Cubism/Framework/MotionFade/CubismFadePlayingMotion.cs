
using UnityEngine;

namespace Live2D.Cubism.Framework.MotionFade
{
    public struct CubismFadePlayingMotion
    {
        // AnimationClipの再生開始時刻
        [SerializeField]
        public float StartTime;

        [SerializeField]
        public float FadeInStartTime;

        [SerializeField]
        public float EndTime;

        [SerializeField]
        public CubismFadeMotionData Motion;
    }
}