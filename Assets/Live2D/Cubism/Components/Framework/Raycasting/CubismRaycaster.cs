/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using Live2D.Cubism.Core;
using Live2D.Cubism.Rendering;
using UnityEngine;


namespace Live2D.Cubism.Framework.Raycasting
{
    /// <summary>
    /// Allows casting rays against <see cref="CubismRaycastable"/>s.
    /// </summary>
    public sealed class CubismRaycaster : MonoBehaviour
    {
        /// <summary>
        /// <see cref="CubismRenderer"/>s with <see cref="CubismRaycastable"/>s attached.
        /// </summary>
        private CubismRenderer[] Raycastables { get; set; }


        /// <summary>
        /// Refreshes the controller. Call this method after adding and/or removing <see cref="CubismRaycastable"/>.
        /// </summary>
        private void Refresh()
        {
            var candidates = this
                .FindCubismModel()
                .Drawables;


            // Find raycastable drawables.
            var raycastables = new List<CubismRenderer>();
            

            for (var i = 0; i < candidates.Length; i++)
            {
                // Skip non-raycastables.
                if (candidates[i].GetComponent<CubismRaycastable>() == null)
                {
                    continue;
                }


                raycastables.Add(candidates[i].GetComponent<CubismRenderer>());
            }


            // Cache raycastables.
            Raycastables = raycastables.ToArray();
        }

        #region Unity Event Handling

        /// <summary>
        /// Called by Unity. Makes sure cache is initialized.
        /// </summary>
        private void Start()
        {
            // Initialize cache.
            Refresh();
        }

        #endregion

        /// <summary>
        /// Casts a ray.
        /// </summary>
        /// <param name="origin">The origin of the ray.</param>
        /// <param name="direction">The direction of the ray.</param>
        /// <param name="result">The result of the cast.</param>
        /// <param name="maximumDistance">[Optional] The maximum distance of the ray.</param>
        /// <returns><see langword="true"/> in case of a hit; <see langword="false"/> otherwise.</returns>
        /// <returns>The numbers of drawables had hit</returns>
        public int Raycast(Vector3 origin, Vector3 direction, CubismRaycastHit[] result, float maximumDistance = Mathf.Infinity)
        {
            return Raycast(new Ray(origin, direction), result, maximumDistance);
        }

        /// <summary>
        /// Casts a ray.
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="result">The result of the cast.</param>
        /// <param name="maximumDistance">[Optional] The maximum distance of the ray.</param>
        /// <returns><see langword="true"/> in case of a hit; <see langword="false"/> otherwise.</returns>
        /// <returns>The numbers of drawables had hit</returns>
        public int Raycast(Ray ray, CubismRaycastHit[] result, float maximumDistance = Mathf.Infinity)
        {
            // Cast ray against model plane.
            var intersectionInWorldSpace = ray.origin + ray.direction * (ray.direction.z / ray.origin.z);
            var inersectionInLocalSpace = transform.InverseTransformPoint(intersectionInWorldSpace);


            inersectionInLocalSpace.z = 0;


            var distance = intersectionInWorldSpace.magnitude;


            // Return non-hits.
            if (distance > maximumDistance)
            {
                return 0;
            }

            // Cast against each raycastable.
            var hitCount = 0;


            for (var i = 0; i < Raycastables.Length; i++)
            {
                var raycastable = Raycastables[i];


                // Skip inactive raycastables.
                if (!raycastable.MeshRenderer.enabled)
                {
                    continue;
                }
                

                var bounds = raycastable.Mesh.bounds;

                // Skip non hits
                if (!bounds.Contains(inersectionInLocalSpace))
                {
                    continue;
                }


                result[hitCount].Drawable = raycastable.GetComponent<CubismDrawable>();
                result[hitCount].Distance = distance;
                result[hitCount].LocalPosition = inersectionInLocalSpace;
                result[hitCount].WorldPosition = intersectionInWorldSpace;


                ++hitCount;


                // Exit if result buffer is full.
                if (hitCount == result.Length)
                {
                    break;
                }
            }


            return hitCount;
        }
    }
}
