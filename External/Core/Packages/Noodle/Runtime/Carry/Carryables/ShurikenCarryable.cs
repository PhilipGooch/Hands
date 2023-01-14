using NBG.Entities;
using Recoil;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{

    public class ShurikenCarryable : MetaStickCarryable
    {
        public override bool alwaysGetFirstAvailableGrip => false;
        protected override Spring GetWorldJointSpring() => new Spring(100, 10); // connect to world

        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
            => CoinCarryable.AcquireCoinGrip(reach, ref target, gripId, pivotTarget, alignX);

    }
}
