/**
 * Copyright(c) Live2D Inc. All rights reserved.
 *
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at https://www.live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core.Unmanaged;
using Live2D.Cubism.Framework;
using System;
using Unity.Jobs;
using UnityEngine;


#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#elif UNITY_2018_1_OR_NEWER
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;
#endif


namespace Live2D.Cubism.Core
{
    /// <summary>
    /// Runtime Cubism model.
    /// </summary>
    [ExecuteInEditMode, CubismDontMoveOnReimport]
    public sealed class CubismModel : MonoBehaviour
    {
        #region Delegates

        /// <summary>
        /// Handler for <see cref="CubismDynamicDrawableData"/>.
        /// </summary>
        /// <param name="sender">Model the dymanic data applies to.</param>
        /// <param name="data">New data.</param>
        public delegate void DynamicDrawableDataHandler(CubismModel sender, CubismDynamicDrawableData[] data);

        #endregion

        #region Events

        /// <summary>
        /// Event triggered if new <see cref="CubismDynamicDrawableData"/> is available for instance.
        /// </summary>
        public event DynamicDrawableDataHandler OnDynamicDrawableData;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Instantiates a <see cref="CubismMoc"/>.
        /// </summary>
        /// <param name="moc">Cubism moc to instantiate.</param>
        /// <returns>Instance.</returns>
        public static CubismModel InstantiateFrom(CubismMoc moc)
        {
            // Return if argument is invalid.
            if (moc == null)
            {
                return null;
            }


            // Create model.
            var model = new GameObject(moc.name)
                .AddComponent<CubismModel>();


            // Initialize it by resetting it.
            model.Reset(moc);


            return model;
        }

        #endregion

        /// <summary>
        /// Resets a <see cref="CubismMoc"/> reference in <see cref="CubismModel"/>.
        /// </summary>
        /// <param name="model">Target Cubism model.</param>
        /// <param name="moc">Cubism moc to reset.</param>
        public static void ResetMocReference(CubismModel model, CubismMoc moc)
        {
            model.Moc = moc;
        }

        /// <summary>
        /// Resets non-serialized fields of a <see cref="CubismModel"/>.
        /// </summary>
        /// <remarks>
        /// Call after <see cref="UnityEditor.PrefabUtility.SaveAsPrefabAsset"/> to clear stale
        /// component references that may have been cached by <see cref="OnValidate"/>
        /// during the prefab replacement.
        /// </remarks>
        /// <param name="model">Target Cubism model.</param>
        public static void ResetNonSerializedFields(CubismModel model)
        {
            if (model.TaskableModel != null)
            {
                model.TaskableModel.ReleaseUnmanaged();
                model.TaskableModel = null;

                ++model._nativeModelRevision;
            }

            model._parameters = null;
            model._parts = null;
            model._drawables = null;
            model._offscreens = null;
            model._canvasInformation = null;
            model._allDrawObjectsRenderOrder = null;
            model._pendingFetchResults = false;
            model._pendingParameterRestore = false;
        }

        /// <summary>
        /// <see cref="Moc"/> backing field.
        /// </summary>
        [SerializeField, HideInInspector]
        private CubismMoc _moc;

        /// <summary>
        /// Moc the instance was instantiated from.
        /// </summary>
        public CubismMoc Moc
        {
            get { return _moc; }
            private set { _moc = value; }
        }


        /// <summary>
        /// TaskableModel for unmanaged backend.
        /// </summary>
        private CubismTaskableModel TaskableModel { get; set; }


        /// <summary>
        /// Unmanaged model backing this instance. Used by the renderer to read Core vertex buffers directly.
        /// </summary>
        internal CubismUnmanagedModel UnmanagedModel
        {
            get { return TaskableModel?.UnmanagedModel; }
        }
        /// <summary>
        /// <see cref="NativeModelRevision"/> backing field.
        /// </summary>
        [NonSerialized]
        private int _nativeModelRevision;

        /// <summary>
        /// Counts how often the backend model was released or recreated.
        /// </summary>
        /// <remarks>
        /// Lets a caller notice that data it received earlier belongs to a model that no longer exists, which
        /// happens when an editor reimport rebuilds the backend while that data is still being processed.
        /// </remarks>
        internal int NativeModelRevision
        {
            get { return _nativeModelRevision; }
        }


        /// <summary>
        /// <see cref="Parameters"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismParameter[] _parameters;

        /// <summary>
        /// Parameters of model.
        /// </summary>
        public CubismParameter[] Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    Revive();
                }


                return _parameters;
            }
            private set { _parameters = value; }
        }

        /// <summary>
        /// <see cref="Parts"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismPart[] _parts;

        /// <summary>
        /// Drawables of model.
        /// </summary>
        public CubismPart[] Parts
        {
            get
            {
                if (_parts == null)
                {
                    Revive();
                }


                return _parts;
            }
            private set { _parts = value; }
        }

        /// <summary>
        /// <see cref="Drawables"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismDrawable[] _drawables;

        /// <summary>
        /// Drawables of model.
        /// </summary>
        public CubismDrawable[] Drawables
        {
            get
            {
                if (_drawables == null)
                {
                    Revive();
                }


                return _drawables;
            }
            private set { _drawables = value; }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Checks whether the drawable cache matches the current unmanaged model.
        /// </summary>
        internal bool HasValidDrawableCache()
        {
            if (TaskableModel == null || TaskableModel.UnmanagedModel == null || _drawables == null)
            {
                return false;
            }

            var unmanagedModel = TaskableModel.UnmanagedModel;

            if (_drawables.Length != unmanagedModel.Drawables.Count)
            {
                return false;
            }

            for (var i = 0; i < _drawables.Length; ++i)
            {
                if (_drawables[i] == null || _drawables[i].UnmanagedIndex != i || !_drawables[i].IsBoundToUnmanagedModel(unmanagedModel))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether the draw object cache is stale in a way that rebuilding the native model can fix, i.e.
        /// the native model is missing, a cached drawable/offscreen was destroyed, or a cached drawable still
        /// references a previously released unmanaged model (as a reimport produces).
        /// </summary>
        /// <remarks>
        /// A count or index mismatch against the <em>current</em> unmanaged model is not reported here: another
        /// native rebuild would reproduce the same result, so releasing and recreating the native model would
        /// only churn without changing the outcome.
        /// </remarks>
        internal bool IsDrawableCacheStale()
        {
            if (TaskableModel == null || TaskableModel.UnmanagedModel == null || _drawables == null)
            {
                return true;
            }

            var unmanagedModel = TaskableModel.UnmanagedModel;

            for (var i = 0; i < _drawables.Length; ++i)
            {
                // A destroyed drawable (Unity's overloaded equality reports it as null), or one still bound to a
                // previously released unmanaged model, means the cache must be rebuilt. A reimport that removes
                // drawables produces destroyed references that would otherwise be dereferenced while (re)building
                // renderers. The short-circuit keeps a destroyed entry from being accessed.
                if (_drawables[i] == null || !_drawables[i].IsBoundToUnmanagedModel(unmanagedModel))
                {
                    return true;
                }
            }

            // Offscreen renderers are built from this cache the same way, so a destroyed offscreen reference
            // (produced by a reimport that removed offscreens) must also trigger a rebuild.
            if (_offscreens != null)
            {
                for (var i = 0; i < _offscreens.Length; ++i)
                {
                    if (_offscreens[i] == null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
#endif

        /// <summary>
        /// <see cref="CanvasInformation"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismCanvasInformation _canvasInformation;

        /// <summary>
        /// Canvas information of model.
        /// </summary>
        public CubismCanvasInformation CanvasInformation
        {
            get
            {
                if (_canvasInformation == null)
                {
                    Revive();
                }


                return _canvasInformation;
            }
            private set { _canvasInformation = value; }
        }

        /// <summary>
        /// <see cref="Offscreens"/> backing field.
        /// </summary>
        [NonSerialized]
        private CubismOffscreen[] _offscreens;

        /// <summary>
        /// Offscreens of model.
        /// </summary>
        public CubismOffscreen[] Offscreens
        {
            get
            {
                if (_offscreens == null)
                {
                    Revive();
                }

                return _offscreens;
            }
            private set { _offscreens = value; }
        }

        /// <summary>
        /// <see cref="AllDrawObjectsRenderOrder"/> backing snapshot.
        /// </summary>
        [NonSerialized]
        private int[] _allDrawObjectsRenderOrder;

        /// <summary>
        /// All draw objects render order.
        /// </summary>
        /// <remarks>
        /// A managed snapshot taken when the update completes, so it is safe to read on the main thread while the
        /// next update runs on a worker thread across the frame boundary. The values belong to the same completed
        /// generation as the rest of the frame's consumed results.
        /// </remarks>
        public int[] AllDrawObjectsRenderOrder
        {
            get
            {
                // Lazily capture for early readers (such as renderer setup) that run before the first update
                // has been consumed. Completing first keeps the native read off any in-flight worker.
                if (_allDrawObjectsRenderOrder == null)
                {
                    EnsureUpdateCompleted();
                    CaptureRenderOrders();
                }

                return _allDrawObjectsRenderOrder;
            }
        }

        /// <summary>
        /// Parameter store cache.
        /// </summary>
        CubismParameterStore _parameterStore;

        /// <summary>
        /// Whether parameter repetition is performed for the entire model.
        /// </summary>
        [SerializeField]
        private bool _isOverriddenParameterRepeat = true;


        /// <summary>
        /// Checks whether parameter repetition is performed for the entire model.
        /// </summary>
        /// <returns>True if parameter repetition is performed for the entire model; otherwise returns false.</returns>
        public bool GetOverrideFlagForModelParameterRepeat()
        {
            return _isOverriddenParameterRepeat;
        }

        /// <summary>
        /// Sets whether parameter repetition is performed for the entire model.
        /// </summary>
        /// <param name="isRepeat">Use true to perform parameter repetition for the entire model, or false to not perform it.</param>
        public void SetOverrideFlagForModelParameterRepeat(bool isRepeat)
        {
            _isOverriddenParameterRepeat = isRepeat;
        }

        /// <summary>
        /// True if instance is revived.
        /// </summary>
        public bool IsRevived
        {
            get { return TaskableModel != null; }
        }

        /// <summary>
        /// True if instance can revive.
        /// </summary>
        private bool CanRevive
        {
            get { return Moc != null; }
        }

#if UNITY_2018_1_OR_NEWER
        /// <summary>
        /// Per-model "schedule the native update" functions, invoked as the first player-loop phase.
        /// </summary>
        [NonSerialized]
        private static Action _modelUpdateFunctions;

        /// <summary>
        /// Per-model "complete the native update and consume results" functions, invoked as the second
        /// player-loop phase (same frame) so every model's update is scheduled before any is completed and
        /// they overlap across worker threads.
        /// </summary>
        [NonSerialized]
        private static Action _modelCompleteFunctions;

        private bool WasAttachedModelUpdateFunction { get; set; }
#endif


        /// <summary>
        /// True on the frame the instance was enabled.
        /// </summary>
        private bool WasJustEnabled { get; set; }

        /// <summary>
        /// Frame number last update was done.
        /// </summary>
        private int LastTick { get; set; }

        /// <summary>
        /// True when an update job was completed early by a live Core read (see <see cref="EnsureUpdateCompleted"/>)
        /// but its results have not been consumed by <see cref="FetchResults"/> yet.
        /// </summary>
        [NonSerialized]
        private bool _pendingFetchResults;

        /// <summary>
        /// True when <see cref="ForceUpdateNow"/> updated the model and the next complete phase must still sync
        /// the parameters back and restore them, because no update job was scheduled to do it.
        /// </summary>
        [NonSerialized]
        private bool _pendingParameterRestore;

        /// <summary>
        /// Revives instance.
        /// </summary>
        internal void Revive()
        {
            // Return if already revive.
            if (IsRevived)
            {
                return;
            }


            // Return if revive isn't possible.
            if (!CanRevive)
            {
                return;
            }


            Reset(Moc);
        }

        /// <summary>
        /// Initializes instance for first use.
        /// </summary>
        /// <param name="moc">Moc to instantiate from.</param>
        private void Reset(CubismMoc moc)
        {
            Moc = moc;
            TaskableModel = new CubismTaskableModel(moc);

            ++_nativeModelRevision;

            if (TaskableModel == null || TaskableModel.UnmanagedModel == null)
            {
                return;
            }

            Parameters = GetComponentsInChildren<CubismParameter>();
            if (Parameters.Length < 1 && (transform.Find("Parameters") == null))
            {
                // Create and initialize proxies.
                var parameters = CubismParameter.CreateParameters(TaskableModel.UnmanagedModel);
                parameters.transform.SetParent(transform);
                Parameters = parameters.GetComponentsInChildren<CubismParameter>();
            }
            else
            {
                // Filter stale entries whose UnmanagedIndex exceeds the new Moc count.
                var unmanagedParameterCount = TaskableModel.UnmanagedModel.Parameters.Count;
                if (Parameters.Length > unmanagedParameterCount)
                {
                    var filtered = new CubismParameter[unmanagedParameterCount];
                    var n = 0;
                    for (var i = 0; i < Parameters.Length; i++)
                    {
                        if (Parameters[i].UnmanagedIndex < unmanagedParameterCount)
                        {
                            filtered[n++] = Parameters[i];
                        }
                    }

                    if (n < unmanagedParameterCount)
                    {
                        Array.Resize(ref filtered, n);
                    }

                    Parameters = filtered;
                }

                Parameters.Revive(TaskableModel.UnmanagedModel);
            }


            Parts = GetComponentsInChildren<CubismPart>();
            if (Parts.Length < 1 && (transform.Find("Parts") == null))
            {
                // Create and initialize proxies.
                var parts = CubismPart.CreateParts(TaskableModel.UnmanagedModel);
                parts.transform.SetParent(transform);
                Parts = parts.GetComponentsInChildren<CubismPart>();
            }
            else
            {
                // Filter stale entries whose UnmanagedIndex exceeds the new Moc count.
                var unmanagedPartCount = TaskableModel.UnmanagedModel.Parts.Count;
                if (Parts.Length > unmanagedPartCount)
                {
                    var filtered = new CubismPart[unmanagedPartCount];
                    var n = 0;
                    for (var i = 0; i < Parts.Length; i++)
                    {
                        if (Parts[i].UnmanagedIndex < unmanagedPartCount)
                        {
                            filtered[n++] = Parts[i];
                        }
                    }

                    if (n < unmanagedPartCount)
                    {
                         Array.Resize(ref filtered, n);
                    }

                    Parts = filtered;
                }
                Parts.Revive(TaskableModel.UnmanagedModel);
            }


            Drawables = GetComponentsInChildren<CubismDrawable>();
            if (Drawables.Length < 1 && (transform.Find("Drawables") == null))
            {
                // Create and initialize proxies.
                var drawables = CubismDrawable.CreateDrawables(TaskableModel.UnmanagedModel);
                drawables.transform.SetParent(transform);
                Drawables = drawables.GetComponentsInChildren<CubismDrawable>();
            }
            else
            {
                // Filter stale entries whose UnmanagedIndex exceeds the new Moc count.
                var unmanagedDrawableCount = TaskableModel.UnmanagedModel.Drawables.Count;
                if (Drawables.Length > unmanagedDrawableCount)
                {
                    var filtered = new CubismDrawable[unmanagedDrawableCount];
                    var n = 0;
                    for (var i = 0; i < Drawables.Length; i++)
                    {
                        if (Drawables[i].UnmanagedIndex < unmanagedDrawableCount)
                        {
                            filtered[n++] = Drawables[i];
                        }
                    }

                    if (n < unmanagedDrawableCount)
                    {
                        Array.Resize(ref filtered, n);
                    }

                    Drawables = filtered;
                }

                Drawables.Revive(TaskableModel.UnmanagedModel);
            }

            if (0 < CubismCoreDll.GetOffscreenCount(TaskableModel.UnmanagedModel.Ptr))
            {
                Offscreens = GetComponentsInChildren<CubismOffscreen>();
                if (Offscreens.Length < 1 && (transform.Find("Offscreens") == null))
                {
                    // Create and initialize proxies.
                    var offscreens = CubismOffscreen.CreateOffscreens(TaskableModel.UnmanagedModel);
                    offscreens.transform.SetParent(transform);
                    Offscreens = offscreens.GetComponentsInChildren<CubismOffscreen>();
                }
                else
                {
                    // Filter stale entries whose UnmanagedIndex exceeds the new Moc count.
                    var unmanagedOffscreenCount = TaskableModel.UnmanagedModel.Offscreens.Count;
                    if (Offscreens.Length > unmanagedOffscreenCount)
                    {
                        var filtered = new CubismOffscreen[unmanagedOffscreenCount];
                        var n = 0;
                        for (var i = 0; i < Offscreens.Length; i++)
                        {
                            if (Offscreens[i].UnmanagedIndex < unmanagedOffscreenCount)
                            {
                                filtered[n++] = Offscreens[i];
                            }
                        }

                        if (n < unmanagedOffscreenCount)
                        {
                            Array.Resize(ref filtered, n);
                        }

                        Offscreens = filtered;
                    }

                    Offscreens.Revive(TaskableModel.UnmanagedModel);
                }
            }

            CanvasInformation = new CubismCanvasInformation(TaskableModel.UnmanagedModel);

            RefreshParameterStore();

            SetOverrideFlagForModelParameterRepeat(_isOverriddenParameterRepeat);
        }

        /// <summary>
        /// Forces update.
        /// </summary>
        public void ForceUpdateNow()
        {
            Revive();

            if (!IsRevived)
            {
                return;
            }


            // Finish any in-flight update job first. Writing parameters is skipped while a job is scheduled, so
            // without this the synchronous update below would run with stale parameters. Complete without reading
            // so the in-flight update's dynamic flags are kept and its moved drawables are not left stale.
            TaskableModel.CompleteWithoutRead();

            // Synchronous immediate update and result consumption. Used in the Editor and for on-demand
            // refreshes where the player-loop two-phase (schedule/complete) flow is not running.
            // A job already completed by a live Core read (see EnsureUpdateCompleted) has had its dynamic flags
            // read and reset in Core but not consumed yet, so this update's flags are merged into them instead
            // of replacing them. Otherwise the drawables that moved only in that update would keep a stale mesh.
            TaskableModel.TryWriteParametersAndParts(Parameters, Parts);
            TaskableModel.UpdateNow(_pendingFetchResults);


            WasJustEnabled = false;

            // Mark this frame ticked so the player-loop schedule phase does not update the model again.
            LastTick = Time.frameCount;


            FetchResults();

            // LastTick makes this frame's schedule phase skip the model, so the next complete phase has no job
            // to consume. Parameters changed after this call (e.g. by LateUpdate effects) would then never be
            // restored and additive effects would accumulate, so request the restore explicitly.
            _pendingParameterRestore = true;
        }

        /// <summary>
        /// Completes any in-flight update job so the native buffers are safe to read on the main thread.
        /// </summary>
        /// <remarks>
        /// Called by live Core readers (such as <see cref="CubismDrawable.VertexPositions"/>) before touching the
        /// native buffers, which the update job writes on a worker thread across the frame boundary. Only the
        /// worker is stopped here. The main thread does not wait for the results to be consumed (see <see cref="FetchResults"/>).
        /// </remarks>
        internal void EnsureUpdateCompleted()
        {
            if (!IsRevived || !TaskableModel.IsExecuting)
            {
                return;
            }

            TaskableModel.Complete();

            _pendingFetchResults = true;
        }

        /// <summary>
        /// Copies the native render orders into the managed snapshot exposed by <see cref="AllDrawObjectsRenderOrder"/>.
        /// </summary>
        /// <remarks>
        /// Must be called only when no update job is in flight (the native buffer is being read here).
        /// </remarks>
        private void CaptureRenderOrders()
        {
            if (!IsRevived)
            {
                return;
            }

            var renderOrders = TaskableModel.UnmanagedModel.AllDrawObjectRenderOrders;

            if (_allDrawObjectsRenderOrder == null || _allDrawObjectsRenderOrder.Length != renderOrders.Length)
            {
                _allDrawObjectsRenderOrder = new int[renderOrders.Length];
            }

            for (var i = 0; i < renderOrders.Length; i++)
            {
                _allDrawObjectsRenderOrder[i] = renderOrders[i];
            }
        }

        /// <summary>
        /// Updates the <see cref="CubismParameterStore"/> reference and refreshes its data.
        /// </summary>
        public void RefreshParameterStore()
        {
            // Get the CubismParameterStore component attached to this GameObject.
            _parameterStore = GetComponent<CubismParameterStore>();


            // Return early if empty.
            if (_parameterStore == null)
            {
                return;
            }


            // Update the CubismParameterStore with the current parameters and parts.
            _parameterStore.Refresh();
        }


#if UNITY_2018_1_OR_NEWER
        /// <summary>
        /// Player-loop schedule phase (after <see cref="PreLateUpdate"/>): every model writes its parameters and
        /// schedules its native update job, then the job batch is kicked so the workers start immediately.
        /// </summary>
        /// <remarks>
        /// The job scheduled here is completed at the start of the NEXT frame (see <see cref="OnModelsComplete"/>),
        /// so it runs across the frame boundary (this frame's remaining work, rendering and idle time) on a
        /// worker thread. The main thread therefore does not wait for the native update — the cost is hidden
        /// even for a single model — at the price of one frame of latency (the frame renders the previous frame's result).
        /// </remarks>
        private static void OnModelsUpdate()
        {
            _modelUpdateFunctions?.Invoke();

            // Kick the batched jobs so the workers begin the native updates now, during this frame's remaining
            // main-thread work and rendering, instead of only when the next frame completes them.
            JobHandle.ScheduleBatchedJobs();
        }

        /// <summary>
        /// Player-loop complete phase (at frame start, <see cref="PreUpdate"/>): every model completes the update it
        /// scheduled last frame — which by now has finished on a worker, so this rarely blocks — and consumes
        /// the results (raising <see cref="OnDynamicDrawableData"/>) before this frame's rendering.
        /// </summary>
        /// <remarks>
        /// Placed at frame start so completion (and the native reads it entails) happens before any main-thread
        /// code reads the model this frame (e.g. LateUpdate blend-color/pose reads), keeping the worker and the
        /// main thread from racing on the native model.
        /// </remarks>
        private static void OnModelsComplete()
        {
            _modelCompleteFunctions?.Invoke();
        }


        /// <summary>
        /// Register the model update functions into the player loop.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void RegisterCallbackFunction()
        {
            // Get the default player loop.
            var playerLoopSystem =
#if UNITY_2019_3_OR_NEWER
                PlayerLoop.GetCurrentPlayerLoop();
#else
                PlayerLoop.GetDefaultPlayerLoop();
#endif

            // Complete phase: at frame start ("PreUpdate"), before this frame's LateUpdate reads the model,
            // finish the update scheduled last frame (already done on a worker) and consume its results.
            var completed = TryAppendToPlayerLoopPhase(ref playerLoopSystem, typeof(PreUpdate),
                new PlayerLoopSystem { type = typeof(CubismModel), updateDelegate = OnModelsComplete });

            // Schedule phase: after "PreLateUpdate" (once this frame's parameters are applied), write and
            // schedule the native update. It is completed at the next frame's complete phase above.
            var scheduled = TryAppendToPlayerLoopPhase(ref playerLoopSystem, typeof(PreLateUpdate),
                new PlayerLoopSystem { type = typeof(CubismModel), updateDelegate = OnModelsUpdate });

            if (!completed || !scheduled)
            {
                Debug.LogError("CubismModel : Failed to add processing to PlayerLoop.");
                return;
            }

            PlayerLoop.SetPlayerLoop(playerLoopSystem);
        }

        /// <summary>
        /// Appends <paramref name="entry"/> to the sub-system list of the player-loop phase of type
        /// <paramref name="phaseType"/>.
        /// </summary>
        /// <param name="root">Player loop to modify in place.</param>
        /// <param name="phaseType">Top-level phase to append into (e.g. <see cref="PreLateUpdate"/>).</param>
        /// <param name="entry">Sub-system to append.</param>
        /// <returns><see langword="true"/> when the phase was found and the entry appended.</returns>
        private static bool TryAppendToPlayerLoopPhase(ref PlayerLoopSystem root, Type phaseType, PlayerLoopSystem entry)
        {
            for (var i = 0; i < root.subSystemList.Length; i++)
            {
                if (root.subSystemList[i].type != phaseType)
                {
                    continue;
                }

                var phase = root.subSystemList[i];
                var subSystemList = phase.subSystemList;

                Array.Resize(ref subSystemList, subSystemList.Length + 1);
                subSystemList[subSystemList.Length - 1] = entry;

                phase.subSystemList = subSystemList;
                root.subSystemList[i] = phase;

                return true;
            }

            return false;
        }
#endif

#region Unity Event Handling

        /// <summary>
        /// Called by Unity. Triggers <see langword="this"/> to update.
        /// </summary>
        private void Update()
        {
#if UNITY_2018_1_OR_NEWER
            if (!WasAttachedModelUpdateFunction)
            {
                // Two player-loop phases: schedule (write + Schedule) then complete (Complete + consume).
                _modelUpdateFunctions += OnModelSchedule;
                _modelCompleteFunctions += OnModelComplete;


                WasAttachedModelUpdateFunction = true;
            }

            // On 2018+ the update is scheduled (OnModelSchedule) and completed/consumed at the next frame's
            // start (OnModelComplete) via the player loop, so nothing is read here. The native update thus runs
            // on a worker across the frame boundary and is hidden from the main thread, at the cost of one
            // frame of latency.
#else
            // Legacy path (no player loop): consume the previous frame's results here.
            if (WasJustEnabled)
            {
                return;
            }

            FetchResults();
#endif
        }

        /// <summary>
        /// Reads back the completed update results and raises <see cref="OnDynamicDrawableData"/>.
        /// </summary>
        private void FetchResults()
        {
            // Return unless revived.
            if (!IsRevived)
            {
                return;
            }


            // Return if the backend has not produced a result yet.
            if (!TaskableModel.DidExecute)
            {
                return;
            }


            // The results are being consumed now, so clear any deferred-consume request.
            _pendingFetchResults = false;

            // Snapshot the render orders while the native buffers are stable (the update job has completed).
            CaptureRenderOrders();

            SyncParametersBack();

            // Trigger event.
            if (OnDynamicDrawableData == null)
            {
                return;
            }

            OnDynamicDrawableData(this, TaskableModel.DynamicDrawableData);
        }

        /// <summary>
        /// Reads the parameters back from the native model and restores the values saved by the <see cref="CubismParameterStore"/>.
        /// </summary>
        private void SyncParametersBack()
        {
            // The restore is being done now, so clear any deferred-restore request.
            _pendingParameterRestore = false;

            // Sync parameters back.
            TaskableModel.TryReadParameters(Parameters);

            // restore last frame parameters value and parts opacity.
            if (_parameterStore != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    _parameterStore.RestoreParameters();
                }
#else
                _parameterStore.RestoreParameters();
#endif
            }
        }


        /// <summary>
        /// Called by Unity. Legacy (pre-2018) update entry point.
        /// </summary>
        private void OnRenderObject()
        {
#if !UNITY_2018_1_OR_NEWER
            // No player loop available: update synchronously here. Results are consumed next frame in Update().
            if (!IsRevived)
            {
                return;
            }

            if (LastTick == Time.frameCount && Application.isPlaying)
            {
                return;
            }

            LastTick = Time.frameCount;

            TaskableModel.TryWriteParametersAndParts(Parameters, Parts);
            TaskableModel.UpdateNow();

            WasJustEnabled = false;
#endif
        }

        /// <summary>
        /// Player-loop phase 1: writes parameters/parts and schedules the native update on a worker thread.
        /// </summary>
        private void OnModelSchedule()
        {
            // Return unless revived.
            if (!IsRevived)
            {
                return;
            }


            // Return if already ticked this frame.
            if (LastTick == Time.frameCount && Application.isPlaying)
            {
                return;
            }

            LastTick = Time.frameCount;

            // Sync parameters and parts into the native model.
            TaskableModel.TryWriteParametersAndParts(Parameters, Parts);

            // First enabled frame: update synchronously and consume now, so this frame already shows valid
            // data instead of one frame of default pose before the 1-frame-lagged async pipeline fills in.
            if (WasJustEnabled)
            {
                TaskableModel.UpdateNow();
                WasJustEnabled = false;
                FetchResults();

                return;
            }

            // Schedule the native update job (runs on a worker thread, completed next frame in OnModelComplete).
            TaskableModel.Schedule();
        }

        /// <summary>
        /// Player-loop phase 2: completes the native update and consumes the results in the same frame.
        /// </summary>
        private void OnModelComplete()
        {
            if (!IsRevived)
            {
                return;
            }

            // Only consume results for a model that was actually scheduled this frame. This skips a model
            // already updated synchronously this frame (e.g. via ForceUpdateNow, which marks LastTick so the
            // schedule phase does nothing), avoiding a duplicate OnDynamicDrawableData and mesh swap.
            // A job completed early by a live Core read still has pending results to consume here.
            if (!TaskableModel.IsExecuting && !_pendingFetchResults)
            {
                // A model updated by ForceUpdateNow still needs its parameters restored once, as the
                // per-frame consume did before the update was split into the schedule / complete phases.
                if (_pendingParameterRestore)
                {
                    SyncParametersBack();
                }

                return;
            }

            // Complete the scheduled update (waits for the worker) and read the results.
            if (TaskableModel.IsExecuting)
            {
                TaskableModel.Complete();
            }

            WasJustEnabled = false;

            FetchResults();
        }

        /// <summary>
        /// Called by Unity. Revives instance.
        /// </summary>
        private void OnEnable()
        {
            WasJustEnabled = true;


            Revive();
        }

        private void OnDisable()
        {
#if UNITY_2018_1_OR_NEWER
            if (WasAttachedModelUpdateFunction)
            {
                _modelUpdateFunctions -= OnModelSchedule;
                _modelCompleteFunctions -= OnModelComplete;


                WasAttachedModelUpdateFunction = false;
            }

            // Finish any in-flight update job before this model stops being ticked, so a later release or
            // re-enable never races the worker. Complete without reading so the in-flight update's dynamic flags
            // survive. The next update after re-enable then refreshes every drawable that had moved.
            if (IsRevived)
            {
                TaskableModel.CompleteWithoutRead();
            }

            if (_pendingFetchResults)
            {
                FetchResults();
            }
#endif
        }

        /// <summary>
        /// Called by Unity. Releases unmanaged memory.
        /// </summary>
        private void OnDestroy()
        {
            if (!IsRevived)
            {
                return;
            }


            TaskableModel.ReleaseUnmanaged();


            TaskableModel = null;
        }

        /// <summary>
        /// Called by Unity. Triggers <see cref="OnEnable"/>.
        /// </summary>
        private void OnValidate()
        {
            OnEnable();
        }

#endregion
    }
}
