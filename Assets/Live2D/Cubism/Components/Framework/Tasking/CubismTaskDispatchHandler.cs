/*
 * Copyright(c) Live2D Inc. All rights reserved.
 * 
 * Use of this source code is governed by the Live2D Open Software license
 * that can be found at http://live2d.com/eula/live2d-open-software-license-agreement_en.html.
 */


using Live2D.Cubism.Core;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace Live2D.Cubism.Framework.Tasking
{
    /// <summary>
    /// Spawns async workers for <see cref="ICubismTask"/>s.
    /// </summary>
    public class CubismTaskDispatchHandler : MonoBehaviour
    {
        /// <summary>
        /// Number of workers to spawn.
        /// </summary>
        /// <remarks>
        /// The number of workers actually spawned is the number requested by the first instance instantiated.
        /// </remarks>
        public int WorkerCount = 1;


        /// <summary>
        /// The number of active workers.
        /// </summary>
        public int ActiveWorkerCount
        {
            get
            {
                return (AreWorkersInitialized)
                    ? Workers.Length
                    : 0;
            }
        }
        
        #region Unity Event Handling

        /// <summary>
        /// Requires workers.
        /// </summary>
        private void Start()
        {
            TryInitializeWorkers(WorkerCount);
        }

        /// <summary>
        /// Unrequires workers.
        /// </summary>

        private void OnDestroy()
        {
            DeinitializeWorkers();
        }

        #endregion

        #region Workers

        /// <summary>
        /// <see cref="ICubismTask"/>s waiting for execution.
        /// </summary>
        private static Queue<ICubismTask> Tasks { get; set; }

        /// <summary>
        /// Background worker threads.
        /// </summary>
        private static Thread[] Workers { get; set; }

        /// <summary>
        /// Lock for syncing access to <see cref="Tasks"/> and <see cref="CallItADay"/>.
        /// </summary>
        private static object Lock { get; set; }

        /// <summary>
        /// Signal for waking up workers.
        /// </summary>
        private static ManualResetEvent Signal { get; set; }

        /// <summary>
        /// Reference count used for freeing workers.
        /// </summary>
        private static int ReferenceCount { get; set; }


        /// <summary>
        /// <see cref="CallItADay"/> backing field. ALWAYS ACCESS THROUGH PROPERTY!
        /// </summary>
        private static bool _callItADay = false;

        /// <summary>
        /// True if workers should exit.
        /// </summary>
        private static bool CallItADay
        {
            get
            {
                lock (Lock)
                {
                    return _callItADay;
                }
            }
            set
            {
                lock (Lock)
                {
                    _callItADay = value;
                }
            }
        }

        /// <summary>
        /// True if workers are initialized.
        /// </summary>
        private static bool AreWorkersInitialized
        {
            get { return ReferenceCount > 0; }
        }


        /// <summary>
        /// Initializes workers.
        /// </summary>
        /// <param name="workerCount">Number of workers to initialize.</param>
        private static void TryInitializeWorkers(int workerCount)
        {
            // Return early if already initialized.
            if (AreWorkersInitialized)
            {
                ++ReferenceCount;


                return;
            }


            // Initialize fields.
            Tasks = new Queue<ICubismTask>();
            Workers = new Thread[workerCount];
            Lock = new object();
            Signal = new ManualResetEvent(false);
            ReferenceCount = 1;
            CallItADay = false;


            // Become handler.
            CubismTaskQueue.OnTask = EnqueueTask;


            // Start workers.
            for (var i = 0; i < Workers.Length; ++i)
            {
                Workers[i] = new Thread(Work);


                Workers[i].Start();
            }
        }

        private static void DeinitializeWorkers()
        {
            // Return early if not initialized.
            if (Tasks == null)
            {
                return;
            }


            // Return early if still referenced.
            --ReferenceCount;


            if (ReferenceCount > 0)
            {
                return;
            }


            // Unbecome handler.
            CubismTaskQueue.OnTask = null;


            // Stop workers.
            CallItADay = true;


            for (var i = 0; i < Workers.Length; ++i)
            {
                Signal.Set();
                Workers[i].Join();
            }


            // Reset fields
            Tasks = null;
            Workers = null;
            Lock = null;
            Signal = null;
            ReferenceCount = 0;
        } 


        /// <summary>
        /// Enqueues a new task.
        /// </summary>
        /// <param name="task">Task to enqueue.</param>
        private static void EnqueueTask(ICubismTask task)
        {
            lock (Lock)
            {
                Tasks.Enqueue(task);
                Signal.Set();
            }
        }

        /// <summary>
        /// Dequeues a task.
        /// </summary>
        /// <returns>A valid task on success; <see langword="null"/> otherwise.</returns>
        private static ICubismTask DequeueTask()
        {
            lock (Lock)
            {
                return (Tasks.Count > 0)
                    ? Tasks.Dequeue()
                    : null;
            }
        }


        /// <summary>
        /// Entry point for workers.
        /// </summary>
        private static void Work()
        {
            while (!CallItADay)
            {
                // Try to dequeue a task.
                var task = DequeueTask();


                // Execute task if available.
                if (task != null)
                {
                  task.Execute();
                }


                // Wait for a task to become available.
                else
                {
                    Signal.WaitOne();
                    Signal.Reset();
                }
            }
        }

        #endregion
    }
}
