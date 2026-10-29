/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Utils;
using Live2D.Cubism.Rendering.Masking;
using Live2D.Cubism.Rendering.URP.RenderingInterceptor;
using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Live2D.Cubism.Rendering.URP
{
    /// <summary>
    /// Provides data for rendering events `CubismRenderingInterceptorsManager.GetInstance().OnPreRendering` and `CubismRenderingInterceptorsManager.GetInstance().OnPostRendering`.
    /// </summary>
    public struct CubismRenderedEventArgs
    {
        /// <summary>
        /// Render pass data.
        /// </summary>
        public CubismRenderPassFeature.CubismRenderPass.PassData PassData;
        /// <summary>
        /// Command buffer for rendering.
        /// </summary>
        public CommandBuffer CommandBuffer;
        /// <summary>
        /// Color buffer render texture.
        /// </summary>
        public RenderTexture ColorBuffer;
        /// <summary>
        /// Depth buffer texture handle.
        /// </summary>
#if UNITY_6000_0_OR_NEWER
        public TextureHandle DepthBuffer;
#else
        public RenderTexture DepthBuffer;
#endif
        /// <summary>
        /// Sorting mode of the draw object's render controller.
        /// </summary>
        public CubismSortingMode SortingMode;
        /// <summary>
        /// Group sorting order.
        /// </summary>
        public int GroupSortingOrder;
        /// <summary>
        /// Sorting order of the draw object.
        /// NOTE: If SortingMode is set to sort by depth, multiple draw objects may have the same sorting order.
        /// </summary>
        public int SortingOrder;
        /// <summary>
        /// The drawable being rendered.
        /// </summary>
        public CubismDrawable Drawable;
        /// <summary>
        /// Distance to the camera.
        /// NOTE: If SortingMode is set to sort by order, multiple draw objects may have the same distance.
        /// </summary>
        public float Distance;
        /// <summary>
        /// Distance to the previous draw object in the sorted order.
        /// </summary>
        public float? PreviousDistance;
        /// <summary>
        /// Distance to the next draw object in the sorted order.
        /// </summary>
        public float? NextDistance;
        /// <summary>
        /// Position of the camera.
        /// </summary>
        public Vector3 CameraPos;
        /// <summary>
        /// Forward direction of the camera.
        /// </summary>
        public Vector3 CameraForward;
    }

    /// <summary>
    /// Scriptable renderer feature for rendering Cubism models in URP.
    /// </summary>
    public class CubismRenderPassFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Greater than or equal to comparison value on Z Test.
        /// </summary>
        internal static readonly int GEqual = 7;

        /// <summary>
        /// Less than or equal to comparison value on Z Test.
        /// </summary>
        internal static readonly int LEqual = 4;

        /// <summary>
        /// Scriptable render pass for rendering Cubism models.
        /// </summary>
        public class CubismRenderPass : ScriptableRenderPass
        {
            /// <summary>
            /// When true, consecutive drawables sharing the same material state are combined into a single DrawMesh.
            /// Set from <see cref="CubismRenderPassFeature"/> each frame so inspector edits take effect immediately.
            /// </summary>
            internal bool EnableBatching = true;

            /// <summary>
            /// When true, render controller groups whose models need no intermediate RenderTexture are drawn
            /// directly to the camera color target (no per-group blit). Set from <see cref="CubismRenderPassFeature"/>
            /// each frame so inspector edits take effect immediately. Unlike <see cref="EnableBatching"/> (read
            /// directly while drawing), this value selects IsDirectGroup, which is cached per group in
            /// <see cref="SetUpRendererGroup"/>. <see cref="SortingRendererGroups"/> detects a change to it and
            /// forces that cache to refresh, so a runtime toggle is applied at once rather than at the next sort.
            /// </summary>
            internal bool EnableDirectRendering = false;

            /// <summary>
            /// Minimum number of consecutive batchable drawables required to trigger a combined DrawMesh.
            /// Set from <see cref="CubismRenderPassFeature"/> each frame so inspector edits take effect immediately.
            /// </summary>
            internal int BatchThreshold = 2;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            /// <summary>
            /// When true, restores the pre-optimization full-screen clear of every pooled offscreen render
            /// texture at the start of the draw call. Measurement aid only. See
            /// <see cref="CubismRenderPassFeature.EnableOffscreenPreClear"/>.
            /// </summary>
            internal bool EnableOffscreenPreClear = false;
#endif


            /// <summary>
            /// Structure that groups renderers by their sorting index.
            /// </summary>
            private struct RendererGroupData
            {
                /// <summary>
                /// Sorting index for the renderer group.
                /// </summary>
                public int SortingIndex;

                /// <summary>
                /// Array of render controllers in this group.
                /// </summary>
                public CubismRenderController[] RenderControllers;

                /// <summary>
                /// Array of renderers in this group.
                /// </summary>
                public CubismRenderer[] Renderers;

                /// <summary>
                /// Batch buffer/mesh pool shared by all controllers in this group.
                /// </summary>
                public CubismBatchPool BatchPool;

                /// <summary>
                /// Batcher that merges drawables across controllers within this group.
                /// </summary>
                public CubismDrawableBatcher Batcher;

                /// <summary>
                /// When true, every controller in this group can render directly to the camera target
                /// (no background-reading blend, no offscreen), so the group skips the common RT and blit.
                /// </summary>
                public bool IsDirectGroup;
            }

            /// <summary>
            /// Command buffer used for rendering operations.
            /// </summary>
            private static CommandBuffer _commandBuffer;

            /// <summary>
            /// Array of renderer groups sorted by their sorting index.
            /// </summary>
            private static RendererGroupData[] _sortedRendererGroupDataArray;

            /// <summary>
            /// EnableDirectRendering value applied during the previous <see cref="SortingRendererGroups"/> call.
            /// IsDirectGroup / CurrentFrameBuffer are only (re)computed in <see cref="SetUpRendererGroup"/>, which
            /// runs for a group only when its sort order changes. Comparing against this lets a runtime toggle
            /// force that setup to re-run so the change is not deferred until an unrelated sort happens. Nullable
            /// so the first call after a domain reload always re-evaluates.
            /// </summary>
            private static bool? _previousEnableDirectRendering;

            /// <summary>
            /// Mesh used for blitting render textures.
            /// </summary>
            private static Mesh _blitRenderTextureMesh;

            /// <summary>
            /// Material used for blitting render textures.
            /// </summary>
            private static Material _blitRenderTextureMaterial;

            /// <summary>
            /// Material used by <see cref="CopyRegionToTarget"/>. Separate from
            /// <see cref="_blitRenderTextureMaterial"/> because the depth comparison comes from a material
            /// property, which a property block cannot override: a copy has to pass the test unconditionally.
            /// </summary>
            private static Material _blitCopyMaterial;

            /// <summary>
            /// Property block carrying the source texture of <see cref="CopyRegionToTarget"/>.
            /// Shared, because DrawMesh copies the block as it records the call.
            /// </summary>
            private static MaterialPropertyBlock _blitCopyPropertyBlock;

            /// <summary>
            /// RTHandle for common rendering operations.
            /// </summary>
            private RTHandle _commonRenderingTextureHandle;

            /// <summary>
            /// RTHandle for temporary rendering operations.
            /// </summary>
            private RTHandle _commonTemporaryTextureHandle;

            /// <summary>
            /// RTHandle for mask rendering.
            /// </summary>
            private RTHandle _maskTextureHandle;

#if !UNITY_6000_0_OR_NEWER
            /// <summary>
            /// Reused pass data for the compatibility (non-render-graph) <see cref="Execute"/> path, so a new
            /// PassData is not allocated every frame. In the render-graph path the graph owns the instance.
            /// </summary>
            private PassData _passData;
#endif

            /// <summary>
            /// Camera data container that works across Unity versions.
            /// </summary>
            public struct CubismPassCameraData
            {
                /// <summary>Camera used for this render pass.</summary>
                public Camera Camera;
                /// <summary>World space position of the camera.</summary>
                public Vector3 WorldSpaceCameraPos;
                /// <summary>Whether this is a scene view camera.</summary>
                public bool IsSceneViewCamera;
            }

            /// <summary>
            /// This class stores the data needed by the render pass.
            /// It is passed as a parameter to the delegate function that executes the render pass.
            /// </summary>
            public class PassData
            {
                /// <summary>
                /// Array of render controllers to be rendered.
                /// </summary>
                public CubismRenderController[] RenderControllers;

                /// <summary>
                /// Array of render controller groups.
                /// </summary>
                public CubismRenderControllerGroup.RenderControllerGroupData[] RenderControllerGroupDaraArray;

                /// <summary>
                /// Camera data for the current rendering pass.
                /// </summary>
                public CubismPassCameraData CameraData;

#if UNITY_6000_0_OR_NEWER
                /// <summary>
                /// Resource data for the current rendering pass.
                /// </summary>
                public UniversalResourceData ResourceData;
#endif

                /// <summary>
                /// Full-screen texture for high-precision / offscreen masks (redrawn per draw object,
                /// not packed into the atlas).
                /// </summary>
#if UNITY_6000_0_OR_NEWER
                public TextureHandle HighPrecisionMaskTextureHandle;
#else
                public RTHandle HighPrecisionMaskTextureHandle;
#endif

                /// <summary>
                /// Clip group whose masks currently occupy the shared high-precision mask texture. Lets
                /// consecutive high-precision drawables that share a mask set skip re-rendering it. Reset
                /// per pass and cleared whenever the texture is overwritten (offscreen masks, different set).
                /// </summary>
                internal CubismClippingManager.CubismClippingContext ActiveHighPrecisionMaskContext;

                /// <summary>
                /// Texture handle for the camera render target.
                /// </summary>
#if UNITY_6000_0_OR_NEWER
                public TextureHandle CameraTextureHandle;
#else
                public RTHandle CameraTextureHandle;
#endif

                /// <summary>
                /// Pixel size of the camera-sized descriptor that the intermediate / mask textures are allocated from.
                /// NOTE: Taken from the descriptor rather than a texture handle so it stays valid even when URP
                /// renders straight to the backBuffer and a color handle does not resolve to a RenderTexture.
                /// </summary>
                public Vector2 CameraTextureSize;

                /// <summary>
                /// Texture handle for the camera depth buffer.
                /// </summary>
#if UNITY_6000_0_OR_NEWER
                public TextureHandle CameraDepthTextureHandle;
#else
                public RTHandle CameraDepthTextureHandle;
#endif

                /// <summary>
                /// Texture handle for common rendering operations.
                /// </summary>
#if UNITY_6000_0_OR_NEWER
                public TextureHandle CommonRenderingTextureHandle;
#else
                public RTHandle CommonRenderingTextureHandle;
#endif

                /// <summary>
                /// Texture handle for temporary rendering operations.
                /// </summary>
#if UNITY_6000_0_OR_NEWER
                public TextureHandle CommonTemporaryTextureHandle;
#else
                public RTHandle CommonTemporaryTextureHandle;
#endif

                /// <summary>
                /// When true, consecutive drawables sharing the same material state are combined into a single DrawMesh.
                /// </summary>
                public bool EnableBatching;

                /// <summary>
                /// When true, render controller groups that need no intermediate RenderTexture are drawn directly
                /// to the camera color target.
                /// </summary>
                public bool EnableDirectRendering;

                /// <summary>
                /// True while <see cref="CubismRenderPass.DrawObjects"/> is drawing a direct group (one that
                /// renders straight to the camera color target and is never blitted back from the common RT).
                /// Updated per group so the per-drawable / batch fallback paths can keep direct draws on the
                /// camera target instead of diverting them to the common RT, which a direct group never
                /// composites back to the camera.
                /// </summary>
                public bool CurrentGroupRendersDirect;

                /// <summary>
                /// Minimum number of consecutive batchable drawables required to trigger a combined DrawMesh.
                /// </summary>
                public int BatchThreshold;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                /// <summary>
                /// When true, restores the pre-optimization full-screen clear of every pooled offscreen
                /// render texture at the start of the draw call. Measurement aid only.
                /// </summary>
                public bool EnableOffscreenPreClear;
#endif

                /// <summary>
                /// <see cref="CommonRenderingTexture"/>'s backing field.
                /// </summary>
                private RenderTexture _commonRenderingTexture;

                /// <summary>
                /// Texture that common rendering texture work has to use: the common rendering texture, or
                /// the camera color target when it is unavailable, or null when neither resolves.
                /// The common rendering texture is only allocated when at least one render controller group
                /// composites through it, and the handle can stop resolving to a RenderTexture when the
                /// scene changes after the pass was set up (model swap, released handles).
                /// Resolving a handle is a lookup through the render graph's resource registry and the
                /// per-draw-object paths read this for every draw object, so it is resolved once per pass by
                /// <see cref="ResolveTextureHandles"/> and read from here afterwards. Only valid while the
                /// pass is being executed, which is the only phase that draws.
                /// </summary>
                public RenderTexture CommonRenderingTexture
                {
                    get
                    {
                        return _commonRenderingTexture;
                    }
                }

                /// <summary>
                /// Resolves the texture handles the draw paths read per draw object, which also refreshes a
                /// PassData instance that the render graph pooled from an earlier pass.
                /// Call only from inside the pass execution: a handle resolves through the render graph's
                /// resource registry, which is set for the duration of the execution and throws when a handle
                /// is cast while the graph is only being recorded.
                /// </summary>
                internal void ResolveTextureHandles()
                {
                    var commonRenderingTexture = (RenderTexture)CommonRenderingTextureHandle;

                    _commonRenderingTexture = commonRenderingTexture
                        ? commonRenderingTexture
                        : (RenderTexture)CameraTextureHandle;
                }
            }

            /// <summary>
            /// Sets up and fills the renderer group.
            /// </summary>
            /// <param name="targetIndex">Target index in the sorted renderer groups array.</param>
            /// <param name="target">Added target renderer group.</param>
            /// <param name="source">Source render controller group.</param>
            /// <param name="cameraPos">Position of the camera</param>
            /// <param name="data">Pass data containing render controller groups and camera data</param>
            private static void SetUpRendererGroup(int targetIndex, RendererGroupData target, CubismRenderControllerGroup.RenderControllerGroupData source, Vector3 cameraPos, PassData data)
            {
                // A group renders directly to the camera target only when the feature is enabled and
                // every controller in the group qualifies (no background-reading blend / no offscreen).
                var isDirectGroup = data.EnableDirectRendering;
                for (var checkIndex = 0; checkIndex < source.Controllers.Length; checkIndex++)
                {
                    var checkController = source.Controllers[checkIndex];
                    if (checkController && !checkController.CanRenderDirectly)
                    {
                        isDirectGroup = false;
                        break;
                    }
                }
                target.IsDirectGroup = isDirectGroup;

                // Direct groups draw straight to the camera color target. The rest use the common RT.
                var groupFrameBuffer = isDirectGroup
                    ? (RenderTexture)data.CameraTextureHandle
                    : data.CommonRenderingTexture;

                var previousCount = 0;
                for (var renderControllerIndex = 0; renderControllerIndex < source.Controllers.Length; renderControllerIndex++)
                {
                    var controller = source.Controllers[renderControllerIndex];

                    controller.CurrentFrameBuffer = groupFrameBuffer;

                    if (!controller || controller.Renderers == null)
                    {
                        continue;
                    }

                    // Copy the renderers from the controller to the sorted array.
                    for (var rendererIndex = 0; rendererIndex < controller.Renderers.Length; rendererIndex++)
                    {
                        target.Renderers[previousCount + rendererIndex] = controller.Renderers[rendererIndex];
                        target.Renderers[previousCount + rendererIndex].IsLastDrawObjectInModel = false;
                    }

                    // Update the previous count for the next controller
                    previousCount += controller.Renderers.Length;
                }

                // Sort the renderers by their sorting order.
                SortBySortingOrder(target.Renderers, cameraPos, data.CameraData.Camera.transform.forward);

                // Assign the filled target to the sorted renderer groups array.
                _sortedRendererGroupDataArray[targetIndex] = target;
            }

            /// <summary>
            /// Destroys the batch meshes held by every pooled renderer group, then clears the array.
            /// Call before dropping references to <see cref="_sortedRendererGroupDataArray"/> entries,
            /// otherwise the HideAndDontSave batch meshes leak (they survive GC and scene unload).
            /// </summary>
            internal static void DisposeRendererGroupPools()
            {
                if (_sortedRendererGroupDataArray == null)
                {
                    return;
                }

                for (var i = 0; i < _sortedRendererGroupDataArray.Length; i++)
                {
                    _sortedRendererGroupDataArray[i].BatchPool?.Dispose();
                }

                // Reset to an empty array (not null) so callers that read it without a null check
                // (e.g. the DrawObjects group loop, before SortingRendererGroups repopulates it) stay safe.
                _sortedRendererGroupDataArray = Array.Empty<RendererGroupData>();
            }

            /// <summary>
            /// Creates the full-screen quad and the materials the blit paths draw with, if they do not exist yet.
            /// </summary>
            private static void EnsureBlitResources()
            {
                if (!_blitRenderTextureMesh)
                {
                    _blitRenderTextureMesh = new Mesh
                    {
                        vertices = CubismRenderer.OffscreenVertices,
                        uv = CubismRenderer.OffscreenUVs,
                        triangles = CubismRenderer.OffscreenTriangle
                    };

                    _blitRenderTextureMesh.RecalculateBounds();
                }

                if (!_blitRenderTextureMaterial)
                {
                    _blitRenderTextureMaterial = new Material(CubismBuiltinMaterials.UnlitBlit);
                }

                if (!_blitCopyMaterial)
                {
                    _blitCopyMaterial = new Material(CubismBuiltinMaterials.UnlitBlit);

                    // A copy must land whatever the destination's depth holds, so the comparison always passes.
                    _blitCopyMaterial.SetInt(CubismShaderVariables.ReversedZ, (int)CompareFunction.Always);

                    // Replace the destination rather than compositing onto it. Without this the copy would
                    // have to clear the destination first, and that clear costs a full screen of fragment
                    // work however small the copied region is - which is the cost the scissor is there to avoid.
                    _blitCopyMaterial.SetInt(CubismShaderVariables.SourceColorBlend, (int)BlendMode.One);
                    _blitCopyMaterial.SetInt(CubismShaderVariables.DestinationColorBlend, (int)BlendMode.Zero);
                    _blitCopyMaterial.SetInt(CubismShaderVariables.SourceAlphaBlend, (int)BlendMode.One);
                    _blitCopyMaterial.SetInt(CubismShaderVariables.DestinationAlphaBlend, (int)BlendMode.Zero);
                }

                _blitCopyPropertyBlock ??= new MaterialPropertyBlock();
            }

            /// <summary>
            /// Copies <paramref name="source"/> into <paramref name="destination"/>, optionally limited to
            /// <paramref name="rect"/>.
            /// </summary>
            /// <param name="buffer">Command buffer to record into.</param>
            /// <param name="source">Texture to copy from.</param>
            /// <param name="destination">Render texture to copy into.</param>
            /// <param name="rect">Region to limit the copy to, in bottom-left origin pixels.</param>
            /// <param name="limitToRect">False copies the whole destination and ignores <paramref name="rect"/>.</param>
            /// <remarks>
            /// Replaces <see cref="CommandBuffer.Blit(RenderTargetIdentifier, RenderTargetIdentifier)"/>, which
            /// cannot be limited to a region: it binds the destination itself, and that render target change
            /// drops a scissor rectangle set beforehand, so the copy covers the whole destination however small
            /// the region of interest is. Binding the destination first and setting the scissor afterwards is
            /// what keeps it, and is the reason this method exists.
            ///
            /// The destination is not cleared: the copy material replaces what it writes over, so only the
            /// region matters. Clearing first would undo the saving, because a clear issued inside the pass
            /// costs a full screen of fragment work whether or not a scissor rectangle is set.
            /// </remarks>
            internal static void CopyRegionToTarget(CommandBuffer buffer, Texture source, RenderTexture destination, RectInt rect, bool limitToRect)
            {
                if (buffer == null
                    || !source
                    || !destination)
                {
                    return;
                }

                EnsureBlitResources();

                // Bind first. A scissor set before this point would not survive the change of render target.
                buffer.SetRenderTarget(destination);

                var scissored = limitToRect
                                && rect.width > 0
                                && rect.height > 0;

                if (scissored)
                {
                    buffer.EnableScissorRect(new Rect(rect.x, rect.y, rect.width, rect.height));
                }

                _blitCopyPropertyBlock.SetTexture(CubismShaderVariables.MainTexture, source);
                buffer.DrawMesh(_blitRenderTextureMesh, Matrix4x4.identity, _blitCopyMaterial, 0, 0, _blitCopyPropertyBlock);

                if (scissored)
                {
                    buffer.DisableScissorRect();
                }
            }

            /// <summary>
            /// Destroys the static blit mesh/material lazily created in ExecutePass. Like the batch
            /// meshes, these are not reclaimed by GC or scene unload, so they must be released explicitly.
            /// </summary>
            internal static void DisposeBlitResources()
            {
                if (_blitRenderTextureMesh)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(_blitRenderTextureMesh);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(_blitRenderTextureMesh);
                    }

                    _blitRenderTextureMesh = null;
                }

                if (_blitRenderTextureMaterial)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(_blitRenderTextureMaterial);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(_blitRenderTextureMaterial);
                    }

                    _blitRenderTextureMaterial = null;
                }

                if (_blitCopyMaterial)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(_blitCopyMaterial);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(_blitCopyMaterial);
                    }

                    _blitCopyMaterial = null;
                }

                _blitCopyPropertyBlock = null;
            }

            /// <summary>
            /// Sorts the renderer groups based on their sorting order.
            /// </summary>
            /// <param name="data">Pass data containing render controller groups.</param>
            private static void SortingRendererGroups(PassData data)
            {
                var didChangeSortingRenderControllerGroup = CubismRenderControllerGroup.GetInstance().DidChangeSortingRenderControllerGroup;

                // Detect an EnableDirectRendering change (e.g. a runtime inspector toggle) since the last sort.
                // IsDirectGroup / CurrentFrameBuffer are only (re)computed in SetUpRendererGroup, which runs for a
                // group only when its sort order changes. Folding this into didChangeSortingOrder below forces
                // every group through SetUpRendererGroup on the frame the value changes, so the toggle takes
                // effect immediately instead of waiting for an unrelated sort change.
                var didChangeDirectRendering = _previousEnableDirectRendering != data.EnableDirectRendering;
                _previousEnableDirectRendering = data.EnableDirectRendering;

                if (data.RenderControllerGroupDaraArray == null
                    || data.RenderControllerGroupDaraArray.Length < 1)
                {
                    return;
                }

                // Iterate through each render controller group.
                for (var groupsIndex = 0; groupsIndex < data.RenderControllerGroupDaraArray.Length; groupsIndex++)
                {
                    var group = data.RenderControllerGroupDaraArray[groupsIndex];
                    var rendererCount = 0;
                    // Seeded with the EnableDirectRendering change so a toggle re-runs SetUpRendererGroup for
                    // this group (refreshing IsDirectGroup / CurrentFrameBuffer) even when its own sort order
                    // is unchanged.
                    var didChangeSortingOrder = didChangeDirectRendering;

                    for (var i = 0; i < group.Controllers.Length; i++)
                    {
                        var controller = group.Controllers[i];

                        // Skip if controller is null or has no renderers
                        if (!controller || controller.Renderers == null)
                        {
                            continue;
                        }

                        // Update sorting state from camera position.
                        controller.UpdateDidChangeSortingFromZ(data.CameraData.WorldSpaceCameraPos);

                        // Update offscreen render order if it has changed.
                        if (controller.DidChangeDrawableRenderOrder)
                        {
                            for (var offscreenIndex = 0; offscreenIndex < controller.OffscreenRenderers?.Length; offscreenIndex++)
                            {
                                var offscreenRenderer = controller.OffscreenRenderers[offscreenIndex];
                                offscreenRenderer.SetDrawObjectRenderOrder(controller.Model.AllDrawObjectsRenderOrder[controller.DrawableRenderers.Length + offscreenRenderer.Offscreen.UnmanagedIndex]);
                            }
                        }

                        // Check if any controller has changed its drawable render order.
                        didChangeSortingOrder |=
                            controller.DidChangeSorting
                             || controller.DidChangeDrawableRenderOrder
                             || didChangeSortingRenderControllerGroup;

                        rendererCount += controller.Renderers.Length;
                    }

                    // If nothing changed, no need to sort again.
                    if (!didChangeSortingOrder)
                    {
                        continue;
                    }

                    // Create an empty array if it doesn't exist. Groups that disappeared are dropped by
                    // AlignRendererGroupOrder at the end of this method, which releases only their pools.
                    if (_sortedRendererGroupDataArray == null)
                    {
                        // Destroy the batch meshes of the groups being dropped before losing their
                        // references. HideAndDontSave meshes are not reclaimed by GC or scene unload.
                        DisposeRendererGroupPools();
                        _sortedRendererGroupDataArray = Array.Empty<RendererGroupData>();
                    }

                    var hasGroupFound = false;
                    for (var targetIndex = 0; targetIndex < _sortedRendererGroupDataArray.Length; targetIndex++)
                    {
                        var target = _sortedRendererGroupDataArray[targetIndex];

                        // This Renderers == null guard also skips not-yet-initialized slots. Entries are
                        // only created via the new-group path below, which sets Renderers/BatchPool/Batcher
                        // together, so Renderers != null implies BatchPool/Batcher != null. Reusing the
                        // entry without re-initializing them is safe.
                        if (target.Renderers == null
                            || target.SortingIndex != group.SortingGroupIndex)
                        {
                            continue;
                        }

                        if (target.Renderers.Length != rendererCount)
                        {
                            // Resize the renderers array to fit all renderers in the group.
                            Array.Resize(ref target.Renderers, rendererCount);
                        }

                        var hasControllersNull = false;
                        for (var controllerIndex = 0; controllerIndex < target.RenderControllers.Length; controllerIndex++)
                        {
                            if (target.RenderControllers[controllerIndex])
                            {
                                continue;
                            }

                            hasControllersNull = true;
                            break;
                        }

                        if (hasControllersNull
                            || didChangeSortingRenderControllerGroup
                            || target.RenderControllers.Length != data.RenderControllerGroupDaraArray[groupsIndex].Controllers.Length)
                        {
                            target.RenderControllers = data.RenderControllerGroupDaraArray[groupsIndex].Controllers;
                        }

                        // Set up and fill the renderer group. 'target' is a struct copy of an
                        // already-initialized entry. SetUpRendererGroup only fills Renderers and writes it
                        // back, preserving BatchPool/Batcher, so no re-initialization is needed here.
                        SetUpRendererGroup(targetIndex, target, group, data.CameraData.WorldSpaceCameraPos, data);

                        hasGroupFound = true;
                        break;
                    }

                    // Initialize the renderer group if necessary
                    if (hasGroupFound)
                    {
                        continue;
                    }

                    // Resize the sorted renderer groups array to add a new group. This is the only path
                    // that appends an entry, and it always initializes BatchPool/Batcher below. Together
                    // with the reuse path above (which preserves them), every array entry is guaranteed to
                    // have a non-null BatchPool/Batcher.
                    Array.Resize(ref _sortedRendererGroupDataArray, _sortedRendererGroupDataArray.Length + 1);
                    var newPool = new CubismBatchPool();
                    var newRendererGroup = new RendererGroupData
                    {
                        SortingIndex = group.SortingGroupIndex,
                        RenderControllers = data.RenderControllerGroupDaraArray[groupsIndex].Controllers,
                        Renderers = new CubismRenderer[rendererCount],
                        BatchPool = newPool,
                        Batcher = new CubismDrawableBatcher(newPool)
                    };

                    // Set up and fill the renderer group.
                    SetUpRendererGroup(_sortedRendererGroupDataArray.Length - 1, newRendererGroup, group, data.CameraData.WorldSpaceCameraPos,data);
                }

                // New groups are appended above, so the array order no longer matches the group order.
                AlignRendererGroupOrder(data.RenderControllerGroupDaraArray);
            }

            /// <summary>
            /// Reorders <see cref="_sortedRendererGroupDataArray"/> to match the given render controller
            /// groups and drops the entries whose group no longer exists.
            /// </summary>
            /// <remarks>
            /// The draw loop renders the entries in array order, so this array - not the sorting index it
            /// stores - is what decides which model is drawn in front. Entries are appended as their group
            /// is first seen, which puts a group created at runtime (an inspector edit of the grouped
            /// sorting index) last no matter how small its index is, so the order is restored here.
            /// Entries are moved rather than rebuilt because each one owns a batch pool and a batcher
            /// holding meshes that would have to be recreated.
            /// </remarks>
            /// <param name="groups">Render controller groups, sorted by their sorting index.</param>
            private static void AlignRendererGroupOrder(CubismRenderControllerGroup.RenderControllerGroupData[] groups)
            {
                if (_sortedRendererGroupDataArray == null
                    || groups == null)
                {
                    return;
                }

                // Number of leading entries already matching the group order.
                var alignedCount = 0;

                for (var groupIndex = 0; groupIndex < groups.Length; groupIndex++)
                {
                    for (var targetIndex = alignedCount; targetIndex < _sortedRendererGroupDataArray.Length; targetIndex++)
                    {
                        if (_sortedRendererGroupDataArray[targetIndex].SortingIndex != groups[groupIndex].SortingGroupIndex)
                        {
                            continue;
                        }

                        // Swap the entry into the slot the group order asks for. The entry currently there is
                        // not yet aligned, so moving it back is harmless.
                        var alignedGroup = _sortedRendererGroupDataArray[alignedCount];
                        _sortedRendererGroupDataArray[alignedCount] = _sortedRendererGroupDataArray[targetIndex];
                        _sortedRendererGroupDataArray[targetIndex] = alignedGroup;

                        alignedCount++;
                        break;
                    }
                }

                // Everything past the aligned entries belongs to a group that is gone.
                if (alignedCount >= _sortedRendererGroupDataArray.Length)
                {
                    return;
                }

                // Destroy the batch meshes of the groups being dropped before losing their references.
                // HideAndDontSave meshes are not reclaimed by GC or scene unload.
                for (var targetIndex = alignedCount; targetIndex < _sortedRendererGroupDataArray.Length; targetIndex++)
                {
                    _sortedRendererGroupDataArray[targetIndex].BatchPool?.Dispose();
                }

                Array.Resize(ref _sortedRendererGroupDataArray, alignedCount);
            }

            /// <summary>
            /// Sorts the renderers by their sorting order.
            /// Calculates the distance to camera for each renderer and sorts them accordingly.
            /// </summary>
            /// <param name="renderers">Array of renderers to sort</param>
            /// <param name="cameraPosition">Position of the camera</param>
            /// <param name="cameraForward">Forward direction of the camera (normalized vector)</param>
            private static void SortBySortingOrder(CubismRenderer[] renderers, Vector3 cameraPosition, Vector3 cameraForward)
            {
                for (var index = 0; index < renderers.Length; index++)
                {
                    var renderer = renderers[index];

                    if (!renderer || !renderer.MeshRenderer)
                    {
                        continue;
                    }

                    renderer.CalculateDistanceToCamera(cameraPosition, cameraForward);

                    // Capture the native sorting order once so the comparator can read it from a field
                    // instead of calling the getter on every one of its O(n log n) comparisons.
                    renderer.CachedSortingOrder = renderer.MeshRenderer.sortingOrder;
                }

                Array.Sort(renderers, CompareBySortingOrder);
            }

            /// <summary>
            /// Compares two renderers by their sorting order.
            /// </summary>
            /// <param name="a">First renderer to compare</param>
            /// <param name="b">Second renderer to compare</param>
            /// <returns>Negative value if a should be rendered before b, positive if after, zero if equal.</returns>
            private static int CompareBySortingOrder(CubismRenderer a, CubismRenderer b)
            {
                // Fail-safe check.
                // NOTE: Use `is null` instead of `==` for the object references to avoid triggering Unity's
                // native null check, which can be expensive when Array.Sort calls this O(n log n) times. The
                // comparison body only reads managed members (CachedSortingOrder, SortingMode, DistanceToCamera,
                // RenderController.DepthOffset), so a managed check is sufficient. The MeshRenderer null checks
                // keep the native check because they read a native component.
                if (a is null || b is null
                       || !a.MeshRenderer || !b.MeshRenderer
                       || a.RenderController is null || b.RenderController is null)
                {
                    return 0;
                }

                var result = a.CachedSortingOrder - b.CachedSortingOrder;

                if (result != 0
                    || !(a.SortingMode.SortByDepth() && b.SortingMode.SortByDepth()))
                {
                    return result;
                }

                // If sorting order is the same, compare by local Z position.
                var sortValue = (b.DistanceToCamera / b.RenderController.DepthOffset) - (a.DistanceToCamera / a.RenderController.DepthOffset);
                result = sortValue >= 0.0f
                    ? Mathf.CeilToInt(sortValue)
                    : Mathf.FloorToInt(sortValue);

                return result;
            }

            /// <summary>
            /// Computes each renderer's <see cref="CubismRenderer.SkipRendering"/> flag from managed scene
            /// state (active / enabled state, displayed opacity, and offscreen-part visibility).
            /// The result is used to skip draw calls for invisible renderers, improving performance and avoiding redundant
            /// offscreen / mask work for fully hidden models.
            /// </summary>
            /// <param name="data">Pass data containing the render controllers.</param>
            private static void PrecomputeSkipRendering(PassData data)
            {
                if (data.RenderControllers == null)
                {
                    return;
                }

                // Reset skip rendering flag for each renderer.
                for (var controllerIndex = 0; controllerIndex < data.RenderControllers.Length; controllerIndex++)
                {
                    var controller = data.RenderControllers[controllerIndex];

                    if (!controller || controller.Renderers == null)
                    {
                        continue;
                    }

                    for (var rendererIndex = 0; rendererIndex < controller.Renderers.Length; rendererIndex++)
                    {
                        var renderer = controller.Renderers[rendererIndex];

                        // NOTE: Use `is not null` instead of `!=` to avoid triggering Unity's native null check, which can be expensive.
                        if (renderer is not null)
                        {
                            renderer.SkipRendering = false;
                        }
                    }
                }

                // Check and set skip rendering flag for each renderer.
                for (var controllerIndex = 0; controllerIndex < data.RenderControllers.Length; controllerIndex++)
                {
                    var controller = data.RenderControllers[controllerIndex];

                    if (!controller || controller.Renderers == null)
                    {
                        continue;
                    }

                    // The controller's own active/enabled state is identical for every renderer it owns
                    // (renderer.RenderController == controller here, and controller is non-null), so read
                    // those native properties once per controller instead of once per renderer.
                    var controllerInactive = !controller.gameObject.activeSelf
                                             || !controller.enabled;

                    for (var rendererIndex = 0; rendererIndex < controller.Renderers.Length; rendererIndex++)
                    {
                        var renderer = controller.Renderers[rendererIndex];

                        if (!renderer
                            || renderer.SkipRendering)
                        {
                            continue;
                        }

                        // A renderer without a controller cannot be drawn, so exclude it here instead of leaving
                        // SkipRendering unset and letting it reach the draw loop (which dereferences RenderController).
                        if (!renderer.RenderController)
                        {
                            renderer.SkipRendering = true;
                            continue;
                        }

                        // Read the MeshRenderer once: its getter runs TryInitializeMeshRenderer (another
                        // native null check) on every access, so caching it halves the getter calls per
                        // renderer compared with dereferencing renderer.MeshRenderer twice below.
                        var meshRenderer = renderer.MeshRenderer;

                        renderer.SkipRendering |= controllerInactive
                                     || !renderer.gameObject.activeSelf
                                     || !meshRenderer
                                     || !meshRenderer.enabled;

                        switch (renderer.DrawObjectType)
                        {
                            case CubismModelTypes.DrawObjectType.Drawable:
                                renderer.SkipRendering |= renderer.DisplayedOpacity <= 0.0f;
                                break;
                            case CubismModelTypes.DrawObjectType.Offscreen:
                                renderer.SkipRendering |= renderer.DisplayedOffscreenOpacity <= 0.0f;

                                if (!renderer.SkipRendering)
                                {
                                    break;
                                }

                                // Skip rendering for all child drawables and offscreens of the offscreen part.
                                renderer.SkipDescendantRenderers();
                                break;
                            default:
                                renderer.SkipRendering = true;
                                break;
                        }
                    }
                }
            }

            /// <summary>
            /// Finalizes the execution-phase render state over the sorted group array: skips empty offscreens,
            /// computes each offscreen's drawn region, and marks the last drawn (non-skipped) renderer of each
            /// controller so the draw loop knows when to flush and submit a model's offscreen. Depends on the
            /// sort order and the skip flags, so call it after <see cref="SortingRendererGroups"/> and
            /// <see cref="PrecomputeSkipRendering"/>.
            /// </summary>
            /// <param name="data">Pass data containing the camera used to project the offscreen regions.</param>
            private static void MarkLastDrawObjectInModel(PassData data)
            {
                if (_sortedRendererGroupDataArray == null)
                {
                    return;
                }

                SkipEmptyOffscreens();

                UpdateOffscreenScreenRects(data);

                for (var i= 0; i < _sortedRendererGroupDataArray.Length; i++)
                {
                    var target = _sortedRendererGroupDataArray[i];

                    // Reset the last draw object flag for each renderer.
                    for (var rendererIndex = 0; rendererIndex < target.Renderers.Length; rendererIndex++)
                    {
                        var targetRenderer = target.Renderers[rendererIndex];
                        targetRenderer.IsLastDrawObjectInModel = false;
                    }

                    // Reset the per-controller "already marked" flags for this group.
                    for (var controllerIndex = 0; controllerIndex < target.RenderControllers.Length; controllerIndex++)
                    {
                        var controller = target.RenderControllers[controllerIndex];

                        // NOTE: Use `is not null` instead of `!=` to avoid triggering Unity's native null check, which can be expensive.
                        if (controller is not null)
                        {
                            controller.DidMarkLastDrawObject = false;
                        }
                    }

                    // Single reverse pass over the sorted renderers: the first non-skipped renderer met for a
                    // controller (i.e. the last in draw order) is its last draw object. This is O(renderers)
                    // instead of O(controllers * renderers) — the previous per-controller full reverse scan.
                    for (var rendererIndex = target.Renderers.Length - 1; rendererIndex >= 0; rendererIndex--)
                    {
                        var targetRenderer = target.Renderers[rendererIndex];

                        if (!targetRenderer
                            || targetRenderer.SkipRendering)
                        {
                            continue;
                        }

                        var controller = targetRenderer.RenderController;

                        // Match the previous behavior: skip disabled/inactive controllers, and mark each
                        // controller only once (its last drawn renderer).
                        if (!controller
                            || controller.DidMarkLastDrawObject
                            || !controller.enabled
                            || !controller.gameObject.activeSelf)
                        {
                            continue;
                        }

                        targetRenderer.IsLastDrawObjectInModel = true;
                        controller.DidMarkLastDrawObject = true;
                    }
                }
            }

            /// <summary>
            /// Skips the offscreens that hold no content this frame, along with everything below them.
            /// </summary>
            /// <remarks>
            /// Runs after every per-renderer skip flag is final, because what makes an offscreen empty is
            /// its drawables being skipped: a drawable is only known to be invisible once its own flag has
            /// been computed, and drawables come after their offscreen in draw order.
            ///
            /// A pass of its own rather than part of the loop above for the same reason. The order within
            /// this pass does not matter for the result: emptiness is derived from the drawables, so
            /// skipping a nested offscreen never changes whether an outer one is empty. It does decide who
            /// gets credited with a skip - an outer offscreen skipped first takes its nested ones down with
            /// it through the descendant propagation, and those never reach the test.
            /// </remarks>
            private static void SkipEmptyOffscreens()
            {
                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray.Length; groupIndex++)
                {
                    var rendererGroup = _sortedRendererGroupDataArray[groupIndex];

                    for (var rendererIndex = 0; rendererIndex < rendererGroup.Renderers.Length; rendererIndex++)
                    {
                        var renderer = rendererGroup.Renderers[rendererIndex];

                        if (!renderer
                            || renderer.SkipRendering
                            || renderer.DrawObjectType != CubismModelTypes.DrawObjectType.Offscreen
                            || !renderer.RenderController
                            || !renderer.RenderController.EnableEmptyOffscreenSkip)
                        {
                            continue;
                        }

                        // Skipping is only unobservable when an empty composite would have left the
                        // parent's pixels alone.
                        if (!renderer.IsDirectDrawBlendMode
                            && renderer.RenderController.Opacity < 1.0f)
                        {
                            continue;
                        }

                        if (renderer.HasVisibleDescendantDrawable())
                        {
                            continue;
                        }

                        renderer.SkipRendering = true;
                        renderer.SkipDescendantRenderers();
                    }
                }
            }

            /// <summary>
            /// Computes, for every offscreen being drawn, the region of its buffer that receives content
            /// this frame.
            /// </summary>
            /// <param name="data">Pass data containing the camera used for projection.</param>
            /// <remarks>
            /// One pass over the drawables rather than one pass per offscreen: each drawable is projected
            /// exactly once and its region is then added to every offscreen above it in the part chain.
            /// Runs after the skip flags are final, so that invisible drawables neither pay for a
            /// projection nor inflate a region.
            /// </remarks>
            private static void UpdateOffscreenScreenRects(PassData data)
            {
                if (data == null)
                {
                    return;
                }

                var camera = data.CameraData.Camera;

                // The regions are consumed as scissor rectangles on the pooled offscreen textures, the
                // temporary texture and the common rendering texture - all sized by the common RT's
                // camera-sized descriptor. The camera depth target may differ from it, so the common RT
                // is the only reference that keeps the regions in the same pixel space as every consumer.
                var target = data.CommonRenderingTexture;

                // Regions are dropped and left unset when they cannot be computed, which makes every
                // consumer fall back to whole-buffer work.
                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray.Length; groupIndex++)
                {
                    var rendererGroup = _sortedRendererGroupDataArray[groupIndex];

                    for (var rendererIndex = 0; rendererIndex < rendererGroup.Renderers.Length; rendererIndex++)
                    {
                        var renderer = rendererGroup.Renderers[rendererIndex];

                        // NOTE: Use `is not null` instead of `!=` to avoid triggering Unity's native null check, which can be expensive.
                        // This loop walks every renderer each frame, and DrawObjectType / ResetOffscreenScreenRect are managed-only.
                        if (renderer is not null
                            && renderer.DrawObjectType == CubismModelTypes.DrawObjectType.Offscreen)
                        {
                            renderer.ResetOffscreenScreenRect();
                        }
                    }

                    for (var controllerIndex = 0; controllerIndex < rendererGroup.RenderControllers?.Length; controllerIndex++)
                    {
                        rendererGroup.RenderControllers[controllerIndex]?.ResetModelScreenRect();
                    }
                }

                if (camera == null
                    || !target
                    || target.width <= 0
                    || target.height <= 0)
                {
                    return;
                }

                // OffscreenScreenRect / ModelScreenRect are consumed only by the offscreen clear/composite
                // path (TryGetOffscreenScissorRect). When no controller renders an offscreen, nothing reads
                // them, so the whole per-drawable projection below is pure waste and can be skipped. The reset
                // loops above already left every region unset, which is the correct fallback state. The scan
                // walks the same controllers the projection does (the sorted groups) so it never skips a
                // projection that would have run.
                var anyOffscreen = false;
                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray.Length && !anyOffscreen; groupIndex++)
                {
                    var groupControllers = _sortedRendererGroupDataArray[groupIndex].RenderControllers;
                    for (var controllerIndex = 0; controllerIndex < groupControllers?.Length; controllerIndex++)
                    {
                        var controller = groupControllers[controllerIndex];
                        if (controller
                            && controller.OffscreenRenderers != null
                            && controller.OffscreenRenderers.Length > 0)
                        {
                            anyOffscreen = true;
                            break;
                        }
                    }
                }

                if (!anyOffscreen)
                {
                    return;
                }

                // Hoisted: both halves are native properties.
                var viewProjection = CubismMath.GetViewProjection(camera);

                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray.Length; groupIndex++)
                {
                    var rendererGroup = _sortedRendererGroupDataArray[groupIndex];

                    for (var rendererIndex = 0; rendererIndex < rendererGroup.Renderers.Length; rendererIndex++)
                    {
                        var renderer = rendererGroup.Renderers[rendererIndex];

                        if (!renderer
                            || renderer.SkipRendering
                            || renderer.DrawObjectType != CubismModelTypes.DrawObjectType.Drawable)
                        {
                            continue;
                        }

                        var controller = renderer.RenderController;

                        if (!controller)
                        {
                            continue;
                        }

                        // Nothing consumes the regions of a model that opted out, so do not project it.
                        if (!controller.EnableOffscreenScissor)
                        {
                            continue;
                        }

                        // A controller with no offscreens never consumes these regions either (only the
                        // offscreen clear/composite path reads them), so skip its projection in mixed scenes.
                        var offscreenRenderers = controller.OffscreenRenderers;
                        if (offscreenRenderers == null
                            || offscreenRenderers.Length == 0)
                        {
                            continue;
                        }

                        var parts = controller.Model?.Parts;
                        var drawable = renderer.Drawable;

                        if (parts == null
                            || !drawable)
                        {
                            continue;
                        }

                        var rect = renderer.ComputeScreenRect(viewProjection, target.width, target.height);

                        if (rect.width <= 0
                            || rect.height <= 0)
                        {
                            continue;
                        }

                        controller.UnionModelScreenRect(rect);

                        // Add to every offscreen this drawable renders into, walking up the part chain.
                        // ParentPartIndex and UnmanagedParentIndex are unmanaged part indices, which are
                        // not positions in Parts, so every step resolves through the controller's lookup.
                        // The step count is bounded by the part count: a chain cannot visit more parts
                        // than the model has, and the bound also keeps a malformed chain from looping.
                        var partUnmanagedIndex = drawable.ParentPartIndex;

                        for (var step = 0; step < parts.Length && partUnmanagedIndex >= 0; step++)
                        {
                            var part = controller.FindPart(partUnmanagedIndex);

                            if (!part)
                            {
                                break;
                            }

                            if (part.OffscreenIndex != -1)
                            {
                                controller.FindOffscreenRenderer(part.OffscreenIndex)?.UnionOffscreenScreenRect(rect);
                            }

                            partUnmanagedIndex = part.UnmanagedParentIndex;
                        }
                    }
                }
            }

            /// <summary>
            /// Draws the objects using the provided command buffer and pass data.
            /// </summary>
            /// <param name="commandBuffer">Command buffer to record draw commands.</param>
            /// <param name="data">Pass data containing render controllers and camera data.</param>
            /// <param name="copyPerGroup">
            /// When true, each common-RT group composites independently and blits to the camera in sort
            /// order. When false, common-RT groups accumulate and ExecutePass issues a single final blit.
            /// Decided by ExecutePass: forced true when direct and common-RT groups coexist in accumulate
            /// mode, otherwise <see cref="CubismRenderControllerGroup.IsCopiedToCameraTexture"/>.
            /// </param>
            private static void DrawObjects(CommandBuffer commandBuffer, PassData data, bool copyPerGroup)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                // Measurement aid: restore the pre-optimization behavior of clearing every pooled
                // offscreen render texture up front. Redundant in normal use, because SetOffscreen()
                // clears each texture immediately before drawing into it.
                if (data.EnableOffscreenPreClear)
                {
                    CubismOffscreenRenderTextureManager.GetInstance().ClearRenderTextures(commandBuffer);
                }
#endif

                // Mark each model's last drawn object for offscreen flush/submit.
                MarkLastDrawObjectInModel(data);

                // Before drawing any objects, render each masked model's whole clipping-mask atlas into its own buffers (once per frame).
                // Also computes this frame's clipping contexts.
                if (data.RenderControllers != null)
                {
                    // The high-precision mask buffer shares the camera-sized descriptor, so use its stored pixel size.
                    var highPrecisionMaskBufferSize = data.CameraTextureSize;

                    for (var controllerIndex = 0; controllerIndex < data.RenderControllers.Length; controllerIndex++)
                    {
                        data.RenderControllers[controllerIndex]?.PrepareMaskAtlas(commandBuffer, highPrecisionMaskBufferSize);
                    }
                }

                // In the accumulate mode the common RT color is cleared only once before the first common-RT group.
                var commonRtCleared = false;

                // Common RT as it is this frame. A common-RT group draws to the camera color target instead
                // when it does not resolve, which also means the clears and the blits back below - both of
                // which only ever make sense against the dedicated texture - have to be left out.
                var commonRenderingTexture = (RenderTexture)data.CommonRenderingTextureHandle;

                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray?.Length; groupIndex++)
                {
                    var rendererGroup = _sortedRendererGroupDataArray[groupIndex];

                    if (rendererGroup.Renderers == null)
                    {
                        continue;
                    }

                    var isDirectGroup = rendererGroup.IsDirectGroup;

                    // Expose the group kind to the per-drawable / batch fallback paths (DrawDrawable / Flush)
                    // so a resolution-check miss in a direct group falls back to the camera target rather than
                    // the common RT, which a direct group never blits back to the camera.
                    data.CurrentGroupRendersDirect = isDirectGroup;

                    // Re-point every controller in this group at the group's render target each frame.
                    // Syncing it here keeps CurrentFrameBuffer consistent with the group render target on every frame.
                    // A common-RT group falls back to the camera color target when the common RT is
                    // unavailable, so the group still has a valid destination to draw into.
                    var frameBuffer = isDirectGroup
                        ? (RenderTexture)data.CameraTextureHandle
                        : data.CommonRenderingTexture;
                    for (var controllerIndex = 0; controllerIndex < rendererGroup.RenderControllers?.Length; controllerIndex++)
                    {
                        var controller = rendererGroup.RenderControllers[controllerIndex];

                        // NOTE: Use `is not null` instead of `!=` to avoid triggering Unity's native null check, which can be expensive.
                        if (controller is not null)
                        {
                            controller.CurrentFrameBuffer = frameBuffer;
                        }
                    }

                    // Set up the render target for this group.
                    // A common-RT group with no common RT is bound the same way a direct group is, which
                    // includes not clearing: the camera target already holds the background, the scene and
                    // whatever earlier groups drew, and it is never blitted back over.
                    // The camera handle is bound rather than the RenderTexture it resolves to, because it
                    // stays a valid render target even when it resolves to none (rendering to the backBuffer).
                    if (isDirectGroup || !commonRenderingTexture)
                    {
                        // Draw straight to the camera color target, preserving existing color (background)
                        // and depth (other Unity objects) so the model occludes / is occluded correctly.
                        commandBuffer.SetRenderTarget(data.CameraTextureHandle, data.CameraDepthTextureHandle);
                    }
                    else
                    {
                        commandBuffer.SetRenderTarget(commonRenderingTexture, data.CameraDepthTextureHandle);

                        if (copyPerGroup)
                        {
                            // Each group composites independently. Clear the common RT COLOR only.
                            // Do NOT clear depth: the bound depth is the shared camera depth, which holds
                            // the scene's opaque depths that Cubism drawables (ZTest LEqual / ZWrite Off)
                            // test against. Clearing it here would wipe those depths and break occlusion
                            // against other Unity objects for every group after the first (matches the
                            // pre-existing behavior, which never cleared the camera depth).
                            commandBuffer.ClearRenderTarget(false, true, Color.clear);
                        }
                        else if (!commonRtCleared)
                        {
                            // Accumulate mode: clear the common RT color once before the first common-RT
                            // group. Subsequent groups accumulate. Depth is preserved (see above).
                            commandBuffer.ClearRenderTarget(false, true, Color.clear);
                            commonRtCleared = true;
                        }
                    }

                    // Camera forward is constant across this group's renderers. Read the native transform once.
                    var cameraForward = data.CameraData.Camera.transform.forward;

#if UNITY_EDITOR
                    // Calculate distance to camera for each renderer for CubismRenderInterceptorsManager events.
                    // HACK: Unity may mix scene camera and game camera information, so recalculate the distance each time in the Editor.
                    for (var rendererIndex = 0; rendererIndex < rendererGroup.Renderers.Length; rendererIndex++)
                    {
                        var target = rendererGroup.Renderers[rendererIndex];

                        if (!target)
                        {
                            continue;
                        }

                        target.CalculateDistanceToCamera(data.CameraData.WorldSpaceCameraPos, cameraForward);
                    }
#endif

                    // Interceptor coexistence: drawables that would trigger interceptor injection get
                    // batched alone (flushed before/after their events) so injected DrawRenderer calls
                    // land in the correct order. The rest are batched normally.
                    var batchingEnabled = data.EnableBatching;
                    var interceptorsManager = CubismRenderingInterceptorsManager.GetInstance();
                    var hasInterceptors = interceptorsManager.Interceptors.Length > 0;

                    if (hasInterceptors)
                    {
                        // The interceptors' sort keys are public fields, so the manager has to poll them.
                        // Ask for that poll once here rather than letting it run on every per-drawable
                        // rendering event, which would make it cost interceptors times drawables.
                        interceptorsManager.RequestSortDataRefresh();
                    }

                    // SortingGroup-scoped batcher: drawables from any controller in this group can be merged.
                    var groupBatcher = rendererGroup.Batcher;
                    if (groupBatcher != null)
                    {
                        groupBatcher.IsBatchingEnabled = batchingEnabled;
                        groupBatcher.BatchThreshold = data.BatchThreshold;
                    }

                    // Reset the per-frame mesh pool index so meshes can be reused this frame.
                    rendererGroup.BatchPool?.Reset();

                    for (var rendererIndex = 0; rendererIndex < rendererGroup.Renderers.Length; rendererIndex++)
                    {
                        var renderer = rendererGroup.Renderers[rendererIndex];

                        // Skip if renderer is null or inactive
                        if (!renderer
                            || renderer.SkipRendering)
                        {
                            continue;
                        }

                        // The per-drawable event args and the pre/post callbacks are only observable when an
                        // interceptor is registered. Building the args reads several native properties per
                        // drawable, so skip it and the callbacks entirely when there are no interceptors.
                        var willInject = false;
                        CubismRenderedEventArgs args = default;

                        if (hasInterceptors)
                        {
                            var previousRenderer = rendererIndex > 0
                                ? rendererGroup.Renderers[rendererIndex - 1]
                                : null;
                            var nextRenderer = rendererIndex < rendererGroup.Renderers.Length - 1
                                ? rendererGroup.Renderers[rendererIndex + 1]
                                : null;

                            args = new CubismRenderedEventArgs()
                            {
                                PassData = data,
                                CommandBuffer = commandBuffer,
                                ColorBuffer = renderer.RenderController.CurrentFrameBuffer,
                                DepthBuffer = data.CameraDepthTextureHandle,
                                SortingOrder = renderer.MeshRenderer.sortingOrder,
                                Drawable = renderer.Drawable,
                                SortingMode = renderer.RenderController.SortingMode,
                                GroupSortingOrder = rendererGroup.SortingIndex,
                                Distance = renderer.DistanceToCamera,
                                NextDistance = nextRenderer ? nextRenderer.DistanceToCamera : null,
                                PreviousDistance = previousRenderer ? previousRenderer.DistanceToCamera : null,
                                CameraPos = data.CameraData.WorldSpaceCameraPos,
                                CameraForward = cameraForward
                            };

                            // Predict whether any interceptor will inject draws for this event. When yes,
                            // flush the batcher around the events so injected commands stay correctly
                            // ordered relative to batched drawables.
                            willInject = interceptorsManager.WillAnyInterceptorInject(args);

                            if (willInject)
                            {
                                // Commit pending batched drawables before OnPreRendering injection.
                                groupBatcher?.Flush(commandBuffer, data);
                            }

                            // Pre-rendering event.
                            interceptorsManager.OnPreRendering(args);
                        }

                        // Draw the object (either appended to the batch or drawn immediately).
                        if (groupBatcher != null)
                        {
                            groupBatcher.AddOrDraw(renderer, commandBuffer, data);
                        }
                        else
                        {
                            renderer.DrawObject(commandBuffer, data);
                        }

                        if (hasInterceptors)
                        {
                            if (willInject)
                            {
                                // Commit this drawable before OnPostRendering injection so Post sees it as drawn.
                                groupBatcher?.Flush(commandBuffer, data);
                            }

                            // Post-rendering event.
                            interceptorsManager.OnPostRendering(args);
                        }

                        if (renderer.IsLastDrawObjectInModel)
                        {
                            // Flush before SubmitDrawOffscreen — it changes render-target state.
                            groupBatcher?.Flush(commandBuffer, data);
                            renderer.RenderController.SubmitDrawOffscreen(commandBuffer, data);
                        }
                    }

                    // Flush any drawables left in the batcher at the end of the group.
                    groupBatcher?.Flush(commandBuffer, data);

                    // Direct groups already drew straight to the camera. Only common-RT groups blit back.
                    // Nothing to blit back when the common RT is unavailable: the group already drew to the
                    // camera target above, so its content is on the camera texture already.
                    // In accumulate mode (copyPerGroup == false) the single final blit is issued by ExecutePass.
                    if (!isDirectGroup && copyPerGroup && commonRenderingTexture)
                    {
                        _commandBuffer.SetRenderTarget(data.CameraTextureHandle, data.CameraDepthTextureHandle);

                        // Draw the full-screen quad to blit the common rendering texture to the camera texture.
                        _blitRenderTextureMaterial.SetTexture(CubismShaderVariables.MainTexture, commonRenderingTexture);

                        // Check for reversed Z buffer.
                        var reversedZ = SystemInfo.usesReversedZBuffer ? GEqual : LEqual;
                        _blitRenderTextureMaterial.SetInt(CubismShaderVariables.ReversedZ, reversedZ);

                        // Draw the full-screen quad.
                        _commandBuffer.DrawMesh(_blitRenderTextureMesh, Matrix4x4.identity, _blitRenderTextureMaterial);

                        // The next common-RT group re-clears the common texture at its setup, so no clear here.
                    }
                }

                // Complete every group's combine jobs together and upload the combined meshes. Deferred until
                // after the whole group loop so each group's combine job overlaps the following groups'
                // main-thread work instead of blocking per group. The uploads still land before the command
                // buffer executes.
                for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray?.Length; groupIndex++)
                {
                    _sortedRendererGroupDataArray[groupIndex].Batcher?.FinalizeFrame();
                }

                // Reset the flag after processing.
                for (var i = 0; i < data.RenderControllers?.Length; i++)
                {
                    var controller = data.RenderControllers[i];
                    if (!controller || !controller.enabled || !controller.gameObject.activeSelf)
                    {
                        continue;
                    }

                    controller.DidChangeSorting = false;
                    controller.DidChangeDrawableRenderOrder = false;
                }

                CubismRenderControllerGroup.GetInstance().DidChangeSortingRenderControllerGroup = false;
            }

            /// <summary>
            /// Executes the render pass draw commands. Shared by the Unity 6000 render-graph path (which
            /// resolves the native command buffer from the graph context) and the Unity 2022 compatibility
            /// path (which passes its pooled command buffer directly).
            /// </summary>
            /// <param name="data">Pass data containing render controllers, camera data, and texture handles.</param>
            /// <param name="commandBuffer">Command buffer for executing render commands.</param>
            private static void ExecutePass(PassData data, CommandBuffer commandBuffer)
            {
                // Check if we have any render controllers to process
                if (data.RenderControllers == null || data.RenderControllers.Length == 0)
                {
                    return;
                }

                // Resolve the texture handles once for the whole draw phase; every draw path reads them from
                // the pass data afterwards.
                data.ResolveTextureHandles();

                // Common RT as it is this frame. The clears and the blits back to the camera only make sense
                // against the dedicated texture, so they are left out when it did not resolve, unlike the
                // draw target itself, which falls back to the camera color target.
                var commonRenderingTexture = (RenderTexture)data.CommonRenderingTextureHandle;

                // Determine the initial frame buffer to use for rendering. If a common rendering texture is available, use it; otherwise, fall back to the camera texture.
                var initialFrameBuffer = data.CommonRenderingTexture;

                // Initializes the per-pass rendering context for each render controller
                // HACK: The FrameDebugger can sometimes cause unstable rendering, so it is initialized here.
                for (var rendererIndex = 0; rendererIndex < data.RenderControllers.Length; rendererIndex++)
                {
                    var target = data.RenderControllers[rendererIndex];

                    if (!target)
                    {
                        continue;
                    }

                    data.RenderControllers[rendererIndex].CurrentFrameBuffer = initialFrameBuffer;
                    data.RenderControllers[rendererIndex].CurrentOffscreenUnmanagedIndex = -1;

                    // Snapshot the model root transform once at the head of draw preparation. The model is
                    // not stepped again after OnLateUpdate, so the root stays constant for the whole pass.
                    // Capturing it here lets per-drawable transform baking read cached values instead of
                    // re-walking the hierarchy (lossyScale in particular) for every drawable.
                    data.RenderControllers[rendererIndex].CaptureRootTransform();
                }

                _commandBuffer = commandBuffer;

                EnsureBlitResources();

                // Sort the renderers by their sorting order.
                SortingRendererGroups(data);

                // Classify groups: direct groups draw straight to the camera target. Common-RT groups
                // composite through the common RT and need a blit.
                var anyDirectGroup = false;
                var anyCommonRtGroup = false;
                for (var i = 0; i < _sortedRendererGroupDataArray?.Length; i++)
                {
                    if (_sortedRendererGroupDataArray[i].IsDirectGroup)
                    {
                        anyDirectGroup = true;
                    }
                    else
                    {
                        anyCommonRtGroup = true;
                    }
                }

                // Decide the copy timing. Accumulate mode (single final blit) only orders correctly when no
                // direct group is interleaved with common-RT groups: direct groups draw to the camera
                // immediately, so a later single blit of the accumulated common RT would composite over them
                // regardless of sort order. When both kinds coexist in accumulate mode, force per-group copy
                // so every group commits to the camera in sort order.
                var copyPerGroup = CubismRenderControllerGroup.GetInstance().IsCopiedToCameraTexture
                                   || (anyDirectGroup && anyCommonRtGroup);

#if UNITY_EDITOR
                // HACK: In the editor, Scene view camera may not have the latest texture data.
                // Only needed when something composites through the common RT.
                if (data.CameraData.IsSceneViewCamera && anyCommonRtGroup && commonRenderingTexture)
                {
                    _commandBuffer.Blit(data.CameraTextureHandle, commonRenderingTexture);
                }
#endif

                // Draw the objects. Per-group render-target setup (camera target for direct groups,
                // common RT for the rest), per-group blits and depth clears are handled in DrawObjects.
                try
                {
                    DrawObjects(_commandBuffer, data, copyPerGroup);
                }
                finally
                {
                    // Release any MeshDataArray acquired for batch combining even if the pass aborted mid-way,
                    // so a source mesh is never left read-only locked (which would break all later frames).
                    // A no-op on the normal path, where DrawObjects already finalized every group's batcher.
                    for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray?.Length; groupIndex++)
                    {
                        _sortedRendererGroupDataArray[groupIndex].Batcher?.AbortFrame();
                    }
                }

                // Single final blit for the accumulate path (per-group copy off) when the common RT was used.
                // Skipped when the common RT is unavailable: the groups drew to the camera target instead.
                if (anyCommonRtGroup && !copyPerGroup && commonRenderingTexture)
                {
                    _commandBuffer.SetRenderTarget(data.CameraTextureHandle, data.CameraDepthTextureHandle);

                    // Draw the full-screen quad to blit the common rendering texture to the camera texture.
                    _blitRenderTextureMaterial.SetTexture(CubismShaderVariables.MainTexture, commonRenderingTexture);

                    // Check for reversed Z buffer.
                    var reversedZ = SystemInfo.usesReversedZBuffer ? GEqual : LEqual;
                    _blitRenderTextureMaterial.SetInt(CubismShaderVariables.ReversedZ, reversedZ);

                    // Draw the full-screen quad.
                    _commandBuffer.DrawMesh(_blitRenderTextureMesh, Matrix4x4.identity, _blitRenderTextureMaterial);
                }

                // Clear the common rendering texture for the next frame (only when it was used).
                if (anyCommonRtGroup && commonRenderingTexture)
                {
                    _commandBuffer.SetRenderTarget(commonRenderingTexture);
                    _commandBuffer.ClearRenderTarget(true, true, Color.clear);
                }
            }

#if UNITY_6000_0_OR_NEWER
            /// <summary>
            /// This method is called by the render graph to record render passes.
            /// RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            /// FrameData is a context container through which URP resources can be accessed and managed.
            /// </summary>
            /// <param name="renderGraph">Render graph to add render passes to.</param>
            /// <param name="frameData">Context container providing access to URP resources and camera data.</param>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                const string renderCustomPass = "Cubism URP Render Pass";

                // Get render controllers - this should work in both play mode and edit mode
                var renderControllers = CubismRenderControllerGroup.GetInstance().RenderControllers;

                // Skip if no render controllers are available
                if (renderControllers == null || renderControllers.Length == 0)
                {
                    return;
                }

                // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
                using (var builder = renderGraph.AddUnsafePass<PassData>(renderCustomPass, out var passData))
                {
                    // Set up pass data
                    passData.RenderControllers = renderControllers;

                    var renderControllerGroups = CubismRenderControllerGroup.GetInstance().GroupDataArray;
                    passData.RenderControllerGroupDaraArray = renderControllerGroups;

                    var cameraData = frameData.Get<UniversalCameraData>();
                    var resourceData = frameData.Get<UniversalResourceData>();

                    passData.CameraData = new CubismPassCameraData
                    {
                        Camera = cameraData.camera,
                        WorldSpaceCameraPos = cameraData.worldSpaceCameraPos,
                        IsSceneViewCamera = cameraData.isSceneViewCamera
                    };
                    passData.ResourceData = resourceData;

                    var descriptor = resourceData.activeColorTexture.GetDescriptor(renderGraph);
                    descriptor.wrapMode = TextureWrapMode.Repeat;
                    descriptor.filterMode = FilterMode.Point;

                    // The descriptor is inherited from the camera color target, which carries the pipeline's MSAA
                    // sample count. The common / temporary textures (and the offscreen textures created from the
                    // common one) are bound together with the camera depth, so they keep that sample count:
                    // graphics APIs require the color and depth attachments to match. They are still sampled in
                    // shaders, so they are bound as resolved (non-MS) textures.
                    descriptor.bindTextureMS = false;

                    // Keep the camera-sized descriptor dimensions. The intermediate / high-precision mask textures
                    // are allocated from this descriptor, and its size is needed to scale-fit the mask even when no
                    // handle resolves to a RenderTexture (direct-to-backBuffer / all-direct scenes).
                    passData.CameraTextureSize = new Vector2(descriptor.width, descriptor.height);

                    // The common / temporary full-screen RTs are only used when at least one render controller group
                    // composites through the common RT (a non-direct group).
                    var needsCommonRenderingTexture = !EnableDirectRendering;
                    if (!needsCommonRenderingTexture)
                    {
                        for (var controllerIndex = 0; controllerIndex < renderControllers.Length; controllerIndex++)
                        {
                            var controller = renderControllers[controllerIndex];
                            if (controller && !controller.CanRenderDirectly)
                            {
                                needsCommonRenderingTexture = true;
                                break;
                            }
                        }
                    }

                    // Also honor the groups sorted on a previous frame.
                    // If any group was sorted on a previous frame, it may have been a common-RT group, so the common RT must be allocated to avoid missing a blit.
                    if (!needsCommonRenderingTexture && _sortedRendererGroupDataArray != null)
                    {
                        for (var groupIndex = 0; groupIndex < _sortedRendererGroupDataArray.Length; groupIndex++)
                        {
                            if (!_sortedRendererGroupDataArray[groupIndex].IsDirectGroup)
                            {
                                needsCommonRenderingTexture = true;
                                break;
                            }
                        }
                    }

                    if (needsCommonRenderingTexture)
                    {
                        descriptor.name = "CommonTexture";
                        passData.CommonRenderingTextureHandle = renderGraph.CreateTexture(descriptor);
                        builder.UseTexture(passData.CommonRenderingTextureHandle, AccessFlags.ReadWrite);

                        descriptor.name = "TempTexture";
                        passData.CommonTemporaryTextureHandle = renderGraph.CreateTexture(descriptor);
                        builder.UseTexture(passData.CommonTemporaryTextureHandle, AccessFlags.ReadWrite);
                    }
                    else
                    {
                        passData.CommonRenderingTextureHandle = TextureHandle.nullHandle;
                        passData.CommonTemporaryTextureHandle = TextureHandle.nullHandle;
                    }

                    // The clipping-mask atlas is rendered per controller into that controller's own buffers
                    // (owned by CubismRenderController, bound directly in the mask pre-pass), so nothing to
                    // allocate here. Only the shared full-screen texture for high-precision / offscreen masks
                    // is a render-graph resource, and only when the scene actually uses it.
                    var needHighPrecisionMask = false;
                    for (var controllerIndex = 0; controllerIndex < renderControllers.Length; controllerIndex++)
                    {
                        var cubismRenderController = renderControllers[controllerIndex];
                        if (!cubismRenderController || !cubismRenderController.HasMask)
                        {
                            continue;
                        }

                        // High-precision drawables and offscreen masks use the dedicated full-screen texture.
                        var offscreenRenderers = cubismRenderController.OffscreenRenderers;
                        if (cubismRenderController.MaskTextureController
                            && cubismRenderController.MaskTextureController.UseHighPrecisionMask
                            || (offscreenRenderers != null && offscreenRenderers.Length > 0))
                        {
                            needHighPrecisionMask = true;
                            break;
                        }
                    }

                    if (needHighPrecisionMask)
                    {
                        // High-precision / offscreen masks are drawn per draw object into a separate full-screen texture
                        // (rendered in screen space, cannot be packed into the atlas).
                        descriptor.name = "HighPrecisionMaskTexture";

                        // The mask is rendered without a depth attachment, so it does not need to match the camera
                        // depth. Drop the sample count to avoid paying MSAA bandwidth and resolves for it.
                        descriptor.msaaSamples = MSAASamples.None;
                        var highPrecisionMaskTextureHandle = renderGraph.CreateTexture(descriptor);
                        builder.UseTexture(highPrecisionMaskTextureHandle, AccessFlags.ReadWrite);
                        passData.HighPrecisionMaskTextureHandle = highPrecisionMaskTextureHandle;
                    }
                    else
                    {
                        passData.HighPrecisionMaskTextureHandle = TextureHandle.nullHandle;
                    }

                    // Freshly set-up pass: no clip group occupies the high-precision mask texture yet.
                    // Explicit reset is required because RenderGraph may pool PassData instances across passes.
                    passData.ActiveHighPrecisionMaskContext = null;

                    // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                    passData.CameraTextureHandle = resourceData.activeColorTexture;
                    builder.UseTexture(passData.CameraTextureHandle, AccessFlags.ReadWrite);

                    // Set up depth buffer for proper depth testing with other Unity objects
                    passData.CameraDepthTextureHandle = resourceData.activeDepthTexture;
                    builder.UseTexture(passData.CameraDepthTextureHandle);

                    passData.EnableBatching = EnableBatching;
                    passData.EnableDirectRendering = EnableDirectRendering;
                    passData.BatchThreshold = BatchThreshold;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    passData.EnableOffscreenPreClear = EnableOffscreenPreClear;
#endif

                    // Precompute the per-renderer skip flags before recording draws.
                    PrecomputeSkipRendering(passData);

                    // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                    // The native command buffer is resolved from the graph context and passed to the shared ExecutePass.
                    builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                        ExecutePass(data, CommandBufferHelpers.GetNativeCommandBuffer(context.cmd)));
                }
            }
#else
            /// <summary>
            /// This method is called by the render graph to record render passes.
            /// RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
            /// RTHandles are allocated here once per camera setup and reused across frames.
            /// </summary>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                // The common / temporary textures are bound together with the camera depth, so they keep the
                // camera's MSAA sample count: graphics APIs require the color and depth attachments to match.
                // They are still sampled in shaders, so they are bound as resolved (non-MS) textures.
                descriptor.bindMS = false;
                RenderingUtils.ReAllocateIfNeeded(ref _commonRenderingTextureHandle, descriptor,
                    FilterMode.Point, TextureWrapMode.Repeat, name: "CommonTexture");
                RenderingUtils.ReAllocateIfNeeded(ref _commonTemporaryTextureHandle, descriptor,
                    FilterMode.Point, TextureWrapMode.Repeat, name: "TempTexture");

                // The mask is rendered without a depth attachment, so it does not need to match the camera depth.
                descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref _maskTextureHandle, descriptor,
                    FilterMode.Point, TextureWrapMode.Repeat, name: "MaskTexture");
            }

            /// <summary>
            /// Executes the render pass.
            /// </summary>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var renderControllers = CubismRenderControllerGroup.GetInstance().RenderControllers;
                if (renderControllers == null || renderControllers.Length == 0)
                {
                    return;
                }

                var cmd = CommandBufferPool.Get("Cubism URP Render Pass");

                var urpRenderer = renderingData.cameraData.renderer;

                // Reuse a single PassData instance instead of allocating one every frame.
                if (_passData == null)
                {
                    _passData = new PassData();
                }
                _passData.RenderControllers = renderControllers;
                _passData.RenderControllerGroupDaraArray = CubismRenderControllerGroup.GetInstance().GroupDataArray;
                _passData.CameraData = new CubismPassCameraData
                {
                    Camera = renderingData.cameraData.camera,
                    WorldSpaceCameraPos = renderingData.cameraData.worldSpaceCameraPos,
                    IsSceneViewCamera = renderingData.cameraData.isSceneViewCamera
                };
                _passData.HighPrecisionMaskTextureHandle = _maskTextureHandle;
                _passData.CameraTextureHandle = urpRenderer.cameraColorTargetHandle;
                _passData.CameraDepthTextureHandle = urpRenderer.cameraDepthTargetHandle;

                // The intermediate / mask RTHandles are allocated in OnCameraSetup from this descriptor, so its
                // dimensions are the correct scale-fit size for the high-precision mask.
                var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                _passData.CameraTextureSize = new Vector2(cameraTargetDescriptor.width, cameraTargetDescriptor.height);
                _passData.CommonRenderingTextureHandle = _commonRenderingTextureHandle;
                _passData.CommonTemporaryTextureHandle = _commonTemporaryTextureHandle;
                _passData.EnableBatching = EnableBatching;
                _passData.EnableDirectRendering = EnableDirectRendering;
                _passData.BatchThreshold = BatchThreshold;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                _passData.EnableOffscreenPreClear = EnableOffscreenPreClear;
#endif
                // Reset per-pass transient state so the reused instance matches a freshly allocated one.
                _passData.ActiveHighPrecisionMaskContext = null;
                _passData.CurrentGroupRendersDirect = false;

                // Precompute the per-renderer skip flags before recording draws.
                PrecomputeSkipRendering(_passData);

                ExecutePass(_passData, cmd);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <summary>
            /// Releases RTHandles allocated in <see cref="OnCameraSetup"/>.
            /// Must be called when the feature/pass is disposed (domain reload, feature removal)
            /// so GPU resources do not leak.
            /// </summary>
            internal void DisposeRTHandles()
            {
                _commonRenderingTextureHandle?.Release();
                _commonRenderingTextureHandle = null;
                _commonTemporaryTextureHandle?.Release();
                _commonTemporaryTextureHandle = null;
                _maskTextureHandle?.Release();
                _maskTextureHandle = null;
            }
#endif
        }

        /// <summary>
        /// When enabled, consecutive drawables sharing the same material state are combined into a single DrawMesh call.
        /// </summary>
        [SerializeField]
        [Tooltip("When enabled, consecutive drawables sharing the same material state are combined into a single DrawMesh call.")]
        public bool EnableBatching = true;

        /// <summary>
        /// When enabled, models that use no background-reading blend mode and no offscreen drawing are rendered
        /// directly to the camera color target, skipping the intermediate RenderTexture blit to save fill rate
        /// and VRAM. Models that cannot be drawn directly fall back to the conventional RenderTexture path
        /// automatically. Default off for backward compatibility.
        /// </summary>
        [SerializeField]
        [Tooltip("Render eligible models directly to the camera target (skipping the intermediate RenderTexture) to save fill rate and VRAM. Models requiring blend modes or offscreen drawing fall back to the conventional path automatically.")]
        public bool EnableDirectRendering = false;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// When enabled, every pooled offscreen RenderTexture is cleared with a full-screen clear at the
        /// start of each Cubism draw call, restoring the behavior from before that clear was removed.
        ///
        /// Debug and profiling aid only, so it exists in the editor and in development builds only and is
        /// compiled out of release builds. The clear is redundant, because every offscreen RenderTexture is
        /// cleared immediately before it is drawn into and a freshly allocated one is cleared once on
        /// creation. Enable it to A/B compare the removal: on reproduces the old cost, off is the
        /// shipping behavior.
        /// </summary>
        [SerializeField]
        [Tooltip("Debug only (editor and development builds). Restores the redundant per-frame full-screen clear of every pooled offscreen RenderTexture so its cost can be A/B compared. Leave disabled outside of profiling.")]
        public bool EnableOffscreenPreClear = false;
#endif

        /// <summary>
        /// Minimum number of consecutive batchable drawables required to trigger a combined DrawMesh.
        /// Below this threshold, drawables are drawn individually.
        /// </summary>
        private const int BatchThreshold = 2;

        /// <summary>
        /// Scriptable render pass that will be injected into the renderer.
        /// </summary>
        private CubismRenderPass _mScriptablePass;

        /// <summary>
        /// Creates the scriptable render pass and configures its injection point.
        /// </summary>
        public override void Create()
        {
#if !UNITY_6000_0_OR_NEWER
            // Create can be re-invoked (e.g. on an inspector edit) without Dispose running in between.
            // Release the previous pass's RTHandles before dropping its reference so the ones allocated
            // in OnCameraSetup do not leak when the instance is overwritten below.
            _mScriptablePass?.DisposeRTHandles();
#endif

            _mScriptablePass = new CubismRenderPass
            {
                // Configures where the render pass should be injected.
                renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
            };
        }

        /// <summary>
        /// Injects one or multiple render passes into the renderer.
        /// This method is called when setting up the renderer once per-camera.
        /// </summary>
        /// <param name="renderer">The scriptable renderer to add passes to.</param>
        /// <param name="renderingData">Rendering state information.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            // Skip adding the pass for scene view and game view cameras in the editor.
            if (!(renderingData.cameraData.cameraType == CameraType.Game
                || renderingData.cameraData.cameraType == CameraType.SceneView))
            {
                return;
            }
#endif

            _mScriptablePass.EnableBatching = EnableBatching;
            _mScriptablePass.EnableDirectRendering = EnableDirectRendering;
            _mScriptablePass.BatchThreshold = BatchThreshold;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _mScriptablePass.EnableOffscreenPreClear = EnableOffscreenPreClear;
#endif

            renderer.EnqueuePass(_mScriptablePass);
        }

        /// <summary>
        /// Releases the batch meshes pooled by the (static) renderer groups when the feature is
        /// disposed — e.g. on domain reload or when the feature is removed.
        /// Without this the HideAndDontSave batch meshes would leak across reloads.
        /// Also releases RTHandles allocated for Unity 2022 compatibility path.
        /// </summary>
        /// <param name="disposing">True when called from the IDisposable.Dispose path.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                CubismRenderPass.DisposeRendererGroupPools();
                CubismRenderPass.DisposeBlitResources();
#if !UNITY_6000_0_OR_NEWER
                _mScriptablePass?.DisposeRTHandles();
#endif
            }
        }
    }
}
