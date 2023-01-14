using NBG.Core.GameSystems;
using Recoil;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using NBG.Core.Events;
using NBG.Net;
using Unity.Jobs;
using Unity.Collections;

namespace NBG.Water
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsBeforeSolve))]
    public class WaterSystem : GameSystemWithJobs
    {
        class FloatingMeshEntry
        {
            public IFloatingMesh target;
            public IFloatingMeshBackend system;
        }
        List<FloatingMeshEntry> floatingMeshes = new List<FloatingMeshEntry>();

        public WaterSystem()
        {
            WritesData(typeof(Recoil.WorldJobData));
        }

        protected override void OnCreate()
        {
            EventBus.Get().Register<NetworkAuthorityChangedEvent>(OnNetworkAuthorityChangedEvent);
        }

        protected override void OnDestroy()
        {
            EventBus.Get().Unregister<NetworkAuthorityChangedEvent>(OnNetworkAuthorityChangedEvent);
        }

        void OnNetworkAuthorityChangedEvent(NetworkAuthorityChangedEvent evt)
        {
            switch (evt.networkAuthority)
            {
                case NetworkAuthority.Server:
                    Enabled = true;
                    break;
                case NetworkAuthority.Client:
                    Enabled = false;
                    break;
                default:
                    throw new System.NotImplementedException();
            };
        }

        protected override JobHandle OnUpdate(JobHandle dependsOn)
        {
            Profiler.BeginSample("WaterSystem.OnUpdate");

            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(floatingMeshes.Count, Allocator.Temp);
            for (int i = 0; i < floatingMeshes.Count; ++i)
            {
                var entry = floatingMeshes[i];
                handles[i] = entry.system.ScheduleJobs(dependsOn);
            }
            JobHandle finalHandle = JobHandle.CombineDependencies(handles);
            Profiler.EndSample();

            Profiler.BeginSample("WaterSystem.WaitForJobs");
            finalHandle.Complete(); //TODO: eliminate somehow?
            for (int i = 0; i < floatingMeshes.Count; ++i)
            {
                var entry = floatingMeshes[i];
                entry.system.ApplyForces();
            }
            Profiler.EndSample();

            return new JobHandle();
        }

        public IFloatingMeshBackend Register(IFloatingMesh fm, IFloatingMeshSettings fmSettings)
        {
            Profiler.BeginSample("WaterSystem.Register");

            Debug.Assert(!floatingMeshes.Exists(x => x.target == fm));
            var entry = new FloatingMeshEntry();
            entry.target = fm;
            entry.system = new FloatingMeshBackend(fm, fmSettings);
            floatingMeshes.Add(entry);

            Profiler.EndSample();

            return entry.system;
        }

        public void Unregister(IFloatingMesh fm)
        {
            Profiler.BeginSample("WaterSystem.Unregister");

            Debug.Assert(floatingMeshes.Count(x => x.target == fm) == 1);
            var idx = floatingMeshes.FindIndex(x => x.target == fm);
            var entry = floatingMeshes[idx];
            entry.system.Dispose();
            floatingMeshes.RemoveAt(idx);

            Profiler.EndSample();
        }

        FloatingMeshEntry FindEntry(IFloatingMesh fm)
        {
            var idx = floatingMeshes.FindIndex(x => x.target == fm);
            if (idx != -1)
            {
                return floatingMeshes[idx];
            }
            else
            {
                return null;
            }
        }

        public void DrawDebugGizmos(IFloatingMesh fm)
        {
            var entry = FindEntry(fm);
            if (entry != null)
            {
                entry.system.DrawDebugGizmos();
            }
        }
    }
}
