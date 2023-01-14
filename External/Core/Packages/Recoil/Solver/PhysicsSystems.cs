using NBG.Core;
using NBG.Core.GameSystems;
using NBG.Entities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Recoil
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    [UpdateBefore(typeof(AfterPhysicsSystemGroup))]
    [UpdateAfter(typeof(OnFixedUpdateSystem))]
    public class PhysicsSystemGroup : GameSystemGroup { }

    [DisableAutoRegistration]
    public class ReadState : GameSystemWithJobs
    {
        public ReadState()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return ManagedWorld.main.ScheduleRead(dependsOn);
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    public class PhysicsBeforeSolve : GameSystemGroup
    {
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsBeforeSolve))]
    public class PhysicSolve : GameSystemGroup
    {
    }

    [UpdateInGroup(typeof(PhysicSolve))]
    public class BuildArticulationJacobians : GameSystemWithJobs
    {
        public BuildArticulationJacobians()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return new ArticulationJacobianJob().Schedule(dependsOn);
        }
    }
    [UpdateInGroup(typeof(PhysicSolve))]
    [UpdateAfter(typeof(BuildArticulationJacobians))]
    public class PhysicsJacobians : GameSystemGroup
    {
    }

    [UpdateInGroup(typeof(PhysicsJacobians))]
    public class BuildConstraintJacobians : GameSystemWithJobs
    {
        public BuildConstraintJacobians()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return new ConstraintJacobianJob().Schedule(dependsOn);
        }
    }



    [UpdateInGroup(typeof(PhysicSolve))]
    [UpdateAfter(typeof(PhysicsJacobians))]
    [UpdateBefore(typeof(ExecuteVelocityIterations))]
    public class PhysicsAfterJacobian : GameSystemGroup
    {
    }

    #region Velocity iterations
    [UpdateInGroup(typeof(PhysicSolve))]
    [UpdateAfter(typeof(PhysicsJacobians))]
    public class ExecuteVelocityIterations : GameSystemGroup
    {
        protected override void OnUpdate()
        {
            var group = World.GetExistingSystem<PhysicsVelocityIterationSystemGroup>();

            if (Recoil.World.main.iterations > 0)
            {
                var iterationReset = World.GetExistingSystem<PhysicsVelocityIterationReset>();
                iterationReset.Update();
                group.Update();
            }

            for (int i = 1; i < Recoil.World.main.iterations; ++i)
            {
                var iterationIncrement = World.GetExistingSystem<PhysicsVelocityIterationIncrement>();
                iterationIncrement.Update();
                group.Update();
            }
        }
    }

    [DisableAutoRegistration]
    public class PhysicsVelocityIterationSystemGroup : GameSystemGroup { }

    [DisableAutoRegistration]
    public class PhysicsVelocityIterationReset : GameSystemWithJobs
    {
        public PhysicsVelocityIterationReset()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new ResetIteration().Schedule(inputDeps);
            return job;
        }

        [BurstCompile]
        struct ResetIteration : IJob
        {
            public void Execute()
            {
                Recoil.World.main.currentIteration = 0;
            }
        }
    }

    [DisableAutoRegistration]
    public class PhysicsVelocityIterationIncrement : GameSystemWithJobs
    {
        public PhysicsVelocityIterationIncrement()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var job = new IncrementIteration().Schedule(inputDeps);
            return job;
        }

        [BurstCompile]
        struct IncrementIteration : IJob
        {
            public void Execute()
            {
                Recoil.World.main.currentIteration++;
            }
        }
    }
    #endregion

    [UpdateInGroup(typeof(PhysicSolve))]
    [UpdateAfter(typeof(ExecuteVelocityIterations))]
    public class FrictionAndSelfAligning : GameSystemWithJobs
    {
        public FrictionAndSelfAligning()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return new ManagedWorld.FricitonAndSelfAlignJob().Schedule(Recoil.World.main.count, 1, dependsOn);
        }
    }

    [UpdateInGroup(typeof(PhysicSolve))]
    [UpdateAfter(typeof(FrictionAndSelfAligning))]
    public class LimitArticulationVelocity : GameSystemWithJobs
    {
        public LimitArticulationVelocity()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return new LimitArticulationVelocityJob().Schedule(dependsOn);
        }

        [BurstCompile]
        struct LimitArticulationVelocityJob : IJobParallelFor
        {
            // references to world data

            public unsafe void Execute(int index)
            {
                ref var articulation = ref Recoil.World.main.GetArticulation(index);
                if (articulation.destroyed) return;
                Recoil.World.main.LimitArticulationVelocity(ref articulation);
            }

            public unsafe JobHandle Schedule(JobHandle dependsOn = default)
            {
                return this.Schedule(Recoil.World.main.articulationCount, 1, dependsOn);
            }

        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicSolve))]
    public class PhysicsAfterSolve : GameSystemGroup
    {
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsAfterSolve))]
    [AlwaysSynchronizeSystem]
    public class WriteState : GameSystemWithJobs
    {
        public WriteState()
        {
            ReadsData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            ManagedWorld.main.WriteState();
            return dependsOn;
        }
    }

    [UpdateInGroup(typeof(PhysicsVelocityIterationSystemGroup))]
    [UpdateBefore(typeof(VelocityIteration))]
    public class PhysicsBeforeVelocityIteration : GameSystemGroup
    {
    }

    [UpdateInGroup(typeof(PhysicsVelocityIterationSystemGroup))]
    public class VelocityIteration : GameSystemWithJobs
    {
        public VelocityIteration()
        {
            WritesData(typeof(WorldJobData));
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            dependsOn = new ArticulationVelocityIterationJob().Schedule(dependsOn);
            dependsOn = new ConstraintVelocityIterationJob().Schedule(dependsOn);
            return dependsOn;
        }
    }

    [UpdateInGroup(typeof(PhysicsVelocityIterationSystemGroup))]
    [UpdateAfter(typeof(VelocityIteration))]
    public class PhysicsAfterVelocityIteration : GameSystemGroup
    {
    }
}
