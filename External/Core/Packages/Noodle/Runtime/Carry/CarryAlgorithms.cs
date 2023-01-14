using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public enum CarryAlgorithmSingle
    {
        None,
        MetaGun,
        MetaStick,
        MetaStone
    }

    public static class CarryAlgorithms
    {
        public static void CarryableCarryOneHanded(in HandCarryData hand, PivotPose pivot, ref HandPose pose, Aim aim)
        {
            //if (hand.gripId < 0) return; //TODO
            ref var constraint = ref World.main.GetConstraint(hand.blockId);
            switch (hand.singleCarryFn)
            {
                case CarryAlgorithmSingle.MetaGun: CarryGun(pivot, hand, ref pose, ref constraint, aim); break;
                case CarryAlgorithmSingle.MetaStick: CarryStick(pivot, hand, ref pose, ref constraint, aim); break;
                case CarryAlgorithmSingle.MetaStone: CarryStone(pivot, hand, ref pose, ref constraint, aim); break;
            }
        }
        public static void CarryableCarryTwoHanded(in CarryData data, in HandCarryData handMain, in HandCarryData handOthr, PivotPose pivot, ref HandPose poseMain, ref HandPose poseOthr, in Articulation articulation, in Aim aim)
        {
            if (handMain.allowTwoHanded)
            {
                ref var constraint = ref World.main.GetConstraint(handMain.blockId);
                CarryTwoHanded(pivot, data, handMain, handOthr, ref poseMain, ref poseOthr, ref constraint, aim);
            }
        }
        //TODO Stick and Gun share a lot of code
        public static void CarryGun(PivotPose pivot, in HandCarryData data, ref HandPose pose, ref ConstraintBlock constraint, in Aim aim)
        {
            var bodies = constraint.GetBodies();
            ref var joint = ref constraint.solver.GetJoint<AngularArticulationJoint>(0);
            ref var hand = ref bodies.GetBody(joint.connectedLink);
            ref var body = ref bodies.GetBody(joint.link);

            //pose = carryableData.handTrajectory.Sample(aim.pitch01);

            // calculate object target rotation
            var worldTarget = pivot.ToRotation(false, aim.yaw); //GetPivotTarget(carryableData, data.isLeft, aim);
            worldTarget = math.mul(worldTarget, data.carryableToPivotRot);

            var aimTarget = worldTarget; // store for other joint
            worldTarget = CarryBlocks.ClampSwingTwistToCurrent(worldTarget, body.x.rot);

            var steadyTonus = 1 - 1f * math.saturate(math.max(aim.moveMagnitude* aim.moveMagnitude, aim.aimVecocity * 0)); // when running use relative spring, when steady - use world spring

            joint.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            joint.targetRotation = worldTarget;
            joint.spring = data.jointSpring * (1 - steadyTonus);

            // now configure gun to world constraint
            CarryBlocks.ConvertJointToRelative(ref joint, pose, aim.yaw, data.isLeft); // add hand lag
            // extra joint to world
            ref var worldJoint = ref constraint.solver.GetJoint<AngularArticulationJoint>(2);
            worldJoint.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            worldJoint.targetRotation = aimTarget;
            worldJoint.spring = data.worldJointSpring * steadyTonus;
        }

        public static void CarryStick(PivotPose pivot, in HandCarryData data, ref HandPose pose, ref ConstraintBlock constraint, in Aim aim)
        {
            var bodies = constraint.GetBodies();
            ref var joint = ref constraint.solver.GetJoint<AngularArticulationJoint>(0);
            ref var hand = ref bodies.GetBody(joint.connectedLink);
            ref var body = ref bodies.GetBody(joint.link);


            // calculate object target rotation
            var worldTarget = pivot.ToRotation(false, aim.yaw);
            worldTarget = math.mul(worldTarget, data.carryableToPivotRot);
            var aimTarget = worldTarget; // store for other joint

            worldTarget = CarryBlocks.RemapToCurrentHandRot(worldTarget, data.isLeft, pose, aim.yaw, hand.x.rot);

            worldTarget = CarryBlocks.ClampSwingTwistToCurrent(worldTarget, body.x.rot);

            joint.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            joint.targetRotation = worldTarget;
            joint.spring = data.jointSpring;

            ref var worldJoint = ref constraint.solver.GetJoint<AngularArticulationJoint>(2);
            worldJoint.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            worldJoint.targetRotation = aimTarget;
            worldJoint.spring = data.worldJointSpring;
        }

        public static void CarryStone(PivotPose pivot, in HandCarryData data, ref HandPose pose, ref ConstraintBlock constraint, in Aim aim)
        {
            var bodies = constraint.GetBodies();
            ref var joint = ref constraint.solver.GetJoint<AngularArticulationJoint>(0);
            ref var hand = ref bodies.GetBody(joint.connectedLink);
            ref var body = ref bodies.GetBody(joint.link);

            // calculate object target rotation
            var target = pivot.ToRotation(false, aim.yaw);
            target = math.mul(target, data.carryableToPivotRot);

            joint.rotationMode = RotationTargetMode.Relative;
            joint.targetRotation = target;
            joint.spring = data.jointSpring;

        }
        public unsafe static void CarryTwoHanded(PivotPose pivot, in CarryData data, in HandCarryData handMain, in HandCarryData handOthr, ref HandPose poseMain, ref HandPose poseOthr, ref ConstraintBlock constraint, in Aim aim)
        {
            var bodies = constraint.GetBodies();

            ref var joint = ref constraint.solver.GetJoint<AngularArticulationJoint>(4);
            var body = bodies.GetBody(joint.link);
            ref var jointMain = ref constraint.solver.GetJoint<AngularArticulationJoint>(handMain.jointId);
            ref var jointOthr = ref constraint.solver.GetJoint<AngularArticulationJoint>(handOthr.jointId);


            // calculate object target rotation
            var worldTarget = pivot.ToRotation(false, aim.yaw);// GetPivotTarget(carryableConfig, data.isLeft, aim);
            worldTarget = math.mul(worldTarget, data.carryableToPivotRot);

            worldTarget = CarryBlocks.ClampSwingTwistToCurrent(worldTarget, body.x.rot);

            // configure wrists
            jointMain.rotationMode = jointOthr.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            jointMain.targetRotation = jointOthr.targetRotation = worldTarget;
            jointMain.spring =
            jointOthr.spring = handMain.jointSpring;

            // configure main joint // world spring can be used for extra precision
            joint.rotationMode = RotationTargetMode.AbsolutePosRelativeVel;
            joint.targetRotation = worldTarget;
            joint.spring = handMain.worldJointSpring;

            // remap to relative
            CarryBlocks.ConvertJointToRelative(ref jointMain, poseMain, aim.yaw, handMain.isLeft);
            CarryBlocks.ConvertJointToRelative(ref jointOthr, poseOthr, aim.yaw, handOthr.isLeft);


        }
    }
}
