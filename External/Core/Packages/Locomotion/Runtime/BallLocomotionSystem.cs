using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace NBG.Locomotion
{
    public class BallLocomotionSystem
    {
        List<BallLocomotion> agents;
        List<ILocomotionHandler> locomotionHandlers = new List<ILocomotionHandler>();
        NativeList<LocomotionData> locomotionData;
        NativeList<BallLocomotion.BallLocomotionData> ballLocomotionState;
        LayerMask groundLayers;

        public BallLocomotionSystem(List<BallLocomotion> agents, LayerMask groundLayers)
        {
            this.agents = agents;
            locomotionData = new NativeList<LocomotionData>(agents.Count, Allocator.Persistent);
            ballLocomotionState = new NativeList<BallLocomotion.BallLocomotionData>(agents.Count, Allocator.Persistent);
            this.groundLayers = groundLayers;
        }

        public void AddLocomotionHandler(ILocomotionHandler handler)
        {
            if (!locomotionHandlers.Contains(handler))
            {
                locomotionHandlers.Add(handler);
            }
        }

        public void RemoveLocomotionHandler(ILocomotionHandler handler)
        {
            locomotionHandlers.Remove(handler);
        }

        public void UpdateLocomotion()
        {
            Profiler.BeginSample("LocomotionGroup.HandleLocomotion");
            locomotionData.Clear();
            ballLocomotionState.Clear();

            Profiler.BeginSample("Initialize");
            for (int i = 0; i < agents.Count; i++)
            {
                var state = agents[i].UpdateGroundInfo(groundLayers);
                ballLocomotionState.Add(state);
            }
            Profiler.EndSample();

            JobHandle dependsOn = default;
            dependsOn = new BallLocomotionJobs.InitializeLocomotionStateJob(ballLocomotionState).Schedule(dependsOn);
            dependsOn = new BallLocomotionJobs.ExtractLocomotionDataJob(ballLocomotionState, locomotionData).Schedule(dependsOn);

            for (int i = 0; i < locomotionHandlers.Count; i++)
            {
                var targetHandler = locomotionHandlers[i];
                if (targetHandler.Enabled)
                {
                    dependsOn = targetHandler.HandleLocomotion(locomotionData, dependsOn);
                }
            }
            dependsOn = new BallLocomotionJobs.ApplyMovementJob(ballLocomotionState, locomotionData).Schedule(dependsOn);
            dependsOn.Complete();

            Profiler.BeginSample("Apply State");
            for (int i = 0; i < ballLocomotionState.Length; i++)
            {
                agents[i].ApplyState(ballLocomotionState[i]);
            }
            Profiler.EndSample();

            Profiler.EndSample();
        }

        public void Dispose()
        {
            foreach (var h in locomotionHandlers)
            {
                h.Dispose();
            }
            locomotionHandlers.Clear();
            locomotionData.Dispose();
            ballLocomotionState.Dispose();
        }
    }
}
