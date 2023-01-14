
using NBG.Entities;
using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // Stone carryable locks object to hand, aligning CG on palm axis
    public class StoneCarryable : MetaStoneCarryable
    {
        public override bool alwaysGetFirstAvailableGrip => false;
        public override bool useRotationToPivotFromFirstGrip => false;
        protected override bool AllowTwoHanded(in HandCarryData first, in HandCarryData second) => true;
        protected override Spring GetWorldJointSpringDual() => Spring.free;

        public override HandTargetGrip AcquireCollisionGrip(in HandReachInfo reach, float3 surfacePos, float3 surfaceHandPos, quaternion pivotTarget, int gripId, RigidTransform gripX, bool twoHanded, in HandCarryData otherHand)
        {
            var bodyRot = gripX.rot;
            quaternion desiredRot;
            if (twoHanded)
            {
                var otherpos = Carry.GetWorldAnchor(otherHand);
                var anchorMidpoint = (otherpos + surfacePos) / 2;
                var currentForward=  gripX.pos-anchorMidpoint;// from both grip points
                var currentRight = reach.left ? otherpos - surfacePos : surfacePos - otherpos;
                var currentPivot = quaternion.LookRotationSafe(currentForward, math.cross(currentForward, currentRight));

                //Debug.DrawRay(gripX.pos, math.rotate(worldToBody, re.right), Color.red);
                //Debug.DrawRay(gripX.pos, math.rotate(worldToBody, re.up), Color.green);
                //Debug.DrawRay(gripX.pos, math.rotate(worldToBody, re.forward), Color.blue);
                var rotationToPivot = re.invmul(currentPivot, bodyRot);
                desiredRot = math.mul(pivotTarget, rotationToPivot);
            }
            else
            {

                var currentAxis = gripX.pos - surfacePos;// current direction from surface to cg
                var targetAxis = reach.worldPalmDir;// desired direction matches palm vector
                desiredRot = math.mul(re.FromToRotation(currentAxis, targetAxis), bodyRot);


            }
            var target = HandTargetGrip.FromRelativeHandAnchor(surfacePos, reach.relativePalmAnchor);
            AcquireDefaultGrip(ref target, gripId, pivotTarget, desiredRot, CarryablePivotAlign.None, GrabJointCollision.IgnoreLower);

            return target;
        }
    }
}
