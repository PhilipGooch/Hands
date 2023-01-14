using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public static partial class re
    {
        #region Angle operations
        /// <summary>
        /// wraps angle to [-PI;+PI]
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        
        public static float NormalizeAngle(float angle)
        {
            angle = math.fmod(angle, math.PI * 2);
            if (angle > math.PI) angle -= math.PI * 2;
            if (angle < -math.PI) angle += math.PI * 2;
            return angle;

        }
        /// <summary>
        /// wraps angle to [origin-PI;origin+PI]
        /// </summary>
        public static float NormalizeAngleAround(float angle, float origin) =>
            NormalizeAngle(angle - origin) + origin;


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float NormalizeAngleDeg(float angle)
        //{
        //    angle = math.fmod(angle, 360);
        //    while (angle < -180)
        //        angle += 360;
        //    while (angle > 180)
        //        angle -= 360;
        //    return angle;
        //}

        // Cosine law
        public static float SolveTriangleAngle(float a, float b, float c)
        {
            var cos = (a * a + b * b - c * c) / (2 * a * b);
            //Debug.LogFormat("{0} {1} {2} {3} ",  cos, a,b,c);
            if (cos > 1) return 0;
            if (cos < -1) return math.PI;
            return math.acos(cos);
        }
        public static float SolveTriangleEdge(float a, float b, float angle)
        {
            var cos = math.cos(angle);
            var c2 = a * a + b * b- 2 * a * b* cos;
            return math.sqrt(c2);
        }

        public static float CalculateYaw(float3 dir)=> math.atan2(dir.x, dir.y);
        public static float CalculatePitch(float3 dir) => math.atan2(math.length( dir.ZeroY()), dir.y)-math.PI/2;
        #endregion

        #region AngleAxis <-> Quaternion conversion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ToAngleAxis(this quaternion q1, out float angle, out float3 axis)
        {
            if (q1.value.w < 0)
                q1 = new quaternion(-q1.value);
            if (q1.value.w > 1)
                q1 = math.normalize(
                    q1); // if w>1 acos and sqrt will produce errors, this cant happen if quaternion is normalised
            var w = q1.value.w;
            angle = 2 * math.acos(w);
            var s = math.sqrt(1 -
                              w * w); // assuming quaternion normalised then w is less than 1, so term always positive.
            if (s < 0.0000001f)
                axis = new float3(q1.value.x, q1.value.y, q1.value.z);
            else
                axis = new float3(q1.value.x / s, q1.value.y / s, q1.value.z / s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToAngleAxis(this quaternion q1)
        {
            ToAngleAxis(q1, out var angle, out var axis);
            return angle * axis;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ToQuaternion(this float3 vec)
        {
            var len = math.length(vec);
            if (len < FLT_EPSILON) return quaternion.identity;

            return quaternion.AxisAngle(vec / len, len);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ToQuaternion(float angle, float3 axis)
        {
            var len = math.length(axis);
            if (len < FLT_EPSILON) return quaternion.identity;

            return quaternion.AxisAngle(axis, angle);
        }
        #endregion

        #region Rotations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Rotate(this float2 p, float angle)
        {
            math.sincos(angle, out var sn, out var cs);
            
            return new float2(
                    p.x * cs + p.y * sn,
                    p.y * cs - p.x * sn );

        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float2 rotateDeg(this float2 p, float angle)
        //{
        //    angle = math.radians(angle);
        //    var cs = math.cos(-angle);
        //    var sn = math.sin(-angle);
        //    return new float2(
        //            p.x * cs - p.y * sn,
        //            p.x * sn + p.y * cs);
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float2 RotateCW90(this float2 p) => new float2(p.y, -p.x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float2 RotateCCW90(this float2 p) => new float2(-p.y, p.x);

        // Rotates vector around axis
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Rotate(this float3 p, float3 axis, float angle)
        {
            return math.rotate(quaternion.AxisAngle(axis, angle), p);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateXCW90(this float3 p) => new float3(p.x, -p.z, p.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateXCCW90(this float3 p) => new float3(p.x, p.z, -p.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateYCW90(this float3 p) => new float3(p.z, p.y, -p.x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateYCCW90(this float3 p) => new float3(-p.z, p.y, p.x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateZCW90(this float3 p) => new float3(-p.y, p.x, p.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 RotateZCCW90(this float3 p) => new float3(p.y, -p.x, p.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotateYDeg(this float3 p, float angle)
        {
            return p.RotateY(math.radians(angle));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotateX(this float3 p, float angle)
        {
            math.sincos(angle, out var sn, out var cs);
            return new float3(
                    p.x ,
                    p.y * cs - p.z * sn,
                    p.z * cs + p.y * sn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotateY(this float3 p, float angle)
        {
            math.sincos(angle, out var sn, out var cs);
            return new float3(
                    p.x * cs + p.z * sn,
                    p.y,
                    p.z * cs - p.x * sn);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RotateZ(this float3 p, float angle)
        {
            math.sincos(angle, out var sn, out var cs);
            return new float3(
                    p.x * cs - p.y * sn,
                    p.y * cs + p.x * sn,
                    p.z );
        }

        #endregion


        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector3.cs
        // Returns the angle in radians between /from/ and /to/. This is always the smallest
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleBetween(float3 from, float3 to)
        {
            // sqrt(a) * sqrt(b) = sqrt(a * b) -- valid for real numbers
            var denominator = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            if (denominator < FLT_EPSILON)
                return 0F;

            var dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
            return math.acos(dot);
            //return math.degrees(math.acos(dot));
        }

        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector3.cs
        // The smaller of the two possible angles between the two vectors is returned, therefore the result will never be greater than 180 degrees or smaller than -180 degrees.
        // If you imagine the from and to vectors as lines on a piece of paper, both originating from the same point, then the /axis/ vector would point up out of the paper.
        // The measured angle between the two vectors would be positive in a clockwise direction and negative in an anti-clockwise direction.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SignedAngleBetween(float3 from, float3 to, float3 axis)
        {
            var unsignedAngle = AngleBetween(from, to);
            var sign = math.sign(math.dot(math.cross(from, to), axis));
            return unsignedAngle * sign;
        }

        // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Math/Vector3.cs
        // calculates rotation between from and to as angle and axis
        public static void FromToRotationAngleAxis(float3 from, float3 to, out float3 axis, out float angle)
        {
            var denominator = math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            if (denominator < FLT_EPSILON)
            {
                axis = float3.zero;
                angle = 0;
            }

            var dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);

            axis = math.normalizesafe(math.cross(from, to));
            angle = math.acos(dot);
        }
        // calculates rotation between from and to
        public static quaternion FromToRotation(float3 from, float3 to)
        {
            FromToRotationAngleAxis(from, to, out var axis, out var angle);
            
            return re.ToQuaternion(angle, axis);

        }
        // calculates rotation between from and to, limiting maximum delta
        public static unsafe quaternion ClampedFromToRotation(float3 from, float3 to, float maxDelta)
        {
            FromToRotationAngleAxis(from, to, out var axis, out var angle);
            angle = re.Clamp(angle, maxDelta);
            return re.ToQuaternion(angle,axis);
        }
        // blends between two rotations limiting maximum delta
        public static unsafe quaternion MoveTowardsRotation(quaternion fromRot, quaternion toRot, float maxDelta)
        {
            var delta = math.normalize(math.mul(toRot, math.inverse(fromRot)));
            delta.ToAngleAxis(out var angle, out var axis);
            if (angle > maxDelta)
                return math.normalize(math.mul(re.ToQuaternion(maxDelta, axis), fromRot));
            else if (angle < -maxDelta)
                return math.normalize(math.mul(re.ToQuaternion(-maxDelta, axis), fromRot));
            else
                return toRot;
        }
       
        // blends between two rotations, preserving the twist and limiting maximum delta
        public static quaternion MoveTowardsSwingRotation(quaternion currentRot, quaternion targetRot, float3 localTwistAxis, float maxDelta)
        {
            var targetAxis = math.rotate(targetRot, localTwistAxis);
            var currentAxis = math.mul(currentRot, localTwistAxis);
            var delta = ClampedFromToRotation(currentAxis, targetAxis, maxDelta);
            return math.mul(delta, currentRot);
        }
        public static quaternion MoveTowardsTwistRotation(quaternion currentRot, quaternion targetRot, float3 localTwistAxis, float maxDelta)
        {
            var currentAxis = math.mul(currentRot, localTwistAxis);
            var twistAngle = GetTwistAngle(math.normalize(math.mul(targetRot, math.inverse(currentRot))), currentAxis);
            twistAngle = math.clamp(twistAngle, -maxDelta, maxDelta);
            var delta = quaternion.AxisAngle(currentAxis, twistAngle);
            return math.mul(delta, currentRot);
        }

        public static quaternion SwingTo(quaternion currentRot, quaternion targetRot, float3 localTwistAxis)
        {
            var targetAxis = math.rotate(targetRot, localTwistAxis);
            var currentAxis = math.mul(currentRot, localTwistAxis);
            var delta = FromToRotation(currentAxis, targetAxis);
            return math.mul(delta, currentRot);
        }

        public static quaternion TwistTo(quaternion currentRot, quaternion targetRot, float3 localTwistAxis)
        {
            var currentAxis = math.mul(currentRot, localTwistAxis);
            var delta = GetTwist(math.normalize( math.mul(targetRot, math.inverse(currentRot))),currentAxis);
            return math.mul(delta, currentRot);
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTwistAngle(quaternion q, float3 twistAxis)
        {
            return NormalizeAngle(math.dot(ToAngleAxis(q), twistAxis));
        }

        //http://allenchou.net/2018/05/game-math-swing-twist-interpolation-sterp/
        public static quaternion GetTwist(quaternion q, float3 twistAxis)
        {
            float3 r = q.value.xyz; // new Vector3(q.x, q.y, q.z);

            // singularity: rotation by 180 degree
            if (math.lengthsq(r) < FLT_EPSILON)
                return quaternion.AxisAngle(twistAxis, math.PI);

            // meat of swing-twist decomposition
            float3 p = Project(r, twistAxis);
            var twist = new quaternion(p.x, p.y, p.z, q.value.w);
            twist = math.normalize(twist);
            return twist;
        }


        //http://allenchou.net/2018/05/game-math-swing-twist-interpolation-sterp/
        public static void ToSwingTwist(quaternion q, float3 twistAxis, out quaternion swing, out quaternion twist)
        {
            float3 r = q.value.xyz; // new Vector3(q.x, q.y, q.z);

            // singularity: rotation by 180 degree
            // TS: actually it's either identity.quaternion, or -identity.quaterion which is no rotation
            if (math.lengthsq(r) < FLT_EPSILON)
            {
                float3 rotatedTwistAxis = math.rotate(q, twistAxis);
                float3 swingAxis = math.cross(twistAxis, rotatedTwistAxis);

                if (math.lengthsq(swingAxis) > FLT_EPSILON)
                {

                    float swingAngle = AngleBetween(twistAxis, rotatedTwistAxis);
                    swing = quaternion.AxisAngle(swingAxis, swingAngle);
                }
                else
                {
                    // more singularity: 
                    // rotation axis parallel to twist axis
                    swing = quaternion.identity; // no swing
                }
                // always twist 180 degree on singularity
                //twist = quaternion.AxisAngle(twistAxis, math.PI);
                twist = quaternion.identity; // TS:don't twist
                return;

            }

            // meat of swing-twist decomposition
            float3 p = Project(r, twistAxis);
            twist = new quaternion(p.x, p.y, p.z, q.value.w);
            twist = math.normalize(twist);
            swing = math.mul(q, math.inverse(twist));
        }

        public static quaternion SwingTwistYXZ(float3 angles) => SwingTwistYXZ(angles.x, angles.y, angles.z);

        public static quaternion SwingTwistYXZ(float swingX, float twist, float swingZ)
        {
            var swingQ = new float3(swingX, 0, swingZ).ToQuaternion();
            var twistQ = quaternion.RotateY(twist);
            return math.mul(swingQ, twistQ);
        }
        public static float3 ToSwingTwistYXZ(quaternion rotation)
        {
            // decompose
            re.ToSwingTwist(rotation, math.rotate(rotation, re.up), out var swingQ, out var twistQ);
            var inv = math.inverse(rotation);

            // calculate twist
            twistQ.ToAngleAxis(out var twist, out var axis);
            axis = math.rotate(inv, axis);
            //            twist *= twistSign;
            if (axis.y < 0)
                twist *= -1;

            // calculate swing
            swingQ.ToAngleAxis(out var angle, out axis);
            axis = math.rotate(inv, axis);
            var swing1 = axis.x * angle;
            var swing2 = axis.z * angle;

            return new Vector3(re.NormalizeAngle(swing1), re.NormalizeAngle(twist), re.NormalizeAngle(swing2));
        }
        public static quaternion SwingTwistZXY(float3 angles) => SwingTwistZXY(angles.x, angles.y, angles.z);

        public static quaternion SwingTwistZXY(float swingX, float swingY, float twist)
        {
            var swingQ = new float3(swingX, swingY,0).ToQuaternion();
            var twistQ = quaternion.RotateZ(twist);
            return math.mul(swingQ, twistQ);
        }
        public static float3 ToSwingTwistZXY(quaternion rotation)
        {
            // decompose
            re.ToSwingTwist(rotation, math.rotate(rotation, re.forward), out var swingQ, out var twistQ);
            var inv = math.inverse(rotation);

            // calculate twist
            twistQ.ToAngleAxis(out var twist, out var axis);
            axis = math.rotate(inv, axis);
            //            twist *= twistSign;
            if (axis.z < 0)
                twist *= -1;

            // calculate swing
            swingQ.ToAngleAxis(out var angle, out axis);
            axis = math.rotate(inv, axis);
            var swing1 = axis.x * angle;
            var swing2 = axis.y * angle;

            return new Vector3(re.NormalizeAngle(swing1),  re.NormalizeAngle(swing2), re.NormalizeAngle(twist));
        }
    }
}
