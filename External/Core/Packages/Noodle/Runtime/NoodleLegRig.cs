#define SWING_TWIST_KNEE
using System.Collections;
using System.Collections.Generic;
using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class NoodleLegRig 
    {
        public static void ApplyLegPose(in SolverBodies bodies, LegPose pose, in NoodleLegJoints leg, in NoodleLegDimensions dim, float aimYaw, float3 ballOffset, bool left)
        {
            // normalize ikWeight to avoid degenerate matrices
            if (pose.ikParent < .01f) pose.ikParent = 0;
            else if (pose.ikParent > .99f) pose.ikParent = 1;
            // hips rotation in neutral yaw
            var hipsRot = bodies.GetBody(leg.upper.connectedLink).x.rot;
            hipsRot = math.mul(quaternion.RotateY(-aimYaw), hipsRot);
            //var hipsRot = math.mul(quaternion.RotateY(-aimYaw), bodies.GetBody(leg.upper.connectedLink).x.rot);

            // FK
            CalculateLegTargetRotations(pose, hipsRot, out leg.upper.targetRotation, out leg.lower.targetRotation, left);
            leg.upper.relativeVelInfluence = pose.fkParent;
            leg.upper.targetRotation = math.mul(quaternion.RotateY(aimYaw), leg.upper.targetRotation);
            // IK

            // need to adjust relative pos when IK anchor does not match rig anchor
            var relativeShift = bodies.TransformDirection(leg.upper.connectedLink, dim.hipAnchor - leg.IK.anchor1);

            leg.IK.targetPosition = math.lerp(pose.ikPos.RotateY(aimYaw)-ballOffset, pose.ikPosRelative.RotateY(aimYaw)+relativeShift, pose.ikParent);
            leg.IK.weights = new float4(1-pose.ikParent,pose.ikParent,0,0);

        }
        public static void ApplyLegSprings(NoodleSprings springs, in LegPose pose, in NoodleLegJoints leg)
        {
            leg.upper.spring = springs.upperLeg * pose.upperTonus * pose.upperTonus;
            leg.lower.springX = springs.lowerLeg * pose.lowerTonus * pose.lowerTonus;
            leg.IK.springX = leg.IK.springY = leg.IK.springZ = springs.cgFeet * pose.ikDrive;
        }

        public static void CalculateLegTargetRotations(in LegPose pose, quaternion hipsRot, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left)
        {
            CalculateLegTargetRotationsKnee(pose, hipsRot, out upperTargetRotation, out lowerTargetRotation, left);
        }

        //public static float3 CalculateLegTarget(in LegPose pose, bool left)
        //{
        //    var sign = left ? -1 : 1;
        //    var a = NoodleConstants.UPPER_LEG_LENGTH_TEMP;
        //    var b = NoodleConstants.LOWER_LEG_LENGTH_TEMP;

        //    CalculateLegTargetRotations(pose, out var upperRot, out var lowerRot, left);
        //    return math.rotate(upperRot, new float3(0, a, 0) + math.rotate(lowerRot, new float3(0, b, 0)));
        //}
        public static void SolveLeg(float3 target, ref LegPose pose, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_LEG_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_LEG_LENGTH_TEMP;

            // remap target to right arm
            target.x = sign * target.x;
            target = target.RotateXCCW90(); // bring target forward
            pose.twist -= math.radians(90); // knee from leg to arm
            NoodleArmRig.SolveLimbElbow(target, a, b, math.radians(178), ref pose.pitch, ref pose.stretch, ref pose.twist, ref pose.bend);
            pose.twist += math.radians(90);
        }

        private static void CalculateLegTargetRotationsKnee(LegPose pose, quaternion hipsRot, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left)
        {
            var sign = left ? -1 : 1;

            upperTargetRotation = re.SwingTwistYXZ(pose.pitch, -sign * pose.twist, -sign * pose.stretch);
            lowerTargetRotation = quaternion.RotateX(pose.bend);

            //var toWorld = math.mul(hipsRot, quaternion.RotateY(-aimYaw));
            upperTargetRotation = math.mul(quaternion.RotateX(math.PI), upperTargetRotation);//match rig
            upperTargetRotation = math.mul(math.slerp(quaternion.identity, hipsRot, pose.fkParent), upperTargetRotation);

        }
        //public static void SolveLegKnee(float3 target, ref LegPose pose, bool left = false)
        //{

        //}

    }
}
