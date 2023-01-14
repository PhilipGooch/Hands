// Different ways to interpret HandPose
#define SWING_TWIST_FIST // pitch-yaw is swing of shoulder->fist axis
//#define SWING_TWIST_ELBOW  // pitch-yaw is swing of shoulder->elbow axis
//#define PITCH_YAW_FIST // pitch-yaw is pitch-yaw of shoulder->fist axis

using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class NoodleArmRig

    {
        public const float YAW_CENTER = 0 * 45f * math.PI / 180;

#if PITCH_YAW_FIST
        public const float DEFAULT_EXTRA_YAW_DEG = 7.5f;
        public const float DEFAULT_BEND_DEG = 30;
#else
        public const float DEFAULT_EXTRA_YAW_DEG = 10f;
        public const float DEFAULT_BEND_DEG = 20;
#endif


        public static HandPose GetIdlePose(in NoodleAnimatorData animator, float aimPitch01, bool left = false) =>
            left ?
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.idle, aimPitch01).flipped :
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.idle, aimPitch01);
        public static HandPose GetGrabPose(in NoodleAnimatorData animator, float aimPitch01, bool left = false) =>
            left ?
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.grab, aimPitch01).flipped :
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.grab, aimPitch01);

        public static HandPose GetHoldPose(in NoodleAnimatorData animator, float aimPitch01, bool left = false) =>
            left ?
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.hold, aimPitch01).flipped :
                NoodleAnimationLayout.armR.GetPose01(animator.animationDB.hold, aimPitch01);

        public static HandPose GetClimbPose(in NoodleAnimatorData animator, float aimPitch01)
        {
            return NoodleAnimationLayout.armR.GetPose01(animator.animationDB.climb, aimPitch01);
        }

        public static HandPose GetSwingPose(in NoodleAnimatorData animator, float aimPitch01)
        {
            return NoodleAnimationLayout.armR.GetPose01(animator.animationDB.swing, aimPitch01);
        }
        public static void ApplyArmPose(in SolverBodies bodies, HandPose pose, in NoodleArmJoints arm, in NoodleArmDimensions dim, float aimYaw, float3 ballOffset, bool left)
        {
            // normalize ikWeight to avoid degenerate matrices
            if (pose.ikParent < .01f) pose.ikParent = 0;
            else if (pose.ikParent > .99f) pose.ikParent = 1;

            // hips rotation in neutral yaw
            var chestRot = bodies.GetBody(arm.upper.connectedLink).x.rot;
            chestRot = math.mul(quaternion.RotateY(-aimYaw), chestRot);

            ref var upperArm = ref arm.upper;
            ref var lowerArm = ref arm.lower;
            ref var handIK = ref arm.IK;
            CalculateArmTargetRotations(pose, chestRot, out upperArm.targetRotation, out lowerArm.targetRotation, left);
            arm.upper.relativeVelInfluence = pose.fkParent;

            upperArm.targetRotation = math.mul(quaternion.RotateY(aimYaw), upperArm.targetRotation);

            // need to adjust relative pos when IK anchor does not match rig anchor
            var relativeShift = bodies.TransformDirection(upperArm.connectedLink,  dim.shoulderAnchor- arm.IK.anchor1);
            
            // IK
            arm.IK.targetPosition = math.lerp(pose.ikPos.RotateY(aimYaw)-ballOffset, (pose.ikPosRelative).RotateY(aimYaw)+relativeShift, pose.ikParent);
            arm.IK.weights = new float4(1-pose.ikParent,pose.ikParent,0,0);

            var shoulder = bodies.TransformPoint(arm.upperLinear.connectedLink, arm.upperLinear.connectedAnchor);
            var currentFwd = bodies.TransformPoint(handIK.link, handIK.anchor) - shoulder;
            var upperRot = quaternion.LookRotationSafe(currentFwd, re.up);

            //Debug.DrawRay(bodies.TransformPoint(upperArm.linkA, upperArm.anchorA), currentFwd, Color.blue);
            //Debug.DrawRay(bodies.TransformPoint(upperArm.linkA, upperArm.anchorA), CalculateArmTarget(upperArm.targetRotation, lowerArm.targetRotation), Color.blue);

            // upper arm - align twist with hand pitch
            upperArm.axisX = math.mul(upperRot, re.forward);
            upperArm.axisY = math.mul(upperRot, re.up);
            upperArm.axisZ = math.mul(upperRot, re.right);
            upperArm.worldAxes = true;

            // lower arm - twist along Y, bend along parent Z
            var lowerRot = bodies.GetBody(lowerArm.link).x.rot;
            upperRot = bodies.GetBody(upperArm.link).x.rot;
            lowerArm.axisX = math.mul(upperRot, re.forward); // bend
            lowerArm.axisY = math.mul(lowerRot, re.up); // twist
            lowerArm.axisZ = math.mul(upperRot, re.right);
            re.OrthoNormalize(ref lowerArm.axisY, ref lowerArm.axisX, ref lowerArm.axisZ);
            lowerArm.worldAxes = true;
        }
        public static void ApplyArmSprings(NoodleSprings springs, HandPose pose, in NoodleArmJoints arm)
        {
            ref var upperArm = ref arm.upper;
            ref var lowerArm = ref arm.lower;

            ref var handIK = ref arm.IK;
            handIK.springX = springs.handIK * pose.muscle.ikDrive;
            handIK.springY = springs.handIKV * pose.muscle.ikDrive;
            handIK.springZ = springs.handIK * pose.muscle.ikDrive;

            var ikHandTonus = math.sqrt( .1f);
            var ikHandTonusLower = math.sqrt(.25f);
            upperArm.springX = Spring.Lerp(springs.upperArmIdle, springs.upperArm, math.max(pose.muscle.upperTonus, pose.muscle.ikDrive));
            upperArm.springY = upperArm.springZ = Spring.Lerp(springs.upperArmIdle, springs.upperArm, math.lerp(pose.muscle.upperTonus, ikHandTonus, pose.muscle.ikDrive));
            lowerArm.springX = Spring.Lerp(springs.lowerArmIdle, springs.lowerArm, math.lerp(pose.muscle.lowerTonus, ikHandTonusLower, pose.muscle.ikDrive));

            lowerArm.springY = new Spring(10000, 1000); // wrist twist strong but not impossible
            //lowerArm.springZ = Spring.stiff; // fixed on the remaining axis, but applying it makes thing a bit less stable 
        }

        public static float3 CalculateArmTarget(quaternion upperArmTarget, quaternion lowerArmTarget)
        {
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;
            return math.rotate(upperArmTarget, new float3(0, a, 0) + math.rotate(lowerArmTarget, new float3(0, b, 0)));
        }

        public static float3 CalculateArmTarget(HandPose pose, bool left = false)
        {
#if PITCH_YAW_FIST
            return CalculateArmTargetLegacyFist(pose, left);
#elif SWING_TWIST_FIST
            return CalculateArmTargetFist(pose, left);
#else
            return CalculateArmTargetElbow(pose, left);
#endif
        }
        public static void CalculateArmTargetRotations(HandPose pose, quaternion chestRot, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left = false)
        {
#if PITCH_YAW_FIST
            CalculateArmTargetRotationsLegacyFist(pose, out upperTargetRotation, out lowerTargetRotation, left);
#elif SWING_TWIST_FIST
            CalculateArmTargetRotationsFist(pose, chestRot, out upperTargetRotation, out lowerTargetRotation, left);
#else
            CalculateArmTargetRotationsElbow(pose, out upperTargetRotation, out lowerTargetRotation, left);
#endif
        }
        public static void SolveArm(float3 target, ref HandPose pose, bool left = false)
        {
#if PITCH_YAW_FIST
            SolveArmLegacyFist(target, ref pose, left);
#elif SWING_TWIST_FIST
            SolveArmFist(target, ref pose, left);
#else
            SolveArmElbow(target, ref pose, left);
#endif
        }

        private static float3 CalculateArmTargetElbow(HandPose pose, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;
            var l = new float3(0, 0, b).RotateY(-sign * pose.bend).RotateZ(-sign * pose.elbowAngle);
            return math.rotate(new float3(pose.pitch, sign * (pose.yaw - sign * YAW_CENTER), 0).ToQuaternion(), l + new float3(0, 0, a)).RotateY(sign * YAW_CENTER);
        }

        private static void CalculateArmTargetRotationsElbow(HandPose pose, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left = false)
        {
            var sign = left ? -1 : 1;
            upperTargetRotation = re.SwingTwistYXZ(-pose.pitch, -sign * pose.elbowAngle, sign * (pose.yaw - YAW_CENTER));

            lowerTargetRotation = quaternion.EulerYXZ(0, -sign * pose.wristAngle, -sign * pose.bend);

            // bring human rig to point forward (zero pose)
            var toNeutral = quaternion.LookRotation(new float3(0, 1, 0), new float3(0, 0, 1));
            upperTargetRotation = math.mul(toNeutral, upperTargetRotation);
            upperTargetRotation = math.mul(quaternion.RotateY(sign * YAW_CENTER), upperTargetRotation);
        }


        private static void SolveArmElbow(float3 target, ref HandPose pose, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;

            target.x = sign*target.x;
            SolveLimbElbow(target, a, b, math.radians(170), ref pose.pitch, ref pose.yaw, ref pose.elbowAngle, ref pose.bend);

        }
        private static void SolveArmFist(float3 target, ref HandPose pose, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;
            target.x = sign * target.x;
            SolveLimbFist(target, a, b, math.radians(170), ref pose.pitch, ref pose.yaw, ref pose.elbowAngle, ref pose.bend);

        }
       
        private static float3 CalculateArmTargetFist(HandPose pose, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;

            var c = re.SolveTriangleEdge(a, b, math.PI - pose.bend);
            var finalRot = new float3(pose.pitch, sign * (pose.yaw - sign * YAW_CENTER), 0).ToQuaternion();

            return math.rotate(finalRot, new float3(0, 0, c)).RotateY(sign * YAW_CENTER);
        }

        private static void CalculateArmTargetRotationsFist(HandPose pose, quaternion chestRot, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left = false)
        {
            var sign = left ? -1 : 1;
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;

            // solve rotation at shoulder to keep shoulder-fist direction when arm bend is applied
            var c = re.SolveTriangleEdge(a, b, math.PI - pose.bend);
            var shouderDelta = re.SolveTriangleAngle(a, c, b);

            upperTargetRotation = re.SwingTwistYXZ(-pose.pitch, -sign * pose.elbowAngle, sign * (pose.yaw - YAW_CENTER));
            upperTargetRotation = math.mul(upperTargetRotation, quaternion.RotateZ(sign * shouderDelta));
            lowerTargetRotation = quaternion.EulerYXZ(0, -sign * pose.wristAngle, -sign * pose.bend);

            // bring human rig to point forward (zero pose)
            var toNeutral = quaternion.LookRotation(new float3(0, 1, 0), new float3(0, 0, 1));
            upperTargetRotation = math.mul(toNeutral, upperTargetRotation);
            upperTargetRotation = math.mul(quaternion.RotateY(sign * YAW_CENTER), upperTargetRotation);
            upperTargetRotation = math.mul(math.slerp(quaternion.identity, chestRot, pose.fkParent), upperTargetRotation);
        }
       





        private static float3 CalculateArmTargetLegacyFist(HandPose pose, bool left = false)
        {
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;
            if (left) pose.yaw = -pose.yaw;
            var cosBend = math.cos(math.PI - pose.bend);
            var dist = math.sqrt(a * a + b * b - (2 * a * b) * cosBend);
            var finalRot = math.mul(quaternion.Euler(pose.pitch, 0, 0), quaternion.RotateY(pose.yaw));
            return math.rotate(finalRot, new float3(0, 0, dist));
        }

        private static void CalculateArmTargetRotationsLegacyFist(HandPose pose, out quaternion upperTargetRotation, out quaternion lowerTargetRotation, bool left = false)
        {
            var sign = left ? -1 : 1;

            // point forward
            upperTargetRotation = quaternion.LookRotation(new float3(0, 1, 0), new float3(0, 0, 1));
            // bend at elbow
            lowerTargetRotation = quaternion.Euler(0, 0, -sign * pose.bend);
            // wrist twist
            lowerTargetRotation = math.mul(lowerTargetRotation, quaternion.RotateY(-sign * pose.wristAngle));

            // ALT1: counter upper to keep pitch
            //upperTargetRotation = math.mul(quaternion.Euler(0, sign * bend / 2, 0), upperTargetRotation); 
            // ALT2: more accurate coutering
            var offset = new float2(NoodleConstants.UPPER_ARM_LENGTH_TEMP, 0) + new float2(NoodleConstants.LOWER_ARM_LENGTH_TEMP, 0).Rotate(pose.bend);
            upperTargetRotation = math.mul(quaternion.Euler(0, sign * math.atan2(-offset.y, offset.x), 0), upperTargetRotation);
            // twist
            upperTargetRotation = math.mul(quaternion.Euler(0, 0, -sign * pose.elbowAngle), upperTargetRotation);
            // spread a bit
            upperTargetRotation = math.mul(quaternion.Euler(0, sign * pose.yaw, 0), upperTargetRotation);
            //aim
            upperTargetRotation = math.mul(quaternion.Euler(pose.pitch, 0, 0), upperTargetRotation); // pitch yaw (roll)
        }
        private static void SolveArmLegacyFist(float3 target, ref HandPose pose, bool left = false)
        {
            var a = NoodleConstants.UPPER_ARM_LENGTH_TEMP;
            var b = NoodleConstants.LOWER_ARM_LENGTH_TEMP;

            var neutralYaw = YAW_CENTER;// math.radians(45);
            if (left) neutralYaw = -neutralYaw;

            pose.pitch = re.NormalizeAngle(math.atan2(-target.y, target.z));

            target = target.RotateX(-pose.pitch);
            if (math.lengthsq(target.xz) > re.FLT_EPSILON) // if looking straight up or down any yaw is valid, so preserve current
                pose.yaw = re.NormalizeAngle(math.atan2(target.x, target.z) - neutralYaw);
            else
                pose.yaw -= neutralYaw;

            if (pose.yaw < -math.PI / 2)
            {
                pose.yaw += math.PI;
                pose.pitch = pose.pitch > 0 ? math.PI - pose.pitch : -math.PI - pose.pitch;
            }
            if (pose.yaw > math.PI / 2)
            {
                pose.yaw -= math.PI;
                pose.pitch = pose.pitch > 0 ? math.PI - pose.pitch : -math.PI - pose.pitch;
            }

            pose.yaw += neutralYaw;

            var cosBend = (a * a + b * b - math.lengthsq(target)) / (2 * a * b);
            if (cosBend <= -1)
                pose.bend = 0;
            else if (cosBend >= 1)
                pose.bend = math.PI;
            else
                pose.bend = math.PI - math.acos(cosBend);

            if (pose.bend < math.radians(10))
                pose.bend = math.radians(10);

            if (left)
                pose.yaw = -pose.yaw;
        }

        public static void SolveLimbElbow(float3 target, float a, float b, float maxAngle, ref float pitch, ref float yaw, ref float twist, ref float bend)
        {
            // hand delta rotations from looking forward to pose
            var upper = re.SwingTwistZXY(pitch, yaw, -twist);
            var lower = quaternion.RotateY(-bend);
            var current = math.rotate(upper, new float3(0, 0, a) + math.rotate(lower, new float3(0, 0, b)));

            // calculate targetAngle at elbow (PI = fully extended)
            var targetC = math.length(target);
            var targetAngle = re.SolveTriangleAngle(a, b, targetC);
            if (targetAngle > maxAngle) // ensure minimum bend to minimize singularities
            {
                targetAngle = maxAngle;
                targetC = re.SolveTriangleEdge(a, b, targetAngle); // recalculate c
            }

            var shoulderDelta = re.SolveTriangleAngle(a, targetC, b); // rotation at shoulder, that preserves fist direction with desired bend in elbow
            if (bend > 0) // if already bent pose, calculate shoulderDelta to undo the bend
            {
                var currentAngle = math.PI - bend;
                var currentC = re.SolveTriangleEdge(a, b, currentAngle);
                shoulderDelta -= re.SolveTriangleAngle(a, currentC, b); // undo current rotation
            }
            // swing to target
            upper = math.mul(re.FromToRotation(current, target), upper);

            // bend to target
            upper = math.mul(quaternion.AxisAngle(math.rotate(upper, re.up), shoulderDelta), upper);

            var st = re.ToSwingTwistZXY(upper);
            pitch = st.x;
            yaw = st.y;
            twist = -st.z;

            bend = math.PI - targetAngle;
        }
        public static void SolveLimbFist(float3 target, float a, float b, float maxAngle, ref float pitch, ref float yaw, ref float twist, ref float bend)
        {

            var armRot = re.SwingTwistZXY(pitch, yaw, -twist); // current rotation
            var currentDir = math.rotate(armRot, re.forward); // current dir
            armRot = math.mul(re.FromToRotation(currentDir, target), armRot); // after swinging to target direction

            // extract swing twist
            var st = re.ToSwingTwistZXY(armRot);
            pitch = st.x;
            yaw = st.y;

            // calculate targetAngle at elbow (PI = fully extended)
            var targetC = math.length(target);
            var targetAngle = re.SolveTriangleAngle(a, b, targetC);
            if (targetAngle > math.radians(170)) // ensure minimum bend to minimize singularities
                targetAngle = math.radians(170);
            bend = math.PI - targetAngle;
        }
    }
}
