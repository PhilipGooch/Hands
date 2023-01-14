
using NBG.Entities;
using Noodles.Animation;
using Recoil;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [BurstCompile]
    // Stick carryable ignores twist - aligning it's axis with pivot
    public abstract class MetaStickCarryable : CarryableBase
    {
        //protected override Spring GetWorldJointSpring() => new Spring(25000, 30); // connect to world
        protected override Spring GetWorldJointSpringDual() => new Spring(250, 30); // connect to world

        protected override CarryAlgorithmSingle GetCarryFunction(in HandCarryData hand)
            =>IsValidFirstGrip(hand)? CarryAlgorithmSingle.MetaStick : CarryAlgorithmSingle.None;
        


        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
            //=> GripBlocks.AcquireAxisGrip(reach, pivotRot, gripId, gripX, CarryablePivotAlign.ZAxis, GrabJointCollision.IgnoreLower, out target);
            => AcquireDefaultGrip(ref target, gripId, pivotTarget, alignX.rot, CarryablePivotAlign.ZAxis, GrabJointCollision.IgnoreLower);


    }
}