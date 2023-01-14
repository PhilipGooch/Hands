using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class ShieldCarryable : MetaStickCarryable
    {
        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
            => AcquireDefaultGrip(ref target, gripId, pivotTarget, alignX.rot, CarryablePivotAlign.AlignXFlipZ, GrabJointCollision.IgnoreLower);

    }
}