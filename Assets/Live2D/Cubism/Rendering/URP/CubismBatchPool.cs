/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Live2D.Cubism.Rendering.URP
{
    /// <summary>
    /// SortingGroup-scoped batching scratch space.
    /// Holds a pool of per-batch native buffer sets (filled by <see cref="CubismBatchCombineJob"/>) and a
    /// per-frame mesh pool used for the resulting <see cref="UnityEngine.Rendering.CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material, int, int, MaterialPropertyBlock)"/> calls.
    /// </summary>
    /// <remarks>
    /// One instance is owned per SortingGroup. Each combined batch bakes into its own <see cref="BatchBuffers"/>
    /// set so that many batches can have their combine jobs in flight at once and be completed together
    /// (see <see cref="CubismDrawableBatcher"/>), instead of one blocking Complete per batch.
    /// </remarks>
    internal sealed class CubismBatchPool
    {
        /// <summary>
        /// Initial capacity (in vertices) for a batch's vertex buffers.
        /// </summary>
        private const int InitialVertexCapacity = 256;

        /// <summary>
        /// Initial capacity (in indices) for a batch's index buffer.
        /// </summary>
        private const int InitialIndexCapacity = 512;

        /// <summary>
        /// Initial capacity (in drawables) for a batch's per-drawable job-input buffers.
        /// </summary>
        private const int InitialDrawableCapacity = 64;

        /// <summary>
        /// Native buffer set for a single combined batch: the combine job's per-drawable inputs and its
        /// concatenated vertex/index outputs. One set is acquired per batch so completes can be deferred.
        /// </summary>
        internal sealed class BatchBuffers
        {
            /// <summary>
            /// Concatenated vertex positions for all drawables in the batch (combine job output).
            /// </summary>
            internal NativeArray<Vector3> VertexPositions;

            /// <summary>
            /// Concatenated vertex UVs for all drawables in the batch (combine job output).
            /// </summary>
            internal NativeArray<Vector2> VertexUvs;

            /// <summary>
            /// Concatenated vertex colors for all drawables in the batch (combine job output).
            /// </summary>
            internal NativeArray<Color32> VertexColors;

            /// <summary>
            /// Concatenated indices for all drawables in the batch (combine job output).
            /// </summary>
            internal NativeArray<int> Indices;

            /// <summary>
            /// Per-drawable start index into the combined vertex buffers (combine job input).
            /// </summary>
            internal NativeArray<int> VertexOffsets;

            /// <summary>
            /// Per-drawable start index into the combined index buffer (combine job input).
            /// </summary>
            internal NativeArray<int> IndexOffsets;

            /// <summary>
            /// Per-drawable vertex count (combine job input).
            /// </summary>
            internal NativeArray<int> VertexCounts;

            /// <summary>
            /// Per-drawable index count (combine job input).
            /// </summary>
            internal NativeArray<int> IndexCounts;

            /// <summary>
            /// Per-drawable root transform used to bake vertices into world space (combine job input).
            /// </summary>
            internal NativeArray<BatchTransform> Transforms;

            /// <summary>
            /// Per-drawable source mesh layout describing stride and index format (combine job input).
            /// </summary>
            internal NativeArray<BatchMeshLayout> Layouts;

            /// <summary>
            /// Ensures the combined vertex/index buffers hold at least the requested capacity.
            /// Reallocation does not preserve contents (buffers are overwritten each use).
            /// </summary>
            internal void EnsureCapacity(int vertexCapacity, int indexCapacity)
            {
                if (!VertexPositions.IsCreated || VertexPositions.Length < vertexCapacity)
                {
                    var newSize = Mathf.Max(vertexCapacity, VertexPositions.IsCreated ? VertexPositions.Length * 2 : InitialVertexCapacity);

                    if (VertexPositions.IsCreated)
                    {
                        VertexPositions.Dispose();
                    }

                    if (VertexUvs.IsCreated)
                    {
                        VertexUvs.Dispose();
                    }

                    if (VertexColors.IsCreated)
                    {
                        VertexColors.Dispose();
                    }

                    VertexPositions = new NativeArray<Vector3>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    VertexUvs = new NativeArray<Vector2>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    VertexColors = new NativeArray<Color32>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }

                if (!Indices.IsCreated || Indices.Length < indexCapacity)
                {
                    var newSize = Mathf.Max(indexCapacity, Indices.IsCreated ? Indices.Length * 2 : InitialIndexCapacity);

                    if (Indices.IsCreated)
                    {
                        Indices.Dispose();
                    }

                    Indices = new NativeArray<int>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
            }

            /// <summary>
            /// Ensures the per-drawable job-input buffers hold at least <paramref name="drawableCapacity"/> entries.
            /// </summary>
            internal void EnsureDrawableCapacity(int drawableCapacity)
            {
                if (Transforms.IsCreated && Transforms.Length >= drawableCapacity)
                {
                    return;
                }

                var newSize = Mathf.Max(drawableCapacity, Transforms.IsCreated ? Transforms.Length * 2 : InitialDrawableCapacity);

                if (VertexOffsets.IsCreated)
                {
                    VertexOffsets.Dispose();
                }

                if (IndexOffsets.IsCreated)
                {
                    IndexOffsets.Dispose();
                }

                if (VertexCounts.IsCreated)
                {
                    VertexCounts.Dispose();
                }

                if (IndexCounts.IsCreated)
                {
                    IndexCounts.Dispose();
                }

                if (Transforms.IsCreated)
                {
                    Transforms.Dispose();
                }

                if (Layouts.IsCreated)
                {
                    Layouts.Dispose();
                }

                VertexOffsets = new NativeArray<int>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                IndexOffsets = new NativeArray<int>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                VertexCounts = new NativeArray<int>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                IndexCounts = new NativeArray<int>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Transforms = new NativeArray<BatchTransform>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Layouts = new NativeArray<BatchMeshLayout>(newSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            internal void Dispose()
            {
                if (VertexPositions.IsCreated)
                {
                    VertexPositions.Dispose();
                }

                if (VertexUvs.IsCreated)
                {
                    VertexUvs.Dispose();
                }

                if (VertexColors.IsCreated)
                {
                    VertexColors.Dispose();
                }

                if (Indices.IsCreated)
                {
                    Indices.Dispose();
                }

                if (VertexOffsets.IsCreated)
                {
                    VertexOffsets.Dispose();
                }

                if (IndexOffsets.IsCreated)
                {
                    IndexOffsets.Dispose();
                }

                if (VertexCounts.IsCreated)
                {
                    VertexCounts.Dispose();
                }

                if (IndexCounts.IsCreated)
                {
                    IndexCounts.Dispose();
                }

                if (Transforms.IsCreated)
                {
                    Transforms.Dispose();
                }

                if (Layouts.IsCreated)
                {
                    Layouts.Dispose();
                }
            }
        }

        /// <summary>
        /// Pool of per-batch buffer sets, reused across frames. One is acquired per combined batch.
        /// </summary>
        private readonly List<BatchBuffers> _bufferSets = new List<BatchBuffers>();

        /// <summary>
        /// Current write index into the buffer-set pool. Incremented on every <see cref="AcquireBuffers"/>.
        /// </summary>
        private int _currentBufferIndex;

        /// <summary>
        /// Pool of reusable mesh instances. Meshes are lazily allocated and reused across frames.
        /// </summary>
        private readonly List<Mesh> _meshes = new List<Mesh>();

        /// <summary>
        /// Current write index into the mesh pool. Incremented on every <see cref="AcquireMesh"/>.
        /// </summary>
        private int _currentMeshIndex;

        /// <summary>
        /// Acquires the next per-batch buffer set. Lazily allocated. Reused across frames after <see cref="Reset"/>.
        /// </summary>
        internal BatchBuffers AcquireBuffers()
        {
            BatchBuffers buffers;

            if (_bufferSets.Count <= _currentBufferIndex)
            {
                buffers = new BatchBuffers();
                _bufferSets.Add(buffers);
            }
            else
            {
                buffers = _bufferSets[_currentBufferIndex];
            }

            ++_currentBufferIndex;
            return buffers;
        }

        /// <summary>
        /// Acquires the next batch mesh. New meshes are lazily allocated. Subsequent frames reuse the
        /// same mesh instances after <see cref="Reset"/> is called.
        /// </summary>
        /// <returns>A mesh ready to be cleared and refilled.</returns>
        internal Mesh AcquireMesh()
        {
            Mesh mesh;

            if (_meshes.Count <= _currentMeshIndex)
            {
                mesh = new Mesh
                {
                    name = $"cubism_batch_mesh_{_currentMeshIndex}",
                    hideFlags = HideFlags.HideAndDontSave,
                };
                mesh.MarkDynamic();
                _meshes.Add(mesh);
            }
            else
            {
                mesh = _meshes[_currentMeshIndex];
            }

            ++_currentMeshIndex;
            return mesh;
        }

        /// <summary>
        /// Resets per-frame state so the mesh and buffer pools can be reused on the next frame.
        /// Called at the start of every frame before any batch draws are issued.
        /// </summary>
        internal void Reset()
        {
            _currentMeshIndex = 0;
            _currentBufferIndex = 0;
        }

        /// <summary>
        /// Destroys all pooled meshes and releases the native buffers. Must be called before this pool is
        /// discarded: the meshes use <see cref="HideFlags.HideAndDontSave"/> and the native arrays use
        /// <see cref="Allocator.Persistent"/>, so neither is reclaimed automatically and both would leak.
        /// </summary>
        internal void Dispose()
        {
            for (var i = 0; i < _meshes.Count; i++)
            {
                if (!_meshes[i])
                {
                    continue;
                }

                // Use the deferred Destroy while playing so the mesh is freed only after the current
                // frame's rendering completes. DestroyImmediate is required in edit mode.
                if (Application.isPlaying)
                {
                    Object.Destroy(_meshes[i]);
                }
                else
                {
                    Object.DestroyImmediate(_meshes[i]);
                }
            }

            _meshes.Clear();
            _currentMeshIndex = 0;

            for (var i = 0; i < _bufferSets.Count; i++)
            {
                _bufferSets[i].Dispose();
            }

            _bufferSets.Clear();
            _currentBufferIndex = 0;
        }
    }
}
