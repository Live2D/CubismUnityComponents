/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */

/* THIS FILE WAS AUTO-GENERATED. ALL CHANGES WILL BE LOST UPON RE-GENERATION. */


using System;


namespace Live2D.Cubism.Core.Unmanaged
{
    /// <summary>
    /// Unmanaged moc.
    /// </summary>
    public sealed class CubismUnmanagedMoc
    {
        #region Factory Methods

        /// <summary>
        /// Creates <see cref="CubismUnmanagedMoc"/> from bytes.
        /// </summary>
        /// <param name="bytes">Moc bytes.</param>
        /// <returns>Instance on success; <see langword="null"/> otherwise.</returns>
        public static CubismUnmanagedMoc FromBytes(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }


            var moc = new CubismUnmanagedMoc(bytes);


            return (moc.Ptr != IntPtr.Zero)
                ? moc
                : null;
        }

        #endregion

        /// <summary>
        /// Native moc pointer.
        /// </summary>
        public IntPtr Ptr { get; private set; }


        /// <summary>
        /// Releases instance.
        /// </summary>
        public void Release()
        {
            if (Ptr == IntPtr.Zero)
            {
                return;
            }


            CubismUnmanagedMemory.Deallocate(Ptr);


            Ptr = IntPtr.Zero;
        }

        #region Ctors

        /// <summary>
        /// Initializes instance.
        /// </summary>
        /// <param name="bytes">Moc bytes.</param>
        private CubismUnmanagedMoc(byte[] bytes)
        {
            // Allocate and initialize memory (returning on fail).
            var memory = CubismUnmanagedMemory.Allocate(bytes.Length, CubismCoreDll.AlignofMoc);


            if (memory == IntPtr.Zero)
            {
                return;
            }


            CubismUnmanagedMemory.Write(bytes, memory);


            // Revive native moc (cleaning up on fail).
            Ptr = CubismCoreDll.ReviveMocInPlace(memory, (uint)bytes.Length);


            if (Ptr == IntPtr.Zero)
            {
                CubismUnmanagedMemory.Deallocate(memory);
            }
        }

        #endregion
    }
}
