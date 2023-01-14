using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Core
{
    public enum CoroutineStatus
    {
        Running,
        Completed,
        Interrupted,
        Exception
    }

    public interface ICoroutine
    {
        string Name { get; }
        CoroutineStatus Status { get; }
        float DurationSeconds { get; }
        void Stop();
    }

    internal class ManagedCoroutine : ICoroutine
    {
        public MonoBehaviour owner;
        public IEnumerator job;
        public Coroutine coroutine;

        public string goPath;
        public float startTime;
        public float endTime;

        public string Name => job.ToString();
        public CoroutineStatus Status { get; internal set; }
        public float DurationSeconds => (endTime == 0.0f) ? Time.time - startTime : endTime - startTime;

        public void Stop()
        {
            Coroutines.StopManagedCoroutine(this);
        }
    }

    public static class Coroutines
    {
        public const string LoggerScopeName = "Coroutines";
        static Logger log = new Logger(LoggerScopeName);

        // Leave coroutine wrappers on the list for some time after coroutines complete or are stopped for debugging purposes
        public static float LingerSeconds { get; set; } = 0.0f;

        public static IEnumerable<ICoroutine> All => _mcs;

        private static List<ManagedCoroutine> _mcs = new List<ManagedCoroutine>(64); // Removed after LingerSeconds
        private static CoroutineController _controller;

        const string kGameObjectName = "NBG_COROUTINES";

        static Coroutines()
        {
            // Allow trace logging
            var logger = Core.Log.GetOrCreateBackend(LoggerScopeName);
            logger.Level = LogLevel.Trace;

            // Setup the global controller
            var go = GameObject.Find(kGameObjectName);
            if (go == null)
            {
                go = new GameObject(kGameObjectName);
                go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
                _controller = go.AddComponent<CoroutineController>(); //Note: CoroutineController calls DontDestroyOnLoad
            }
            else
            {
                _controller = go.GetComponent<CoroutineController>();
            }

            Debug.Assert(_controller != null);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
#endif
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChange(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingEditMode || stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                int count = _mcs.Count;
                for (int i = count - 1; i >= 0; i--)
                {
                    var mc = _mcs[i];
                    if (mc.Status == CoroutineStatus.Running)
                        FinalizeManagedCoroutine(mc, CoroutineStatus.Interrupted);
                }
                _mcs.Clear();
            }
        }
#endif

        private static IEnumerator Run(ManagedCoroutine mc)
        {
            bool running = true;
            bool exception = false;
            do
            {
                try
                {
                    var suspended = false;
                    var custom = (mc.job.Current as CustomYieldInstruction);
                    if (custom != null)
                    {
                        suspended = custom.keepWaiting;
                    }
                    else if (mc.job.Current is IEnumerator)
                    {
                        throw new System.InvalidOperationException($"ManagedCoroutines don't support nesting IEnumerator {mc.job.Current} @ {mc.job}. Start a coroutine instead.");
                    }

                    if (!suspended)
                        running = mc.job.MoveNext();
                }
                catch (System.Exception e)
                {
                    running = false;
                    exception = true;
                    log.LogException(e, mc.owner);
                }

                if (running)
                {
                    var instruction = mc.job.Current as YieldInstruction;
                    yield return instruction;
                }
            }
            while (running);

            FinalizeManagedCoroutine(mc, exception ? CoroutineStatus.Exception : CoroutineStatus.Completed);

            var duration = mc.endTime - mc.startTime;
            log.LogTrace($"Finished coroutine {mc.job} on {mc.goPath} after {duration}s on frame {Time.frameCount}.");
        }

        public static ICoroutine StartManagedCoroutine(IEnumerator job)
        {
            return _controller.StartManagedCoroutine(job);
        }

        public static ICoroutine StartManagedCoroutine(this MonoBehaviour mb, IEnumerator job)
        {
            var mc = new ManagedCoroutine();
            _mcs.Add(mc);

            mc.Status = CoroutineStatus.Running;
            mc.owner = mb;
            mc.job = job;
            mc.goPath = mb.gameObject.GetFullPath();
            mc.startTime = Time.time;

            log.LogTrace($"Starting coroutine {job} on {mc.goPath} on frame {Time.frameCount}");

            var wrapper = Run(mc);
            mc.coroutine = mb.StartCoroutine(wrapper);

            if (mc.Status == CoroutineStatus.Running)
                log.LogTrace($"Started coroutine {job} on {mc.goPath} on frame {Time.frameCount}");

            return mc;
        }

        internal static void StopManagedCoroutine(ManagedCoroutine mc)
        {
            var interrupted = (mc.Status == CoroutineStatus.Running);
            mc.owner.StopCoroutine(mc.coroutine);

            FinalizeManagedCoroutine(mc, CoroutineStatus.Interrupted);

            var duration = mc.endTime - mc.startTime;
            if (interrupted)
                log.LogTrace($"Interrupted coroutine {mc.job} on {mc.goPath} after {duration}s on frame {Time.frameCount}.");
        }

        internal static void FinalizeManagedCoroutine(ManagedCoroutine mc, CoroutineStatus status)
        {
            mc.endTime = Time.time;
            mc.Status = status;

            if (LingerSeconds == 0.0f)
            {
                var removed = _mcs.Remove(mc);
                Assert.IsTrue(removed);
            }
        }

        public static void StopManagedCoroutines(this MonoBehaviour mb)
        {
            var owned = _mcs.Where(mc => mc.owner == mb);
            foreach (var mc in owned)
            {
                StopManagedCoroutine(mc);
            }
        }

        public static void Update()
        {
            if (LingerSeconds == 0.0f)
                return;

            var time = Time.time;

            int i = 0;
            while (i < _mcs.Count)
            {
                var mc = _mcs[i];

                if (mc.Status == CoroutineStatus.Running)
                {
                    i++;
                    continue;
                }

                if (time - mc.endTime < LingerSeconds)
                {
                    i++;
                    continue;
                }

                _mcs.RemoveAt(i);
            }
        }
    }
}
