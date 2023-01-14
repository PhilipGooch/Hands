using NBG.Core.GameSystems;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine.Scripting;

[assembly: Preserve] // IL2CPP

namespace NBG.Entities
{
    public abstract class QuerySystemBase<T> : GameSystemWithJobs where T : unmanaged
    {
        private EntityQuery query;

        protected override void OnCreate()
        {
            base.OnCreate();
            query = EntityStore.Query<T>();
        }

        protected sealed override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return Execute(query.Execute(), dependsOn);
        }

        public abstract JobHandle Execute(EntityQueryResults results, JobHandle dependsOn);
    }

    public abstract class QuerySystemBase<T1,T2> : GameSystemWithJobs
        where T1 : unmanaged
        where T2 : unmanaged
    {
        private EntityQuery query;

        protected override void OnCreate()
        {
            base.OnCreate();
            query = EntityStore.Query<T1,T2>();
        }

        protected sealed override JobHandle OnUpdate(JobHandle dependsOn)
        {
            return Execute(query.Execute(), dependsOn);
        }

        public abstract JobHandle Execute(EntityQueryResults results, JobHandle dependsOn);
    }

    public abstract class QuerySystem<TData> : QuerySystemBase<TData> where TData: unmanaged
    {
        public abstract void Execute(EntityReference entity);

        public sealed override JobHandle Execute(EntityQueryResults results, JobHandle dependsOn)
        {
            for (int i = 0; i < results.count; i++)
                Execute(results.GetEntity(i));
            return dependsOn;
        }
    }

    public interface IExecutyEntity
    {
        public void ExecuteEntity(in EntityReference entity);
    }

    public interface IExecutyEntityPayload<TPayload> where TPayload : unmanaged
    {
        public void ExecuteEntity(in EntityReference entity, in TPayload payload);
    }

    public abstract class JobQuerySystem<TData, TExecute> : QuerySystemBase<TData> where TData : unmanaged
        where TExecute : unmanaged, IExecutyEntity
    {

        public sealed override JobHandle Execute(EntityQueryResults results, JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(default(JobHandle), dependsOn);
            return new JobQuerySystemJob(results).Schedule(results.count, 1, dependsOn);
        }


        [BurstCompile(CompileSynchronously = true)]
        public unsafe struct JobQuerySystemJob : IJobParallelFor
        {
            EntityQueryResults results;
            public JobQuerySystemJob(EntityQueryResults results) 
            { 
                this.results = results; 
            }

            public void Execute(int index)
            {
                default(TExecute).ExecuteEntity(results.GetEntity(index));
            }
        }
    }

    public abstract class JobQuerySystem<TData, TPayload, TExecute> : QuerySystemBase<TData> where TData : unmanaged where TPayload : unmanaged
        where TExecute: unmanaged, IExecutyEntityPayload<TPayload>
    {
        protected abstract TPayload GetPayload();
        public sealed override JobHandle Execute(EntityQueryResults results, JobHandle dependsOn)
        {
            return new JobQuerySystemJob()
            {
                results = results,
                payload = GetPayload()
            }.Schedule(results.count, 1, dependsOn);
        }


        [BurstCompile(CompileSynchronously = true)]
        public unsafe struct JobQuerySystemJob : IJobParallelFor
        {
            public EntityQueryResults results;
            public TPayload payload;

            public void Execute(int index)
            {
                default(TExecute).ExecuteEntity(results.GetEntity(index), payload);
            }
        }
    }
}
