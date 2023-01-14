using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;
using Unity.Collections;
using Unity.Mathematics;
using Recoil;

public class Threat 
{
    public NativeList<BoxBoundThreat> all;
    public NativeList<BoxBoundThreat> pokeThreats;
    const float minScareAmount = 1f;

    static Threat instance;
    public static Threat GetInstance()
    {
        return instance;
    }

    public static void Initialize()
    {
        instance = new Threat();
    }

    public static void Dispose()
    {
        if (instance != null)
        {
            instance.all.Dispose();
            instance.pokeThreats.Dispose();
            instance = null;
        }
    }

    Threat()
    {
        all = new NativeList<BoxBoundThreat>(Allocator.Persistent);
        pokeThreats = new NativeList<BoxBoundThreat>(Allocator.Persistent);
    }

    public static int RegularThreatCount
    {
        get
        {
            return GetInstance().all.Length;
        }
    }

    public static void AddRegularThreat(BoxBoundThreat threat)
    {
        GetInstance().all.Add(threat);
    }

    public static float3 GetClosestPointToRegularThreat(int index, float3 position)
    {
        return GetInstance().all[index].GetClosestPoint(position);
    }

    public static int PokeThreatCount
    {
        get
        {
            return GetInstance().pokeThreats.Length;
        }
    }

    public static void AddPokeThreat(BoxBoundThreat threat)
    {
        GetInstance().pokeThreats.Add(threat);
    }

    public static void ClearAllThreats()
    {
        GetInstance().all.Clear();
        GetInstance().pokeThreats.Clear();
    }
    //public static Vector3 CalculateScare(Vector3 pos, float bravery = 1)
    //{
    //    var sum = Vector2.zero;
    //    for (int t = 0; t < all.Count; t++)
    //    {
    //        var threat = all[t];
    //        var dir = pos - threat.pos;

    //        //sum = Max(sum, 1.5f * dir.To2D().normalized * Mathf.InverseLerp(9 * threat.strength, 1 * threat.strength, dir.sqrMagnitude / bravery) * bravery);
    //        sum = Max(sum, 1.5f * dir.To2D().normalized * Mathf.InverseLerp(4 * threat.strength, 1 * threat.strength, dir.sqrMagnitude / bravery) * bravery);
    //    }
    //    return sum.To3D();
    //}
    public static Vector3 CalculateScare(Vector3 headPos, Vector3 tailPos, float dist, Sheep.SheepScareTypes scareTypes)
    {
        return CalculateScare(headPos, tailPos, dist, scareTypes, GetInstance().all, GetInstance().pokeThreats);
    }

    // You can't access static variables from a job, therefore you have to pass them into the job beforehand
    public static float3 CalculateScare(float3 headPos, float3 tailPos, float dist, Sheep.SheepScareTypes scareTypes,
        NativeList<BoxBoundThreat> regularThreats, NativeList<BoxBoundThreat> pokeThreats)
    {
        var sum = float2.zero;
        if ((scareTypes & Sheep.SheepScareTypes.Herding) != 0)
        {
            sum = CalculateScare(sum, headPos, tailPos, dist, regularThreats);
        }
        if ((scareTypes & Sheep.SheepScareTypes.Poking) != 0)
        {
            sum = CalculateScare(sum, headPos, tailPos, dist, pokeThreats);
        }
        return new float3(sum.x, 0f, sum.y);
    }

    static float2 CalculateScare(float2 sum, float3 headPos, float3 tailPos, float dist, NativeList<BoxBoundThreat> threats)
    {
        for (int t = 0; t < threats.Length; t++)
        {
            var threat = threats[t];
            var dir = headPos - threat.GetClosestPoint(tailPos);

            //sum = Max(sum, 1.5f * dir.To2D().normalized * Mathf.InverseLerp(9 * threat.strength, 1 * threat.strength, dir.sqrMagnitude / bravery) * bravery);
            // MAX SCARE < MinDist < SCARE FALLOFF < MaxDist < NO SCARE
            var minDist = math.lerp(1f, 1.5f, dist) * (.25f + .75f * threat.Range);
            var maxDist = math.lerp(1.5f, 3f, dist) * (.25f + .75f * threat.Range);
            var s = re.InverseLerp(maxDist * maxDist, minDist * minDist, math.lengthsq(dir));

            sum = Max(sum, threat.Strength * math.normalize(dir.xz) * math.pow(s, 1.5f) * threat.Range);
        }

        var scareMagnitude = math.length(sum);
        if (scareMagnitude > 0f && scareMagnitude < minScareAmount)
        {
            sum = math.normalize(sum) * minScareAmount;
        }
        return sum;
    }

    public static float3 Max(float3 a, float3 b)
    {
        var aMag = math.length(a);
        var bMag = math.length(b);
        return math.normalizesafe(a * aMag + b * bMag, float3.zero) * math.max(aMag, bMag);
    }
    public static float2 Max(float2 a, float2 b)
    {
        var aMag = math.length(a);
        var bMag = math.length(b);
        return math.normalizesafe(a * aMag + b * bMag, float2.zero) * math.max(aMag, bMag);
    }
}

public struct BoxBoundThreat
{
    public BoxBounds bounds;
    public float Range { get; set; }
    public float Strength { get; set; }

    public BoxBoundThreat(Collider collider, float range, float strength)
    {
        bounds = new BoxBounds(collider);
        Range = range;
        Strength = strength;
    }

    public BoxBoundThreat(float3 position, float range)
    {
        bounds = new BoxBounds
        {
            center = position,
            size = float3.zero,
            rotation = quaternion.identity
        };
        Range = range;
        Strength = 1.5f;
    }

    public float3 GetClosestPoint(float3 position)
    {
        var closestPoint = bounds.ClosestPoint(position);
        return closestPoint;
    }
}
