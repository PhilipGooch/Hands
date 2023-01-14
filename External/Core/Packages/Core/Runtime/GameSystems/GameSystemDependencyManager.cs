using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace NBG.Core.GameSystems
{
    /// <summary>
    /// The GameSystemDependencyManager maintains JobHandles for each type with any jobs that read or write those types.
    /// Reading depends on the last write.
    /// Writing depends on the last write and all reads since.
    ///
    /// Note that current implementation assumes one instance per job data type per world.
    /// </summary>
    internal class GameSystemDependencyManager
    {
        class TypeState
        {
            public JobHandle writeFence = new JobHandle();
            public JobHandle combinedReads = new JobHandle();
        }

        Dictionary<Type, TypeState> _typeStates = new Dictionary<Type, TypeState>();

        TypeState GetOrCreate(Type type)
        {
            TypeState ret;
            if (!_typeStates.TryGetValue(type, out ret))
            {
                ret = new TypeState();
                _typeStates.Add(type, ret);
            }
            return ret;
        }

        public JobHandle GetReadingDependency(Type type)
        {
            var state = GetOrCreate(type);
            return state.writeFence;
        }

        public JobHandle GetWritingDependency(Type type)
        {
            var state = GetOrCreate(type);
            return JobHandle.CombineDependencies(state.combinedReads, state.writeFence);
        }

        public void AddReadingDependency(Type type, JobHandle handle)
        {
            var state = GetOrCreate(type);
            state.combinedReads = JobHandle.CombineDependencies(state.combinedReads, handle);
        }

        public void AddWritingDependency(Type type, JobHandle handle)
        {
            var state = GetOrCreate(type);
            state.writeFence = JobHandle.CombineDependencies(state.writeFence, handle); //TODO: can we just overwrite writeFence, or does that break the dependency graph?
            //state.readFence = new JobHandle(); //TODO: do we care about invalidating this?
        }

        public void CompleteDependencies(Type type)
        {
            var state = GetOrCreate(type);
            state.writeFence.Complete();
            state.combinedReads.Complete();
        }

        public void CompleteAll()
        {
            foreach (var state in _typeStates.Values)
            {
                state.writeFence.Complete();
                state.combinedReads.Complete();
            }
        }
    }
}
