using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


namespace Noodles
{
    // tools to easily write carryables
    public static class CarryBlocks
    {       
        public static void ConvertJointToRelative(ref AngularArticulationJoint joint, in HandPose pose, float aimYaw, bool left)
        {

            joint.rotationMode = RotationTargetMode.Relative;
            //joint.targetRotation = MakeRelativeToPose(joint.targetRotation, pose, aimYaw, left);
            var handTargetRotation = GetLowerArmWorldRotation(pose, aimYaw, left);
            joint.targetRotation= re.invmul(handTargetRotation, joint.targetRotation);
        }

        public static quaternion GetLowerArmWorldRotation(in HandPose pose, float aimYaw, bool left)
        {
#warning Make it compile, but carryables need to be revised for new pose model
            var chestRot = quaternion.identity;
            NoodleArmRig.CalculateArmTargetRotations(pose, chestRot, out var upper, out var lower, left);
            upper = math.mul(quaternion.RotateY(aimYaw), upper);
            return math.mul(upper, lower);
        }
        // remap target to actual hand portation instead of pose - creates nice lag
        public static quaternion RemapToCurrentHandRot(quaternion worldTarget, bool left, HandPose pose, float aimYaw, quaternion handRot)
        {
            var poseRot = GetLowerArmWorldRotation(pose, aimYaw, left);
            var poseToHand = math.mul(handRot, math.inverse(poseRot));
            worldTarget = math.mul(poseToHand, worldTarget);
            return worldTarget;
        }

        // clamp to limit rotation
        public static quaternion ClampSwingTwistToCurrent(quaternion worldTarget, quaternion currentRot)
        {
            worldTarget = re.MoveTowardsSwingRotation(
                re.MoveTowardsTwistRotation(currentRot, worldTarget, re.forward, math.radians(90)),
                worldTarget, re.forward, math.radians(90));
            return worldTarget;
        }

        // IK snap hands to an axis on carryable
        // relativeTargetPos: target position relative to midpoint between shoulders
        // rightWeight: 0 - left hand snapped to targetPos, 1 - right hand
        public static void SolveTwoHandsFromPositionAndRotation(in HandCarryData handL, in HandCarryData handR, ref HandPose poseL, ref HandPose poseR, in ConstraintBlock constraint,
           float3 relativeTargetPos, quaternion worldTargetRot, float aimYaw, float rightWeight)
        {
            var anchorL = constraint.solver.GetJoint<LinearArticulationJoint>(handL.jointId + 1).anchor;
            var anchorR = constraint.solver.GetJoint<LinearArticulationJoint>(handR.jointId + 1).anchor;
            var shoulderOffset = .2f * re.right;
            relativeTargetPos = relativeTargetPos.RotateY(-aimYaw);
            var holdDir = math.rotate(worldTargetRot, anchorR - anchorL).RotateY(-aimYaw);
            NoodleArmRig.SolveArm(relativeTargetPos - holdDir * (rightWeight) + shoulderOffset, ref poseL, left: true);
            NoodleArmRig.SolveArm(relativeTargetPos + holdDir * (1 - rightWeight) - shoulderOffset, ref poseR, left: false);

            //var shoulderDir= .4f * re.right.RotateY(aimYaw);
            //var holdDir = math.rotate(worldTargetRot, anchorR - anchorL);
            //Carry.SolveHand(relativeTargetPos - (holdDir-shoulderDir) * (rightWeight) , aimYaw, NoodleJoints.UPPER_ARM_LENGTH_TEMP, NoodleJoints.LOWER_ARM_LENGTH_TEMP, ref poseL, left: true);
            //Carry.SolveHand(relativeTargetPos + (holdDir - shoulderDir) * (1 - rightWeight) , aimYaw, NoodleJoints.UPPER_ARM_LENGTH_TEMP, NoodleJoints.LOWER_ARM_LENGTH_TEMP, ref poseR, left: false);


        }

        public static void SolveTwoHandsFromGivenPosesAndRotation(in HandCarryData handMain, in HandCarryData handOthr, ref HandPose poseMain, ref HandPose poseOthr, in ConstraintBlock constraint,
           quaternion worldTargetRot, float aimYaw, float mainWeight)
        {

            var anchorMain = constraint.solver.GetJoint<LinearArticulationJoint>(handMain.jointId + 1).anchor;
            var anchorOthr = constraint.solver.GetJoint<LinearArticulationJoint>(handOthr.jointId + 1).anchor;
            SolveTwoHandsFromGivenPosesAndRotation(handMain, handOthr, ref poseMain, ref poseOthr, worldTargetRot, aimYaw, mainWeight, anchorMain, anchorOthr);

        }

        public static void SolveTwoHandsFromGivenPosesAndRotation(in HandCarryData handMain, in HandCarryData handOthr, ref HandPose poseMain, ref HandPose poseOthr, quaternion worldTargetRot, float aimYaw, float mainWeight, float3 anchorMain, float3 anchorOthr)
        {
            var anchor = math.lerp(anchorOthr, anchorMain, mainWeight);

            var targetPos = math.lerp(PoseToRelativePosition(handOthr, poseOthr, aimYaw), PoseToRelativePosition(handMain, poseMain, aimYaw), mainWeight);
            var targetMain = targetPos + math.rotate(worldTargetRot, anchorMain - anchor);
            var targetOthr = targetPos + math.rotate(worldTargetRot, anchorOthr - anchor);

            SolveRelativePosition(targetMain, handMain, ref poseMain, aimYaw);
            SolveRelativePosition(targetOthr, handOthr, ref poseOthr, aimYaw);
        }

        public static float3 PoseToRelativePosition(in HandCarryData data, in HandPose pose, float aimYaw)
        {
            var shoulderOffset = re.right * (data.isLeft ? -.2f : .2f);
            return (NoodleArmRig.CalculateArmTarget(pose, data.isLeft) + shoulderOffset).RotateY(aimYaw);
        }

        public static void SolveRelativePosition(float3 pos, in HandCarryData data, ref HandPose pose, float aimYaw)
        {
            var shoulderOffset = re.right * (data.isLeft ? -.2f : .2f);
            NoodleArmRig.SolveArm(pos.RotateY(-aimYaw) - shoulderOffset, ref pose, data.isLeft);
        }
        
    }

}
