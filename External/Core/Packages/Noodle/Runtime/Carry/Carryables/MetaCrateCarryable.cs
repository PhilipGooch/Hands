using Recoil;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [BurstCompile]
    public abstract class MetaCrateCarryable : CarryableBase
    {
        protected override Spring GetHandJointSpring() => new Spring(100, 50); // bit weaker joint for two handed
        protected override Spring GetWorldJointSpringDual() => new Spring(150, 50,200);
        
        public override bool useRotationToPivotFromFirstGrip => false;
        public override bool alwaysGetFirstAvailableGrip => false;
        //public override bool allowDual => true;


    }
}