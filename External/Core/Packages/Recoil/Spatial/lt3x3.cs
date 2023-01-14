using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Recoil
{
   public struct lt3x3
    {

        public float m00;
        public float m10, m11;
        public float m20, m21, m22;

        // Returns a matrix with all elements set to zero (RO).
        public static lt3x3 zero { get; } = new lt3x3(0, 0, 0, 0, 0, 0);
        public static lt3x3 identity { get; } = new lt3x3(1, 0, 1, 0, 0, 1);
        public static lt3x3 Diagonal(float v)
        {
            return new lt3x3(v, 0, v, 0, 0, v);
        }
        public static lt3x3 Diagonal(float3 v)
        {
            return new lt3x3(v.x, 0, v.y, 0, 0, v.z);
        }
        public static lt3x3 Diagonal(float m00, float m11, float m22)
        {
            return new lt3x3(m00, 0, m11, 0, 0, m22);
        }


        public lt3x3(
            float m00,
            float m10, float m11,
            float m20, float m21, float m22)
        {
            this.m00 = m00;
            this.m10 = m10;
            this.m11 = m11;
            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
        }

        public lt3x3(float3x3 m)
        {
            this.m00 = m.c0.x;
            this.m10 = m.c0.y;
            this.m11 = m.c1.y;
            this.m20 = m.c0.z;
            this.m21 = m.c1.z;
            this.m22 = m.c2.z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 operator *(lt3x3 a, float3 b)
        {
            return math.float3(
                a.m00 * b.x + a.m10 * b.y + a.m20 * b.z,
                a.m10 * b.x + a.m11 * b.y + a.m21 * b.z,
                a.m20 * b.x + a.m21 * b.y + a.m22 * b.z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 operator *(float3 a, lt3x3 b )
        {
            return math.float3(
                a.x * b.m00 + a.y * b.m10 + a.z * b.m20,
                a.x * b.m10 + a.y * b.m11 + a.z * b.m21,
                a.x * b.m20 + a.y * b.m21 + a.z * b.m22);
        }
        public static lt3x3 operator *(lt3x3 lhs, float rhs)
        {
            return new lt3x3(
                lhs.m00 * rhs,
                lhs.m10 * rhs, lhs.m11 * rhs,
                lhs.m20 * rhs, lhs.m21 * rhs, lhs.m22*rhs);

        }

        public static lt3x3 operator +(lt3x3 lhs, lt3x3 rhs)
        {
            return new lt3x3(
                lhs.m00 + rhs.m00,
                lhs.m10 + rhs.m10, lhs.m11 + rhs.m11,
                lhs.m20 + rhs.m20, lhs.m21 + rhs.m21, lhs.m22 + rhs.m22);

        }

        public static lt3x3 operator -(lt3x3 lhs, lt3x3 rhs)
        {
            return new lt3x3(
                lhs.m00 - rhs.m00,
                lhs.m10 - rhs.m10, lhs.m11 - rhs.m11,
                lhs.m20 - rhs.m20, lhs.m21 - rhs.m21, lhs.m22 - rhs.m22);
        }
        public static lt3x3 operator -(lt3x3 rhs)
        {
            return new lt3x3(
                 - rhs.m00,
                 - rhs.m10, - rhs.m11,
                 - rhs.m20, - rhs.m21,  - rhs.m22);
        }
      

        public static float3x3 operator -(float3x3 a, lt3x3 b)
        {
            return math.float3x3(
                a.c0.x - b.m00, a.c1.x - b.m10, a.c2.x - b.m20,
                a.c0.y - b.m10, a.c1.y - b.m11, a.c2.y - b.m21,
                a.c0.z - b.m20, a.c1.z - b.m21, a.c2.z - b.m22);

        }

        public static float3x3 operator -(lt3x3 a, float3x3 b)
        {
            return math.float3x3(
                a.m00 - b.c0.x, a.m10 - b.c1.x, a.m20 - b.c2.x,
                a.m10 - b.c0.y, a.m11 - b.c1.y, a.m21 - b.c2.y,
                a.m20 - b.c0.z, a.m21 - b.c1.z, a.m22 - b.c2.z);

        }
        public static float3x3 operator +(lt3x3 a, float3x3 b)
        {
            return math.float3x3(
                a.m00 + b.c0.x, a.m10 + b.c1.x, a.m20 + b.c2.x,
                a.m10 + b.c0.y, a.m11 + b.c1.y, a.m21 + b.c2.y,
                a.m20 + b.c0.z, a.m21 + b.c1.z, a.m22 + b.c2.z);

        } public static float3x3 operator +(float3x3 a, lt3x3 b)
        {
            return math.float3x3(
                a.c0.x + b.m00, a.c1.x + b.m10, a.c2.x + b.m20,
                a.c0.y + b.m10, a.c1.y + b.m11, a.c2.y + b.m21,
                a.c0.z + b.m20, a.c1.z + b.m21, a.c2.z + b.m22);

        }

        public float3x3 ToFloat3x3()
        {
            return math.float3x3(
                m00, m10, m20,
                m10, m11, m21,
                m20, m21, m22);

        }
        public float4x4 ToFloat4x4()
        {
            return math.float4x4(
                m00, m10, m20, 0,
                m10, m11, m21, 0,
                m20, m21, m22, 0,
                0,   0,   0,   1);

        }


        public override string ToString()
        {
            return $"{m00:F5}\t{m10:F5}\t{m20:F5}\n{m10:F5}\t{m11:F5}\t{m21:F5}\n{m20:F5}\t{m21:F5}\t{m22:F5}\n";
        }
    }
}
