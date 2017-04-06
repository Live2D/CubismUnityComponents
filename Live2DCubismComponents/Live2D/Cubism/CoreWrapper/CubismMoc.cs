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
    /// Cubism moc asset.
    /// </summary>
    public sealed class CubismMoc : ScriptableObject
    {
        #region Factory Methods

        /// <summary>
        /// Creates a <see cref="CubismMoc"/> asset from raw bytes.
        /// </summary>
        /// <param name="moc3">Source.</param>
        /// <returns>Instance.</returns>
        public static CubismMoc CreateFrom(byte[] moc3)
        {
            var moc = CreateInstance<CubismMoc>();


            moc.Bytes = moc3;


            return moc;
        }

        #endregion

        /// <summary>
        /// <see cref="Bytes"/> backing field.
        /// </summary>
        [SerializeField]
        private byte[] _bytes;

        /// <summary>
        /// Raw moc bytes.
        /// </summary>
        private byte[] Bytes
        {
            get { return _bytes; }
            set { _bytes = value; }
        }


        /// <summary>
        /// TaskableModel to unmanaged moc.
        /// </summary>
        internal IntPtr UnmanagedMoc { get; private set; }

        private int ReferenceCount { get; set; }


        /// <summary>
        /// True if instance is revived.
        /// </summary>
        public bool IsRevived
        {
            get { return UnmanagedMoc != IntPtr.Zero; }
        }


        /// <summary>
        /// Acquires native handle.
        /// </summary>
        /// <returns>Valid handle on success; <see cref="IntPtr.Zero"/> otherwise.</returns>
        public IntPtr AcquireUnmanagedMoc()
        {
            ++ReferenceCount;


            Revive();


            return UnmanagedMoc;
        }

        /// <summary>
        /// Releases native handle.
        /// </summary>
        public void ReleaseUnmanagedMoc()
        {
            -- ReferenceCount;


            // Free unmanaged memory in case the instance isn't referenced any longer.
            if (ReferenceCount == 0)
            {
                CubismMemory.DeallocateUnmanaged(UnmanagedMoc);


                UnmanagedMoc = IntPtr.Zero;
            }


            // Deal with invalid reference counts.
            else if (ReferenceCount < 0)
            {
                ReferenceCount = 0;
            }
        }


        /// <summary>
        /// Revives instance without acquiring it.
        /// </summary>
        private void Revive()
        {
            // Return if already revived.
            if (IsRevived)
            {
                return;
            }


            // Return if no bytes are available.
            if (Bytes == null)
            {
                return;
            }


            // Allocate and initialize memory (returning if allocation failed).
            var memory = CubismMemory.AllocateUnmanaged(Bytes.Length, csmAlignofMoc);


            if (memory == IntPtr.Zero)
            {
                return;
            }


            CubismMemory.Write(Bytes, memory);


            // Try revive.
            UnmanagedMoc = csmReviveMocInPlace(memory, (uint)Bytes.Length);
        }

        #region Extern C

        // ReSharper disable once InconsistentNaming
        private const int csmAlignofMoc = 64;


        [DllImport(CubismDll.Name)]
        private static extern IntPtr csmReviveMocInPlace(IntPtr address, uint size);

        #endregion
    }
}
