using NBG.Entities;
using Recoil;
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{

    public class AxeCarryable : MetaStickCarryable
    {
        //public override bool allowSingle => true;

        //public override bool allowDual => true;

        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
             => AcquireDefaultGrip(ref target, gripId, pivotTarget, alignX.rot, CarryablePivotAlign.AlignYFlipZ, GrabJointCollision.IgnoreLower);
    }
}
