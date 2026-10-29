/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Live2D.Cubism.Rendering.URP
{
    /// <summary>
    /// World transform (the transform the shader would otherwise apply per-drawable) baked into a batched
    /// drawable's vertices on a worker thread. Composed on the main thread from the drawable's transforms.
    /// </summary>
    internal struct BatchTransform
    {
        /// <summary>
        /// Scale to apply to the drawable's vertices, in the X axis.
        /// The scale is applied before rotation and translation.
        /// </summary>
        public float ScaleX;

        /// <summary>
        /// Scale to apply to the drawable's vertices, in the Y axis.
        /// The scale is applied before rotation and translation.
        /// </summary>
        public float ScaleY;

        /// <summary>
        /// Rotation to apply to the drawable's vertices, in quaternion form.
        /// The rotation is applied after scaling and before translation.
        /// </summary>
        public Quaternion Rotation;

        /// <summary>
        /// Translation to apply to the drawable's vertices, in the X axis.
        /// </summary>
        public float OffsetX;

        /// <summary>
        /// Translation to apply to the drawable's vertices, in the Y axis.
        /// </summary>
        public float OffsetY;

        /// <summary>
        /// Translation to apply to the drawable's vertices, in the Z axis.
        /// </summary>
        public float OffsetZ;
    }

    /// <summary>
    /// A batched drawable's interleaved vertex/index buffer layout, queried on the main thread so the Burst
    /// job can read the raw buffer directly instead of calling the non-Burst <see cref="Mesh.MeshData"/> helpers.
    /// </summary>
    internal struct BatchMeshLayout
    {
        /// <summary>
        /// Stride of the interleaved vertex buffer, in bytes.
        /// </summary>
        public int Stride;

        /// <summary>
        /// Offset of the position attribute in the interleaved vertex buffer, in bytes.
        /// </summary>
        public int PositionOffset;

        /// <summary>
        /// Offset of the UV attribute in the interleaved vertex buffer, in bytes.
        /// </summary>
        public int UvOffset;

        /// <summary>
        /// Offset of the color attribute in the interleaved vertex buffer, in bytes.
        /// </summary>
        public int ColorOffset;

        /// <summary>
        /// Whether the drawable has UVs (and thus a valid <see cref="UvOffset"/>).
        /// </summary>
        public bool HasUv;

        /// <summary>
        /// Whether the drawable has colors (and thus a valid <see cref="ColorOffset"/>).
        /// </summary>
        public bool HasColor;

        /// <summary>
        /// Whether the drawable's index buffer is 16-bit (and thus <see cref="Mesh.MeshData.GetIndexData"/>
        /// should be called with <see cref="ushort"/> instead of <see cref="int"/>).
        /// </summary>
        public bool Index16;
    }

    /// <summary>
    /// Reads each batched drawable's raw mesh buffer, bakes its transform into the vertices, and concatenates
    /// the result into the shared combined-mesh buffers. Burst-compiled: it reads the raw vertex/index buffers
    /// (via <see cref="Mesh.MeshData.GetVertexData"/> / <see cref="Mesh.MeshData.GetIndexData"/>) using the
    /// layout gathered on the main thread, so no non-Burst MeshData accessor is called from the worker threads.
    /// </summary>
    /// <remarks>
    /// Sources are the drawables' own (front-buffer) meshes via <see cref="Mesh.AcquireReadOnlyMeshData"/>,
    /// so the combined result matches exactly what the per-drawable path would draw this frame.
    /// </remarks>
    [BurstCompile]
    internal struct CubismBatchCombineJob : IJobParallelFor
    {
        /// <summary>
        /// The raw mesh data for each drawable, acquired on the main thread via <see cref="Mesh.AcquireReadOnlyMeshData"/>.
        /// </summary>
        [ReadOnly]
        public Mesh.MeshDataArray MeshData;

        /// <summary>
        /// The offset of each drawable's vertices in the shared combined vertex buffer, in elements.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> VertexOffsets;

        /// <summary>
        /// The offset of each drawable's indices in the shared combined index buffer, in elements.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> IndexOffsets;

        /// <summary>
        /// The number of vertices in each drawable's mesh.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> VertexCounts;

        /// <summary>
        /// The number of indices in each drawable's mesh.
        /// </summary>
        [ReadOnly]
        public NativeArray<int> IndexCounts;

        /// <summary>
        /// The transform to apply to each drawable's vertices.
        /// </summary>
        [ReadOnly]
        public NativeArray<BatchTransform> Transforms;

        /// <summary>
        /// The layout of each drawable's interleaved vertex/index buffer.
        /// </summary>
        [ReadOnly]
        public NativeArray<BatchMeshLayout> Layouts;

        /// <summary>
        /// The shared combined vertex buffer to write to, in elements.
        /// </summary>
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector3> OutVertices;

        /// <summary>
        /// The shared combined UV buffer to write to, in elements.
        /// </summary>
        [NativeDisableParallelForRestriction]
        public NativeArray<Vector2> OutUvs;

        /// <summary>
        /// The shared combined color buffer to write to, in elements.
        /// </summary>
        [NativeDisableParallelForRestriction]
        public NativeArray<Color32> OutColors;

        /// <summary>
        /// The shared combined index buffer to write to, in elements.
        /// </summary>
        [NativeDisableParallelForRestriction]
        public NativeArray<int> OutIndices;

        /// <summary>
        /// Execute the job for a single drawable, reading its raw mesh data, baking its transform into the vertices,
        /// and writing the result into the shared combined buffers.
        /// </summary>
        /// <param name="index">Index of the drawable to combine.</param>
        public unsafe void Execute(int index)
        {
            var data = MeshData[index];
            var layout = Layouts[index];
            var transform = Transforms[index];
            var vertexCount = VertexCounts[index];
            var vertexOffset = VertexOffsets[index];

            var vertexBuffer = (byte*)data.GetVertexData<byte>(0).GetUnsafeReadOnlyPtr();

            for (var i = 0; i < vertexCount; i++)
            {
                // Positions. Shader order: scale(xy) -> rotate(xyz) -> translate(xy, z).
                var position = UnsafeUtility.ReadArrayElementWithStride<Vector3>(vertexBuffer + layout.PositionOffset, i, layout.Stride);

                position.x *= transform.ScaleX;
                position.y *= transform.ScaleY;
                position = Rotate(transform.Rotation, position);
                position.x += transform.OffsetX;
                position.y += transform.OffsetY;
                position.z += transform.OffsetZ;
                OutVertices[vertexOffset + i] = position;

                // UVs (default to zero when the drawable has none, matching the previous batch behavior).
                if (layout.HasUv)
                {
                    OutUvs[vertexOffset + i] = UnsafeUtility.ReadArrayElementWithStride<Vector2>(vertexBuffer + layout.UvOffset, i, layout.Stride);
                }
                else
                {
                    OutUvs[vertexOffset + i] = default;
                }

                // Colors (default to white when the drawable has none, matching the previous batch behavior).
                if (layout.HasColor)
                {
                    OutColors[vertexOffset + i] = UnsafeUtility.ReadArrayElementWithStride<Color32>(vertexBuffer + layout.ColorOffset, i, layout.Stride);
                }
                else
                {
                    OutColors[vertexOffset + i] = new Color32(255, 255, 255, 255);
                }
            }

            // Indices: read then rebase onto this drawable's slot in the combined vertex buffer.
            var indexCount = IndexCounts[index];
            var indexOffset = IndexOffsets[index];

            if (layout.Index16)
            {
                var indices = data.GetIndexData<ushort>();
                for (var i = 0; i < indexCount; i++)
                {
                    OutIndices[indexOffset + i] = indices[i] + vertexOffset;
                }
            }
            else
            {
                var indices = data.GetIndexData<int>();
                for (var i = 0; i < indexCount; i++)
                {
                    OutIndices[indexOffset + i] = indices[i] + vertexOffset;
                }
            }
        }

        /// <summary>
        /// Rotates a vector by a quaternion using the explicit formula, so it Burst-compiles without relying
        /// on the <see cref="Quaternion"/> multiplication operator.
        /// </summary>
        /// <param name="rotation">Rotation to apply.</param>
        /// <param name="position">Position to rotate.</param>
        /// <returns>The rotated position.</returns>
        private static Vector3 Rotate(Quaternion rotation, Vector3 position)
        {
            var tx = 2.0f * (rotation.y * position.z - rotation.z * position.y);
            var ty = 2.0f * (rotation.z * position.x - rotation.x * position.z);
            var tz = 2.0f * (rotation.x * position.y - rotation.y * position.x);

            return new Vector3(
                position.x + rotation.w * tx + (rotation.y * tz - rotation.z * ty),
                position.y + rotation.w * ty + (rotation.z * tx - rotation.x * tz),
                position.z + rotation.w * tz + (rotation.x * ty - rotation.y * tx));
        }
    }
}
