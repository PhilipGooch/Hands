using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public static partial class re
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(lt3x3 a, float3 b)
        {
            return a * b;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(float3 a, lt3x3 b)
        {
            return a * b;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul(float3x3 a, float3 b)
        {
            return math.mul( a, b);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static lt3x3 mul(lt3x3 lhs, lt3x3 rhs)
        {
            var res = new lt3x3
            {
                m00 = lhs.m00 * rhs.m00 + lhs.m10 * rhs.m10 + lhs.m20 * rhs.m20,
                m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m21 * rhs.m20,
                m11 = lhs.m10 * rhs.m10 + lhs.m11 * rhs.m11 + lhs.m21 * rhs.m21,
                m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20,
                m21 = lhs.m20 * rhs.m10 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21,
                m22 = lhs.m20 * rhs.m20 + lhs.m21 * rhs.m21 + lhs.m22 * rhs.m22
            };
            //res.m01 = lhs.m00 * rhs.m10 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
            //res.m02 = lhs.m00 * rhs.m20 + lhs.m01 * rhs.m21 + lhs.m02 * rhs.m22;

            //res.m12 = lhs.m10 * rhs.m20 + lhs.m11 * rhs.m21 + lhs.m12 * rhs.m22;


            return res;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mul(float3x3 a, float3x3 b)
        {
            return math.mul(a, b);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mul(float3x3 a, lt3x3 b)
        {
            return math.float3x3(
                a.c0.x * b.m00 + a.c1.x * b.m10 + a.c2.x * b.m20, a.c0.x * b.m10 + a.c1.x * b.m11 + a.c2.x * b.m21, a.c0.x * b.m20 + a.c1.x * b.m21 + a.c2.x * b.m22,
                a.c0.y * b.m00 + a.c1.y * b.m10 + a.c2.y * b.m20, a.c0.y * b.m10 + a.c1.y * b.m11 + a.c2.y * b.m21, a.c0.y * b.m20 + a.c1.y * b.m21 + a.c2.y * b.m22,
                a.c0.z * b.m00 + a.c1.z * b.m10 + a.c2.z * b.m20, a.c0.z * b.m10 + a.c1.z * b.m11 + a.c2.z * b.m21, a.c0.z * b.m20 + a.c1.z * b.m21 + a.c2.z * b.m22);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 mul(lt3x3 a, float3x3 b)
        {
            return math.float3x3(
                a.m00 * b.c0.x + a.m10 * b.c0.y + a.m20 * b.c0.z, a.m00 * b.c1.x + a.m10 * b.c1.y + a.m20 * b.c1.z, a.m00 * b.c2.x + a.m10 * b.c2.y + a.m20 * b.c2.z,
                a.m10 * b.c0.x + a.m11 * b.c0.y + a.m21 * b.c0.z, a.m10 * b.c1.x + a.m11 * b.c1.y + a.m21 * b.c1.z, a.m10 * b.c2.x + a.m11 * b.c2.y + a.m21 * b.c2.z,
                a.m20 * b.c0.x + a.m21 * b.c0.y + a.m22 * b.c0.z, a.m20 * b.c1.x + a.m21 * b.c1.y + a.m22 * b.c1.z, a.m20 * b.c2.x + a.m21 * b.c2.y + a.m22 * b.c2.z);
        }

        // multiply transposed matrix by vector
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 tmul(float4x4 a, float4 b)
        {
            return new float4(
                math.dot(a.c0, b),
                math.dot(a.c1, b),
                math.dot(a.c2, b),
                math.dot(a.c3, b));
        }
        //returns  vx
        public static float3x3 cross(float3 v)
        {
            return new float3x3(
                0, -v.z, v.y,
                v.z, 0, -v.x,
                -v.y, v.x, 0);
        }

        //multiplies M by vx
        public static lt3x3 ltcross(float3x3 M, float3 v)
        {
            return new lt3x3(
                M.c0.x * 0000 + M.c1.x * +v.z + M.c2.x * -v.y,
                // M.c0.x * -v.z + M.c1.x * 0000 + M.c2.x * +v.x,
                // M.c0.x * +v.y + M.c1.x * -v.x + M.c2.x * 0000,

                M.c0.y * 0000 + M.c1.y * +v.z + M.c2.y * -v.y,
                M.c0.y * -v.z + M.c1.y * 0000 + M.c2.y * +v.x,
                // M.c0.y * +v.y + M.c1.y * -v.x + M.c2.y * 0000,

                M.c0.z * 0000 + M.c1.z * +v.z + M.c2.z * -v.y,
                M.c0.z * -v.z + M.c1.z * 0000 + M.c2.z * +v.x,
                M.c0.z * +v.y + M.c1.z * -v.x + M.c2.z * 0000);
        }

        public static lt3x3 ltcrosscross( float3 l, float3 r)
        {
            return new lt3x3(
                0 * 0000 - l.z * +r.z + l.y * -r.y,
                //  0 * -r.z - l.z * 0000 + l.y * +r.x,
                //  0 * +r.y - l.z * -r.x + l.y * 0000,

                l.z * 0000 + 0 * +r.z - l.x * -r.y,
                l.z * -r.z + 0 * 0000 - l.x * +r.x,
                //  l.z * +r.y + 0 * -r.x - l.x * 0000,

                -l.y * 0000 + l.x * +r.z + 0 * -r.y,
                -l.y * -r.z + l.x * 0000 + 0 * +r.x,
                -l.y * +r.y + l.x * -r.x + 0 * 0000);
        }
// l XrX
        // public static lt3x3_2 crosscross(float3 l, float3 r)
        // {
        //     return new lt3x3_2(
        //         -l.z * r.z - l.y * r.y,
        //
        //         l.x * r.y,
        //         -l.z * r.z - l.x * r.x,
        //
        //         l.x * +r.z,
        //         l.y * r.z,
        //         -l.y * r.y - l.x * r.x);
        // }
        //multiplies M by vx
        public static float3x3 cross(float3x3 M, float3 v)
        {
            return new float3x3(
                M.c0.x * 0000 + M.c1.x * +v.z + M.c2.x * -v.y,
                M.c0.x * -v.z + M.c1.x * 0000 + M.c2.x * +v.x,
                M.c0.x * +v.y + M.c1.x * -v.x + M.c2.x * 0000,

                M.c0.y * 0000 + M.c1.y * +v.z + M.c2.y * -v.y,
                M.c0.y * -v.z + M.c1.y * 0000 + M.c2.y * +v.x,
                M.c0.y * +v.y + M.c1.y * -v.x + M.c2.y * 0000,

                M.c0.z * 0000 + M.c1.z * +v.z + M.c2.z * -v.y,
                M.c0.z * -v.z + M.c1.z * 0000 + M.c2.z * +v.x,
                M.c0.z * +v.y + M.c1.z * -v.x + M.c2.z * 0000);
        }

        //multiplies vx by M.transposed
        public static lt3x3 ltcrossT(float3 v, float3x3 M)
        {
            var res = new lt3x3
            {
                m00 = 0 * M.c0.x - v.z * M.c1.x + v.y * M.c2.x,
                m10 = v.z * M.c0.x + 0 * M.c1.x - v.x * M.c2.x,
                m11 = v.z * M.c0.y + 0 * M.c1.y - v.x * M.c2.y,
                m20 = -v.y * M.c0.x + v.x * M.c1.x + 0 * M.c2.x,
                m21 = -v.y * M.c0.y + v.x * M.c1.y + 0 * M.c2.y,
                m22 = -v.y * M.c0.z + v.x * M.c1.z + 0 * M.c2.z
            };
            //res.m01 = 0 * M.m10 - v.z * M.m11 + v.y * M.m12;
            //res.m02 = 0 * M.m20 - v.z * M.m21 + v.y * M.m22;

            //res.m12 = v.z * M.m20 + 0 * M.m21 - v.x * M.m22;


            return res;
        }

        //multiplies vx by M
        public static float3x3 cross(float3 v, lt3x3 M)
        {
            return new float3x3(
                0 * M.m00 - v.z * M.m10 + v.y * M.m20,
                0 * M.m10 - v.z * M.m11 + v.y * M.m21,
                0 * M.m20 - v.z * M.m21 + v.y * M.m22,

                v.z * M.m00 + 0 * M.m10 - v.x * M.m20,
                v.z * M.m10 + 0 * M.m11 - v.x * M.m21,
                v.z * M.m20 + 0 * M.m21 - v.x * M.m22,

                -v.y * M.m00 + v.x * M.m10 + 0 * M.m20,
                -v.y * M.m10 + v.x * M.m11 + 0 * M.m21,
                -v.y * M.m20 + v.x * M.m21 + 0 * M.m22);
        }

        //multiplies vx by M
        public static float3x3 cross(float3 v, float3x3 M)
        {
            return new float3x3(
                0 * M.c0.x - v.z * M.c0.y + v.y * M.c0.z,
                0 * M.c1.x - v.z * M.c1.y + v.y * M.c1.z,
                0 * M.c2.x - v.z * M.c2.y + v.y * M.c2.z,

                v.z * M.c0.x + 0 * M.c0.y - v.x * M.c0.z,
                v.z * M.c1.x + 0 * M.c1.y - v.x * M.c1.z,
                v.z * M.c2.x + 0 * M.c2.y - v.x * M.c2.z,

                -v.y * M.c0.x + v.x * M.c0.y + 0 * M.c0.z,
                -v.y * M.c1.x + v.x * M.c1.y + 0 * M.c1.z,
                -v.y * M.c2.x + v.x * M.c2.y + 0 * M.c2.z);

        }
        public static float3x3 crosscross( float3 l, float3 r)
        {
            return new float3x3(
                0 * 0000 - l.z * +r.z + l.y * -r.y,
                0 * -r.z - l.z * 0000 + l.y * +r.x,
                0 * +r.y - l.z * -r.x + l.y * 0000,

                l.z * 0000 + 0 * +r.z - l.x * -r.y,
                l.z * -r.z + 0 * 0000 - l.x * +r.x,
                l.z * +r.y + 0 * -r.x - l.x * 0000,

                -l.y * 0000 + l.x * +r.z + 0 * -r.y,
                -l.y * -r.z + l.x * 0000 + 0 * +r.x,
                -l.y * +r.y + l.x * -r.x + 0 * 0000);
        }



        public static float3x3 inverse(float3x3 m) => math.inverse(m);

        public static lt3x3 inverse(lt3x3 m)
        {
            var DET = m.m00 * (m.m22 * m.m11 - m.m21 * m.m21)
                      - m.m10 * (m.m22 * m.m10 - m.m21 * m.m20)
                      + m.m20 * (m.m21 * m.m10 - m.m11 * m.m20);

            if (DET == 0.0f)
            {
                Debug.LogError("Can't calculate inverse for matrix with 0 D");
                return lt3x3.identity;
            }

            return new lt3x3(
                (m.m22 * m.m11 - m.m21 * m.m21) / DET,
                -(m.m22 * m.m10 - m.m20 * m.m21) / DET, (m.m22 * m.m00 - m.m20 * m.m20) / DET,
                (m.m21 * m.m10 - m.m20 * m.m11) / DET, -(m.m21 * m.m00 - m.m20 * m.m10) / DET, (m.m11 * m.m00 - m.m10 * m.m10) / DET);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForceVector mul(RigidBodyInertia I, MotionVector vec)
        {
            var w = vec.angular;
            var v = vec.linear;
            return new ForceVector(re.mul(I.I,w) + math.cross(I.h, v), I.m * v - math.cross(I.h, w));
            
            // var Ia =  ArticulatedBodyInertia.FromRigidBodyInertia(I);
            // return mul(Ia, vec);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 mul_get_angular(RigidBodyInertia I, MotionVector vec)
        {
            var w = vec.angular;
            var v = vec.linear;
            return re.mul(I.I,w) + math.cross(I.h, v);
        }
        
        
        
        public static ForceVector mul(ArticulatedBodyInertia IA, MotionVector vec)
        {
            var w = vec.angular;
            var v = vec.linear;
            var M = IA.M;
            var H = IA.H;
            var I = IA.I;

            return new ForceVector(re.mul( I, w) + math.mul(H, v), re.mul( M, v) + math.mul(math.transpose(H), w));
        }
        public static MotionVector mul(ArticulatedBodyInertia IA, ForceVector vec)
        {
            var w = vec.angular;
            var v = vec.linear;
            var M = IA.M;
            var H = IA.H;
            var I = IA.I;

            return new MotionVector(re.mul( I, w) + math.mul(H, v), re.mul( M, v) + math.mul(math.transpose(H), w));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MotionVector invmul(ArticulatedBodyInertia I, ForceVector vec)
        {
            var w = vec.angular;
            var v = vec.linear;

            var IA = inverse(I);
            var Minv = IA.M;
            var Hinv = IA.H;
            var Iinv = IA.I;

            return new MotionVector(re.mul( Iinv, w) + math.mul(Hinv, v), re.mul( Minv, v) + math.mul(math.transpose(Hinv), w));
        }
        //https://www.dr-lex.be/random/matrix-inv.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MotionVector invmul(RigidBodyInertia I, ForceVector vec)
        {
            return invmul(ArticulatedBodyInertia.FromRigidBodyInertia(I), vec);
        }
      
        //https://en.wikipedia.org/wiki/Invertible_matrix#Blockwise_inversion
        public static ArticulatedBodyInertia inverse(ArticulatedBodyInertia r)
        {
            var A = r.I;
            var B = r.H;
            var C = math.transpose(r.H);
            var D = r.M;

            var Ainv = re.inverse(A);

            var DCABinv = inverse( D - new lt3x3( math.mul(re.mul(C, Ainv), B)));
            var AiB = re.mul(Ainv, B);
            var CAi = math.transpose(AiB); //re.mul(C, Ainv);
            var Ai = Ainv + new lt3x3( math.mul(re.mul(AiB, DCABinv), CAi));
            var Bi = -re.mul(AiB, DCABinv);
            //var Ci = math.transpose(Bi);// -math.mul( DCABinv,CAi);
            var Di = DCABinv;
            
            return  new ArticulatedBodyInertia(Di, Bi, Ai );
        }

        public static RigidTransform invmul(RigidTransform a, RigidTransform b )
        {
            return math.mul(math.inverse(a), b);
        }
        public static float3 invmul(RigidTransform a, float3 b)
        {
            return math.transform(math.inverse(a), b);
        }

        public static quaternion invmul(quaternion a, quaternion b)
        {
            return math.mul(math.inverse(a), b);
        }
        public static float3 invmul(quaternion a, float3 b)
        {
            return math.rotate(math.inverse(a), b);
        }

    }
}
