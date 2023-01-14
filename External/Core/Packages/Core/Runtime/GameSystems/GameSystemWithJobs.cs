using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace NBG.Core.GameSystems
{
    /// <summary>
    /// System that will only run on the main thread and might also schedule jobs.
    /// </summary>
    public abstract class GameSystemWithJobs : GameSystemBase
    {
        JobHandle BeforeOnUpdate()
        {
            if (AlwaysSynchronizeWorld)
            {
                World.DependencyManager.CompleteAll();
                return default;
            }
            else if (AlwaysSynchronizeSystem)
            {
                CompleteDependencies();
                return default;
            }

            var dependencies = new JobHandle();

            foreach (var type in _reads)
            {
                var dep = World.DependencyManager.GetReadingDependency(type);
                dependencies = JobHandle.CombineDependencies(dependencies, dep);
            }

            foreach (var type in _writes)
            {
                var dep = World.DependencyManager.GetWritingDependency(type);
                dependencies = JobHandle.CombineDependencies(dependencies, dep);
            }

            return dependencies;
        }

        void AfterOnUpdate(JobHandle outputJob)
        {
            CheckedState().lastJobHandle = outputJob;

            foreach (var type in _reads)
            {
                World.DependencyManager.AddReadingDependency(type, outputJob);
            }

            foreach (var type in _writes)
            {
                World.DependencyManager.AddWritingDependency(type, outputJob);
            }

            JobHandle.ScheduleBatchedJobs(); //TODO: optimize

            if (AlwaysCompleteWorldAfterUpdate)
            {
                World.DependencyManager.CompleteAll();
            }
        }

        public sealed override void Update()
        {
            var state = CheckedState();

            if (Enabled && ShouldRunSystem())
            {
                if (!state.previouslyEnabled)
                {
                    state.previouslyEnabled = true;
                    OnStartRunning();
                }

                var inputDeps = BeforeOnUpdate();
                var outputJob = new JobHandle();

                try
                {
                    outputJob = OnUpdate(inputDeps);
                }
                catch
                {
                    AfterOnUpdate(outputJob);
                    throw;
                }
                finally
                {
                }

                AfterOnUpdate(outputJob);
            }
            else if (state.previouslyEnabled)
            {
                state.previouslyEnabled = false;
                OnStopRunning();
            }
        }

        protected override void OnDestroy()
        {
            CheckedState().lastJobHandle.Complete();
        }

        internal override void OnBeforeCreateInternal(GameSystemWorld world)
        {
            AlwaysSynchronizeSystem = GetType().GetCustomAttributes(typeof(AlwaysSynchronizeSystemAttribute), true).Length != 0;
            AlwaysSynchronizeWorld = GetType().GetCustomAttributes(typeof(AlwaysSynchronizeWorldAttribute), true).Length != 0;
            AlwaysCompleteWorldAfterUpdate = false;
        }

        /// <summary>Implement OnUpdate to perform the major work of this system.</summary>
        /// <remarks>
        /// The system invokes OnUpdate once per frame on the main thread.
        ///
        /// To run a Job, create an instance of the Job struct, assign appropriate values to the struct fields and call
        /// one of the Job schedule functions. The system passes any current dependencies between Jobs. Your function
        /// must combine the input dependencies with any dependencies of the Jobs created in OnUpdate and return the
        /// combined <see cref="JobHandle"/> object.
        /// </remarks>
        /// <param name="inputDeps">Existing dependencies for this system.</param>
        /// <returns>A Job handle that contains the dependencies of the Jobs in this system.</returns>
        protected abstract JobHandle OnUpdate(JobHandle inputDeps);
    }
}
