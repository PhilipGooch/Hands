using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public static class NoodlePoseSolver
    {
        //public static NoodlePose ApplyControlRig(NoodlePose pose, in NoodleDimensions dim, RigidTransform debugPoseTransform)
        //{
        //    //// Calculate FK positions
        //    //var fkPos = NoodlePoseTransforms.GetJointTransforms(pose, dim, false);
        //    //// move CG to supplied in pose
        //    //var cgAnchor = math.transform(fkPos.waist, -dim.hipsToWaist + dim.hipsAnchor); // worldspace cg anchor (point on waist matching hips origin in idle pose)
        //    //fkPos.Transform(new RigidTransform(quaternion.identity, pose.torso.cg-cgAnchor));

        //    // legs IK->FK
        //    var ik = NoodleIK.FromPose(pose, dim);
        //    var targets = new IKTargets()
        //    {
        //        handL = new IKTarget(0, pose.handL.ikPos, float3.zero, pose.handL.ikBlend),
        //        handR = new IKTarget(0, pose.handR.ikPos, float3.zero, pose.handR.ikBlend),
        //        footL = new IKTarget(0, pose.legL.ikPos, float3.zero, pose.legL.ikBlend),
        //        footR = new IKTarget(0, pose.legR.ikPos, float3.zero, pose.legR.ikBlend)
        //    };
        //    NoodleIK.SolveInverseKinematics(ref ik, targets, pose, dim);
        //    //NoodleIK.ToPose(ik, ref pose, dim, out var hipsOffset);
        //    DebugDraw(pose, dim, debugPoseTransform, ik);
        //    return pose;
        //}

        [BurstDiscard]
        public static void DebugDraw(NoodlePose pose, NoodleDimensions dim, RigidTransform debugPoseTransform)//, IKData ik)
        {
#if ENABLE_NOODLE_DEBUG
            using (NoodleDebug.builder.WithMatrix(Matrix4x4.TRS(debugPoseTransform.pos, debugPoseTransform.rot, Vector3.one)))
            {
                NoodleDebug.builder.WireBox(new Bounds(pose.handL.ikPos, new float3(.01f + .09f * pose.handL.muscle.ikDrive)), new Color(1, .75f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.handR.ikPos, new float3(.01f + .09f * pose.handR.muscle.ikDrive)), new Color(1, .75f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.legL.ikPos, new float3(.01f + .09f * pose.legL.ikDrive)), new Color(1, .75f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.legR.ikPos, new float3(.01f + .09f * pose.legR.ikDrive)), new Color(1, .75f, 0));

                var p = NoodlePoseTransforms.GetJointTransforms(pose, dim);//.Transform(debugPoseTransform);
                NoodleDebug.builder.WireBox(new Bounds(pose.handL.ikPosRelative+math.transform(p.chest,dim.armL.chestToUpperArm), new float3(.01f + .09f * pose.handL.muscle.ikDrive)), new Color(1, .5f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.handR.ikPosRelative + math.transform(p.chest, dim.armR.chestToUpperArm), new float3(.01f + .09f * pose.handR.muscle.ikDrive)), new Color(1, .5f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.legL.ikPosRelative + math.transform(p.hips, dim.legL.hipsToUpperLeg), new float3(.01f + .09f * pose.legL.ikDrive)), new Color(1, .5f, 0));
                NoodleDebug.builder.WireBox(new Bounds(pose.legR.ikPosRelative + math.transform(p.hips, dim.legR.hipsToUpperLeg), new float3(.01f + .09f * pose.legR.ikDrive)), new Color(1, .5f, 0));
            }
            //var hipsOffset = float3.zero;
            // draw pose after IK
            //NoodleIK.DebugDraw(ik, debugPoseTransform, Color.cyan);
            NoodlePoseTransforms.DebugDraw(pose, dim, debugPoseTransform, Color.white);
#endif
        }
    }
}
