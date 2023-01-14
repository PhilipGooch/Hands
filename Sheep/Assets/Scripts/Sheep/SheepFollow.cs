using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SheepFollow 
{
    public static bool Begin(Sheep s)
    {
        s.neighbor = null;
        var bestPriority = 0f;
        for (int i = 0; i < s.neighbors.Count; i++)
        {
            var n = s.neighbors[i];
            var pos = s.relaxHead * 1.5f - s.relaxTail * .5f;
            var offset = pos - n.ClosestPointOnSheep(pos);// + scare * Time.fixedDeltaTime);
            if(offset.magnitude < .3f)// too close to a sheep
            {
                s.neighbor = null;
                return false;
            }

            var dir = (pos - s.relaxTail).normalized;
            var strength = Mathf.InverseLerp(3f, 0f, offset.magnitude);
            var lookStrength = Mathf.Lerp(1f, .1f, Vector3.Angle(dir, offset) / 180); // more pull to front

            var priority = 10 - offset.magnitude;// strength * lookStrength;
            if (priority > bestPriority)
            {
                bestPriority = priority;
                s.neighbor = n;
            }
        }
        return s.neighbor != null;
    }
    public static bool Process(Sheep s)
    {

        var pos = s.relaxHead * 1.5f - s.relaxTail * .5f;
        //var pos = relaxHead;// + scare * Time.fixedDeltaTime;
        var offset = pos - s.neighbor.ClosestPointOnSheep(pos);
        if (offset.magnitude > .25f)
            s.scare += -.2f * offset.normalized;// * Mathf.InverseLerp(.3f, .4f, offset.magnitude);
        else
            s.neighbor = null;
        return s.neighbor != null;

    }
}
