using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: Preserve]
namespace Recoil
{
    public sealed class WorldJobData // Used for GameSystem job dependencies
    {
    }

    public static partial class re
    {
        public const float FLT_EPSILON = 1e-10f;
       
        // Projects a vector onto a plane defined by a normal orthogonal to the plane.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectOnPlane(float3 vector, float3 planeNormal)
        {
            return vector - Project(vector, planeNormal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Project(float3 vector, float3 normal)
        {
            var sqrMag = math.dot(normal, normal);
            if (sqrMag < FLT_EPSILON)
                return float3.zero;

            var dot = math.dot(vector, normal);
            return normal * (dot / sqrMag);
        }
        public static float3 ProjectToPositive(float3 vector, float3 normal)
        {
            var sqrMag = math.dot(normal, normal);
            if (sqrMag < FLT_EPSILON)
                return float3.zero;

            var dot = math.dot(vector, normal);
            return normal * (math.max(0,dot) / sqrMag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ProjectPointOnSegment(float3 point, float3 pointA, float3 pointB)
        {
            var lineVec = pointB - pointA;
            var lineVecSqMag = math.lengthsq(lineVec);
            if (lineVecSqMag < FLT_EPSILON) return pointA;

            var linePointToPoint = point - pointA;

            var t = math.dot(linePointToPoint, lineVec) / lineVecSqMag;
            if (t <= 0) return pointA;
            if (t >= 1) return pointA + lineVec;
            return pointA + lineVec * t;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OrthoNormalize(ref float3 normal, ref float3 tangent, ref float3 binormal)
        {
            var n = (UnityEngine.Vector3)normal;
            var t = (UnityEngine.Vector3)tangent;
            var b = (UnityEngine.Vector3)binormal;
            UnityEngine.Vector3.OrthoNormalize(ref n, ref t, ref b);
            normal = n;
            tangent = t;
            binormal = b;
        }

        // returns transform that maps points on YZ plane to a plane going through 3 points
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RigidTransform GetPlaneTransfrom(float3 a, float3 b, float3 c)
        {
            if (math.lengthsq(a - c) < re.FLT_EPSILON) return RigidTransform.Translate(c);
            return new RigidTransform(quaternion.LookRotation(math.normalize( a - c), math.normalize( b - c)), c);

            //a -= c;
            //b -= c;
            //var x = math.normalize(a);
            //var y = math.normalize(b);
            //var z = math.normalize(math.cross(x, y));
            //re.OrthoNormalize(ref x, ref y, ref z);
            //return new RigidTransform(new float3x3(x, y, z), c);
        }

        // find coordinates of a point relative to plane defined by a,b,c 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToPlaneCoords(float3 a, float3 b, float3 c, float3 p)
        {
            return math.transform(math.inverse(GetPlaneTransfrom(a, b, c)), p);
        }
        // map point described relative to plane a,b,c to world space
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // returns vector expressed in reference frame represented by 3 point plane
        public static float3 FromPlaneCoords(float3 a, float3 b, float3 c, float3 p)
        {
            return math.transform(GetPlaneTransfrom(a, b, c), p);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowardsExp(float current, float target, float halflife, float dt)
        {
            var diff = target - current;
            diff *= math.exp2(-dt/halflife);
            return target - diff;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 MoveTowardsExp(float2 current, float2 target, float halflife, float dt)
        {
            var diff = target - current;
            diff *= math.exp2(-dt / halflife);
            return target - diff;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 MoveTowardsExp(float3 current, float3 target, float halflife, float dt)
        {
            var diff = target - current;
            diff *= math.exp2(-dt / halflife);
            return target - diff;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowards(float current, float target, float maxdelta)
        {
            var delta = target - current;
            delta = math.clamp(delta, -maxdelta, maxdelta);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowards(float current, float target, float maxdeltarise, float maxdeltafall)
        {
            var delta = target - current;
            delta = math.clamp(delta, -maxdeltafall, maxdeltarise);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 MoveTowards(float4 current, float4 target, float maxdelta)
        {
            var delta = target - current;
            delta = delta.Clamp(maxdelta);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 MoveTowards(float3 current, float3 target, float maxdelta)
        {
            var delta = target - current;
            delta = delta.Clamp(maxdelta);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 MoveTowards(float2 current, float2 target, float maxdelta)
        {
            var delta = target - current;
            delta = delta.Clamp(maxdelta);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveTowardsAngle(float current, float target, float maxdelta)
        {
            var delta = target - current;
            delta = NormalizeAngle(delta);
            delta = math.clamp(delta, -maxdelta, maxdelta);
            return current + delta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Clamp(this float4 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            return vec * (maxmag / mag);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Clamp(this float3 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            return vec * (maxmag / mag);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Clamp(this float2 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            return vec * (maxmag / mag);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float val, float maxmag)
        {
            if (val <= -maxmag) return -maxmag;
            if (val >= maxmag) return maxmag;
            return val;
        }
        // Soft clamp has limit at maxmag*2 when the value approaches infinity
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 SoftClamp(this float4 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            var p = mag / maxmag;
            return (2 - math.pow(.5f, p - 1)) / p * vec;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 SoftClamp(this float3 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            var p = mag / maxmag;
            return (2 - math.pow(.5f, p - 1)) / p * vec;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 SoftClamp(this float2 vec, float maxmag)
        {
            var mag = math.length(vec);
            if (mag <= maxmag)
                return vec;
            var p = mag / maxmag;
            return (2 - math.pow(.5f, p - 1))/p*vec;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SoftClamp(float val, float maxmag)
        {
            if (val >= -maxmag && val <= maxmag) return val;
            var sign = math.sign(val);
            var mag = sign* val;
            var p = mag / maxmag;
            return (2 - math.pow(.5f, p - 1)) *maxmag *sign;
        }


        #region Euler integration of angular velocity
        public static quaternion IntegrateOrientation(quaternion orientation, float3 angularVelocity, float timestep)
        {
            quaternion dq = IntegrateAngularVelocity(angularVelocity, timestep);
            quaternion r = math.mul(dq,orientation);
            return math.normalize(r);
        }

        // quicker integrator
        public static quaternion IntegrateAngularVelocityApprox(float3 angularVelocity, float timestep)
        {
            float3 halfDeltaTime = new float3(timestep * 0.5f);
            float3 halfDeltaAngle = angularVelocity * halfDeltaTime;
            return math.normalize(new quaternion(new float4(halfDeltaAngle, 1.0f)));
        }
        public static quaternion IntegrateAngularVelocity(float3 angularVelocity, float timestep)
        {
            return (angularVelocity * timestep).ToQuaternion();
        }
        public static RigidTransform Integrate(this RigidTransform transform, MotionVector mv, float timestep)
        {
            return new RigidTransform(IntegrateOrientation(transform.rot, mv.angular, timestep), transform.pos + mv.linear * timestep);
        }
        #endregion

        public static float4x4 safeinverse(float4x4 m)
        {
            var inv = math.inverse(m);
#if NBG_RECOIL_DEBUG
            //TODO: remove in production
            if (!math.isfinite(inv.c0.x))  
            {
                Debug.LogError("Falling back to double4x4 inverse!");
                inv = new float4x4(math.inverse(new double4x4(m)));
                AssertFinite(inv);
            }
#endif
            return inv;
        }

        [BurstDiscard]
        public static void AssertFinite(float4x4 m)
        {
            AssertFinite(m.c0);
            AssertFinite(m.c1);
            AssertFinite(m.c2);
            AssertFinite(m.c3);
        }
        [BurstDiscard]
        public static void AssertFinite(float4 v)
        {
            if (!math.isfinite(math.csum(v)))
                Debug.LogError("Infinite");
        }
        [BurstDiscard]
        public static void AssertFinite(float3 v)
        {
            if (!math.isfinite(math.csum(v)))
                Debug.LogError("Infinite");
        }
        [BurstDiscard]
        public static void AssertFinite(float v)
        {
            if (!math.isfinite(v))
                Debug.LogError("Infinite");
        }
    }
}