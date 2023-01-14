using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class JackhammerCarryable : MicrochipCarryable
    {
        protected override Spring GetWorldJointSpringDual() => new Spring(250, 30); 
      

        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand) =>
            AcquireTwoHandedGrip(bodyId, reach, pivotTarget, gripId, alignX, ref target, CarryablePivotAlign.AlignXFlipZ);
    }
}
