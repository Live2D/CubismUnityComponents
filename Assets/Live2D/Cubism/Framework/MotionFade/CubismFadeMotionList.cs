
using UnityEngine;

namespace Live2D.Cubism.Framework.MotionFade
{
    [CreateAssetMenu(menuName = "Live2D Cubism/Fade Motion List")]
    public class CubismFadeMotionList : ScriptableObject
    {
        [SerializeField]
        public int[] MotionInstanceIds;


        [SerializeField]
        public CubismFadeMotionData[] CubismFadeMotionObjects;
    }
}