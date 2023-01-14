using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public static class SheepWander
{

    public static bool Begin(Sheep s)
    {
        s.wanderTarget = s.relaxHead + Random.insideUnitCircle.To3D()*Random.Range(.25f,1f);
        s.wanderSpeed = Random.Range(.25f, .5f);
        var dir = (s.relaxHead - s.relaxTail).normalized;
        //Debug.DrawLine(s.relaxHead, s.wanderTarget, Color.blue,2);
        if (Physics.SphereCast(new Ray(s.relaxHead, (s.wanderTarget - s.relaxHead).normalized), Sheep.radius / 2, (s.wanderTarget - s.relaxHead).magnitude, (int)Layers.Walls))
            return false;

        var p1 = s.relaxHead.To2D();
        var v1 = s.wanderTarget.To2D()-p1;
        // check if any of the neighbors is blocking the path
        for (int j = 0; j < s.neighbors.Count; j++)
        {
            var n = s.neighbors[j];
            var p2 = n.relaxTail.To2D();
            var v2 = n.relaxHead.To2D() - p2;
            SheepMath2D.SegmentSegmentIntersection(out Vector2 t1, out Vector2 t2, p1,v1,p2,v2 * 1.5f);
            var dist = (t1 - t2).magnitude;
            if (dist < Sheep.radius * 2)
            {
                //Debug.Log("Bad " + s.id + " " + n.id);
                //Debug.DrawLine(t1.To3D(), t2.To3D(), Color.red, 2);
                return false;
            }

        }
        //Debug.Log("Good " + s.id );
        return true;
    }

    public static bool Process(Sheep s)
    {
        var offset = (s.wanderTarget - s.relaxHead);
        s.scare = offset.normalized * s.wanderSpeed;
        return offset.magnitude>.1f;
    }
}
