using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class PoleCarryable : MetaStickCarryable
    {
        public override bool reuseGrips => true;
        public override bool alwaysGetFirstAvailableGrip => false;
        public override bool useRotationToPivotFromFirstGrip => false;
        public override bool recalculatePivotOffsetTwoHanded => false;
        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotRot, RigidTransform alignX, in HandCarryData otherHand)
        {
            if (!otherHand.IsCarrying(bodyId))// first hand use current object alignment
                return AcquireDefaultGrip(ref target, gripId, pivotRot, alignX.rot, CarryablePivotAlign.None, GrabJointCollision.Ignore);
            else
            {
                // check which way if forward
                var anchor = (Carry.GetWorldAnchor(otherHand) + target.worldAnchor) / 2;
                anchor = re.invmul(alignX.rot, anchor - World.main.GetBodyPosition(bodyId).pos);

                var align = anchor.z > 0 ? CarryablePivotAlign.ZNegative : CarryablePivotAlign.ZAxis;

                return AcquireDefaultGrip(ref target, gripId, pivotRot, alignX.rot, align, GrabJointCollision.Ignore);
            }
           
        }



      
    }
}
