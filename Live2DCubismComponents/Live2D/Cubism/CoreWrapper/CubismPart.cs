/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Single <see cref="CubismModel"/> part.
    /// </summary>
    public sealed class CubismPart : MonoBehaviour
    {
        #region Factory Methods

        /// <summary>
        /// Creates parts for a <see cref="CubismModel"/>.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <returns>Parts root.</returns>
        internal static GameObject CreateParts(IntPtr unmanagedModel)
        {
            var root = new GameObject("Parts");


            // Create drawables.
            var buffer = new CubismPart[csmGetPartCount(unmanagedModel)];


            for (var i = 0; i < buffer.Length; ++i)
            {
                var proxy = new GameObject();


                buffer[i] = proxy.AddComponent<CubismPart>();


                buffer[i].transform.SetParent(root.transform);
                buffer[i].Reset(unmanagedModel, i);
            }


            return root;
        }

        #endregion

        /// <summary>
        /// TaskableModel to unmanaged unmanagedModel.
        /// </summary>
        private IntPtr UnmanagedModel { get; set; }


        /// <summary>
        /// <see cref="UnmanagedIndex"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _unmanagedIndex = -1;

        /// <summary>
        /// Position in unmanaged arrays.
        /// </summary>
        internal int UnmanagedIndex
        {
            get { return _unmanagedIndex; }
            private set { _unmanagedIndex = value; }
        }


        /// <summary>
        /// Copy of Id.
        /// </summary>
        public unsafe string Id
        {
            get
            {
                // Get address.
                var values = csmGetPartIds(UnmanagedModel);


                // Pull data.
                return Marshal.PtrToStringAnsi(new IntPtr(values[UnmanagedIndex]));
            }
        }

        /// <summary>
        /// Current opacity.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Opacity;


        /// <summary>
        /// Revives instance.
        /// </summary>
        /// <param name="unmanagedModel">TaskableModel to unmanaged unmanagedModel.</param>
        internal void Revive(IntPtr unmanagedModel)
        {
            UnmanagedModel = unmanagedModel;
        }

        /// <summary>
        /// Restores instance to initial state.
        /// </summary>
        /// <param name="unmanagedModel">TaskableModel to unmanaged unmanagedModel.</param>
        /// <param name="unmanagedIndex">Position in unmanaged arrays.</param>
        private unsafe void Reset(IntPtr unmanagedModel, int unmanagedIndex)
        {
            Revive(unmanagedModel);


            UnmanagedIndex = unmanagedIndex;
            name = Id;
            Opacity = csmGetPartOpacities(unmanagedModel)[unmanagedIndex];
        }

        #region Extern C

        [DllImport(CubismDll.Name)]
        private static extern int csmGetPartCount(IntPtr model);


        [DllImport(CubismDll.Name)]
        private static extern unsafe char** csmGetPartIds(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe float* csmGetPartOpacities(IntPtr model);

        #endregion
    }
}
