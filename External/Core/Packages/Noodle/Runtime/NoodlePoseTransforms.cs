using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct NoodleArmDimensions
    {
        public float3 shoulderAnchor;
        public float3 upperAnchor;
        public float3 lowerAnchor;
        public float3 handAnchor;
        // dimensions
        public float3 chestToUpperArm;
        public float3 upperToLowerArm => new float3(0, upperArmLength, 0);//=> new float3(-upperToLowerArmR.x, upperToLowerArmR.y, upperToLowerArmR.z);
        public float3 lowerArmToHand => new float3(0, lowerArmLength, 0);
        public float upperArmLength => NoodleConstants.UPPER_ARM_LENGTH_TEMP;
        public float lowerArmLength => NoodleConstants.LOWER_ARM_LENGTH_TEMP;
        public NoodleArmDimensions(List<ArticulationReaderLink> structure, int chest, int upper, int lower, float3 handAnchor)
        {
            this.handAnchor = handAnchor;
            var _upperLinear = structure[upper];
            var _lowerLinear = structure[lower];
            upperAnchor = _upperLinear.anchor;
            lowerAnchor = _lowerLinear.anchor;
            shoulderAnchor = _upperLinear.connectedAnchor;
            chestToUpperArm = _upperLinear.connectedAnchor - structure[chest].anchor;

        }
    }
    public struct NoodleLegDimensions
    {
        public float3 hipAnchor;
        public float3 upperAnchor;
        public float3 lowerAnchor;
        public float3 footAnchor;
        // dimensions
        public float3 hipsToUpperLeg;
        public float3 upperToLowerLeg;
        public float3 lowerLegToFoot => new float3(0, NoodleConstants.LOWER_LEG_LENGTH_TEMP, 0); //footAnchor;

        public NoodleLegDimensions(List<ArticulationReaderLink> structure, float3 hipsAnchor, int upper, int lower, float3 footAnchor)
        {
            this.footAnchor = footAnchor;
            //            = new float3(0, NoodleConstants.LOWER_LEG_LENGTH_TEMP, 0);
            var _upperLinear = structure[upper];
            var _lowerLinear = structure[lower];
            upperAnchor = _upperLinear.anchor;
            lowerAnchor = _lowerLinear.anchor;
            hipAnchor = _upperLinear.connectedAnchor;
            hipsToUpperLeg = _upperLinear.connectedAnchor - hipsAnchor;// - joints.GetJoint<LinearArticulationJoint>(hips).anchorB;
            upperToLowerLeg = _lowerLinear.connectedAnchor - _upperLinear.anchor;
        }
    }
    public unsafe struct NoodleDimensions
    {
        public float ballRadius;
        public float3 hipsAnchor;
        public float3 hipsToWaist;
        public float3 waistAnchor;
        public float3 waistToChest;
        public float3 chestToHead;
        public float3 chestAnchor;
        public float3 headAnchor;

        public NoodleArmDimensions armL;
        public NoodleArmDimensions armR;
        public NoodleLegDimensions legL;
        public NoodleLegDimensions legR;

        // masses
        public float massHips;
        public float massWaist;
        public float massChest;
        public float massHead;
        public float massUpperArm;
        public float massLowerArm;
        public float massUpperLeg;
        public float massLowerLeg;
        public float suspendedMass; // mass without ball
        public float totalMass;
    }


    public struct NoodlePoseTransforms
    {
        public RigidTransform ball;
        public RigidTransform hips;
        public RigidTransform waist;
        public RigidTransform chest;
        public RigidTransform head;
        public RigidTransform upperArmL;
        public RigidTransform upperArmR;
        public RigidTransform lowerArmL;
        public RigidTransform lowerArmR;
        public RigidTransform upperLegL;
        public RigidTransform upperLegR;
        public RigidTransform lowerLegL;
        public RigidTransform lowerLegR;

        // endpoints
        public float3 footL;
        public float3 footR;
        public float3 handL;
        public float3 handR;

        public static NoodlePoseTransforms GetJointTransforms(in NoodlePose pose, in NoodleDimensions dim, bool ignoreCG = false)
        {

            var hipsRot = pose.torso.hipsRotation;
            var waistRot = pose.torso.waistRotation;
            var chestRot = pose.torso.chestRotation;
            var headRot = quaternion.Euler(pose.head.pitch, pose.head.yaw, pose.head.roll);
            headRot = math.mul(math.slerp(quaternion.identity, chestRot, pose.head.fkParent), headRot);
            NoodleArmRig.CalculateArmTargetRotations(pose.handL, chestRot, out var upperArmRotL, out var lowerArmRotL, left: true);
            NoodleArmRig.CalculateArmTargetRotations(pose.handR, chestRot, out var upperArmRotR, out var lowerArmRotR, left: false);
            NoodleLegRig.CalculateLegTargetRotations(pose.legL, hipsRot, out var upperLegRotL, out var lowerLegRotL, left: true);
            NoodleLegRig.CalculateLegTargetRotations(pose.legR, hipsRot, out var upperLegRotR, out var lowerLegRotR, left: false);

            lowerArmRotL = math.mul(upperArmRotL, lowerArmRotL);
            lowerArmRotR = math.mul(upperArmRotR, lowerArmRotR);
            lowerLegRotL = math.mul(upperLegRotL, lowerLegRotL);
            lowerLegRotR = math.mul(upperLegRotR, lowerLegRotR);

            var p = new NoodlePoseTransforms();
            p.ball = new RigidTransform(quaternion.identity, new float3(0, dim.ballRadius, 0));
            //p.hips = new RigidTransform(hipsRot,  new float3(0, pose.torso.hipsHeight, 0));// -math.rotate(hipsRot,dim.hipsAnchor);
            //p.waist = new RigidTransform(waistRot, math.transform(p.hips, dim.hipsToWaist));

            // Rig CG
            p.waist = NoodlePose.WaistTransformFromAnimationCoG(dim, waistRot, pose.torso.cg);// new RigidTransform(waistRot,re.up*pose.torso.cg.y+ math.rotate(waistRot, dim.hipsToWaist));
            p.hips = new RigidTransform(hipsRot, p.waist.pos - math.rotate(hipsRot, dim.hipsToWaist));
            p.chest = new RigidTransform(chestRot, math.transform(p.waist, dim.waistToChest));
            p.head = new RigidTransform(headRot, math.transform(p.chest, dim.chestToHead));

            p.upperLegL = new RigidTransform(upperLegRotL, math.transform(p.hips, dim.legL.hipsToUpperLeg));
            p.upperLegR = new RigidTransform(upperLegRotR, math.transform(p.hips, dim.legR.hipsToUpperLeg));
            p.lowerLegL = new RigidTransform(lowerLegRotL, math.transform(p.upperLegL, dim.legL.upperToLowerLeg));
            p.lowerLegR = new RigidTransform(lowerLegRotR, math.transform(p.upperLegR, dim.legR.upperToLowerLeg));

            p.upperArmL = new RigidTransform(upperArmRotL, math.transform(p.chest, dim.armL.chestToUpperArm));
            p.upperArmR = new RigidTransform(upperArmRotR, math.transform(p.chest, dim.armR.chestToUpperArm));
            p.lowerArmL = new RigidTransform(lowerArmRotL, math.transform(p.upperArmL, dim.armL.upperToLowerArm));
            p.lowerArmR = new RigidTransform(lowerArmRotR, math.transform(p.upperArmR, dim.armR.upperToLowerArm));

            p.footL = math.transform(p.lowerLegL, dim.legL.lowerLegToFoot);
            p.footR = math.transform(p.lowerLegR, dim.legR.lowerLegToFoot);
            p.handL = math.transform(p.lowerArmL, dim.armL.lowerArmToHand);
            p.handR = math.transform(p.lowerArmR, dim.armR.lowerArmToHand);

            p.ball.pos = p.ball.pos.SetY(dim.ballRadius);

            // move CG
            if (!ignoreCG)
            {
                var cg = p.ToBodyTransforms(dim).GetCenterOfMass(dim);
                var cgOffset = (pose.torso.cg - cg).SetY(0);
                p = p.Transform(RigidTransform.Translate(cgOffset));
            }

            return p;
        }

        public NoodlePoseTransforms Transform(RigidTransform root)
        {
            return new NoodlePoseTransforms()
            {
                ball = math.mul(root, ball),
                hips = math.mul(root, hips),
                waist = math.mul(root, waist),
                chest = math.mul(root, chest),
                head = math.mul(root, head),
                upperLegL = math.mul(root, upperLegL),
                upperLegR = math.mul(root, upperLegR),
                lowerLegL = math.mul(root, lowerLegL),
                lowerLegR = math.mul(root, lowerLegR),
                upperArmL = math.mul(root, upperArmL),
                upperArmR = math.mul(root, upperArmR),
                lowerArmL = math.mul(root, lowerArmL),
                lowerArmR = math.mul(root, lowerArmR),

                footL = math.transform(root, footL),
                footR = math.transform(root, footR),
                handL = math.transform(root, handL),
                handR = math.transform(root, handR),
            };
        }

        static RigidTransform Reanchor(RigidTransform t, float3 anchor)
        {
            return new RigidTransform(t.rot, math.transform(t, anchor));
        }
        public NoodlePoseTransforms ToBodyTransforms(in NoodleDimensions dim)
        {
            return new NoodlePoseTransforms()
            {
                ball = ball,
                hips = Reanchor(hips, -dim.hipsAnchor),
                waist = Reanchor(waist, -dim.waistAnchor),
                chest = Reanchor(chest, -dim.chestAnchor),
                head = Reanchor(head, -dim.headAnchor),
                upperLegL = Reanchor(upperLegL, -dim.legL.upperAnchor),
                upperLegR = Reanchor(upperLegR, -dim.legR.upperAnchor),
                lowerLegL = Reanchor(lowerLegL, -dim.legL.lowerAnchor),
                lowerLegR = Reanchor(lowerLegR, -dim.legR.lowerAnchor),
                upperArmL = Reanchor(upperArmL, -dim.armL.upperAnchor),
                upperArmR = Reanchor(upperArmR, -dim.armR.upperAnchor),
                lowerArmL = Reanchor(lowerArmL, -dim.armL.lowerAnchor),
                lowerArmR = Reanchor(lowerArmR, -dim.armR.lowerAnchor),
                footL = footL,
                footR = footR,
                handL = handL,
                handR = handR
            };
        }

        public static NoodlePoseTransforms GetBodyTransforms(in NoodlePose pose, in NoodleDimensions dim, bool ignoreCG = false)
        {
            return GetJointTransforms(pose, dim, ignoreCG)
                .ToBodyTransforms(dim);
        }

        public float3 GetCenterOfMass(in NoodleDimensions dim)
        {
            float3 sum = float3.zero;
            float totalMass = 0;
            void add(float m, RigidTransform t) { sum += m * t.pos; totalMass += m; }

            add(dim.massHips, hips);
            add(dim.massWaist, waist);
            add(dim.massChest, chest);
            add(dim.massHead, head);
            add(dim.massUpperArm, upperArmL);
            add(dim.massUpperArm, upperArmR);
            add(dim.massLowerArm, lowerArmL);
            add(dim.massLowerArm, lowerArmR);
            add(dim.massUpperLeg, upperLegL);
            add(dim.massUpperLeg, upperLegR);
            add(dim.massLowerLeg, lowerLegL);
            add(dim.massLowerLeg, lowerLegR);
            return sum / totalMass;

        }

        [BurstDiscard]
        public static void DebugDraw(in NoodlePose pose, in NoodleDimensions dim, RigidTransform x, Color color, bool ignoreCG = false)
        {
#if ENABLE_NOODLE_DEBUG
            var p = GetJointTransforms(pose, dim, ignoreCG);
            p = p.Transform(x);
            DebugDraw(p,x, color);
            DebugPivot(pose.pivotL, x, p.handL, p.lowerArmL.rot);
            DebugPivot(pose.pivotR, x, p.handR, p.lowerArmR.rot);
        }

        private static void DebugPivot(PivotPose pivot, RigidTransform x, float3 hand, quaternion handRot)
        {
            if (!pivot.isEmpty)
            {
                var rot = pivot.ToRotation(false, 0);
                
                if (pivot.fkParent > 0)
                    rot = math.mul(handRot, rot);
                else
                    rot = math.mul(x.rot, rot);
                NoodleDebug.builder.Ray(hand, math.rotate(rot, re.right * .5f), Color.red);
                NoodleDebug.builder.Ray(hand, math.rotate(rot, re.up * .5f), Color.green);
                NoodleDebug.builder.Ray(hand, math.rotate(rot, re.forward * .5f), Color.blue);
            }
        }

        public static void DebugDraw(NoodlePoseTransforms p, RigidTransform x, Color color )
        {
            
            // root to hips
            NoodleDebug.builder.Line(x.pos, p.waist.pos, color);

            // torso
            NoodleDebug.builder.Line(p.hips.pos, p.waist.pos, color);
            NoodleDebug.builder.Line(p.waist.pos, p.chest.pos, color);
            NoodleDebug.builder.Line(p.chest.pos, p.head.pos, color);
            NoodleDebug.builder.Line(p.head.pos, math.transform(p.head, re.up * .3f), color);

            NoodleDebug.builder.Line(p.chest.pos, p.upperArmL.pos, color);
            NoodleDebug.builder.Line(p.chest.pos, p.upperArmR.pos, color);
            NoodleDebug.builder.Line(p.upperArmL.pos, p.lowerArmL.pos, color);
            NoodleDebug.builder.Line(p.upperArmR.pos, p.lowerArmR.pos, color);
            NoodleDebug.builder.Line(p.lowerArmL.pos, p.handL, color);
            NoodleDebug.builder.Line(p.lowerArmR.pos, p.handR, color);

            NoodleDebug.builder.Line(p.hips.pos, p.upperLegL.pos, color);
            NoodleDebug.builder.Line(p.hips.pos, p.upperLegR.pos, color);
            NoodleDebug.builder.Line(p.upperLegL.pos, p.lowerLegL.pos, color);
            NoodleDebug.builder.Line(p.upperLegR.pos, p.lowerLegR.pos, color);
            NoodleDebug.builder.Line(p.lowerLegL.pos, p.footL, color);
            NoodleDebug.builder.Line(p.lowerLegR.pos, p.footR, color);
#endif 
        }
    }
}
