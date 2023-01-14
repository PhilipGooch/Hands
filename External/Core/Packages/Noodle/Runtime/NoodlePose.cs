using Noodles;
using Recoil;
using Noodles.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct LegPose
    {
        // FK
        public float pitch;
        public float stretch;
        public float bend;
        public float twist;
        public float fkParent; // 0 relative to root, 1 relative to hips

        // IK
        public float3 ikPos; // relative to root
        public float3 ikPosRelative; // relative to hips - calculated, not exposed in editor
        public float ikParent; // 0 - anchor to ball, when 1 to hips
        public float ikBlend; // FK vs IK when calculating final position

        // muscles
        public float upperTonus;
        public float lowerTonus;
        public float ikDrive; // ik spring tonus
        

        public void ConvertToRuntime()
        {
            pitch = math.radians(pitch);
            stretch = math.radians(stretch);
            bend = math.radians(bend);
            twist = math.radians(twist);
        }
        public void ConvertFromRuntime()
        {
            pitch = math.degrees(pitch);
            stretch = math.degrees(stretch);
            bend = math.degrees(bend);
            twist = math.degrees(twist);
        }
        public LegPose ToRadians()
        {
            var res = this;
            res.ConvertToRuntime();
            return res;
        }
        public LegPose flipped { get { var r = this; r.Flip(); return r; } }
        public void Flip()
        {
            ikPos.x *= -1;
            ikPosRelative.x *= -1;
        }
        public static LegPose defaultPose => new LegPose() { fkParent=1, ikBlend = 1, upperTonus = 1, lowerTonus = 1, ikDrive = 1, ikPos = new float3(.2f, 0.05f, 0)  };

        public static LegPose Add(in LegPose a, in LegPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            var d = new LegPose();
            if (pose)
            {
                result.pitch += (b.pitch - d.pitch) * weight;
                result.stretch += (b.stretch - d.stretch) * weight;
                result.bend += (b.bend - d.bend) * weight;
                result.twist += (b.twist - d.twist) * weight;
                result.ikPos += (b.ikPos - d.ikPos) * weight;
                result.ikPosRelative += (b.ikPosRelative - d.ikPosRelative) * weight;
                result.fkParent += (b.fkParent - d.fkParent) * weight;
                result.ikParent += (b.ikParent - d.ikParent) * weight;
                result.ikBlend += (b.ikBlend - d.ikBlend) * weight;
            }
            if (muscle)
            {
                result.upperTonus += (b.upperTonus - d.upperTonus) * weight;
                result.lowerTonus += (b.lowerTonus - d.lowerTonus) * weight;
                result.ikDrive += (b.ikDrive - d.ikDrive) * weight;
            };
            return result;
        }
        public static LegPose Blend(in LegPose a, in LegPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            if (pose)
            {
                result.pitch = math.lerp(a.pitch, b.pitch, weight);
                result.stretch = math.lerp(a.stretch, b.stretch, weight);
                result.bend = math.lerp(a.bend, b.bend, weight);
                result.twist = math.lerp(a.twist, b.twist, weight);
                //result.ikPos = math.lerp(a.ikPos, b.ikPos, weight);
                //result.ikPosRelative = math.lerp(a.ikPosRelative, b.ikPosRelative, weight);
                result.ikPos = re.LerpWeighted(a.ikPos, a.ikBlend, b.ikPos, b.ikBlend, weight);
                result.ikPosRelative = re.LerpWeighted(a.ikPosRelative, a.ikBlend, b.ikPosRelative, b.ikBlend, weight);
                result.fkParent = math.lerp(a.fkParent, b.fkParent, weight);
                result.ikParent = math.lerp(a.ikParent, b.ikParent, weight);
                result.ikBlend = math.lerp(a.ikBlend, b.ikBlend, weight);
            }
            if (muscle)
            {
                result.upperTonus = math.lerp(a.upperTonus, b.upperTonus, weight);
                result.lowerTonus = math.lerp(a.lowerTonus, b.lowerTonus, weight);
                result.ikDrive = math.lerp(a.ikDrive, b.ikDrive, weight);
            };
            return result;
        }
        public override string ToString()
        {
            return $"FK: {math.degrees(pitch)} {math.degrees(stretch)} {math.degrees(bend)} {math.degrees(twist)} {fkParent} IK: {ikPos} {ikPosRelative} {ikParent} {ikBlend} muscle: {upperTonus} {lowerTonus} {ikDrive}";
        }

        public void AsserFinite()
        {
            re.AssertFinite(pitch);
            re.AssertFinite(stretch);
            re.AssertFinite(bend);
            re.AssertFinite(twist);
            re.AssertFinite(fkParent);
            re.AssertFinite(ikPos);
            re.AssertFinite(ikPosRelative);
            re.AssertFinite(ikParent);
            re.AssertFinite(ikBlend);

            re.AssertFinite(upperTonus);
            re.AssertFinite(lowerTonus);
            re.AssertFinite(ikDrive);
        }
    }
    
    public struct TorsoPose
    {

        public float3 cg; // center of gravity position relative to root

        public float hipsPitch;
        public float hipsYaw;
        public float hipsRoll;
        
        public float waistPitch;
        public float waistYaw;
        public float waistRoll;
        
        public float chestPitch;
        public float chestYaw;
        public float chestRoll;

        public float suspensionTonus; // "muscle" holding of hips at certain height
        public float angularTonus; // "muscle" holding hips orientation
        public float tonus;


        public float3 hipsSwingTwist
        {
            get => new float3(hipsPitch, hipsYaw, hipsRoll);
            set { hipsPitch = value.x; hipsYaw = value.y; hipsRoll = value.z; }
        }
        public float3 waistSwingTwist
        {
            get => new float3(waistPitch, waistYaw, waistRoll);
            set { waistPitch = value.x; waistYaw = value.y; waistRoll = value.z; }
        }
        public float3 chestSwingTwist
        {
            get => new float3(chestPitch, chestYaw, chestRoll);
            set { chestPitch = value.x; chestYaw = value.y; chestRoll = value.z; }
        }

        public quaternion hipsRotation
        {
            get => re.SwingTwistYXZ(hipsSwingTwist);
            set => hipsSwingTwist= re.ToSwingTwistYXZ(value);
        }
        public quaternion waistRotation
        {
            get => re.SwingTwistYXZ(waistSwingTwist);
            set => waistSwingTwist = re.ToSwingTwistYXZ(value);
        }
        public quaternion chestRotation
        {
            get => re.SwingTwistYXZ(chestSwingTwist);
            set => chestSwingTwist = re.ToSwingTwistYXZ(value);
        }

        public void ConvertToRuntime()
        {
            hipsPitch = math.radians(hipsPitch);
            hipsYaw = math.radians(hipsYaw);
            hipsRoll = math.radians(hipsRoll);
            waistPitch = math.radians(waistPitch);
            waistYaw = math.radians(waistYaw);
            waistRoll = math.radians(waistRoll);
            chestPitch = math.radians(chestPitch);
            chestYaw = math.radians(chestYaw);
            chestRoll = math.radians(chestRoll);
        }
        public void ConvertFromRuntime()
        {
            hipsPitch = math.degrees(hipsPitch);
            hipsYaw = math.degrees(hipsYaw);
            hipsRoll = math.degrees(hipsRoll);
            waistPitch = math.degrees(waistPitch);
            waistYaw = math.degrees(waistYaw);
            waistRoll = math.degrees(waistRoll);
            chestPitch = math.degrees(chestPitch);
            chestYaw = math.degrees(chestYaw);
            chestRoll = math.degrees(chestRoll);
        }
        public TorsoPose ToRadians()
        {
            var res = this;
            res.ConvertToRuntime();
            return res;
        }

        public void Flip()
        {
            hipsYaw *= -1;
            hipsRoll *= -1;
            waistYaw *= -1;
            waistRoll *= -1;
            chestYaw *= -1;
            chestRoll *= -1;
            cg.x *= -1;
        }

        public static TorsoPose defaultPose => new TorsoPose() { cg=new float3(0,.33f,0), suspensionTonus = 1, angularTonus = .8f, tonus = .7f };
        public static TorsoPose Add(in TorsoPose a, in TorsoPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            var d = new TorsoPose();
            if (pose)
            {
                result.hipsSwingTwist += (b.hipsSwingTwist - d.hipsSwingTwist) * weight;
                result.waistSwingTwist += (b.waistSwingTwist - d.waistSwingTwist) * weight;
                result.chestSwingTwist += (b.chestSwingTwist - d.chestSwingTwist) * weight;
                result.cg += (b.cg - d.cg) * weight;
            }
            if (muscle)
            {
                result.suspensionTonus += (b.suspensionTonus - d.suspensionTonus) * weight;
                result.angularTonus += (b.angularTonus - d.angularTonus) * weight;
                result.tonus += (b.tonus - d.tonus) * weight;
            };
            return result;
        }
        public static TorsoPose Blend(in TorsoPose a, in TorsoPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            if (pose)
            {
                result.hipsSwingTwist = math.lerp(a.hipsSwingTwist, b.hipsSwingTwist, weight);
                result.waistSwingTwist = math.lerp(a.waistSwingTwist, b.waistSwingTwist, weight);
                result.chestSwingTwist = math.lerp(a.chestSwingTwist, b.chestSwingTwist, weight);
                result.cg = math.lerp(a.cg, b.cg, weight);
            }
            if (muscle)
            {
                result.suspensionTonus = math.lerp(a.suspensionTonus, b.suspensionTonus, weight);
                result.angularTonus = math.lerp(a.angularTonus, b.angularTonus, weight);
                result.tonus = math.lerp(a.tonus, b.tonus, weight);
            };
            return result;
        }
        public override string ToString()
        {
            return $"FK: {math.degrees(hipsPitch)} {math.degrees(hipsYaw)} {math.degrees(hipsRoll)} {math.degrees(waistPitch)} {math.degrees(waistYaw)} {math.degrees(waistRoll)} {math.degrees(chestPitch)} {math.degrees(chestYaw)} {math.degrees(chestRoll)} IK: {cg} muscle: {suspensionTonus} {angularTonus} {tonus} ";
        }

        public void AsserFinite()
        {
            re.AssertFinite(cg);
            re.AssertFinite(hipsPitch);
            re.AssertFinite(hipsYaw);
            re.AssertFinite(hipsRoll);
            re.AssertFinite(waistPitch);
            re.AssertFinite(waistYaw);
            re.AssertFinite(waistRoll);
            re.AssertFinite(chestPitch);
            re.AssertFinite(chestYaw);
            re.AssertFinite(chestRoll);
            re.AssertFinite(suspensionTonus);
            re.AssertFinite(angularTonus);
            re.AssertFinite(tonus);
    }

        public void Lean(float3 dir)
        {
            var rotation = re.ToQuaternion(math.cross(re.up, dir.ZeroY()));
            hipsRotation = math.mul(rotation, hipsRotation);
            waistRotation = math.mul(rotation, waistRotation);
            chestRotation = math.mul(rotation, chestRotation);
        }
    }
    public struct HeadPose
    {
        public float pitch;
        public float yaw;
        public float roll;
        public float fkParent;

        public float tonus;

        public float3 swingTwist
        {
            get => new float3(pitch, yaw, roll);
            set { pitch = value.x; yaw = value.y; roll = value.z; }
        }

        public quaternion rotation
        {
            get => re.SwingTwistYXZ(swingTwist);
            set => swingTwist = re.ToSwingTwistYXZ(value);
        }

        public void ConvertToRuntime()
        {
            pitch = math.radians(pitch);
            yaw = math.radians(yaw);
            roll = math.radians(roll);
        }
        public void ConvertFromRuntime()
        {
            pitch = math.degrees(pitch);
            yaw = math.degrees(yaw);
            roll = math.degrees(roll);
        }
        public HeadPose ToRadians()
        {
            var res = this;
            res.ConvertToRuntime();
            return res;
        }
        public void Flip()
        {
            yaw *= -1;
            roll *= -1;
        }
        public static HeadPose defaultPose => new HeadPose() { tonus = .7f };
        public static HeadPose Add(in HeadPose a, in HeadPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            var d = new HeadPose();
            if (pose)
            {
                result.pitch += (b.pitch - d.pitch) * weight;
                result.yaw += (b.yaw - d.yaw) * weight;
                result.roll += (b.roll - d.roll) * weight;
                result.fkParent += (b.fkParent - d.fkParent) * weight;
            }
            if (muscle)
            {
                result.tonus += (b.tonus - d.tonus) * weight;
            };
            return result;
        }
        public static HeadPose Blend(in HeadPose a, in HeadPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            if (pose)
            {
                result.pitch = math.lerp(a.pitch, b.pitch, weight);
                result.yaw = math.lerp(a.yaw, b.yaw, weight);
                result.roll = math.lerp(a.roll, b.roll, weight);
                result.fkParent = math.lerp(a.fkParent, b.fkParent, weight);
            }
            if (muscle)
            {
                result.tonus = math.lerp(a.tonus, b.tonus, weight);
            };
            return result;
        }

        public override string ToString()
        {
            return $"{math.degrees(pitch)} {math.degrees(yaw)} {math.degrees(roll)} {fkParent} {tonus}";
        }

        public void AsserFinite()
        {
            re.AssertFinite(pitch);
            re.AssertFinite(yaw);
            re.AssertFinite(roll);
            re.AssertFinite(fkParent);
            re.AssertFinite(tonus);
        }
    }
    public struct PivotPose
    {
        // carryable pivot rotation rleative to root
        public float pitch;
        public float yaw;
        public float roll;
        public float3 offset; // 2-handed: offset from L to R hands in pivot space
        public float anchor; // 2-handed: which arm is heavier when resolving offset [0 - left preserves location, 1 - right]
        public bool isEmpty => !isTwoHanded && pitch == 0 && yaw == 0 && roll == 0 && fkParent==0;
        public bool isTwoHanded => ikBlend != 0;

        public float fkParent;//unused for now
        public float ikBlend; // 2-handed: which arm is heavier when resolving offset [0 - left preserves location, 1 - right]
        //public float tonus; // unusef for now
        public float3 swingTwist
        {
            get => new float3(pitch, yaw, roll);
            set { pitch = value.x; yaw = value.y; roll= value.z; }
        }
        public void ConvertToRuntime()
        {
            pitch = math.radians(pitch);
            yaw = math.radians(yaw);
            roll = math.radians(roll);
        }
        public void ConvertFromRuntime()
        {
            pitch = math.degrees(pitch);
            yaw = math.degrees(yaw);
            roll = math.degrees(roll);
        }
        public PivotPose ToRadians()
        {
            var res = this;
            res.ConvertToRuntime();
            return res;
        }
        public void Flip()
        {
            yaw *= -1;
            roll *= -1;
            offset *= -1;
            anchor = 1 - anchor;
        }

        public quaternion ToRotation(bool left, float aimYaw)
        {
            var worldTarget = quaternion.Euler(pitch, fkParent == 0 ? aimYaw : 0, 0);

            worldTarget = math.mul(worldTarget, quaternion.RotateY(left ? -yaw : yaw));
            worldTarget = math.mul(worldTarget, quaternion.RotateZ(left ? -roll : roll));

            // pivot is given in reference pose (hand looking forward) so we have to remap to hand body space
            if (fkParent > 0)
                worldTarget = math.mul(CarryableBase.PoseToHand, worldTarget);

            return worldTarget;
        }

        public static PivotPose defaultPose => new PivotPose() { };
        //public static PivotPose Add(in PivotPose a, in PivotPose b, float weight, bool pose, bool muscle)
        //{
        //    if (weight == 0) return a;
        //    var result = a;
        //    var d = new PivotPose();
        //    if (pose)
        //    {
        //        result.pitch += (b.pitch - d.pitch) * weight;
        //        result.yaw += (b.yaw - d.yaw) * weight;
        //        result.offset += (b.offset- d.offset) * weight;
        //        result.anchor+= (b.anchor- d.anchor) * weight;
        //        result.roll += (b.roll - d.roll) * weight;
        //        result.fkParent += (b.fkParent - d.fkParent) * weight;
        //    }
        //    //if (muscle)
        //    //{
        //    //    result.tonus += (b.tonus - d.tonus) * weight;
        //    //};
        //    return result;
        //}
        public static PivotPose Blend(in PivotPose a, in PivotPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            if (pose)
            {
                result.pitch = re.LerpWeighted(a.pitch, a.ikBlend, b.pitch, b.ikBlend, weight);
                result.yaw = re.LerpWeighted(a.yaw, a.ikBlend, b.yaw, b.ikBlend, weight);
                result.roll = re.LerpWeighted(a.roll, a.ikBlend, b.roll, b.ikBlend, weight);
                result.offset = re.LerpWeighted(a.offset, a.ikBlend, b.offset, b.ikBlend, weight);
                result.anchor = re.LerpWeighted(a.anchor, a.ikBlend, b.anchor, b.ikBlend, weight);
                result.fkParent = math.lerp(a.fkParent, b.fkParent, weight);
                result.ikBlend = math.lerp(a.ikBlend, b.ikBlend, weight);
            }
            //if (muscle)
            //{
            //    result.tonus = math.lerp(a.tonus, b.tonus, weight);
            //};
            return result;
        }

        public override string ToString()
        {
            return $"{math.degrees(pitch)} {math.degrees(yaw)} {math.degrees(roll)} {offset} {anchor} {fkParent} {ikBlend}";
        }

    }
    public struct NoodlePose
    {
        public TorsoPose torso;
        public HeadPose head;

        public HandPose handL;
        public HandPose handR;

        public LegPose legL;
        public LegPose legR;

        public PivotPose pivotL;
        public PivotPose pivotR;
        public bool grounded; // different tonus for flying????
        public static NoodlePose defaultPose => new NoodlePose()
        {
            torso = TorsoPose.defaultPose,
            head = HeadPose.defaultPose,
            handL = HandPose.defaultPose,
            handR = HandPose.defaultPose,
            legL = LegPose.defaultPose,
            legR = LegPose.defaultPose,
            pivotL = PivotPose.defaultPose,
            pivotR = PivotPose.defaultPose
        };
        public static NoodlePose Add(in NoodlePose a, in NoodlePose b, float weight) =>
            Add(a, b, weight, weight, weight, weight, weight, weight, true, true);

        public static NoodlePose Add(in NoodlePose a, in NoodlePose b, float torso, float head, float handL, float handR, float legL, float legR, bool pose, bool muscle)
        {
            return new NoodlePose()
            {
                torso = TorsoPose.Add(a.torso, b.torso, torso, pose, muscle),
                head = HeadPose.Add(a.head, b.head, head, pose, muscle),
                handL = HandPose.Add(a.handL, b.handL, handL, pose, muscle),
                handR = HandPose.Add(a.handR, b.handR, handR, pose, muscle),
                legL = LegPose.Add(a.legL, b.legL, legL, pose, muscle),
                legR = LegPose.Add(a.legR, b.legR, legR, pose, muscle),
            };
        }
        public static NoodlePose Blend(in NoodlePose a, in NoodlePose b, float weight) =>
            Blend(a, b, weight, weight, weight, weight, weight, weight, true, true);

        public static NoodlePose Blend(in NoodlePose a, in NoodlePose b, float torso, float head, float handL, float handR, float legL, float legR, bool pose, bool muscle)
        {
            var torsoBlend = TorsoPose.Blend(a.torso, b.torso, torso, pose, muscle);
            return new NoodlePose()
            {
                torso = torsoBlend,
                head = HeadPose.Blend(a.head, b.head, head, pose, muscle),
                handL = HandPose.Blend(a.handL, b.handL, handL, pose, muscle),
                handR = HandPose.Blend(a.handR, b.handR, handR, pose, muscle),
                legL = LegPose.Blend(a.legL, b.legL, legL, pose, muscle),
                legR = LegPose.Blend(a.legR, b.legR, legR, pose, muscle)
            };
        }

        public override string ToString()
        {
            return //$"{legL.support} {legR.support} {handL.support} {handR.support} "+
                $"{head}\n{torso}\n{handL}\n{handR}\n{legL}\n{legR}\n";
        }
        [BurstDiscard]

        public void AssertFinite()
        {
            torso.AsserFinite();
            head.AsserFinite();
            handL.AsserFinite();
            handR.AsserFinite();
            legL.AsserFinite();
            legR.AsserFinite();

        }
        public void ConvertToRuntime()
        {
            torso.ConvertToRuntime();
            head.ConvertToRuntime();
            handL.ConvertToRuntime();
            handR.ConvertToRuntime();
            legL.ConvertToRuntime();
            legR.ConvertToRuntime();
            pivotL.ConvertToRuntime();
            pivotR.ConvertToRuntime();
        }
        public void ConvertFromRuntime()
        {
            torso.ConvertFromRuntime();
            head.ConvertFromRuntime();
            handL.ConvertFromRuntime();
            handR.ConvertFromRuntime();
            legL.ConvertFromRuntime();
            legR.ConvertFromRuntime();
            pivotL.ConvertFromRuntime();
            pivotR.ConvertFromRuntime();
        }

        public static RigidTransform WaistTransformFromAnimationCoG(in NoodleDimensions dim, quaternion waistRot, float3 animationCoG)
        {
            return new RigidTransform(waistRot, animationCoG + math.rotate(waistRot, dim.hipsToWaist));
        }

        public static float3 AnimationCoGFromWaistTransform(in NoodleDimensions dim, quaternion waistRot, float3 waistPos)
        {
            return waistPos - math.rotate(waistRot, dim.hipsToWaist);
        }

        public void Flip()
        {
            torso.Flip();
            head.Flip();
            handL.Flip();
            handR.Flip();
            legL.Flip();
            legR.Flip();
            pivotL.Flip();
            pivotR.Flip();

            var h = handL;handL = handR;handR = h;
            var l = legL; legL = legR; legR = l;
            var p = pivotL; pivotL = pivotR; pivotR = p;
        }
    }

   

}

