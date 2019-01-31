
using Live2D.Cubism.Framework.Json;
using UnityEngine;

namespace Live2D.Cubism.Framework.MotionFade
{
    public class CubismFadeMotionData : ScriptableObject
    {
        [SerializeField]
        public string MotionName;

        [SerializeField]
        public float FadeInTime;

        [SerializeField]
        public float FadeOutTime;

        [SerializeField]
        public string[] ParameterIds;

        [SerializeField]
        public AnimationCurve[] ParameterCurves;


        [SerializeField]
        public float[] ParameterFadeInTimes;

        [SerializeField]
        public float[] ParameterFadeOutTimes;

        [SerializeField]
        public float MotionLength;


        public static CubismFadeMotionData CreateInstance(
            CubismMotion3Json motion3Json, string motionName, float motionLength,
             bool shouldImportAsOriginalWorkflow = false, bool isCallFormModelJson = false)
        {
            var fadeMotion = CreateInstance<CubismFadeMotionData>();
            var curveCount = motion3Json.Curves.Length;
            fadeMotion.ParameterIds = new string[curveCount];;
            fadeMotion.ParameterFadeInTimes = new float[curveCount];
            fadeMotion.ParameterFadeOutTimes = new float[curveCount];
            fadeMotion.ParameterCurves = new AnimationCurve[curveCount];

            return CreateInstance(fadeMotion, motion3Json, motionName, motionLength, shouldImportAsOriginalWorkflow, isCallFormModelJson);
        }

        public static CubismFadeMotionData CreateInstance(
            CubismFadeMotionData fadeMotion, CubismMotion3Json motion3Json, string motionName, float motionLength,
             bool shouldImportAsOriginalWorkflow = false, bool isCallFormModelJson = false)
        {
            fadeMotion.MotionName = motionName;
            fadeMotion.MotionLength = motionLength;
            fadeMotion.FadeInTime = (motion3Json.Meta.FadeInTime < 0.0f) ? 1.0f : motion3Json.Meta.FadeInTime;
            fadeMotion.FadeOutTime = (motion3Json.Meta.FadeOutTime < 0.0f) ? 1.0f : motion3Json.Meta.FadeOutTime;

            for (var i = 0; i < motion3Json.Curves.Length; ++i)
            {
                var curve = motion3Json.Curves[i];

                // In original workflow mode, skip add part opacity curve when call not from model3.json.
                if (curve.Target == "PartOpacity" && shouldImportAsOriginalWorkflow && !isCallFormModelJson)
                {
                    continue;
                }

                fadeMotion.ParameterIds[i] = curve.Id;
                fadeMotion.ParameterFadeInTimes[i] = (curve.FadeInTime < 0.0f) ? -1.0f : curve.FadeInTime;
                fadeMotion.ParameterFadeOutTimes[i] = (curve.FadeOutTime < 0.0f) ? -1.0f : curve.FadeOutTime;
                fadeMotion.ParameterCurves[i] = new AnimationCurve(CubismMotion3Json.ConvertCurveSegmentsToKeyframes(curve.Segments));
            }

            return fadeMotion;
        }
    }
}