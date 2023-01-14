using NBG.Entities;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // Coin will align X axis with pivot space X - allowing inserting things into slots
    public class CoinCarryable : MetaStoneCarryable
    {
        public float coinRadius=.125f;
       
        public override bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
            => AcquireCoinGrip(reach, ref target, gripId, pivotTarget, alignX);
        

        public static bool AcquireCoinGrip(in HandReachInfo reach, ref HandTargetGrip target, int gripId, quaternion pivotRot, RigidTransform alignX)
        { 
            // transform from pose to world
            //pivotRot = math.mul(reach.worldPalmX.rot, pivotRot);
            var bodyPos = alignX.pos;
            var bodyRot = alignX.rot;

            //var anchorToCenter = math.normalize(re.ProjectOnPlane(bodyPos - target.worldAnchor, math.rotate(bodyRot, re.right)));
            var anchorToCenter = math.normalize(bodyPos - target.worldAnchor);

            // rotate coin so it's grabbed axis looks along pivot forward, before aligning
            bodyRot = math.mul(re.FromToRotation(anchorToCenter, math.rotate(pivotRot, re.forward)), bodyRot);
            AcquireDefaultGrip(ref target, gripId, pivotRot, bodyRot, CarryablePivotAlign.XUnsigned, GrabJointCollision.IgnoreLower);
            return true;
        }
    }
}
