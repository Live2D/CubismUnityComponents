
using Live2D.Cubism.Core;

namespace Live2D.Cubism.Framework.Pose
{
    public struct CubismPoseData
    {
        /// <summary>
        /// Cubism pose part.
        /// </summary>
        public CubismPosePart PosePart;

        /// <summary>
        /// Cubism part cache.
        /// </summary>
        public CubismPart Part;

        /// <summary>
        /// Link parts cache.
        /// </summary>
        public CubismPart[] LinkParts;

        /// <summary>
        /// Cubism part opacity.
        /// </summary>
        public float Opacity;
    }
}