using NBG.Core.GameSystems;
using NBG.Entities;
using Recoil;
using Unity.Burst;


namespace Noodles
{
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsBeforeSolve))]
    public class NoodleExecuteSystem : JobQuerySystem<NoodleData, NoodleExecuteSystem.ExecuteImpl>
    {
        public NoodleExecuteSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                ref var articulation = ref Recoil.World.main.GetArticulation(entity.GetComponentData<ArticulationRef>().articulationId);
                Noodle.Execute(entity, articulation);
            }
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsAfterSolve))]
    public class NoodleExecuteAfterSolveSystem : JobQuerySystem<NoodleData, NoodleExecuteAfterSolveSystem.ExecuteImpl>
    {
        public NoodleExecuteAfterSolveSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                ref var articulation = ref Recoil.World.main.GetArticulation(entity.GetComponentData<ArticulationRef>().articulationId);
                Noodle.ExecuteAfterSolve(entity, articulation);
            }
        }
    }
    [UpdateInGroup(typeof(PhysicsBeforeVelocityIteration))]
    public class GetCarryInforFromArticulation : JobQuerySystem<NoodleData, GetCarryInforFromArticulation.ExecuteImpl>
    {
        public GetCarryInforFromArticulation()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                Noodle.ExecuteBeforeVelocityIteration(entity, Recoil.World.main.currentIteration);
                //ref var articulation = ref Recoil.World.main.GetArticulation(entity.GetComponentData<ArticulationRef>().articulationId);
                //if (Recoil.World.main.currentIteration > 0) { }
                ////Noodle.WriteCarryInfoToCG(articulation, ref entity.GetComponentData<CarryData>(), new NoodleJoints(articulation.solver.joints));
                //else
                //    entity.GetComponentData<CarryData>().l.undoneJointForce = entity.GetComponentData<CarryData>().r.undoneJointForce = float3.zero;
            }
        }
    }
    [UpdateInGroup(typeof(PhysicsAfterVelocityIteration))]
    public class NoodleAfterVeloctyIteration : JobQuerySystem<NoodleData, NoodleAfterVeloctyIteration.ExecuteImpl>
    {
        public NoodleAfterVeloctyIteration()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                Noodle.ExecuteAfterVelocityIteration(entity, Recoil.World.main.currentIteration);
            }
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsJacobians))]
    public class NoodleSuspensionCalculateJacobianSystem : JobQuerySystem<NoodleData, NoodleSuspensionCalculateJacobianSystem.ExecuteImpl>
    {
        public NoodleSuspensionCalculateJacobianSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                ref var suspension = ref entity.GetComponentData<NoodleSuspensionData>();
                ref var characterArticulation = ref entity.GetComponentData<ArticulationRef>();
                ref var articulation = ref Recoil.World.main.GetArticulation(characterArticulation.articulationId);

                NoodleSuspension.CalculateJacobian(ref suspension, articulation, entity.GetComponentData<NoodleDimensions>(), entity.GetComponentData<NoodleData>().grounded);
            }
        }
    }
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsAfterVelocityIteration))]
    public class NoodleSuspensionVelocityIterationSystem : JobQuerySystem<NoodleData, NoodleSuspensionVelocityIterationSystem.ExecuteImpl>
    {
        public NoodleSuspensionVelocityIterationSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Noodles.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public unsafe void ExecuteEntity(in EntityReference entity)
            {
                ref var suspension = ref entity.GetComponentData<NoodleSuspensionData>();
                ref var characterArticulation = ref entity.GetComponentData<ArticulationRef>();
                ref var articulation = ref Recoil.World.main.GetArticulation(characterArticulation.articulationId);

                NoodleSuspension.VelocityIteration(ref suspension, articulation, entity.GetComponentData<NoodleData>().grounded, entity.GetComponentData<NoodleData>().groundVelocity);
            }
        }
    }
}
