/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

/* THIS FILE WAS AUTO-GENERATED. ALL CHANGES WILL BE LOST UPON RE-GENERATION. */


using System;
using System.Runtime.InteropServices;


namespace Live2D.Cubism.Core.Unmanaged
{
    /// <summary>
    /// Unmanaged parameters interface.
    /// </sumamry>
    public sealed class CubismUnmanagedParameters
    {
        /// <summary>
        /// Parameter count.
        /// </summary>>
        public int Count { get; private set; }
        
        /// <summary>
        /// Parameter IDs.
        /// </summary>>
        public string[] Ids { get; private set; }
        
        /// <summary>
        /// Minimum parameter values.
        /// </summary>>
        public CubismUnmanagedFloatArrayView MinimumValues { get; private set; }
        
        /// <summary>
        /// Maximum parameter values.
        /// </summary>>
        public CubismUnmanagedFloatArrayView MaximumValues { get; private set; }
        
        /// <summary>
        /// Default parameter values.
        /// </summary>>
        public CubismUnmanagedFloatArrayView DefaultValues { get; private set; }
        
        /// <summary>
        /// Parameter values.
        /// </summary>>
        public CubismUnmanagedFloatArrayView Values { get; private set; }
        
        

        #region Ctors

        /// <summary>
        /// Initializes instance.
        /// </summary>
        internal unsafe CubismUnmanagedParameters(IntPtr modelPtr)
        {
            var length = 0;


            Count = CubismCoreDll.GetParameterCount(modelPtr);


            length = CubismCoreDll.GetParameterCount(modelPtr);
            Ids = new string[length];
            var _ids = (IntPtr *)(CubismCoreDll.GetParameterIds(modelPtr));
            for (var i = 0; i < length; ++i)
            {
                Ids[i] = Marshal.PtrToStringAnsi(_ids[i]);
            }


            length = CubismCoreDll.GetParameterCount(modelPtr);
            MinimumValues = new CubismUnmanagedFloatArrayView(CubismCoreDll.GetParameterMinimumValues(modelPtr), length);

            length = CubismCoreDll.GetParameterCount(modelPtr);
            MaximumValues = new CubismUnmanagedFloatArrayView(CubismCoreDll.GetParameterMaximumValues(modelPtr), length);

            length = CubismCoreDll.GetParameterCount(modelPtr);
            DefaultValues = new CubismUnmanagedFloatArrayView(CubismCoreDll.GetParameterDefaultValues(modelPtr), length);

            length = CubismCoreDll.GetParameterCount(modelPtr);
            Values = new CubismUnmanagedFloatArrayView(CubismCoreDll.GetParameterValues(modelPtr), length);

        }

        #endregion
    }
}
