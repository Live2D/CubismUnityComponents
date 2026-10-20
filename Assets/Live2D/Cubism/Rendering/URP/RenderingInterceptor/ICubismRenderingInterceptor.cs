/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


namespace Live2D.Cubism.Rendering.URP.RenderingInterceptor
{
    /// <summary>
    /// Rendering interceptor interface.
    /// </summary>
    public interface ICubismRenderingInterceptor
    {
        /// <summary>
        /// Group sorting order for rendering.
        /// </summary>
        internal int RenderingGroupSortingOrder { get; }

        /// <summary>
        /// Sorting order for rendering.
        /// </summary>
        internal int RenderingSortingOrder { get; }

        /// <summary>
        /// Called by CubismRenderingInterceptorsManager before rendering for Drawables and Offscreens.
        /// </summary>
        /// <param name="args"> Rendering event arguments. </param>
        internal void OnPreRenderingForPass(CubismRenderedEventArgs args);

        /// <summary>
        /// Called by CubismRenderingInterceptorsManager after rendering for Drawables and Offscreens.
        /// </summary>
        /// <param name="args"> Rendering event arguments. </param>
        internal void OnPostRenderingForPass(CubismRenderedEventArgs args);

        /// <summary>
        /// Predicts whether <see cref="OnPreRenderingForPass"/> or <see cref="OnPostRenderingForPass"/>
        /// will inject draw commands into the command buffer for the given rendering event.
        /// </summary>
        /// <remarks>
        /// Used by the URP render pass to coexist with drawable batching: when this returns true,
        /// the batcher is flushed around the event so injected draw calls land in the correct order.
        /// Implementations should be a side-effect-free predicate equivalent to the early-return
        /// conditions of their actual draw logic. Returning a conservative true is always safe
        /// (only costs a wasted flush); returning a false negative will visibly break draw order.
        /// </remarks>
        /// <param name="args"> Rendering event arguments. </param>
        /// <returns>True if the interceptor would inject one or more draw commands for this event.</returns>
        internal bool WillInjectDraws(CubismRenderedEventArgs args);
    }
}
