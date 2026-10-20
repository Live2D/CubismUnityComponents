/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using System;
using UnityEngine;

namespace Live2D.Cubism.Rendering.URP.RenderingInterceptor
{
    public class CubismRenderingInterceptorsManager
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        private static CubismRenderingInterceptorsManager _instance;

        /// <summary>
        /// Singleton instance of the <see cref="CubismRenderingInterceptorsManager"/>.
        /// </summary>
        public static CubismRenderingInterceptorsManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new CubismRenderingInterceptorsManager();
            }

            return _instance;
        }

        /// <summary>
        /// <see cref="Interceptors"/>'s backing field.
        /// </summary>
        private ICubismRenderingInterceptor[] _interceptors;

        /// <summary>
        /// Registered rendering interceptors.
        /// </summary>
        public ICubismRenderingInterceptor[] Interceptors
        {
            get { return _interceptors; }
            set
            {
                _interceptors = value;
                _shouldSortInterceptors = true;
                ResetInterceptorSortData();
            }
        }

        /// <summary>
        /// Sort data for <see cref="Interceptors"/>.
        /// </summary>
        private InterceptorSortData[] _interceptorSortData;

        /// <summary>
        /// Whether <see cref="Interceptors"/> should be sorted before rendering.
        /// </summary>
        private bool _shouldSortInterceptors;

        /// <summary>
        /// Sort data for rendering interceptors.
        /// </summary>
        private struct InterceptorSortData
        {
            public ICubismRenderingInterceptor Interceptor;
            public bool IsSortable;
            public int GroupSortingOrder;
            public int SortingOrder;
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private CubismRenderingInterceptorsManager()
        {
            _interceptors = Array.Empty<ICubismRenderingInterceptor>();
            _interceptorSortData = Array.Empty<InterceptorSortData>();
        }

        /// <summary>
        /// Adds the given interceptor to the manager.
        /// </summary>
        /// <param name="interceptor"> The interceptor to add. </param>
        public void AddInterceptors(ICubismRenderingInterceptor interceptor)
        {
            RemoveInterceptors(interceptor);

            for (var interceptorIndex = 0; interceptorIndex < Interceptors.Length; interceptorIndex++)
            {
                var current = Interceptors[interceptorIndex];
                if (interceptor != current)
                {
                    continue;
                }

                Debug.LogWarning($"This component is already registered.");
                return;
            }

            Array.Resize(ref _interceptors, Interceptors.Length + 1);

            Interceptors[^1] = interceptor;

            ResetInterceptorSortData();
            _shouldSortInterceptors = true;
        }

        /// <summary>
        /// Removes the given interceptor from the manager.
        /// </summary>
        /// <param name="interceptor"> The interceptor to remove. </param>
        public void RemoveInterceptors(ICubismRenderingInterceptor interceptor)
        {
            var targetIndex = -1;
            for (var interceptorIndex = 0; interceptorIndex < Interceptors.Length; interceptorIndex++)
            {
                if (Interceptors[interceptorIndex] != interceptor)
                {
                    continue;
                }

                targetIndex = interceptorIndex;
                break;
            }

            if (targetIndex < 0)
            {
                return;
            }

            for (var interceptorIndex = targetIndex; interceptorIndex < Interceptors.Length - 1; interceptorIndex++)
            {
                Interceptors[interceptorIndex] = Interceptors[interceptorIndex + 1];
            }

            Array.Resize(ref _interceptors, Interceptors.Length - 1);

            ResetInterceptorSortData();
            _shouldSortInterceptors = true;
        }

        /// <summary>
        /// Reorders the given interceptor to the new index.
        /// </summary>
        /// <param name="interceptor"> The interceptor to reorder. </param>
        /// <param name="newIndex"> The new index to move the interceptor to. </param>
        public void ReorderIndices(ICubismRenderingInterceptor interceptor, int newIndex)
        {
            // Find the current index of the interceptor.
            var currentIndex = Array.IndexOf(Interceptors, interceptor);

            if (currentIndex < 0
                || newIndex < 0
                || newIndex >= Interceptors.Length
                || currentIndex == newIndex)
            {
                // Return silently.
                return;
            }

            var temp = Interceptors[currentIndex];
            if (currentIndex < newIndex)
            {
                // Shift elements to the left.
                for (var i = currentIndex; i < newIndex; i++)
                {
                    Interceptors[i] = Interceptors[i + 1];
                }
            }
            else
            {
                // Shift elements to the right.
                for (var i = currentIndex; i > newIndex; i--)
                {
                    Interceptors[i] = Interceptors[i - 1];
                }
            }

            // Place the interceptor at the new index.
            Interceptors[newIndex] = temp;

            ResetInterceptorSortData();
            _shouldSortInterceptors = true;
        }

        /// <summary>
        /// Called before rendering.
        /// </summary>
        /// <param name="args"> Rendering event arguments. </param>
        public void OnPreRendering(CubismRenderedEventArgs args)
        {
            EnsureInterceptorsSorted();

            for (var index = 0; index < Interceptors.Length; index++)
            {
                Interceptors[index].OnPreRenderingForPass(args);
            }
        }

        /// <summary>
        /// Called after rendering.
        /// </summary>
        /// <param name="args"> Rendering event arguments. </param>
        public void OnPostRendering(CubismRenderedEventArgs args)
        {
            EnsureInterceptorsSorted();

            for (var index = 0; index < Interceptors.Length; index++)
            {
                Interceptors[index].OnPostRenderingForPass(args);
            }
        }

        /// <summary>
        /// Sorts interceptors if their sort keys changed.
        /// </summary>
        private void EnsureInterceptorsSorted()
        {
            _shouldSortInterceptors |= UpdateInterceptorSortData();

            if (!_shouldSortInterceptors)
            {
                return;
            }

            SortInterceptors();
            _shouldSortInterceptors = false;
        }


        /// <summary>
        /// Creates sort data for the given interceptor.
        /// <param name="interceptor">Target interceptor.</param>
        /// </summary>
        private InterceptorSortData CreateSortData(ICubismRenderingInterceptor interceptor)
        {
            var isSortable = interceptor != null;
            return new InterceptorSortData
            {
                Interceptor = interceptor,
                IsSortable = isSortable,
                GroupSortingOrder = isSortable ? interceptor.RenderingGroupSortingOrder : 0,
                SortingOrder = isSortable ? interceptor.RenderingSortingOrder : 0
            };
        }

        /// <summary>
        /// Resets sort data from the current interceptor order.
        /// </summary>
        private void ResetInterceptorSortData()
        {
            _interceptorSortData = new InterceptorSortData[Interceptors.Length];
            for (var index = 0; index < Interceptors.Length; index++)
            {
                _interceptorSortData[index] = CreateSortData(Interceptors[index]);
            }
        }

        /// <summary>
        /// Updates sort data and returns whether any sort key changed.
        /// </summary>
        private bool UpdateInterceptorSortData()
        {
            if (_interceptorSortData.Length != Interceptors.Length)
            {
                ResetInterceptorSortData();
                return true;
            }

            var didChange = false;
            for (var index = 0; index < Interceptors.Length; index++)
            {
                if (_interceptorSortData[index].Interceptor != Interceptors[index])
                {
                    ResetInterceptorSortData();
                    return true;
                }

                var current = CreateSortData(Interceptors[index]);
                if (_interceptorSortData[index].IsSortable != current.IsSortable
                   || _interceptorSortData[index].GroupSortingOrder != current.GroupSortingOrder
                   || _interceptorSortData[index].SortingOrder != current.SortingOrder)
                {
                    _interceptorSortData[index] = current;
                    didChange = true;
                }
            }

            return didChange;
        }

        /// <summary>
        /// Sorts sortable interceptors while keeping non-sortable interceptor slots in place.
        /// </summary>
        private void SortInterceptors()
        {
            var sortableInterceptors = Array.Empty<InterceptorSortData>();
            for (var index = 0; index < _interceptorSortData.Length; index++)
            {
                if (!_interceptorSortData[index].IsSortable)
                {
                    continue;
                }

                Array.Resize(ref sortableInterceptors, sortableInterceptors.Length + 1);
                sortableInterceptors[^1] = _interceptorSortData[index];
            }

            Array.Sort(sortableInterceptors, CompareSortableInterceptors);

            var sortableIndex = 0;
            for (var index = 0; index < _interceptorSortData.Length; index++)
            {
                if (_interceptorSortData[index].IsSortable)
                {
                    _interceptorSortData[index] = sortableInterceptors[sortableIndex];
                    sortableIndex++;
                }

                _interceptors[index] = _interceptorSortData[index].Interceptor;
            }
        }

        /// <summary>
        /// Returns true if any registered interceptor would inject draw commands for the given event.
        /// </summary>
        /// <remarks>
        /// Used by the URP render pass to decide whether to flush the drawable batcher around the
        /// per-drawable interceptor events. False allows batching to continue across the drawable;
        /// true forces a flush so the injected draws land in the correct order.
        /// </remarks>
        /// <param name="args"> Rendering event arguments. </param>
        /// <returns>True if at least one interceptor would inject for this event.</returns>
        internal bool WillAnyInterceptorInject(CubismRenderedEventArgs args)
        {
            for (var index = 0; index < Interceptors.Length; index++)
            {
                if (Interceptors[index].WillInjectDraws(args))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compares two sortable interceptors.
        /// </summary>
        /// <param name="a">First interceptor.</param>
        /// <param name="b">Second interceptor.</param>
        /// <returns>Comparison result.</returns>
        private static int CompareSortableInterceptors(InterceptorSortData a, InterceptorSortData b)
        {
            var result = a.GroupSortingOrder.CompareTo(b.GroupSortingOrder);

            if (result != 0)
            {
                return result;
            }

            return a.SortingOrder.CompareTo(b.SortingOrder);
        }
    }
}
