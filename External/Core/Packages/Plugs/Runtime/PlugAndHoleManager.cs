using NBG.Core.GameSystems;
using NBG.Entities;
using Recoil;
using Unity.Burst;
using UnityEngine.Profiling;

namespace Plugs
{
    public sealed class GlobalJobData // Used for GameSystem job dependencies
    {
    }

    //[ExecuteIn(typeof(PhysicsBeforeSolve))]
    //[ExecuteBefore(typeof(PlugAndSocketSystem))]
    //public class PlugAndSocketBuildConstraintsSystem : QuerySystem<PlugAndSocketData>
    //{
    //    public override void Execute(EntityReference entity)
    //    {
    //        Profiler.BeginSample("PlugAndSocketBuildConstraintsSystem");
    //        //ref var data = ref entity.GetComponentData<PlugAndSocketData>();
    //        var plug = entity.GetComponentObject<Plug>();
    //        plug.BuildConstraints();
    //        Profiler.EndSample();

    //    }
    //}

    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsBeforeSolve))]
    public class PlugAndHoleCollectEventsSystem : JobQuerySystem<PlugAndHoleData, PlugAndHoleCollectEventsSystem.ExecuteImpl>
    {
        public PlugAndHoleCollectEventsSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Plugs.GlobalJobData));
        }

        public struct ExecuteImpl : IExecutyEntity
        {
            public void ExecuteEntity(in EntityReference entity)
            {
                //ProfilerMarkers.instance.profilePlugAndSocket.Begin();

                ref var data = ref entity.GetComponentData<PlugAndHoleData>();
                Plug.CollectEvents(ref data);
                //ProfilerMarkers.instance.profilePlugAndSocket.End();
            }
        }
    }
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(WriteState))]
    public class PlugAndHoleExecuteEventsSystem : QuerySystem<PlugAndHoleData>
    {
        public PlugAndHoleExecuteEventsSystem()
        {
            ReadsData(typeof(Recoil.WorldJobData));
            WritesData(typeof(Plugs.GlobalJobData));
        }

        public override void Execute(EntityReference entity)
        {
            Profiler.BeginSample("PlugAndHoleExecuteEventsSystem");
            //ref var data = ref entity.GetComponentData<PlugAndSocketData>();
            var plug = entity.GetComponentObject<Plug>();
            plug.ExecuteEvents();
            Profiler.EndSample();
        }
    }
}