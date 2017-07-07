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
    /// Single <see cref="CubismModel"/> drawable.
    /// </summary>
    [CubismDontMoveOnReimport]
    public sealed class CubismDrawable : MonoBehaviour
    {
        #region Factory Methods

        /// <summary>
        /// Creates drawables for a <see cref="CubismModel"/>.
        /// </summary>
        /// <param name="unmanagedModel">Handle to unmanaged model.</param>
        /// <returns>Drawables root.</returns>
        internal static GameObject CreateDrawables(IntPtr unmanagedModel)
        {
            var root = new GameObject("Drawables");


            // Create drawables.
            var buffer = new CubismDrawable[csmGetDrawableCount(unmanagedModel)];


            for (var i = 0; i < buffer.Length; ++i)
            {
                var proxy = new GameObject();


                buffer[i] = proxy.AddComponent<CubismDrawable>();


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
                var ids = csmGetDrawableIds(UnmanagedModel);


                // Pull data.
                return Marshal.PtrToStringAnsi(new IntPtr(ids[UnmanagedIndex]));
            }
        }

        /// <summary>
        /// Texture UnmanagedIndex. 
        /// </summary>
        public unsafe int TextureIndex
        {
            get
            {
                // Get native address.
                var indices = csmGetDrawableTextureIndices(UnmanagedModel);


                // Pull data.
                return indices[UnmanagedIndex];
            }
        }

        /// <summary>
        /// Copy of the masks.
        /// </summary>
        public unsafe CubismDrawable[] Masks
        {
            get
            {
                var drawables = this
                    .FindCubismModel(true)
                    .Drawables;


                // Get addresses.
                var counts = csmGetDrawableMaskCounts(UnmanagedModel);
                var indices = csmGetDrawableMasks(UnmanagedModel);


                // Pull data.
                var buffer = new CubismDrawable[counts[UnmanagedIndex]];


                for (var i = 0; i < buffer.Length; ++i)
                {
                    for (var j = 0; j < drawables.Length; ++j)
                    {
                        if (drawables[j].UnmanagedIndex != indices[UnmanagedIndex][i])
                        {
                            continue;
                        }


                        buffer[i] = drawables[j];


                        break;
                    }
                }


                return buffer;
            }
        }

        /// <summary>
        /// Copy of vertex positions.
        /// </summary>
        public unsafe Vector3[] VertexPositions
        {
            get
            {
                // Get addresses.
                var counts = csmGetDrawableVertexCounts(UnmanagedModel);
                var positions = (Vector2**)csmGetDrawableVertexPositions(UnmanagedModel).ToPointer();


                // Pull data.
                var buffer = new Vector3[counts[UnmanagedIndex]];


                for (var i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = positions[UnmanagedIndex][i].ToVertexPosition();
                }


                return buffer;
            }
        }

        /// <summary>
        /// Copy of vertex texture coordinates.
        /// </summary>
        public unsafe Vector2[] VertexUvs
        {
            get
            {
                // Get addresses.
                var counts = csmGetDrawableVertexCounts(UnmanagedModel);
                var uvs = (Vector2 **)csmGetDrawableVertexUvs(UnmanagedModel).ToPointer();


                // Pull data.
                var buffer = new Vector2[counts[UnmanagedIndex]];


                for (var i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = uvs[UnmanagedIndex][i];
                }


                return buffer;
            }
        }

        /// <summary>
        /// Copy of triangle indices.
        /// </summary>
        public unsafe int[] Indices
        {
            get
            {
                // Get addresses.
                var counts = csmGetDrawableIndexCounts(UnmanagedModel);
                var indices = csmGetDrawableIndices(UnmanagedModel);


                // Pull data.
                var buffer = new int[counts[UnmanagedIndex]];


                for (var i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = indices[UnmanagedIndex][i];
                }


                return buffer;
            }
        }


        /// <summary>
        /// True if double-sided.
        /// </summary>
        public unsafe bool IsDoubleSided
        {
            get
            {
                // Get address.
                var flags = csmGetDrawableConstantFlags(UnmanagedModel);


                // Pull data.
                return flags[UnmanagedIndex].HasFlag(csmIsDoubleSided);
            }
        }

        /// <summary>
        /// True if masking is requested.
        /// </summary>
        public unsafe bool IsMasked
        {
            get
            {
                // Get address.
                var counts = csmGetDrawableMaskCounts(UnmanagedModel);


                // Pull data.
                return counts[UnmanagedIndex] > 0;
            }
        }


        /// <summary>
        /// True if additive blending is requested.
        /// </summary>
        public unsafe bool BlendAdditive
        {
            get
            {
                // Get address.
                var flags = csmGetDrawableConstantFlags(UnmanagedModel);


                // Pull data.
                return flags[UnmanagedIndex].HasFlag(csmBlendAdditive);
            }
        }

        /// <summary>
        /// True if multiply blending is setd.
        /// </summary>
        public unsafe bool MultiplyBlend
        {
            get
            {
                // Get address.
                var flags = csmGetDrawableConstantFlags(UnmanagedModel);


                // Pull data.
                return flags[UnmanagedIndex].HasFlag(csmBlendMultiplicative);
            }
        }


        /// <summary>
        /// Revives instance.
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
        }

        #region Extern C
        
        // ReSharper disable once InconsistentNaming
        private const byte csmBlendAdditive = 1 << 0;

        // ReSharper disable once InconsistentNaming
		private const byte csmBlendMultiplicative = 1 << 1;

        // ReSharper disable once InconsistentNaming
        private const byte csmIsDoubleSided = 1 << 2;


        [DllImport(CubismDll.Name)]
        private static extern int csmGetDrawableCount(IntPtr model);


        [DllImport(CubismDll.Name)]
        private static extern unsafe char** csmGetDrawableIds(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe byte* csmGetDrawableConstantFlags(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe int* csmGetDrawableTextureIndices(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe int* csmGetDrawableMaskCounts(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe int** csmGetDrawableMasks(IntPtr model);


        [DllImport(CubismDll.Name)]
        private static extern unsafe int* csmGetDrawableVertexCounts(IntPtr model);

        // HACK Some platforms have problems with struct return types, so we use void* instead and cast in the wrapper methods.
        [DllImport(CubismDll.Name)]
        private static extern IntPtr csmGetDrawableVertexPositions(IntPtr model);
       
        // HACK Some platforms have problems with struct return types, so we use void* instead and cast in the wrapper methods.
        [DllImport(CubismDll.Name)]
        private static extern IntPtr csmGetDrawableVertexUvs(IntPtr model);


        [DllImport(CubismDll.Name)]
        private static extern unsafe int* csmGetDrawableIndexCounts(IntPtr model);

        [DllImport(CubismDll.Name)]
        private static extern unsafe ushort** csmGetDrawableIndices(IntPtr model);

        #endregion
    }
}
