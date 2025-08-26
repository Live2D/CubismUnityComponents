/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using UnityEngine;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// Array extension methods.
    /// </summary>
    public static class ArrayExtensionMethods
    {
        /// <summary>
        /// Combines bounds of multiple <see cref="CubismRenderer"/>s.
        /// </summary>
        /// <param name="self">Renderers.</param>
        /// <returns>Combined bounds.</returns>
        public static Bounds GetMeshRendererBounds(this CubismRenderer[] self)
        {
            var useModelCanvasRenderer = self[0].ModelCanvasRenderer != null;
            var meshRenderer = useModelCanvasRenderer ? self[0].ModelCanvasRenderer : self[0].MeshRenderer;
            var min = meshRenderer.bounds.min;
            var max = meshRenderer.bounds.max;


            for (var i = 1; i < self.Length; ++i)
            {
                meshRenderer = useModelCanvasRenderer ? self[i].ModelCanvasRenderer : self[i].MeshRenderer;
                var boundsI = meshRenderer.bounds;


                if (boundsI.min.x < min.x)
                {
                    min.x = boundsI.min.x;
                }

                if (boundsI.max.x > max.x)
                {
                    max.x = boundsI.max.x;
                }


                if (boundsI.min.y < min.y)
                {
                    min.y = boundsI.min.y;
                }

                if (boundsI.max.y > max.y)
                {
                    max.y = boundsI.max.y;
                }
            }


            return new Bounds
            {
                min = min,
                max = max
            };
        }
    }
}
