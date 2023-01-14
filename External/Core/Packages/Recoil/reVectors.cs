using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    [BurstCompile]
    public static partial class re
    {
        public static readonly float3 zero = new float3(0f, 0f, 0f);
        public static readonly float3 one = new float3(1f, 1f, 1f);
        public static readonly float3 up = new float3(0f, 1f, 0f);
        public static readonly float3 down = new float3(0f, -1f, 0f);
        public static readonly float3 left = new float3(-1f, 0f, 0f);
        public static readonly float3 right = new float3(1f, 0f, 0f);
        public static readonly float3 forward = new float3(0f, 0f, 1f);
        public static readonly float3 back = new float3(0f, 0f, -1f);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 normalized(this float3 v3) => math.normalize(v3);
        //[MethodImpl(MethodImplOptions.AggressiveInlining)] public static float magnitude(this float3 v3) => math.length(v3);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float2 To2D(this float3 v3) => v3.xz;
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 To3D(this float2 v2) => new float3(v2.x, 0, v2.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 To3D(this float2 v2, float y) => new float3(v2.x, y, v2.y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float4 To4D(this float3 v3) => new float4(v3,0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 To3D(this float4 v4) => v4.xyz;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 ZeroY(this float3 v2)=> new float3(v2.x, 0, v2.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 SetY(this float3 v2, float y)=>new float3(v2.x, y, v2.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 SetX(this float3 v2, float x) => new float3(x, v2.y, v2.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 SetZ(this float3 v2, float z) => new float3(v2.x, v2.y, z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 FlipX(this float3 v3) => new float3(-v3.x, v3.y, v3.z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static float3 FlipX(this float3 v3, bool flip) => new float3(flip? - v3.x:v3.x, v3.y, v3.z);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float InverseLerp(float a, float b, float s)
        {
            if (a != b)
                return math.saturate((s - a) / (b - a));
            else
                return 0.0f;
        }

        // blends between weighted values
        public static float LerpWeighted(float a, float wa, float b, float wb, float s)
        {
            var q = wa * (1 - s) + wb * s;
            if (q != 0)
                return (a * wa * (1 - s) + b * wb * s) / q;
            else
                return math.lerp(a, b, s);
        }
        public static float2 LerpWeighted(float2 a, float wa, float2 b, float wb, float s)
        {
            var q = wa * (1 - s) + wb * s;
            if (q != 0)
                return (a * wa * (1 - s) + b * wb * s) / q;
            else
                return math.lerp(a, b, s);
        }

        public static float3 LerpWeighted(float3 a, float wa, float3 b, float wb, float s)
        {
            var q = wa * (1 - s) + wb * s;
            if (q != 0)
                return (a * wa * (1 - s) + b * wb * s) / q;
            else
                return math.lerp(a, b, s);
        }
        public static float4 LerpWeighted(float4 a, float wa, float4 b, float wb, float s)
        {
            var q = wa * (1 - s) + wb * s;
            if (q != 0)
                return (a * wa * (1 - s) + b * wb * s) / q;
            else
                return math.lerp(a, b, s);
        }

    }
}
