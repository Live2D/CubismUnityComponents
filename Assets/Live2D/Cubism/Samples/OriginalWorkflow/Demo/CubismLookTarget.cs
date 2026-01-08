/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework.LookAt;
using UnityEngine;

namespace Live2D.Cubism.Samples.OriginalWorkflow.Demo
{
    public class CubismLookTarget : MonoBehaviour, ICubismLookTarget
    {
        /// <summary>
        /// Get mouse coordinates while dragging.
        /// </summary>
        /// <returns>Mouse coordinates.</returns>
        public Vector3 GetPosition()
        {
            var targetPosition = Input.mousePosition;

            var z = Camera.main.WorldToScreenPoint(transform.position).z;
            targetPosition.z = z;

            var worldPosition = Camera.main.ScreenToWorldPoint(targetPosition);
            return worldPosition;
        }

        /// <summary>
        /// Gets whether the target is active.
        /// </summary>
        /// <returns><see langword="true"/> if the target is active; <see langword="false"/> otherwise.</returns>
        public bool IsActive()
        {
            return Input.GetMouseButton(0);
        }
    }
}
