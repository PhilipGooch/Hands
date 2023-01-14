using NBG.Unsafe;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public unsafe struct IKData
    {

        public float3 root;
        public float3 rootToChest;
        public float3 rootToWaist;

        public float3 shdL;
        public float3 shdR;
        public float3 hipL;
        public float3 hipR;

        public float3 handL;
        public float3 handR;
        public float3 footL;
        public float3 footR;

        // constraints
        internal float shoulderToHipTwist1;
        internal float shoulderToHipTwist2;
        internal float shoulders;
        internal float hips;
        

        public void Translate(float3 offset)
        {
            root += offset;
            shdL += offset;
            shdR += offset;
            hipL += offset;
            hipR += offset;
            handL += offset;
            handR += offset;
            footL += offset;
            footR += offset;
        }
        [BurstDiscard]
        public void AssertFinite()
        {
            re.AssertFinite(root);
            re.AssertFinite(rootToChest);
            re.AssertFinite(rootToWaist);
            re.AssertFinite(shdL);
            re.AssertFinite(shdR);
            re.AssertFinite(hipL);
            re.AssertFinite(hipR);
            re.AssertFinite(handL);
            re.AssertFinite(handR);
            re.AssertFinite(footL);
            re.AssertFinite(footR);
            re.AssertFinite(shoulderToHipTwist1);
            re.AssertFinite(shoulderToHipTwist2);
            re.AssertFinite(shoulders);
            re.AssertFinite(hips);
        }
    }


    [Flags]
    public enum NoodleIKMode 
    {
        Baked=1,
        Runtime=2 ,
        BakedAndRuntime = Baked & Runtime
    }
    public static class NoodleIK
    {
        public const NoodleIKMode ikMode = NoodleIKMode.Runtime;
        public static IKData FromPose(in NoodlePose pose, in NoodleDimensions dim)
        {

            var p = NoodlePoseTransforms.GetJointTransforms(pose, dim);

            var root = (p.waist.pos + p.chest.pos) / 2;
            var data = new IKData()
            {
                root = root,
                rootToChest = p.chest.pos-root,
                rootToWaist = p.waist.pos - root,
                shdL = p.upperArmL.pos,
                shdR = p.upperArmR.pos,
                handL = p.handL,
                handR = p.handR,
                hipL = p.upperLegL.pos,
                hipR = p.upperLegR.pos,
                footL = p.footL,
                footR = p.footR,

            };
            //IKData.ShiftCG(ref data, (pose.cgTarget.To3D() -IKData.CalculateCG(data)).ZeroY()); 

            ProcessContraints<ConstraintReader>(ref data);

            return data;
        }
      
        
        public unsafe static void ToPose(IKData data, ref NoodlePose pose, in NoodleDimensions dim)
        {
            // STEP0: read IK specific dimensions, could cache to IK data
            var spineLen2 = math.length((dim.armL.chestToUpperArm + dim.armR.chestToUpperArm) / 2); // chest anchor to mid shoulder
            var spineLen1 = math.length(dim.waistToChest); // waist to chest
            var spineLen0 = math.length(dim.hipsToWaist - (dim.legL.hipsToUpperLeg + dim.legR.hipsToUpperLeg) / 2); // mid hip to waist

            var aHips = (dim.legR.hipsToUpperLeg + dim.legL.hipsToUpperLeg) / 2;
            var aShoulders = (dim.armR.chestToUpperArm + dim.armL.chestToUpperArm) / 2;
            //  hip anchor coordinates expressed in IK hip triangle
            var hipAnchor = re.ToPlaneCoords(dim.legR.hipsToUpperLeg, dim.hipsToWaist, aHips, float3.zero);

            // read rotation offsets between IK spine segments and spine bodies
            var rotHipsIkToBody = re.FromToRotation(dim.hipsToWaist - aHips, re.up);
            var rotWaistIkToBody = re.FromToRotation(dim.waistToChest, re.up);
            var rotChestIkToBody = re.FromToRotation(aShoulders, re.up);

            // STEP1: calculate spine positions
            // assemble spine segments from data
            var hips = (data.hipL + data.hipR) / 2;
            var shoulders = (data.shdL + data.shdR) / 2;
            var waist = data.root + data.rootToWaist;
            var chest = data.root + data.rootToChest;

            // solve inner spine segments with FABRIK
            for (int i = 0; i < TORSO_SUB_ITERATIONS; i++)
            {
                SolveDistanceConstraint(ref chest, shoulders, spineLen2);
                SolveDistanceConstraint(ref waist, chest, spineLen1);
                SolveDistanceConstraint(ref waist, hips, spineLen0);
                SolveDistanceConstraint(ref chest, waist, spineLen1);
            }

            // STEP2: calculate spine rotations
            // calculate axes for torso segments
            var hipsRight = math.normalize(data.hipR - data.hipL);
            var chestRight = math.normalize(data.shdR - data.shdL);
            var waistRight = math.normalize(hipsRight + chestRight);

            var hipsUp = waist - hips;
            var waistUp = chest - waist;
            var chestUp = shoulders - chest;

            // find rotations for IK torso segments from axes
            var hipsRot = quaternion.LookRotationSafe(math.cross(hipsRight, hipsUp), hipsUp);
            var chestRot = quaternion.LookRotationSafe(math.cross(chestRight, chestUp), chestUp);
            var waistRot = quaternion.LookRotationSafe(math.cross(waistRight, waistUp), waistUp);

            // remap from IK segment to body rotations
            hipsRot = math.mul(hipsRot, rotHipsIkToBody);
            chestRot = math.mul(chestRot, rotChestIkToBody);
            waistRot = math.mul(waistRot, rotWaistIkToBody);

            // STEP3: calculate actual torso positions
            // hips anchor as attached to IK hips triangle
            hipAnchor = re.FromPlaneCoords(data.hipR, waist, hips, hipAnchor);

            // spine anchors based on hipsAnchor and rotations
            hips = hipAnchor;
            waist = hips + math.rotate(hipsRot, dim.hipsToWaist);
            chest = waist + math.rotate(waistRot, dim.waistToChest);
            //shoulders = chest + math.rotate(chestRot, dim.chestToHead);

            // limb connection points
            data.hipL = hips + math.rotate(hipsRot, dim.legL.hipsToUpperLeg);
            data.hipR = hips + math.rotate(hipsRot, dim.legR.hipsToUpperLeg);
            data.shdL = chest + math.rotate(chestRot, dim.armL.chestToUpperArm);
            data.shdR = chest + math.rotate(chestRot, dim.armR.chestToUpperArm);

            // STEP4a: write torso
            //pose.torso.cg = pose.torso.cg.SetY(hipAnchor.y); // just use hip anchor
            //pose.torso.cg = pose.torso.cg.SetY(hipAnchor.y+math.rotate(re.invmul(waistRot, hipsRot),dim.hipsToWaist).y ); // more complete - use relative to waist, but difference should be small to neglect
            pose.torso.hipsRotation = hipsRot;
            pose.torso.chestRotation = chestRot;
            pose.torso.waistRotation = waistRot;

            // STEP4b: solve arms
            var invChestRot = math.inverse(chestRot);// re.SwingTwistYXZ(pose.torso.chestSwingTwist));
            NoodleArmRig.SolveArm(math.rotate(math.slerp(quaternion.identity, invChestRot, pose.handL.fkParent), data.handL - data.shdL), ref pose.handL, left: true);
            NoodleArmRig.SolveArm(math.rotate(math.slerp(quaternion.identity, invChestRot, pose.handR.fkParent), data.handR - data.shdR), ref pose.handR, left: false);

            // STEP4c: solve legs
            var invHipsRot = math.inverse(hipsRot);// re.SwingTwistYXZ(pose.torso.hipsSwingTwist));
            NoodleLegRig.SolveLeg(math.rotate(math.slerp(quaternion.identity, invHipsRot, pose.legL.fkParent), data.footL - data.hipL), ref pose.legL, left: true);
            NoodleLegRig.SolveLeg(math.rotate(math.slerp(quaternion.identity, invHipsRot, pose.legR.fkParent), data.footR - data.hipR), ref pose.legR, left: false);

            // make CG such that root ends up where it was in data
            var animationCoG = NoodlePose.AnimationCoGFromWaistTransform(dim, waistRot, waist); // calculate position that matches ik when no CG shift is used
            pose.torso.cg = animationCoG;
            // calculate center of gravity from pose that matches IK
            var p = NoodlePoseTransforms.GetBodyTransforms(pose, dim, ignoreCG: true);
            var centerOfMass = p.GetCenterOfMass(dim);
            // save center of gravity to pose, so it will be matched when calculating position AND SHIFTING center to specified cg
            pose.torso.cg = centerOfMass.SetY(animationCoG.y);
        }
        // performance vs quality settings
        const bool PRESERVE_CG = true; // expensive but more correct
        const int IK_ITERATIONS = 5;
        const int TORSO_SUB_ITERATIONS = 2;

        const float VERTICAL_BODY_PULL = .5f; // when arm needs to reach it can pull on root making it crouch
        const float HORIZONTAL_BODY_PULL = 0;
        const float SPINE_STIFFNESS = 0.75f;
        const float HAND_PULL = .5f; // how much body is pulled by hand
        const float FOOT_PULL = 1; // how much body is pulled by foot
        const float SHOULDER_PUSH_FORCE = .02f; // how much shoulder is pushed when hand is too close (per iteration, so tune it down)
        const float SHOULDER_REACH_FORCE = .05f; // how much shoulder is pulled too far (per iteration, so tune it down)
        const float PARENT_PUSH_ARM = .25f; // how much shoulder push is pushing on root
        const float PARENT_PUSH_LEG = .25f; // how much hip push is pushing on root

        // calculates weights to use for calculation, so that the resulting point after FBIK is moved from FK towards IK weighted by ikBlend,
        // assuming constraints don't affect the position
        // * wCalc - effect target calculation, lerps from FK position towards ikTarget
        // * wApply - moves point position towards target, each iteration

        private static void CalculateWeights(int iterations, float ikBlend, out float wCalc, out float wApply)
        {
            // move 
            wCalc = 1;// math.pow(ikBlend, .5f);
            wApply = 1 - math.pow(math.saturate(1-ikBlend), 1f / (iterations + 1));
            re.AssertFinite(wCalc);
            re.AssertFinite(wApply);
        }

        public static void SolveInverseKinematics(ref NoodlePose pose, in NoodleDimensions dim, bool recalculateIK, RigidTransform debugPoseTransform)
        {
            var ik = NoodleIK.FromPose(pose, dim);
            
            SolveInverseKinematics(ref ik, ref pose, dim, recalculateIK);
            NoodleIK.ToPose(ik, ref pose, dim);

            // if 
            //NoodleIK.DebugDraw(ik, debugPoseTransform, Color.red);
            //NoodlePoseTransforms.DebugDraw(pose, dim, debugPoseTransform, Color.green);
        }

       
        public static void SolveInverseKinematics(ref IKData data, ref NoodlePose pose, in NoodleDimensions dim, bool recalculateIK)
        {

            var handL = pose.handL;
            var handR = pose.handR;
            var footL = pose.legL;
            var footR = pose.legR;
            var pivot = pose.pivotR.ikBlend > pose.pivotL.ikBlend ? pose.pivotR : pose.pivotL;
            
            var maxArm = re.SolveTriangleEdge(NoodleConstants.UPPER_ARM_LENGTH_TEMP, NoodleConstants.LOWER_ARM_LENGTH_TEMP, math.radians(170));
            var maxLeg = NoodleConstants.LOWER_LEG_LENGTH_TEMP + NoodleConstants.UPPER_LEG_LENGTH_TEMP;
            PullBody(ref data, pose, maxArm);

            var rootToShdL = math.length(data.shdL - data.root);
            var rootToShdR = math.length(data.shdR - data.root);
            var rootToHipL = math.length(data.hipL - data.root);
            var rootToHipR = math.length(data.hipR - data.root);

            var handToShdL = math.length(data.shdL - data.handL);
            var handToShdR = math.length(data.shdR - data.handR);
            var footToHipL = math.length(data.hipL - data.footL);
            var footToHipR = math.length(data.hipR - data.footR);

            CalculateWeights(IK_ITERATIONS, handL.ikBlend, out var wcHandL, out var waHandL);
            CalculateWeights(IK_ITERATIONS, handR.ikBlend, out var wcHandR, out var waHandR);
            CalculateWeights(IK_ITERATIONS, footL.ikBlend,  out var wcFootL, out var waFootL);
            CalculateWeights(IK_ITERATIONS, footR.ikBlend,  out var wcFootR, out var waFootR);
            
            CalculateWeights(IK_ITERATIONS, pivot.ikBlend, out var wc2Hand, out var wa2Hand);
            //Debug.Log($"{pivot.ikBlend} {pivot.offset} {pivot.anchor} {wa2Hand}");
            // global positions
            var handLposG = math.lerp(data.handL, handL.ikPos, wcHandL);
            var handRposG = math.lerp(data.handR, handR.ikPos, wcHandR);
            var footLposG = math.lerp(data.footL, footL.ikPos, wcFootL);
            var footRposG = math.lerp(data.footR, footR.ikPos, wcFootR);

            // local positions
            var handLposL = math.lerp(data.handL - data.shdL, handL.ikPosRelative.Clamp(maxArm), wcHandL);
            var handRposL = math.lerp(data.handR - data.shdR, handR.ikPosRelative.Clamp(maxArm), wcHandR);
            var footLposL = math.lerp(data.footL - data.hipL, footL.ikPosRelative.Clamp(maxLeg), wcFootL);
            var footRposL = math.lerp(data.footR - data.hipR, footR.ikPosRelative.Clamp(maxLeg), wcFootR);

            for (int iteration = 0; iteration < IK_ITERATIONS; iteration++)
            {
                // apply end effectors && 2 hand constraint
                data.handL = math.lerp(math.lerp(data.handL, handLposG, waHandL), data.shdL + math.lerp(data.handL - data.shdL, handLposL, waHandL), handL.ikParent);
                data.handR = math.lerp(math.lerp(data.handR, handRposG, waHandR), data.shdR + math.lerp(data.handR - data.shdR, handRposL, waHandR), handR.ikParent);
                data.footL = math.lerp(math.lerp(data.footL, footLposG, waFootL), data.hipL + math.lerp(data.footL - data.hipL, footLposL, waFootL), footL.ikParent);
                data.footR = math.lerp(math.lerp(data.footR, footRposG, waFootR), data.hipR + math.lerp(data.footR - data.hipR, footRposL, waFootR), footR.ikParent);
                OffsetConstraint(ref data.handL, ref data.handR, pivot, wa2Hand);


                // Push
                data.root += Push(data.handL, ref data.shdL, maxArm) * PARENT_PUSH_ARM;
                data.root += Push(data.handR, ref data.shdR, maxArm) * PARENT_PUSH_ARM;
                data.root += Push(data.footL, ref data.hipL, maxLeg) * PARENT_PUSH_LEG;
                data.root += Push(data.footR, ref data.hipR, maxLeg) * PARENT_PUSH_LEG;

                // Reach
                Reach(ref data.handL, ref data.shdL, maxArm,handToShdL);
                Reach(ref data.handR, ref data.shdR, maxArm,handToShdR);
                Reach(ref data.footL, ref data.hipL, maxLeg,footToHipL);
                Reach(ref data.footR, ref data.hipR, maxLeg,footToHipR);

                // TODO: Maybe apply non end effectors (hips, shoulders)

                // Forward reach limbs
                data.shdL = re.MoveTowards(data.handL, data.shdL, maxArm);
                data.shdR = re.MoveTowards(data.handR, data.shdR, maxArm);
                data.hipL = re.MoveTowards(data.footL, data.hipL, maxLeg);
                data.hipR = re.MoveTowards(data.footR, data.hipR, maxLeg);

                var centroid = data.root;
                for (int i = 0; i < TORSO_SUB_ITERATIONS; i++)
                    ProcessContraints<ConstraintSolver>(ref data);
                float pullSum = 2 * HAND_PULL + 2 * FOOT_PULL;
                if (pullSum > 0)
                {
                    centroid += CalculateError(data.shdL - data.root, rootToShdL) * HAND_PULL / pullSum;
                    centroid += CalculateError(data.shdR - data.root, rootToShdR) * HAND_PULL / pullSum;
                    centroid += CalculateError(data.hipL - data.root, rootToHipL) * HAND_PULL / pullSum;
                    centroid += CalculateError(data.hipR - data.root, rootToHipR) * HAND_PULL / pullSum;
                }
                data.root = centroid;

                // TODO: Maybe apply non end effectors (hips, shoulders)

                // Back reach
                SolveDistanceConstraint(ref data.shdL, centroid, rootToShdL);
                SolveDistanceConstraint(ref data.shdR, centroid, rootToShdR);
                SolveDistanceConstraint(ref data.hipL, centroid, rootToHipL);
                SolveDistanceConstraint(ref data.hipR, centroid, rootToHipR);

                for (int i = 0; i < TORSO_SUB_ITERATIONS; i++)
                    ProcessContraints<ConstraintSolver>(ref data);

                // Children back reach
                data.handL = re.MoveTowards(data.shdL, data.handL, maxArm);
                data.handR = re.MoveTowards(data.shdR, data.handR, maxArm);
                data.footL = re.MoveTowards(data.hipL, data.footL, maxLeg);
                data.footR = re.MoveTowards(data.hipR, data.footR, maxLeg);

                if (PRESERVE_CG)
                {
                    // apply end effectors
                    data.handL = math.lerp(math.lerp(data.handL, handLposG, waHandL), data.shdL + math.lerp(data.handL - data.shdL, handLposL, waHandL), handL.ikParent);
                    data.handR = math.lerp(math.lerp(data.handR, handRposG, waHandR), data.shdR + math.lerp(data.handR - data.shdR, handRposL, waHandR), handR.ikParent);
                    data.footL = math.lerp(math.lerp(data.footL, footLposG, waFootL), data.hipL + math.lerp(data.footL - data.hipL, footLposL, waFootL), footL.ikParent);
                    data.footR = math.lerp(math.lerp(data.footR, footRposG, waFootR), data.hipR + math.lerp(data.footR - data.hipR, footRposL, waFootR), footR.ikParent);
                    OffsetConstraint(ref data.handL, ref data.handR, pivot, wa2Hand);
                    // move data so it's CG matches desired CG
                    var poseCopy = pose;
                    ToPose(data, ref poseCopy, dim); // resolve pose
                    data.Translate((pose.torso.cg - poseCopy.torso.cg).SetY(0));

                }
            }

            // apply end effectors once more
            data.handL = math.lerp(math.lerp(data.handL, handLposG, waHandL), data.shdL + math.lerp(data.handL - data.shdL, handLposL, waHandL), handL.ikParent);
            data.handR = math.lerp(math.lerp(data.handR, handRposG, waHandR), data.shdR + math.lerp(data.handR - data.shdR, handRposL, waHandR), handR.ikParent);
            data.footL = math.lerp(math.lerp(data.footL, footLposG, waFootL), data.hipL + math.lerp(data.footL - data.hipL, footLposL, waFootL), footL.ikParent);
            data.footR = math.lerp(math.lerp(data.footR, footRposG, waFootR), data.hipR + math.lerp(data.footR - data.hipR, footRposL, waFootR), footR.ikParent);
            OffsetConstraint(ref data.handL, ref data.handR, pivot, wa2Hand);

            // TODO: update ik positions for 2hand carry
            if (pose.pivotR.isTwoHanded || recalculateIK)
            {
                pose.handL.ikPos = data.handL;//.FlipX();
                pose.handL.ikPosRelative = (data.handL - data.shdL);//.FlipX();
                pose.handR.ikPos = data.handR;
                pose.handR.ikPosRelative = data.handR - data.shdR;
            }
            if(recalculateIK)
            { 
                pose.legL.ikPos = data.footL.FlipX();
                pose.legL.ikPosRelative = (data.footL - data.hipL).FlipX();
                pose.legR.ikPos = data.footR;
                pose.legR.ikPosRelative = data.footR - data.hipR;
            }
        }

        private static float3 Push(float3 hand, ref float3 shoulder, float maxArm)
        {
            var dir = hand- shoulder;
            var current = math.length(dir);
            if (current < re.FLT_EPSILON) return float3.zero;
            var straight = dir * (maxArm / current);

            var weight = 1 - current / maxArm;
            if (weight < 0) return float3.zero;
            weight *= weight*weight; // exponential push
           
            var offset = - straight * weight * SHOULDER_PUSH_FORCE;
            shoulder += offset;
            return offset;
        }

        // prevents arm/leg being overstretched- bring shoulder closer
        private static void Reach(ref float3 hand, ref float3 shoulder, float maxArm, float nominal)
        {
            var dir = hand - shoulder;
            var current = math.length(dir);
            if (current < re.FLT_EPSILON) return;
            if (current < nominal) return;// don't reach if matches animation
            var straight = dir * (maxArm / current);

            var targetStretch = 0;// 1 - SHOULDER_REACH_FORCE;
            var weight = current / maxArm - targetStretch;
            if (weight <= 0) return;

            weight *= weight; // exponential

            //weight *= re.InverseLerp(nominal, maxArm, current); // effect applied only when moving away from animated length
            weight *= re.InverseLerp(nominal, nominal+maxArm/2, current); // effect applied only when moving away from animated length

            var offset = straight * weight * SHOULDER_REACH_FORCE;
            shoulder += offset;
            hand += offset;
        }

        static float3 CalculateError(float3 offset, float length)
        {
            return offset - math.normalize(offset) * length;
        }
        static void SolveDistanceConstraint(ref float3 pos, float3 anchor, float length)
        {
            pos -= CalculateError(pos - anchor, length);
        }
        static void OffsetConstraint(ref float3 posA, ref float3 posB, in PivotPose pivot, float w)
        {
            if (!pivot.isTwoHanded) return;// inactive
            float3 offset = pivot.offset;
            var rot = pivot.ToRotation(false, 0);
            offset = math.rotate(rot, offset);
            float anchor = pivot.anchor;
            

            var e = (posB - posA-offset)*w;

            posB -= e * (1 - anchor);
            posA += e * anchor;
        }



        private static void PullBody(ref IKData data, NoodlePose pose, float maxArm)
        {
            var offset = GetHandPull(data.shdL, pose.handL, maxArm) + GetHandPull(data.shdR, pose.handR, maxArm);
            var verticalPull = VERTICAL_BODY_PULL;
            var horizontalPull = HORIZONTAL_BODY_PULL;
            offset = (offset.ZeroY() * horizontalPull).SetY(offset.y * verticalPull);
            data.root += offset;
            data.hipL += offset; 
            data.hipR += offset;
            //IKData.ShiftCG(ref data, offset);
        }

        private static float3 GetHandPull(float3 shoulderPos, HandPose pose, float maxArm)
        {
            var dir = pose.ikPos - shoulderPos;
            return (dir - dir.Clamp(maxArm)) * pose.ikBlend * (1-pose.ikParent);
        }

        [BurstDiscard]
        public static void DebugDraw(IKData data, RigidTransform x, Color color)
        {
#if ENABLE_NOODLE_DEBUG
            // root to hips
            NoodleDebug.builder.Line(math.transform(x, float3.zero), math.transform(x, data.root), color);
            NoodleDebug.builder.Line(math.transform(x, data.root), math.transform(x, data.shdL), color);
            NoodleDebug.builder.Line(math.transform(x, data.root), math.transform(x, data.shdR), color);
            NoodleDebug.builder.Line(math.transform(x, data.root), math.transform(x, data.hipL), color);
            NoodleDebug.builder.Line(math.transform(x, data.root), math.transform(x, data.hipR), color);


            // torso
            NoodleDebug.builder.Line(math.transform(x, data.shdL), math.transform(x, data.shdR), color);
            NoodleDebug.builder.Line(math.transform(x, data.hipL), math.transform(x, data.hipR), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderL), math.transform(x, data.hipL), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderR), math.transform(x, data.hipR), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderL), math.transform(x, data.hipR), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderR), math.transform(x, data.hipL), color);

            //NoodleDebug.builder.Line(math.transform(x, data.waist), math.transform(x, data.chest), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderL), math.transform(x, data.chest), color);
            //NoodleDebug.builder.Line(math.transform(x, data.shoulderR), math.transform(x, data.chest), color);
            //NoodleDebug.builder.Line(math.transform(x, data.hipL), math.transform(x, data.waist), color);
            //NoodleDebug.builder.Line(math.transform(x, data.hipR), math.transform(x, data.waist), color);

            // limbs
            NoodleDebug.builder.Line(math.transform(x, data.shdL), math.transform(x, data.handL), color);
            NoodleDebug.builder.Line(math.transform(x, data.shdR), math.transform(x, data.handR), color);
            NoodleDebug.builder.Line(math.transform(x, data.hipL), math.transform(x, data.footL), color);
            NoodleDebug.builder.Line(math.transform(x, data.hipR), math.transform(x, data.footR), color);
#endif
        }
       
        public interface IConstraintProcessing 
        { 
            void Distance(ref float3 p1, ref float3 p2, ref float dist, float push, float pull);
            void Triangle(ref float3 p1, ref float3 p2, ref float3 p3, ref float3 distances, float weight); 
        }

        public struct ConstraintReader : IConstraintProcessing
        {
            public void Distance(ref float3 p1, ref float3 p2, ref float dist, float push, float pull)
            {
                dist = math.length(p1 - p2);
            }

            public void Triangle(ref float3 p1, ref float3 p2, ref float3 p3, ref float3 distances, float weight)
            {
                distances = new float3(
                    math.length(p1 - p2),
                    math.length(p2 - p3),
                    math.length(p3 - p1));
            }
        }
        public struct ConstraintSolver : IConstraintProcessing
        {
            public void Distance(ref float3 p1, ref float3 p2, ref float dist, float push, float pull)
            {
                float3 a = p1 - p2;
                var current = math.length(a);
                if (current < re.FLT_EPSILON) return;
                var weight = current < dist ? push : pull;
                var error = current - dist;
                a *= -weight * .5f * error / current;
                p1 += a;
                p2 += -a;
            }

            public void Triangle(ref float3 p1, ref float3 p2, ref float3 p3, ref float3 distances, float weight)
            {
                float3 a = p1 - p2;
                float3 b = p2 - p3;
                float3 c = p3 - p1;
                var current = new float3(math.length(a),math.length(b),math.length(c));
                if (math.cmin(current) < re.FLT_EPSILON) return;
                var error = current - distances;
                a *= -weight * .5f * error.x / current.x;
                b *= -weight * .5f * error.y / current.y;
                c *= -weight * .5f * error.z / current.z;
                p1 += a - c;
                p2 += b - a;
                p3 += c - b;
            }
        }

        public static void ProcessContraints<T>( ref IKData data) where T: unmanaged,IConstraintProcessing
        {
            var p = default(T);
            p.Distance(ref data.shdL, ref data.shdR, ref data.shoulders, 1,1); // hard constraint for shoulder distance
            p.Distance(ref data.hipL, ref data.hipR, ref data.hips, 1,1); // hard constraint for hip distance

            // soft constraints diagonally
            p.Distance(ref data.shdL, ref data.hipR, ref data.shoulderToHipTwist1, SPINE_STIFFNESS, 0);
            p.Distance(ref data.shdR, ref data.hipL, ref data.shoulderToHipTwist2, SPINE_STIFFNESS, 0);


            //p.Distance(ref data.shoulderL, ref data.hipL, ref data.shoulderToHipL, .025f);
            //p.Distance(ref data.shoulderR, ref data.hipR, ref data.shoulderToHipR, .025f);
            //p.Distance(ref data.shoulderL, ref data.hipR, ref data.shoulderToHipTwist1, .025f);
            //p.Distance(ref data.shoulderR, ref data.hipL, ref data.shoulderToHipTwist2, .025f);
            //p.Distance(ref data.waist, ref data.chest, ref data.waistToChest, 1);
            //p.Triangle(ref data.shoulderL, ref data.shoulderR, ref data.chest, ref data.chestTriangle, 1);
            //p.Triangle(ref data.hipL, ref data.hipR, ref data.waist, ref data.hipsTriangle, 1);

        }

    }
}
