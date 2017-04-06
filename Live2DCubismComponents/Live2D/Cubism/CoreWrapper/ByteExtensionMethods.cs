/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Extension for <see cref="byte"/>s.
    /// </summary>
    internal static class ByteExtensionMethods
    {
        /// <summary>
        /// Checks whether a bit is set.
        /// </summary>
        /// <param name="self">Bit field.</param>
        /// <param name="flag">Bit to test.</param>
        /// <returns><see langword="true"/> if bit is set; <see langword="false"/> otherwise.</returns>
        public static bool HasFlag(this byte self, byte flag)
        {
            return (self & flag) == flag;
        }
    }
}
