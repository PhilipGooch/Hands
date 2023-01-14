using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using NBG.Entities;
using Unity.Jobs;
using NBG.Core.GameSystems;

[UpdateInGroup(typeof(LateUpdateSystemGroup))]
public class SecondaryAnimationChainSystem : GameSystemWithJobs
{
    public static SecondaryAnimationChainSystem Instance { get; private set; }

    List<SecondaryAnimationChain> animations = new List<SecondaryAnimationChain>();

    List<JobHandle> activeJobs = new List<JobHandle>();

    public SecondaryAnimationChainSystem()
    {
        Instance = this;
    }

    public void AddChain(SecondaryAnimationChain chain)
    {
        animations.Add(chain);
    }

    public void RemoveChain(SecondaryAnimationChain chain)
    {
        animations.Remove(chain);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        activeJobs.Clear();
        foreach (var chain in animations)
        {
            activeJobs.Add(chain.Execute(inputDeps));
        }

        for (int i = activeJobs.Count - 1; i >= 0; i--)
        {
            activeJobs[i].Complete();
            animations[i].WriteData();
        }

        return inputDeps;
    }
}
