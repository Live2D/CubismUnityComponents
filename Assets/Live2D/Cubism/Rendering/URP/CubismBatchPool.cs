/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using UnityEngine;


namespace Live2D.Cubism.Rendering
{
    /// <summary>
    /// SortingGroup-scoped batching scratch space.
    /// Holds the dynamic vertex/index buffers that drawables bake into and a per-frame mesh pool used
    /// for the resulting <see cref="UnityEngine.Rendering.CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material, int, int, MaterialPropertyBlock)"/> calls.
    /// </summary>
    /// <remarks>
    /// One instance is owned per SortingGroup so that drawables originating from different
    /// <see cref="CubismRenderController"/>s in the same group can be merged into a single DrawMesh.
    /// </remarks>
    internal sealed class CubismBatchPool
    {
        /// <summary>
        /// Initial capacity (in vertices) for the dynamic vertex buffers and the per-mesh scratch lists.
        /// Buffers grow geometrically beyond this on demand; the value is a working-set seed, not a hard cap.
        /// </summary>
        private const int InitialVertexCapacity = 256;

        /// <summary>
        /// Initial capacity (in indices) for the dynamic index buffer and the per-mesh scratch list.
        /// Buffers grow geometrically beyond this on demand; the value is a working-set seed, not a hard cap.
        /// </summary>
        private const int InitialIndexCapacity = 512;

        /// <summary>
        /// Dynamic vertex buffer used to accumulate batched drawable vertex positions (after transform bake).
        /// </summary>
        internal Vector3[] VertexPositions;

        /// <summary>
        /// Dynamic vertex buffer used to accumulate batched drawable UVs.
        /// </summary>
        internal Vector2[] VertexUVs;

        /// <summary>
        /// Dynamic vertex buffer used to accumulate batched drawable vertex colors.
        /// </summary>
        internal Color[] VertexColors;

        /// <summary>
        /// Dynamic index buffer used to accumulate batched drawable triangle indices (with vertex offsets applied).
        /// </summary>
        internal int[] Indices;

        /// <summary>
        /// Reusable scratch list filled by <see cref="UnityEngine.Mesh.GetVertices(List{Vector3})"/>.
        /// Avoids the per-call array allocation incurred by <see cref="UnityEngine.Mesh.vertices"/>.
        /// </summary>
        internal readonly List<Vector3> ScratchVertices = new List<Vector3>(InitialVertexCapacity);

        /// <summary>
        /// Reusable scratch list filled by <see cref="UnityEngine.Mesh.GetUVs(int, List{Vector2})"/>.
        /// </summary>
        internal readonly List<Vector2> ScratchUVs = new List<Vector2>(InitialVertexCapacity);

        /// <summary>
        /// Reusable scratch list filled by <see cref="UnityEngine.Mesh.GetColors(List{Color})"/>.
        /// </summary>
        internal readonly List<Color> ScratchColors = new List<Color>(InitialVertexCapacity);

        /// <summary>
        /// Reusable scratch list filled by <see cref="UnityEngine.Mesh.GetTriangles(List{int}, int)"/>.
        /// </summary>
        internal readonly List<int> ScratchIndices = new List<int>(InitialIndexCapacity);

        /// <summary>
        /// Pool of reusable mesh instances. Meshes are lazily allocated and reused across frames.
        /// </summary>
        private readonly List<Mesh> _meshes = new List<Mesh>();

        /// <summary>
        /// Current write index into the mesh pool. Incremented on every <see cref="AcquireMesh"/>.
        /// </summary>
        private int _currentMeshIndex;

        /// <summary>
        /// Ensures the vertex/index buffers have at least the requested capacity.
        /// Buffers grow geometrically (doubling) to amortize allocations.
        /// </summary>
        /// <remarks>
        /// These are per-frame throwaway scratch buffers. Each batch overwrites the range it uses
        /// (cursors start at 0 and SubmitCombinedDraw passes that range to SetVertices/SetTriangles),
        /// so reallocation intentionally does not preserve old contents, and stale data beyond the
        /// written range is never read.
        /// </remarks>
        /// <param name="vertexCapacity">Required vertex capacity.</param>
        /// <param name="indexCapacity">Required index capacity.</param>
        internal void EnsureCapacity(int vertexCapacity, int indexCapacity)
        {
            if (VertexPositions == null || VertexPositions.Length < vertexCapacity)
            {
                // All three vertex buffers are reallocated together at the same size to stay
                // length-aligned: WriteBakedVerticesToBatch writes position/UV/color at the same
                // index, so a size mismatch would throw IndexOutOfRange.
                var newSize = Mathf.Max(vertexCapacity, VertexPositions?.Length * 2 ?? InitialVertexCapacity);
                VertexPositions = new Vector3[newSize];
                VertexUVs = new Vector2[newSize];
                VertexColors = new Color[newSize];
            }

            if (Indices == null || Indices.Length < indexCapacity)
            {
                var newSize = Mathf.Max(indexCapacity, Indices?.Length * 2 ?? InitialIndexCapacity);
                Indices = new int[newSize];
            }
        }

        /// <summary>
        /// Acquires the next batch mesh. New meshes are lazily allocated; subsequent frames reuse the
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
        /// Resets per-frame state so the mesh pool can be reused on the next frame.
        /// Called at the start of every frame before any batch draws are issued.
        /// </summary>
        internal void Reset()
        {
            _currentMeshIndex = 0;
        }

        /// <summary>
        /// Destroys all pooled meshes and clears the pool. Must be called before this pool is
        /// discarded: the meshes use <see cref="HideFlags.HideAndDontSave"/>, so they are not
        /// reclaimed by the garbage collector or by scene unload and would otherwise leak native memory.
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
                // frame's rendering completes; DestroyImmediate is required in edit mode.
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
        }
    }
}
