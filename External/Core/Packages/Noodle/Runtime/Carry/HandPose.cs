using NBG.Entities;
using Recoil;
using Noodles.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using NBG.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [Serializable]
    public struct HandMuscle
    {
        public float upperTonus;
        public float lowerTonus;
        public float ikDrive;

        public static HandMuscle defaultMuscle => new HandMuscle() { upperTonus = .1f, lowerTonus = .15f };

        public static HandMuscle operator +(HandMuscle a, HandMuscle b)
        {
            return new HandMuscle()
            {
                upperTonus = a.upperTonus + b.upperTonus,
                lowerTonus = a.lowerTonus + b.lowerTonus,
                ikDrive = a.ikDrive + b.ikDrive,
            };
        }
        public static HandMuscle operator -(HandMuscle a, HandMuscle b)
        {
            return new HandMuscle()
            {
                upperTonus = a.upperTonus - b.upperTonus,
                lowerTonus = a.lowerTonus - b.lowerTonus,
                ikDrive = a.ikDrive - b.ikDrive,
            };
        }
        public static HandMuscle operator *(HandMuscle a, float v)
        {
            return new HandMuscle()
            {
                upperTonus = a.upperTonus * v,
                lowerTonus = a.lowerTonus * v,
                ikDrive = a.ikDrive * v,
            };
        }
        public static HandMuscle Add(in HandMuscle a, in HandMuscle b, float weight)
        {
            if (weight == 0) return a;
            var result = a;
            HandMuscle d = default;

            result.upperTonus += (b.upperTonus - d.upperTonus) * weight;
            result.lowerTonus += (b.lowerTonus - d.lowerTonus) * weight;
            result.ikDrive += (b.ikDrive - d.ikDrive) * weight;


            return result;
        }
        public static HandMuscle Blend(in HandMuscle a, in HandMuscle b, float weight)
        {
            if (weight == 0) return a;
            if (weight == 1) return b;
            var result = a;

            result.upperTonus = math.lerp(a.upperTonus, b.upperTonus, weight);
            result.lowerTonus = math.lerp(a.lowerTonus, b.lowerTonus, weight);
            result.ikDrive = math.lerp(a.ikDrive, b.ikDrive, weight);

            return result;
        }

        public override string ToString()
        {
            return $"({upperTonus} {lowerTonus} {ikDrive})";
        }

        public void AssertFinite()
        {
            re.AssertFinite(upperTonus);
            re.AssertFinite(lowerTonus);
            re.AssertFinite(ikDrive);
        }
    }
    [Serializable]
    public struct HandPose
    {
        public float pitch;
        public float yaw;
        public float bend;
        public float elbowAngle;
        public float wristAngle;
        public float fkParent;

        public float3 ikPos; // relative to root
        public float3 ikPosRelative; // relative to shoulder
        public float ikParent; // 0 - anchor to ball, when 1 to chest
        public float ikBlend; // FK vs IK when calculating final position

        public HandMuscle muscle;

        public static unsafe HandPose Lerp(HandPose a, HandPose b, float mix)
        {
            var ptrA = (float4*)Unsafe.AsPointer(ref a.pitch);
            var ptrB = (float4*)Unsafe.AsPointer(ref b.pitch);
            var result = new HandPose();
            var ptr = (float4*)Unsafe.AsPointer(ref result.pitch);
            *ptr++ = math.lerp(*ptrA++, *ptrB++, mix);
            *ptr++ = math.lerp(*ptrA++, *ptrB++, mix);
            *ptr++ = math.lerp(*ptrA++, *ptrB++, mix);
            *ptr++ = math.lerp(*ptrA++, *ptrB++, mix);
            *(float2*)ptr = math.lerp(*(float2*)ptrA, *(float2*)ptrB, mix);

            result.ikPos = re.LerpWeighted(a.ikPos, a.ikBlend, b.ikPos, b.ikBlend, mix);
            result.ikPosRelative = re.LerpWeighted(a.ikPosRelative, a.ikBlend, b.ikPosRelative, b.ikBlend, mix);

            return result;
        }

        public HandPose(float pitch, float yaw, float bend, float elbow, float wrist = 0) : this()
        {
            this.pitch = pitch;
            this.yaw = yaw;
            this.bend = bend;
            this.elbowAngle = elbow;
            this.wristAngle = wrist;
        }
        public void ConvertToRuntime() {
            pitch = math.radians(pitch);
            yaw = math.radians(yaw);
            bend = math.radians(bend);
            elbowAngle = math.radians(elbowAngle);
            wristAngle = math.radians(wristAngle);
        }
        public void ConvertFromRuntime()
        {
            pitch = math.degrees(pitch);
            yaw = math.degrees(yaw);
            bend = math.degrees(bend);
            elbowAngle = math.degrees(elbowAngle);
            wristAngle = math.degrees(wristAngle);
        }

        public HandPose ToRadians()
        {
            var pose = this;
            pose.pitch = math.radians(pitch);
            pose.yaw = math.radians(yaw);
            pose.bend = math.radians(bend);
            pose.elbowAngle = math.radians(elbowAngle);
            pose.wristAngle = math.radians(wristAngle);
            return pose;
        }
        public void Flip()
        {
            ikPos.x *= -1;
            ikPosRelative.x *= -1;
        }
        public HandPose flipped { get { var r = this; r.Flip(); return r; } }
    
        //public HandPose ToDegrees()
        //{
        //    var pose = this;
        //    pose.pitch = math.degrees(pitch);
        //    pose.yaw = math.degrees(yaw);
        //    pose.bend = math.degrees(bend);
        //    pose.elbowAngle = math.degrees(elbowAngle);
        //    return pose;
        //}

        public float3 GetTargetPos(bool left=false)
        {
            return NoodleArmRig.CalculateArmTarget(this, left);
        }
        public static HandPose defaultPose => new HandPose() {
            pitch = math.radians(22.5f),
            yaw = math.radians(15),
            bend = math.radians(90),
            elbowAngle = math.radians(45),
            wristAngle = math.radians(0),
            fkParent=1,
            muscle = HandMuscle.defaultMuscle
        };

  
        public static HandPose Add(in HandPose a, in HandPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            var d = new HandPose();
            if (pose)
            {
                result.pitch += (b.pitch - d.pitch) * weight;
                result.yaw += (b.yaw - d.yaw) * weight;
                result.bend += (b.bend - d.bend) * weight;
                result.elbowAngle += (b.elbowAngle - d.elbowAngle) * weight;
                result.wristAngle += (b.wristAngle - d.wristAngle) * weight;
                result.fkParent += (b.fkParent - d.fkParent) * weight;

                result.ikPos += (b.ikPos - d.ikPos) * weight;
                result.ikPosRelative += (b.ikPosRelative - d.ikPosRelative) * weight;
                result.ikParent += (b.ikParent - d.ikParent) * weight;
                result.ikBlend += (b.ikBlend - d.ikBlend) * weight;

            }
            if (muscle)
            {
                result.muscle = HandMuscle.Add(a.muscle, b.muscle, weight);
            };
            return result;
        }
        public static HandPose Blend(in HandPose a, in HandPose b, float weight, bool pose, bool muscle)
        {
            if (weight == 0) return a;
            var result = a;
            if (pose)
            {
                result.pitch = math.lerp(a.pitch, b.pitch, weight);
                result.yaw = math.lerp(a.yaw, b.yaw, weight);
                result.bend = math.lerp(a.bend, b.bend, weight);
                result.elbowAngle = math.lerp(a.elbowAngle, b.elbowAngle, weight);
                result.wristAngle = math.lerp(a.wristAngle, b.wristAngle, weight);
                result.fkParent = math.lerp(a.fkParent, b.fkParent, weight);

                result.ikPos = re.LerpWeighted(a.ikPos, a.ikBlend, b.ikPos, b.ikBlend, weight);
                result.ikPosRelative = re.LerpWeighted(a.ikPosRelative, a.ikBlend, b.ikPosRelative, b.ikBlend, weight);
                result.ikParent = math.lerp(a.ikParent, b.ikParent, weight);
                result.ikBlend = math.lerp(a.ikBlend, b.ikBlend, weight);
            }
            if (muscle)
            {
                result.muscle = HandMuscle.Blend(a.muscle, b.muscle, weight);
            };
            return result;
        }

        public override string ToString()
        {
            return $"FK: {math.degrees(pitch)} {math.degrees(yaw)} {math.degrees(bend)} {math.degrees(elbowAngle)} {math.degrees(wristAngle)} {fkParent} IK: {ikPos} {ikPosRelative} {ikParent} {ikBlend} muscle: {muscle}";
        }

        public void AsserFinite()
        {
            re.AssertFinite(pitch);
            re.AssertFinite(yaw);
            re.AssertFinite(bend);
            re.AssertFinite(elbowAngle);
            re.AssertFinite(wristAngle);
            re.AssertFinite(fkParent);

            re.AssertFinite(ikPos);
            re.AssertFinite(ikPosRelative);
            re.AssertFinite(ikParent);
            re.AssertFinite(ikBlend);

            muscle.AssertFinite();
        }
    }
}
