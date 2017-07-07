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
    /// Dynamic <see cref="CubismDrawable"/> data.
    /// </summary>
    public sealed class CubismDynamicDrawableData
    {
        #region Factory Methods

        /// <summary>
        /// Creates buffer for dynamic <see cref="CubismDrawable"/> data.
        /// </summary>
        /// <param name="unmanagedModel">Unmanaged model to create buffer for.</param>
        /// <returns>Buffer.</returns>
        internal static unsafe CubismDynamicDrawableData[] CreateData(IntPtr unmanagedModel)
        {
            var buffer = new CubismDynamicDrawableData[csmGetDrawableCount(unmanagedModel)];


            // Initialize buffers.
            var vertexCounts = csmGetDrawableVertexCounts(unmanagedModel);


            for (var i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = new CubismDynamicDrawableData
                {
                    VertexPositions = new Vector3[vertexCounts[i]]
                };
            }


            return buffer;
        }

        #endregion

        /// <summary>
        /// Dirty flags.
        /// </summary>
        internal byte Flags { private get; set; }


        /// <summary>
        /// Current opacity.
        /// </summary>
        public float Opacity { get; internal set; }

        /// <summary>
        /// Current draw order.
        /// </summary>
        public int DrawOrder { get; internal set; }

        /// <summary>
        /// Current render order.
        /// </summary>
        public int RenderOrder { get; internal set; }

        /// <summary>
        /// Current vertex position.
        /// </summary>
        public Vector3[] VertexPositions { get; internal set; }


        /// <summary>
        /// True if currently visible.
        /// </summary>
        public bool IsVisible
        {
            get { return Flags.HasFlag(csmIsVisible); }
        }


        /// <summary>
        /// True if <see cref="IsVisible"/> did change.
        /// </summary>
        public bool IsVisibilityDirty
        {
            get { return Flags.HasFlag(csmVisibilityDidChange); }
        }

        /// <summary>
        /// True if <see cref="Opacity"/> did change.
        /// </summary>
        public bool IsOpacityDirty
        {
            get { return Flags.HasFlag(csmOpacityDidChange); }
        }

        /// <summary>
        /// True if <see cref="DrawOrder"/> did change.
        /// </summary>
        public bool IsDrawOrderDirty
        {
            get { return Flags.HasFlag(csmDrawOrderDidChange); }
        }

        /// <summary>
        /// True if <see cref="RenderOrder"/> did change.
        /// </summary>
        public bool IsRenderOrderDirty
        {
            get { return Flags.HasFlag(csmRenderOrderDidChange); }
        }

        /// <summary>
        /// True if <see cref="VertexPositions"/> did change.
        /// </summary>
        public bool AreVertexPositionsDirty
        {
            get { return Flags.HasFlag(csmVertexPositionsDidChange); }
        }

        /// <summary>
        /// True if any data did change.
        /// </summary>
        public bool IsAnyDirty
        {
            get { return Flags != 0; }
        }

        #region Extern C

        // ReSharper disable once InconsistentNaming
        private const byte csmIsVisible = 1 << 0;

        // ReSharper disable once InconsistentNaming
        private const byte csmVisibilityDidChange = 1 << 1;

        // ReSharper disable once InconsistentNaming
        private const byte csmOpacityDidChange = 1 << 2;

        // ReSharper disable once InconsistentNaming
        private const byte csmDrawOrderDidChange = 1 << 3;

        // ReSharper disable once InconsistentNaming
        private const byte csmRenderOrderDidChange = 1 << 4;

        // ReSharper disable once InconsistentNaming
        private const byte csmVertexPositionsDidChange = 1 << 5;


        [DllImport(CubismDll.Name)]
        private static extern int csmGetDrawableCount(IntPtr model);


        [DllImport(CubismDll.Name)]
        private static extern unsafe int* csmGetDrawableVertexCounts(IntPtr model);

        #endregion
    }
}
