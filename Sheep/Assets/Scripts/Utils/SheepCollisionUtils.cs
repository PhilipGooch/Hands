using NBG.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SheepCollisionUtils
{
    public static Vector3 ClosestPoint(this Rigidbody body, Vector3 pos)
    {
        Vector3 best = pos;
        float bestD = float.MaxValue;
        foreach (var c in body.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled) continue;
            var candidate = c.ClosestPointSafe(pos);
            var d = (candidate - pos).sqrMagnitude;
            if (d < bestD)
            {
                bestD = d;
                best = candidate;
            }
        }
        return best;
    }

    public static float RayCast(Vector3 pos, Vector3 dir, float dist, float radius, int layers)
    {
        if (Physics.SphereCast(pos, radius, dir, out var hit, dist, layers, QueryTriggerInteraction.Ignore))
        {
            //Debug.DrawRay(pos, dir * (hit.distance+radius), Color.red);
            return dist - (hit.distance + radius);
        }
        else
        {
            //Debug.DrawRay(pos, dir * dist, Color.black);
            return 0;
        }
    }

    // hitpoint - a point on surface where slope is to be measured
    // radius - the radius of sphere used to measure slope (smaller radius, more local measurement)
    public static bool CheckSlope(Vector3 hitpoint, float radius, float minSlopeDeg, int layers, Transform debugSphere = null)
    {
        //var pos = hitpoint + radius * Vector3.up; // move 1 radius up from hitpoint
        //radius *= Mathf.Cos(minSlopeDeg / Mathf.Deg2Rad);

        var height = radius / Mathf.Cos(minSlopeDeg * Mathf.Deg2Rad); // calculate center of the sphere of given radius that fits inside specifield slope angle
        var pos = hitpoint + height * Vector3.up; // move 1 radius up from hitpoint

        if (debugSphere)
        {
            debugSphere.SetParent(null);
            debugSphere.position = pos;
            debugSphere.localScale = Vector3.one * radius * 2;
        }

        return Physics.CheckSphere(pos, radius, layers);
    }
}
