using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace NBG.Locomotion
{
    /// <summary>
    /// Interface to handle locomotion. Use it to implement behaviours like flocking, herding, obstacle avoidance, etc.
    /// </summary>
    public interface ILocomotionHandler
    {
        bool Enabled { get; set; }
        JobHandle HandleLocomotion(NativeList<LocomotionData> locomotionData, JobHandle dependsOn);

        void Dispose();
    }
}
