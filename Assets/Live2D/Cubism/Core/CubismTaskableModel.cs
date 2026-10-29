/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core.Unmanaged;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// 'Atomic' <see cref="CubismModel"/> update task.
    /// </summary>
    /// <remarks>
    /// The native model update (<see cref="CubismCoreDll.UpdateModel"/>) runs on a Unity C# Job System worker thread via a
    /// non-Burst <see cref="IJob"/> (a P/Invoke cannot be Burst-compiled, but a non-Burst job may call it).
    /// Ordering between the main thread and the worker is guaranteed by the <see cref="JobHandle"/> instead of
    /// a lock: write parameters, <see cref="Schedule"/> the update, then <see cref="Complete"/> before reading.
    /// </remarks>
    internal sealed class CubismTaskableModel
    {
        #region Factory Methods

        /// <summary>
        /// Creates a <see cref="CubismTaskableModel"/> from a <see cref="CubismMoc"/>.
        /// </summary>
        /// <param name="moc">Moc source.</param>
        /// <returns>Instance.</returns>
        public static CubismTaskableModel CreateTaskableModel(CubismMoc moc)
        {
            return new CubismTaskableModel(moc);
        }

        #endregion

        /// <summary>
        /// Handle to unmanaged model.
        /// </summary>
        public CubismUnmanagedModel UnmanagedModel { get; private set; }

        /// <summary>
        /// <see cref="CubismMoc"/> the model was instantiated from.
        /// </summary>
        public CubismMoc Moc { get; private set; }


        /// <summary>
        /// Buffer to write dynamic data to.
        /// </summary>
        public CubismDynamicDrawableData[] DynamicDrawableData { get; private set; }


        /// <summary>
        /// Handle to the in-flight native update job. Valid only while <see cref="_isScheduled"/> is true.
        /// </summary>
        private JobHandle _updateHandle;

        /// <summary>
        /// True while an update job has been scheduled but not yet completed.
        /// </summary>
        private bool _isScheduled;

        /// <summary>
        /// True if the model ran to completion at least once.
        /// </summary>
        private bool _didExecute;


        /// <summary>
        /// True if an update is scheduled and not yet completed.
        /// </summary>
        public bool IsExecuting
        {
            get
            {
                return _isScheduled;
            }
        }

        /// <summary>
        /// True if did run to completion at least once.
        /// </summary>
        public bool DidExecute
        {
            get
            {
                return _didExecute;
            }
        }

        /// <summary>
        /// True if unmanaged model and moc should be released.
        /// </summary>
        private bool ShouldReleaseUnmanaged { get; set; }

        #region Constructor

        /// <summary>
        /// Initializes instance.
        /// </summary>
        /// <param name="moc">Moc unmanaged model was instantiated from.</param>
        public CubismTaskableModel(CubismMoc moc)
        {
            Moc = moc;


            // Instantiate unmanaged model.
            var unmanagedMoc = moc.AcquireUnmanagedMoc();


            UnmanagedModel = CubismUnmanagedModel.FromMoc(unmanagedMoc);

            if (UnmanagedModel == null)
            {
                Debug.LogError("This .moc3 file version is \"Unknown\"!!\n" +
                               "It may be broken or you are trying to use a higher version of Moc than Cubism Core.\n" +
                               "Check the supported versions at CubismMoc.LatestVersion.\n" +
                               "The \"CoreDll\" constants indicate which Moc version the numbers are assigned to.");
                return;
            }

            DynamicDrawableData = CubismDynamicDrawableData.CreateData(UnmanagedModel);
            ShouldReleaseUnmanaged = false;
        }

        #endregion

        /// <summary>
        /// Tries to read parameters into a buffer.
        /// </summary>
        /// <param name="parameters">Buffer to write to.</param>
        /// <returns><see langword="true"/> on success; <see langword="false"/> otherwise.</returns>
        public bool TryReadParameters(CubismParameter[] parameters)
        {
            // The Core buffers must not be read while the update job is in flight, and there is nothing
            // to read before the first completion.
            if (_isScheduled || !_didExecute)
            {
                return false;
            }

            parameters.ReadFrom(UnmanagedModel);

            return true;
        }

        /// <summary>
        /// Tries to write parameters and parts to the native model.
        /// </summary>
        /// <param name="parameters">Buffer to read from.</param>
        /// <param name="parts">Buffer to read from.</param>
        /// <returns><see langword="true"/> on success; <see langword="false"/> otherwise.</returns>
        public bool TryWriteParametersAndParts(CubismParameter[] parameters, CubismPart[] parts)
        {
            // Must not touch the Core input buffers while the update job is reading them.
            if (_isScheduled)
            {
                return false;
            }

            parameters.WriteTo(UnmanagedModel);
            parts.WriteTo(UnmanagedModel);

            return true;
        }


        /// <summary>
        /// Schedules the native model update on a Job System worker thread.
        /// </summary>
        /// <remarks>
        /// Call after writing parameters/parts. Pair every <see cref="Schedule"/> with a <see cref="Complete"/>
        /// before reading results. Scheduling while a previous job is still in flight is a no-op.
        /// </remarks>
        public void Schedule()
        {
            if (_isScheduled || UnmanagedModel == null)
            {
                return;
            }

            _updateHandle = new CubismModelUpdateJob
            {
                ModelPtr = UnmanagedModel.Ptr
            }
            .Schedule();

            _isScheduled = true;
        }

        /// <summary>
        /// Completes the scheduled native update and reads the results into the managed buffers.
        /// </summary>
        public void Complete()
        {
            if (!_isScheduled)
            {
                return;
            }

            _updateHandle.Complete();
            _isScheduled = false;


            // Get results.
            DynamicDrawableData.ReadFrom(UnmanagedModel);

            _didExecute = true;


            // Release native if requested while the job was in flight.
            if (ShouldReleaseUnmanaged)
            {
                OnReleaseUnmanaged();
            }
        }

        /// <summary>
        /// Completes the scheduled update job (waits for the worker) without reading the results.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Complete"/>, this does not read the native buffers or reset the dynamic flags, so
        /// the changes the completed update produced remain flagged for a following update to pick up. Use it
        /// when the results are going to be discarded and replaced by another update (or when simply stopping
        /// the worker), so no drawable is left with a stale mesh.
        /// </remarks>
        public void CompleteWithoutRead()
        {
            if (!_isScheduled)
            {
                return;
            }

            _updateHandle.Complete();
            _isScheduled = false;
        }

        /// <summary>
        /// Runs the native update synchronously to completion (no worker thread).
        /// </summary>
        /// <remarks>
        /// Used where the Job System two-phase (schedule/complete) flow is not available, such as the Editor's
        /// non-play update and <see cref="CubismModel.ForceUpdateNow"/>.
        /// </remarks>
        /// <param name="accumulateFlags">
        /// <see langword="true"/> to merge the dynamic flags into the ones already read instead of replacing them.
        /// Use it when the previously read results have not been consumed yet.
        /// </param>
        /// <returns><see langword="true"/> on success.</returns>
        public bool UpdateNow(bool accumulateFlags = false)
        {
            // Finish any in-flight job first so the synchronous update does not race it.
            if (_isScheduled)
            {
                _updateHandle.Complete();
                _isScheduled = false;
            }

            if (UnmanagedModel == null)
            {
                return false;
            }


            // Update native backend.
            UnmanagedModel.Update();


            // Get results.
            DynamicDrawableData.ReadFrom(UnmanagedModel, accumulateFlags);

            _didExecute = true;


            if (ShouldReleaseUnmanaged)
            {
                OnReleaseUnmanaged();
            }

            return true;
        }


        /// <summary>
        /// Releases unmanaged resource.
        /// </summary>
        public void ReleaseUnmanaged()
        {
            ShouldReleaseUnmanaged = true;


            // Finish any in-flight job before releasing the native model it references.
            if (_isScheduled)
            {
                _updateHandle.Complete();
                _isScheduled = false;
            }


            OnReleaseUnmanaged();


            ShouldReleaseUnmanaged = false;
        }


        /// <summary>
        /// Actually releases native resource(s).
        /// </summary>
        private void OnReleaseUnmanaged()
        {
            if (UnmanagedModel == null)
            {
                return;
            }

            UnmanagedModel.Release();
            Moc.ReleaseUnmanagedMoc();


            UnmanagedModel = null;
        }

        #region Job

        /// <summary>
        /// Non-Burst job that runs the native model update on a worker thread.
        /// </summary>
        /// <remarks>
        /// <see cref="CubismCoreDll.UpdateModel"/> is a P/Invoke and cannot be Burst-compiled, so this job is intentionally not
        /// marked <see cref="Unity.Burst.BurstCompileAttribute"/>.
        /// It only moves the native call off the main thread. It captures the model pointer rather than any managed reference.
        /// </remarks>
        private struct CubismModelUpdateJob : IJob
        {
            /// <summary>
            /// Pointer to unmanaged model.
            /// </summary>
            /// <remarks>
            /// NOTE: The model pointer is owned by the managed <see cref="CubismTaskableModel"/> for the model's lifetime and each
            /// model has its own pointer. Ordering with main-thread reads/writes is enforced by the JobHandle,
            /// so the Job System's unsafe-pointer restriction is intentionally disabled here.
            /// </remarks>
            [NativeDisableUnsafePtrRestriction]
            public IntPtr ModelPtr;

            /// <summary>
            /// Executes the job.
            /// </summary>
            public void Execute()
            {
                if (ModelPtr == IntPtr.Zero)
                {
                    return;
                }

                CubismCoreDll.UpdateModel(ModelPtr);
            }
        }

        #endregion
    }
}
