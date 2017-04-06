/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Exposes unmanaged memory methods.
    /// </summary>
    internal static class CubismMemory
    {
        #region Allocation

        /// <summary>
        /// Single allocation.
        /// </summary>
        private struct AllocationItem
        {
            /// <summary>
            /// Address of allocation made.
            /// </summary>
            public IntPtr UnalignedAddress;

            /// <summary>
            /// Address returned to user.
            /// </summary>
            public IntPtr AlignedAddress;
        }


        /// <summary>
        /// Unmanaged allocations.
        /// </summary>
        private static List<AllocationItem> Allocations { get; set; }

        /// <summary>
        /// True if no unmanaged allocation is made.
        /// </summary>
        private static bool ContainsAllocations
        {
            get { return Allocations != null && Allocations.Count > 0; }
        }


        /// <summary>
        /// Allocates unmanaged memory.
        /// </summary>
        /// <param name="size">Number of bytes to allocate.</param>
        /// <param name="align">Allocation alignment in bytes.</param>
        /// <returns>Address of allocated memory.</returns>
        public static IntPtr AllocateUnmanaged(int size, int align)
        {
            // Lazily initialize container.
            if (Allocations == null)
            {
                Allocations = new List<AllocationItem>();
            }


            // Allocate unaligned memory block.
            var unalignedAddress = Marshal.AllocHGlobal(size + align);


            // Get aligned address.
            var shift = (unalignedAddress.ToInt64() % align);
            
            var alignedAddress = (shift != 0)
                ? new IntPtr(unalignedAddress.ToInt64() + align - shift)
                : unalignedAddress;


            // Register allocation.
            Allocations.Add(new AllocationItem
            {
                UnalignedAddress = unalignedAddress,
                AlignedAddress = alignedAddress
            });


            // Return aligned address.
            return alignedAddress;
        }

        /// <summary>
        /// Frees unmanaged memory.
        /// </summary>
        /// <param name="allocation">Address of memory to deallocate.</param>
        public static void DeallocateUnmanaged(IntPtr allocation)
        {
            // Return early in case no allocations exist.
            if (!ContainsAllocations)
            {
                return;
            }


            // Free allocation.
            for (var i = 0; i < Allocations.Count; ++i)
            {
                if (Allocations[i].AlignedAddress != allocation)
                {
                    continue;
                }


                Marshal.FreeHGlobal(Allocations[i].UnalignedAddress);
                Allocations.RemoveAt(i);


                break;
            }
        }

        #endregion

        /// <summary>
        /// Copies contents of a managed array into an unmanaged memory block.
        /// </summary>
        /// <param name="source">Source to copy.</param>
        /// <param name="destination">Memory block to copy to.</param>
        public static void Write(byte[] source, IntPtr destination)
        {
            Marshal.Copy(source, 0, destination, source.Length);
        }
    }
}
