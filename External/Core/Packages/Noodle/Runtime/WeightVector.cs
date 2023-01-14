using NBG.Unsafe;
using Recoil;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public unsafe struct WeightVector4
    {
        public float4 weights;
        public float this[int index] { get { Unsafe.CheckIndex(index, 4); return weights[index]; } }
        public void Pull(int index, float maxDelta)
        {
            var w = this[index];
            if (w == 1) return;
            var sum = math.csum(weights) - w;
            w = re.MoveTowards(w, 1, maxDelta);
            if (sum > 0)
            {
                var scale = (1 - w) / sum;
                weights *= scale;
            }
            weights[index] = w;
        }
    }
    public unsafe struct WeightVector8
    {
        public float4 w0;
        public float4 w1;
        public float this[int index] { get { Unsafe.CheckIndex(index, 8); return index < 4 ? w0[index] : w1[index - 4]; } }
        public void Pull(int index, float maxDelta)
        {
            var w = this[index];
            if (w == 1) return;
            var sum = math.csum(w0)+ math.csum(w1) - w;
            w = re.MoveTowards(w, 1, maxDelta);
            if (sum > 0)
            {
                var scale = (1 - w) / sum;
                w0 *= scale;
                w1 *= scale;
            }
            if (index < 4)
                w0[index] = w;
            else
                w1[index - 4] = w;
        }
    }
}
