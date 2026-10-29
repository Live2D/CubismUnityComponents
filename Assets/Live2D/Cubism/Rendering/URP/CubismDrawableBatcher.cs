/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Live2D.Cubism.Rendering.URP
{
    /// <summary>
    /// Merges consecutive <see cref="CubismRenderer"/>s sharing the same material state into a single
    /// <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material, int, int, MaterialPropertyBlock)"/> call
    /// to reduce SetPass/DrawCall counts.
    /// </summary>
    /// <remarks>
    /// Phase 2 scope:
    /// <list type="bullet">
    /// <item>Drawables from different <see cref="CubismRenderController"/>s in the same SortingGroup can be merged.</item>
    /// <item>Only drawables (not offscreens) are batched.</item>
    /// <item>Masked drawables are not batched.</item>
    /// <item>Drawables using the "Blit + DrawMesh" blend branch are not batched.</item>
    /// <item>When any <see cref="URP.RenderingInterceptor.ICubismRenderingInterceptor"/> is registered, batching is globally disabled.</item>
    /// </list>
    /// </remarks>
    internal sealed class CubismDrawableBatcher
    {
        /// <summary>
        /// Buffer/mesh pool that backs this batcher. Owned by the SortingGroup the batcher serves.
        /// </summary>
        private readonly CubismBatchPool _pool;

        /// <summary>
        /// Currently accumulated drawables waiting for flush.
        /// </summary>
        private readonly List<CubismRenderer> _batched = new List<CubismRenderer>(64);

        /// <summary>
        /// Reusable list of the batched drawables' meshes, handed to <see cref="Mesh.AcquireReadOnlyMeshData(List{Mesh})"/>.
        /// </summary>
        private readonly List<Mesh> _acquireMeshes = new List<Mesh>(64);

        /// <summary>
        /// A combined batch whose combine job was scheduled but whose result has not yet been uploaded.
        /// Uploads are deferred so all of a group's combine jobs can run concurrently and be completed once.
        /// </summary>
        private struct PendingBatch
        {
            public Mesh Mesh;
            public CubismBatchPool.BatchBuffers Buffers;
            public Mesh.MeshDataArray MeshData;
            public int VertexCount;
            public int IndexCount;
        }

        /// <summary>
        /// Combined batches with combine jobs in flight, awaiting <see cref="FinalizeFrame"/>.
        /// </summary>
        private readonly List<PendingBatch> _pending = new List<PendingBatch>(16);

        /// <summary>
        /// Dependency combining every in-flight combine job so they can be completed with a single call.
        /// </summary>
        private JobHandle _combineHandle;

        /// <summary>
        /// Material captured at batch start — reused to detect batch continuation.
        /// </summary>
        private Material _batchMaterial;

        /// <summary>
        /// Main texture captured at batch start.
        /// </summary>
        private Texture2D _batchMainTexture;

        /// <summary>
        /// Multiply color captured at batch start.
        /// </summary>
        private Color _batchMultiplyColor;

        /// <summary>
        /// Screen color captured at batch start.
        /// </summary>
        private Color _batchScreenColor;

        /// <summary>
        /// Render target captured at batch start.
        /// </summary>
        private RenderTexture _batchRenderTarget;

        /// <summary>
        /// Model opacity captured at batch start. Applied via the shared batch property block
        /// (cubism_ModelOpacity), so drawables from models with different opacity must not be merged.
        /// </summary>
        private float _batchModelOpacity;

        /// <summary>
        /// Identity offset/scale used on batched draws since transforms are baked into vertex positions.
        /// </summary>
        private static readonly Vector4 IdentityOffsetScale = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// Identity rotation quaternion used on batched draws (w=1).
        /// </summary>
        private static readonly Vector4 IdentityRotationQuaternion = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        /// <summary>
        /// <see cref="BatchThreshold"/>'s backing field.
        /// </summary>
        private int _batchThreshold = 2;

        /// <summary>
        /// When batch size is below this threshold the accumulated drawables are flushed individually
        /// (one DrawMesh per drawable) instead of being combined. Default 2 means any 2+ drawable batch is combined.
        /// </summary>
        public int BatchThreshold {
            get
            {
                return _batchThreshold;
            }
            set
            {
                _batchThreshold = Mathf.Max(2, value);
            }
        }

        private bool _isBatchingEnabled = true;

        /// <summary>
        /// When false, the batcher always falls through to per-drawable <see cref="CubismRenderer.DrawObject"/> calls.
        /// Set from the render pass based on whether any rendering interceptor is registered.
        /// </summary>
        public bool IsBatchingEnabled
        {
            get
            {
                return _isBatchingEnabled;
            }
            set
            {
                _isBatchingEnabled = value;
            }
        }

        /// <summary>
        /// Number of drawables currently waiting for flush.
        /// </summary>
        public int PendingCount
        {
            get
            {
                return _batched.Count;
            }
        }


        /// <summary>
        /// Constructs a batcher backed by the given pool.
        /// </summary>
        /// <param name="pool">Pool that owns the vertex/index buffers and mesh pool used for combined draws.</param>
        public CubismDrawableBatcher(CubismBatchPool pool)
        {
            _pool = pool;
        }


        /// <summary>
        /// Either appends the drawable to the current batch, flushes and starts a new batch,
        /// or draws it immediately if it is not batchable.
        /// </summary>
        /// <param name="renderer">Renderer to process.</param>
        /// <param name="commandBuffer">Command buffer to record draw commands into.</param>
        /// <param name="passData">Render pass data.</param>
        public void AddOrDraw(CubismRenderer renderer,
            CommandBuffer commandBuffer,
            URP.CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (!IsBatchingEnabled || !renderer.IsBatchable())
            {
                Flush(commandBuffer, passData);
                renderer.DrawObject(commandBuffer, passData);
                return;
            }

            if (_batched.Count == 0)
            {
                CaptureBatchKey(renderer);
                _batched.Add(renderer);
                return;
            }

            if (CanExtendBatch(renderer))
            {
                _batched.Add(renderer);
                return;
            }

            // Different batch key: flush current then start a new one with this renderer.
            Flush(commandBuffer, passData);
            CaptureBatchKey(renderer);
            _batched.Add(renderer);
        }

        /// <summary>
        /// Flushes the currently accumulated drawables.
        /// </summary>
        /// <remarks>
        /// If the pending count is below <see cref="BatchThreshold"/> the drawables are drawn individually via
        /// their regular <see cref="CubismRenderer.DrawObject"/> path. Otherwise, vertices are baked into the
        /// pool's batch mesh and a single <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material, int, int, MaterialPropertyBlock)"/> is issued.
        /// </remarks>
        /// <param name="commandBuffer">Command buffer to record draw commands into.</param>
        /// <param name="passData">Render pass data.</param>
        public void Flush(CommandBuffer commandBuffer,
            CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            if (_batched.Count == 0)
            {
                return;
            }

            if (_batched.Count < BatchThreshold)
            {
                for (var i = 0; i < _batched.Count; i++)
                {
                    _batched[i].DrawObject(commandBuffer, passData);
                }
            }
            else
            {
                SubmitCombinedDraw(commandBuffer, passData);
            }

            ResetBatchState();
        }


        /// <summary>
        /// Resets the current batch key and clears the accumulated drawable list.
        /// </summary>
        private void ResetBatchState()
        {
            _batched.Clear();
            _batchMaterial = null;
            _batchMainTexture = null;
            _batchRenderTarget = null;
            _batchMultiplyColor = default;
            _batchScreenColor = default;
            _batchModelOpacity = 0.0f;
        }


        /// <summary>
        /// Captures the batch key from the first drawable in a new batch.
        /// </summary>
        /// <param name="renderer">First drawable.</param>
        private void CaptureBatchKey(CubismRenderer renderer)
        {
            _batchMaterial = renderer.DrawMaterial ?? renderer.Material;
            _batchMainTexture = renderer.MainTexture;
            _batchMultiplyColor = renderer.DisplayedMultiplyColor;
            _batchScreenColor = renderer.DisplayedScreenColor;
            _batchRenderTarget = renderer.RenderController.CurrentFrameBuffer;
            _batchModelOpacity = renderer.RenderController.Opacity;
        }


        /// <summary>
        /// Tests whether the given renderer can extend the current batch.
        /// </summary>
        /// <remarks>
        /// Cross-controller batching is allowed: only the material/texture/color/render-target keys are
        /// compared. Drawables from two model instances pointing at the same DrawMaterial, MainTexture,
        /// MultiplyColor, ScreenColor, and CurrentFrameBuffer can be merged into a single DrawMesh.
        /// </remarks>
        /// <param name="renderer">Candidate renderer.</param>
        /// <returns>True if the renderer matches the current batch key.</returns>
        private bool CanExtendBatch(CubismRenderer renderer)
        {
            if ((renderer.DrawMaterial ?? renderer.Material) != _batchMaterial)
            {
                return false;
            }
            if (renderer.MainTexture != _batchMainTexture)
            {
                return false;
            }
            if (renderer.DisplayedMultiplyColor != _batchMultiplyColor)
            {
                return false;
            }
            if (renderer.DisplayedScreenColor != _batchScreenColor)
            {
                return false;
            }
            if (renderer.RenderController.CurrentFrameBuffer != _batchRenderTarget)
            {
                return false;
            }
            // ModelOpacity (cubism_ModelOpacity) is applied via the shared batch property block, so a
            // batch must not mix models with different opacity — the first renderer's value would win.
            if (!Mathf.Approximately(renderer.RenderController.Opacity, _batchModelOpacity))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Builds the combined mesh from all pending drawables and issues a single DrawMesh.
        /// </summary>
        /// <param name="commandBuffer">Command buffer to record draw commands into.</param>
        /// <param name="passData">Render pass data.</param>
        private void SubmitCombinedDraw(CommandBuffer commandBuffer,
            URP.CubismRenderPassFeature.CubismRenderPass.PassData passData)
        {
            // Gather the valid batched meshes with their per-drawable offsets, counts and baked transforms
            // into a per-batch native buffer set. Transform component reads must stay on the main thread.
            // The heavy per-vertex work is deferred to the worker-thread combine job below.
            var buffers = _pool.AcquireBuffers();
            buffers.EnsureDrawableCapacity(_batched.Count);
            _acquireMeshes.Clear();

            var vertexCursor = 0;
            var indexCursor = 0;
            var drawableCount = 0;
            for (var i = 0; i < _batched.Count; i++)
            {
                var renderer = _batched[i];
                var batchedMesh = renderer.Mesh;
                if (!batchedMesh)
                {
                    continue;
                }

                // Vertex/index counts are cached per renderer (fixed Live2D topology), so batching does not
                // query the Mesh API for every batched drawable every frame.
                if (!renderer.TryGetBatchMeshCounts(batchedMesh, out var vertexCount, out var indexCount))
                {
                    continue;
                }

                buffers.VertexOffsets[drawableCount] = vertexCursor;
                buffers.VertexCounts[drawableCount] = vertexCount;
                buffers.IndexOffsets[drawableCount] = indexCursor;
                buffers.IndexCounts[drawableCount] = indexCount;
                buffers.Transforms[drawableCount] = renderer.GetBatchTransform();

                // Compute the layout from the exact mesh captured below (the renderer's front buffer). Reading
                // it from that mesh — not a fixed buffer — is required for correctness: the two double-buffered
                // meshes can have different vertex layouts transiently after creation / a domain reload.
                buffers.Layouts[drawableCount] = renderer.GetBatchMeshLayout(batchedMesh);
                _acquireMeshes.Add(batchedMesh);

                vertexCursor += vertexCount;
                indexCursor += indexCount;
                drawableCount++;
            }

            if (vertexCursor == 0 || indexCursor == 0)
            {
                return;
            }

            buffers.EnsureCapacity(vertexCursor, indexCursor);

            // Schedule the read-back + transform + concatenate on worker threads WITHOUT completing here.
            // Every combined batch in the group is scheduled the same way and completed once in FinalizeFrame,
            // where the results are uploaded. The DrawMesh recorded below only references the (still-empty)
            // pooled mesh. Its data is uploaded before the command buffer executes, so the order holds.
            var meshDataArray = Mesh.AcquireReadOnlyMeshData(_acquireMeshes);
            var handle = new CubismBatchCombineJob
                {
                    MeshData = meshDataArray,
                    VertexOffsets = buffers.VertexOffsets,
                    IndexOffsets = buffers.IndexOffsets,
                    VertexCounts = buffers.VertexCounts,
                    IndexCounts = buffers.IndexCounts,
                    Transforms = buffers.Transforms,
                    Layouts = buffers.Layouts,
                    OutVertices = buffers.VertexPositions,
                    OutUvs = buffers.VertexUvs,
                    OutColors = buffers.VertexColors,
                    OutIndices = buffers.Indices,
                }
                .Schedule(drawableCount, 1);

            _combineHandle = JobHandle.CombineDependencies(_combineHandle, handle);

            // Acquire the pooled mesh now so DrawMesh can reference it. The upload is deferred to FinalizeFrame.
            var mesh = _pool.AcquireMesh();
            _pending.Add(new PendingBatch
            {
                Mesh = mesh,
                Buffers = buffers,
                MeshData = meshDataArray,
                VertexCount = vertexCursor,
                IndexCount = indexCursor,
            });

            // Configure property block on the first drawable. Transforms are baked into vertices, so
            // we overwrite the transform uniforms with identity values.
            var first = _batched[0];
            first.SetupBatchPropertyBlock(passData, IdentityOffsetScale, IdentityRotationQuaternion, 0f);

            // Match DrawDrawable: CurrentFrameBuffer can be null or out of sync with the camera depth
            // surface (e.g. after resolution changes). Fall back and sync controllers so state matches the
            // per-drawable path. A direct group renders straight to the camera color target and never blits
            // the common RT back, so it must fall back to the camera target. Only common-RT groups fall back
            // to the common RT.
            var colorBuffer = _batchRenderTarget;

            // The depth surface does not resolve to a RenderTexture when the camera renders straight to the
            // backBuffer, so the size check is only made when there is something to compare against.
            var depthRt = (RenderTexture)passData.CameraDepthTextureHandle;
            if (!colorBuffer
                || (depthRt
                    && (colorBuffer.width != depthRt.width
                        || colorBuffer.height != depthRt.height)))
            {
                var fallbackBuffer = passData.CurrentGroupRendersDirect
                    ? (RenderTexture)passData.CameraTextureHandle
                    : passData.CommonRenderingTexture;

                for (var i = 0; i < _batched.Count; i++)
                {
                    var rc = _batched[i].RenderController;
                    if (rc)
                    {
                        rc.CurrentFrameBuffer = fallbackBuffer;
                    }
                }

                colorBuffer = fallbackBuffer;
            }

            // The fallback itself can resolve to nothing when the camera renders straight to the backBuffer.
            // Bind the camera handle in that case: it stays a valid render target even then.
            if (colorBuffer)
            {
                commandBuffer.SetRenderTarget(colorBuffer, passData.CameraDepthTextureHandle);
            }
            else
            {
                commandBuffer.SetRenderTarget(passData.CameraTextureHandle, passData.CameraDepthTextureHandle);
            }

            commandBuffer.DrawMesh(
                mesh,
                Matrix4x4.identity,
                _batchMaterial,
                0,
                0,
                first.BatchPropertyBlock);
        }

        /// <summary>
        /// Completes all in-flight combine jobs at once and uploads each combined batch to its mesh.
        /// Must be called after the group's final <see cref="Flush"/> and before the command buffer executes.
        /// </summary>
        public void FinalizeFrame()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            // Single Complete for every batch's combine job scheduled this frame.
            _combineHandle.Complete();
            _combineHandle = default;

            for (var i = 0; i < _pending.Count; i++)
            {
                var pending = _pending[i];
                var buffers = pending.Buffers;
                var mesh = pending.Mesh;

                // Upload the combined vertex/index data to the pooled mesh.
                // The mesh is already referenced by the DrawMesh recorded in SubmitCombinedDraw.
                mesh.Clear();
                mesh.SetVertices(buffers.VertexPositions, 0, pending.VertexCount);
                mesh.SetUVs(0, buffers.VertexUvs, 0, pending.VertexCount);
                mesh.SetColors(buffers.VertexColors, 0, pending.VertexCount);
                mesh.SetIndices(buffers.Indices.GetSubArray(0, pending.IndexCount), MeshTopology.Triangles, 0);
            }

            // Release the read-only mesh data (already consumed by the completed job) after uploading, so it is
            // disposed exactly once even if an upload above throws (AbortFrame then handles the rest).
            DisposePending();
        }

        /// <summary>
        /// Releases resources acquired this frame without uploading, so an aborted pass never leaves a
        /// <see cref="Mesh.MeshDataArray"/> undisposed (which would keep its source meshes read-only locked and
        /// break every later frame). Safe to call after <see cref="FinalizeFrame"/> (a no-op once cleared).
        /// </summary>
        public void AbortFrame()
        {
            if (_pending.Count == 0)
            {
                return;
            }

            _combineHandle.Complete();
            _combineHandle = default;

            DisposePending();
        }

        /// <summary>
        /// Disposes every pending batch's read-only mesh data and clears the pending list.
        /// </summary>
        private void DisposePending()
        {
            for (var i = 0; i < _pending.Count; i++)
            {
                _pending[i].MeshData.Dispose();
            }

            _pending.Clear();
        }
    }
}
