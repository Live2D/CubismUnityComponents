/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Class containing the native Dll name to reference.
    /// </summary>
    internal static class CubismDll
    {
        /// <summary>
        /// Name of the native Dll to pass to <see cref="System.Runtime.InteropServices.DllImportAttribute"/>.
        /// </summary>
#if (UNITY_IOS || UNITY_WEBGL || UNITY_SWITCH) && !UNITY_EDITOR
        public const string Name = "__Internal";
#else
        public const string Name = "Live2DCubismCore";
#endif
    }
}
