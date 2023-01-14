using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public struct VectorSet
{
    float3 sum;
    float weight;
    float maxWeight;
    public float3 value => weight > 0 ? sum / weight: float3.zero;
    public float3 max => weight > 0 ? math.normalize(sum)* maxWeight : float3.zero;

    public void AddSquare(float3 v)
    {
        //var mag = v.magnitude;
        //sum += v * mag;
        //weight += mag ;
        //maxWeight = Mathf.Max(maxWeight, mag);
        Add(v, math.length(v));
    }
    public void Add(float3 v, float w)
    {
        sum += v * w;
        weight += w;
        maxWeight = math.max(maxWeight, w);
    }

    public void Add(float3 v)
    {
        Add(v, 1);
        //var mag = v.magnitude;
        //sum += v ;
        //weight += mag ;
        //maxWeight = Mathf.Max(maxWeight, mag);
    }

    internal void Reset()
    {
        sum = float3.zero;
        weight = maxWeight = 0;
    }
}
