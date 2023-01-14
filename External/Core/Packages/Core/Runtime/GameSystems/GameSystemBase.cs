using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace NBG.Core.GameSystems
{
    public class SystemState
    {
        public bool enabled;
        public bool shouldRunSystem = true;
        public bool previouslyEnabled;
        public GameSystemWorld world;
        public JobHandle lastJobHandle;

        public static SystemState CreateDefault(GameSystemWorld world)
        {
            var ss = new SystemState();
            ss.enabled = true;
            ss.shouldRunSystem = true;
            ss.world = world;
            return ss;
        }
    }

    public abstract class GameSystemBase
    {
        internal SystemState _state;

        internal SystemState CheckedState()
        {
            if (_state == null)
            {
                throw new InvalidOperationException("object is not initialized or has already been destroyed");
            }
            return _state;
        }

        /// <summary>
        /// Does this system execute when its OnUpdate function is called?
        /// </summary>
        /// <value>True, if the system is enabled.</value>
        /// <remarks>A system with Enabled set to false will not update, even if its <see cref="ShouldRunSystem"/> function returns true.</remarks>
        public bool Enabled { get => CheckedState().enabled; set => CheckedState().enabled = value; }

        public bool ShouldRunSystem() => CheckedState().shouldRunSystem;

        /// <summary>
        /// Synchronizes all dependencies before updating when true.
        /// </summary>
        public bool AlwaysSynchronizeSystem { get; protected set; }

        /// <summary>
        /// Synchronizes all world jobs before updating, not just declared dependencies.
        /// </summary>
        public bool AlwaysSynchronizeWorld { get; protected set; }

        /// <summary>
        /// Completes all world jobs after updating.
        /// </summary>
        public bool AlwaysCompleteWorldAfterUpdate { get; protected set; }

        protected List<Type> _reads = new List<Type>();
        protected List<Type> _writes = new List<Type>();
        internal IReadOnlyList<Type> Reads => _reads;
        internal IReadOnlyList<Type> Writes => _writes;

        public JobHandle LastJobHandle => CheckedState().lastJobHandle;

        /// <summary>
        /// The World in which this system exists.
        /// </summary>
        /// <value>The World of this system.</value>
        public GameSystemWorld World => CheckedState().world;

        /// <summary>
        /// Called when this system is created.
        /// </summary>
        /// <remarks>
        /// Implement an OnCreate() function to set up system resources when it is created.
        ///
        /// OnCreate is invoked before the first time <see cref="OnStartRunning"/> and <see cref="OnUpdate"/> are invoked.
        /// </remarks>
        protected virtual void OnCreate()
        {
        }

        /// <summary>
        /// Called before the first call to OnUpdate and when a system resumes updating after being stopped or disabled.
        /// </summary>
        protected virtual void OnStartRunning()
        {
        }

        /// <summary>
        /// Called when this system stops running because you change the system <see cref="Enabled"/> property to false.
        /// </summary>
        protected virtual void OnStopRunning()
        {
        }

        internal virtual void OnStopRunningInternal()
        {
            OnStopRunning();
        }

        /// <summary>
        /// Called when this system is destroyed.
        /// </summary>
        /// <remarks>Systems are destroyed when the application shuts down.
        /// In the Unity Editor, system destruction occurs when you exit Play Mode and when scripts are reloaded.</remarks>
        protected virtual void OnDestroy()
        {
        }

        internal void OnDestroy_Internal()
        {
            OnDestroy();
        }

        /// <summary>
        /// Executes the system immediately.
        /// </summary>
        abstract public void Update();



        internal virtual void OnBeforeCreateInternal(GameSystemWorld world)
        {
        }

        internal void CreateInstance(GameSystemWorld world, SystemState newState)
        {
            _state = newState;
            OnBeforeCreateInternal(world);
            try
            {
                OnCreate();
            }
            catch
            {
                OnBeforeDestroyInternal();
                OnAfterDestroyInternal();
                throw;
            }
        }

        internal void DestroyInstance()
        {
            OnBeforeDestroyInternal();
            OnDestroy();
            OnAfterDestroyInternal();
        }

        internal virtual void OnBeforeDestroyInternal()
        {
            var state = CheckedState();

            if (state.previouslyEnabled)
            {
                state.previouslyEnabled = false;
                OnStopRunning();
            }
        }

        internal void OnAfterDestroyInternal()
        {
            _state = null;
        }

        /// <summary>
        /// Declare that some job data is required for reading.
        /// Will extablish a job dependency on the last write.
        /// </summary>
        /// <param name="type">Job data type</param>
        protected void ReadsData(Type type)
        {
            if (_reads.Contains(type))
                throw new InvalidOperationException($"{GetType().Name} already reads {type.Name}.");
            if (_writes.Contains(type))
                throw new InvalidOperationException($"{GetType().Name} already declared to write {type.Name}.");
            _reads.Add(type);
        }

        /// <summary>
        /// Declare that some data will be written. 
        /// Will establish a job dependency on all current reads and the last write.
        /// </summary>
        /// <param name="type">Job data type</param>
        protected void WritesData(Type type)
        {
            if (_writes.Contains(type))
                throw new InvalidOperationException($"{GetType().Name} already writes {type.Name}.");
            if (_reads.Contains(type))
                throw new InvalidOperationException($"{GetType().Name} already declared to read {type.Name}.");
            _writes.Add(type);
        }

        /// <summary>
        /// Complete all declared read and write dependencies.
        /// </summary>
        protected void CompleteDependencies()
        {
            foreach (var type in _reads)
                World.DependencyManager.CompleteDependencies(type);

            foreach (var type in _writes)
                World.DependencyManager.CompleteDependencies(type);
        }

        protected static string GetDebugSystemStatusPrefix(GameSystemBase system)
        {
            return string.Format("{0}", system.Enabled ? '+' : '-');
        }

        public virtual bool DebugContainsRecursive(GameSystemBase system)
        {
            return false;
        }

        public virtual void DebugPrint(System.Text.StringBuilder sb, int indent)
        {
            var desc = new string(' ', indent * 4);
            desc += $"[{GetDebugSystemStatusPrefix(this)}] {GetType().FullName}";
            DebugAppendDeps(ref desc);
            sb.AppendLine(desc);
        }

        protected void DebugAppendDeps(ref string desc)
        {
            var hasDeps = (Reads.Count > 0 || Writes.Count > 0);
            var force = (AlwaysSynchronizeWorld || AlwaysCompleteWorldAfterUpdate);
            if (!force && !hasDeps)
                return;

            desc = desc.PadRight(80, ' ');
            desc += "| ";
            foreach (var type in Reads)
                desc += $"Reads {type.FullName}, ";
            foreach (var type in Writes)
                desc += $"Writes {type.FullName}, ";
            if (AlwaysSynchronizeWorld)
                desc += $"Synchronizes world (all jobs), ";
            else if (AlwaysSynchronizeSystem && hasDeps)
                desc += $"Synchronizes dependencies, ";
            if (AlwaysCompleteWorldAfterUpdate)
                desc += $"Completes world (all jobs).";
        }
    }
}
