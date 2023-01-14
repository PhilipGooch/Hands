using Recoil;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [BurstCompile]

    // Gun carryable uses extra joint to lock rotation to world for improved aiming
    public abstract class MetaGunCarryable : CarryableBase
    {
        protected override Spring GetHandJointSpring() => new Spring(150, 25);

        protected override Spring GetWorldJointSpring() => new Spring(150, 25);

        protected override CarryAlgorithmSingle GetCarryFunction(in HandCarryData hand)
            => IsValidFirstGrip(hand) ? CarryAlgorithmSingle.MetaGun : CarryAlgorithmSingle.None;
        
        public override bool alwaysGetFirstAvailableGrip => true;

        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
            => AcquireDefaultGrip(ref target, gripId, pivotTarget, alignX.rot, CarryablePivotAlign.Locked, GrabJointCollision.IgnoreLower);

        public override void SelectMainHand(ref CarryData data) { } // just keep first hand main
    }
}