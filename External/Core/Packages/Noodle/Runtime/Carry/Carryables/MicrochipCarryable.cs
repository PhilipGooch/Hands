using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class MicrochipCarryable : MetaCrateCarryable
    {

        protected override Spring GetHandJointSpring() => new Spring(250, 20); // default

        protected override CarryAlgorithmSingle GetCarryFunction(in HandCarryData hand)
            => IsValidFirstGrip(hand) ? CarryAlgorithmSingle.MetaStick : CarryAlgorithmSingle.None;
        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand) =>
            AcquireTwoHandedGrip(bodyId, reach, pivotTarget, gripId, alignX, ref target, CarryablePivotAlign.AlignXFlipY);
    }
}
