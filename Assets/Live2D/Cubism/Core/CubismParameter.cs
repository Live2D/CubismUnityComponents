/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Framework;
using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Single <see cref="CubismModel"/> parameter.
    /// </summary>
    [CubismDontMoveOnReimport]
    public sealed class CubismParameter : MonoBehaviour
    {
        #region Factory Methods

        /// <summary>
        /// Creates drawables for a <see cref="CubismModel"/>.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <returns>Drawables root.</returns>
        internal static GameObject CreateParameters(IntPtr unmanagedModel)
        {
            var root = new GameObject("Parameters");


            // Create drawables.
            var buffer = new CubismParameter[csmGetParameterCount(unmanagedModel)];


            for (var i = 0; i < buffer.Length; ++i)
            {
                var proxy = new GameObject();


                buffer[i] = proxy.AddComponent<CubismParameter>();


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
                var values = csmGetParameterIds(UnmanagedModel);


                // Pull data.
                return Marshal.PtrToStringAnsi(new IntPtr(values[UnmanagedIndex]));
            }
        }

        /// <summary>
        /// Minimum value.
        /// </summary>
        public unsafe float MinimumValue
        {
            get
            {
                // Get address.
                var values = csmGetParameterMinimumValues(UnmanagedModel);


                // Pull data.
                return values[UnmanagedIndex];
            }
        }

        /// <summary>
        /// Maximum value.
        /// </summary>
        public unsafe float MaximumValue
        {
            get
            {
                // Get address.
                var values = csmGetParameterMaximumValues(UnmanagedModel);


                // Pull data.
                return values[UnmanagedIndex];
            }
        }

        /// <summary>
        /// Default value.
        /// </summary>
        public unsafe float DefaultValue
        {
            get
            {
                // Get address.
                var values = csmGetParameterDefaultValues(UnmanagedModel);


                // Pull data.
                return values[UnmanagedIndex];
            }
        }

        /// <summary>
        /// Current value.
        /// </summary>
        [SerializeField, HideInInspector]
        public float Value;


        /// <summary>
        /// Revives the instance.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        internal void Revive(IntPtr unmanagedModel)
        {
            UnmanagedModel = unmanagedModel;
        }

        /// <summary>
        /// Restores instance to initial state.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <param name="unmanagedIndex">Position in unmanaged arrays.</param>
        private void Reset(IntPtr unmanagedModel, int unmanagedIndex)
        {
            Revive(unmanagedModel);


            UnmanagedIndex = unmanagedIndex;
            name = Id;
            Value = DefaultValue;
        }

        #region Extern C

        [DllImport(CubismDll.Name)]
        private static extern int csmGetParameterCount(IntPtr model);

        
        [DllImport(CubismDll.Name)]
        private static extern unsafe char** csmGetParameterIds(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe float* csmGetParameterMinimumValues(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe float* csmGetParameterMaximumValues(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe float* csmGetParameterDefaultValues(IntPtr model);

        #endregion
    }
}
