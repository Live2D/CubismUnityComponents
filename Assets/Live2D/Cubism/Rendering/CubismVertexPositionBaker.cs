/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core.Unmanaged;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// XY bounds of a drawable's baked vertices.
    /// </summary>
    internal struct VertexBounds
    {
        /// <summary>
        /// Minimum X coordinate.
        /// </summary>
        public float MinX;

        /// <summary>
        /// Minimum Y coordinate.
        /// </summary>
        public float MinY;

        /// <summary>
        /// Maximum X coordinate.
        /// </summary>
        public float MaxX;

        /// <summary>
        /// Maximum Y coordinate.
        /// </summary>
        public float MaxY;
    }

    /// <summary>
    /// Bakes dirty drawable vertex positions from the Cubism Core buffers into one shared native buffer using
    /// a Burst <see cref="IJobParallelFor"/>, computing each drawable's XY bounds in the same pass.
    /// </summary>
    /// <remarks>
    /// Core vertex pointers are stable for the model lifetime, so they are captured once in
    /// <see cref="EnsureInitialized"/> and reused every frame.
    /// </remarks>
    internal sealed class CubismVertexPositionBaker : IDisposable
    {
        /// <summary>
        /// Per-drawable Core vertex pointer (interleaved x, y).
        /// </summary>
        private NativeArray<IntPtr> _sourcePointers;

        /// <summary>
        /// Per-drawable start index into _baked.
        /// </summary>
        private NativeArray<int> _vertexOffsets;

        /// <summary>
        /// Per-drawable vertex count.
        /// </summary>
        private NativeArray<int> _vertexCounts;

        /// <summary>
        /// Baked positions for all drawables, concatenated.
        /// </summary>
        private NativeArray<Vector3> _baked;

        /// <summary>
        /// Per-drawable XY bounds output.
        /// </summary>
        private NativeArray<VertexBounds> _bounds;

        /// <summary>
        /// Scratch list of drawable indices to bake.
        /// </summary>
        private NativeArray<int> _dirtyIndices;

        /// <summary>
        /// Pointer to the unmanaged model that owns the Core buffers.
        /// </summary>
        private IntPtr _modelPtr;

        /// <summary>
        /// Number of drawables in the unmanaged model.
        /// </summary>
        private int _drawableCount;

        /// <summary>
        /// Handle of the in-flight bake job, if any.
        /// </summary>
        private JobHandle _handle;

        /// <summary>
        /// Whether a bake job is in-flight.
        /// </summary>
        private bool _scheduled;

        /// <summary>
        /// Whether native buffers are allocated.
        /// </summary>
        public bool IsCreated
        {
            get
            {
                return _baked.IsCreated;
            }
        }

        /// <summary>
        /// Baked positions for all drawables.
        /// </summary>
        public NativeArray<Vector3> Baked
        {
            get
            {
                return _baked;
            }
        }

        /// <summary>
        /// Start offset of a drawable's vertices within <see cref="Baked"/>.
        /// </summary>
        /// <param name="drawableIndex">Index of the drawable.</param>
        /// <returns>The start offset of the drawable's vertices.</returns>
        public int GetOffset(int drawableIndex)
        {
            return _vertexOffsets[drawableIndex];
        }

        /// <summary>
        /// Vertex count of a drawable.
        /// </summary>
        /// <param name="drawableIndex">Index of the drawable.</param>
        /// <returns>The number of vertices the drawable has.</returns>
        public int GetCount(int drawableIndex)
        {
            return _vertexCounts[drawableIndex];
        }

        /// <summary>
        /// Baked XY bounds of a drawable.
        /// </summary>
        /// <param name="drawableIndex">Index of the drawable.</param>
        /// <returns>The baked XY bounds of the drawable.</returns>
        public VertexBounds GetBounds(int drawableIndex)
        {
            return _bounds[drawableIndex];
        }

        /// <summary>
        /// (Re)allocates and captures Core pointers when the model or its layout changes.
        /// </summary>
        /// <param name="unmanagedModel">Model that owns the Core buffers to bake from.</param>
        /// <returns><see langword="true"/> if usable buffers are available.</returns>
        public unsafe bool EnsureInitialized(CubismUnmanagedModel unmanagedModel)
        {
            if (unmanagedModel == null)
            {
                return false;
            }

            var drawables = unmanagedModel.Drawables;
            var count = drawables.Count;

            if (IsCreated && _modelPtr == unmanagedModel.Ptr && _drawableCount == count)
            {
                return true;
            }

            Dispose();

            if (count < 1)
            {
                return false;
            }

            var views = drawables.VertexPositions;
            var vertexCounts = drawables.VertexCounts;

            _modelPtr = unmanagedModel.Ptr;
            _drawableCount = count;
            _sourcePointers = new NativeArray<IntPtr>(count, Allocator.Persistent);
            _vertexOffsets = new NativeArray<int>(count, Allocator.Persistent);
            _vertexCounts = new NativeArray<int>(count, Allocator.Persistent);
            _bounds = new NativeArray<VertexBounds>(count, Allocator.Persistent);
            _dirtyIndices = new NativeArray<int>(count, Allocator.Persistent);

            var total = 0;
            for (var i = 0; i < count; i++)
            {
                _vertexOffsets[i] = total;
                _vertexCounts[i] = vertexCounts[i];
                _sourcePointers[i] = (IntPtr)views[i].UnmanagedFixedAddress;
                total += vertexCounts[i];
            }

            _baked = new NativeArray<Vector3>(Mathf.Max(1, total), Allocator.Persistent);

            return true;
        }

        /// <summary>
        /// Schedules and completes baking for the given dirty drawable indices.
        /// </summary>
        /// <param name="dirtyIndices">Drawable indices whose positions are dirty.</param>
        /// <param name="dirtyCount">Number of valid entries in <paramref name="dirtyIndices"/>.</param>
        public void Bake(int[] dirtyIndices, int dirtyCount)
        {
            if (!IsCreated || dirtyCount <= 0)
            {
                return;
            }

            for (var i = 0; i < dirtyCount; i++)
            {
                _dirtyIndices[i] = dirtyIndices[i];
            }

            _handle = new BakeJob
            {
                DirtyIndices = _dirtyIndices,
                SourcePointers = _sourcePointers,
                VertexOffsets = _vertexOffsets,
                VertexCounts = _vertexCounts,
                Baked = _baked,
                Bounds = _bounds,
            }
            .Schedule(dirtyCount, 1);
            _scheduled = true;

            // NOTE: Complete now. the Core buffers must not be touched by the next model update, and the
            // main-thread mesh upload that follows reads the baked results right away.
            Complete();
        }

        /// <summary>
        /// Completes any in-flight bake job.
        /// </summary>
        public void Complete()
        {
            if (_scheduled)
            {
                _handle.Complete();
                _scheduled = false;
            }
        }

        /// <summary>
        /// Completes any running bake job and releases all native buffers.
        /// </summary>
        public void Dispose()
        {
            Complete();

            if (_sourcePointers.IsCreated)
            {
                _sourcePointers.Dispose();
            }

            if (_vertexOffsets.IsCreated)
            {
                _vertexOffsets.Dispose();
            }

            if (_vertexCounts.IsCreated)
            {
                _vertexCounts.Dispose();
            }

            if (_bounds.IsCreated)
            {
                _bounds.Dispose();
            }

            if (_dirtyIndices.IsCreated)
            {
                _dirtyIndices.Dispose();
            }

            if (_baked.IsCreated)
            {
                _baked.Dispose();
            }

            _modelPtr = IntPtr.Zero;
            _drawableCount = 0;
        }


        /// <summary>
        /// Bakes one dirty drawable's Core vertex positions into the shared buffer and computes its XY bounds.
        /// </summary>
        [BurstCompile]
        private struct BakeJob : IJobParallelFor
        {
            /// <summary>
            /// Drawable indices to bake this run.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> DirtyIndices;

            /// <summary>
            /// Per-drawable Core vertex pointer (interleaved x, y).
            /// </summary>
            [ReadOnly]
            public NativeArray<IntPtr> SourcePointers;

            /// <summary>
            /// Per-drawable start index into <see cref="Baked"/>.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> VertexOffsets;

            /// <summary>
            /// Per-drawable vertex count.
            /// </summary>
            [ReadOnly]
            public NativeArray<int> VertexCounts;

            /// <summary>
            /// Baked positions for all drawables, concatenated (job output).
            /// </summary>
            // Each dirty drawable writes to a disjoint region, so parallel writes are safe.
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> Baked;

            /// <summary>
            /// Per-drawable XY bounds (job output).
            /// </summary>
            [NativeDisableParallelForRestriction]
            public NativeArray<VertexBounds> Bounds;

            /// <summary>
            /// Bakes the positions and bounds of the drawable at the given position in <see cref="DirtyIndices"/>.
            /// </summary>
            /// <param name="index">Index into <see cref="DirtyIndices"/>.</param>
            public unsafe void Execute(int index)
            {
                var drawableIndex = DirtyIndices[index];
                var count = VertexCounts[drawableIndex];

                if (count < 1)
                {
                    return;
                }

                var offset = VertexOffsets[drawableIndex];
                var src = (float*)SourcePointers[drawableIndex].ToPointer();

                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;

                for (var v = 0; v < count; v++)
                {
                    var x = src[(v * 2) + 0];
                    var y = src[(v * 2) + 1];

                    Baked[offset + v] = new Vector3(x, y, 0.0f);

                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }

                // Store the computed bounds for this drawable.
                Bounds[drawableIndex] = new VertexBounds
                {
                    MinX = minX,
                    MinY = minY,
                    MaxX = maxX,
                    MaxY = maxY
                };
            }
        }
    }
}
