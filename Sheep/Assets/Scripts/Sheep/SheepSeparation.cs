using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

public static class SheepSeparation
{
    static List<Sheep> pairs = new List<Sheep>();

    public static void SeparateAll(IList<Sheep> all)
    {

        // collect pairs
        pairs.Clear();
        var scanDist = Sheep.separationDist * 2;
        var scanDistSq = scanDist * scanDist;
        var rejectSq = (scanDist + 2 * Sheep.targetLen) * (scanDist + 2 * Sheep.targetLen);
        for (int i = 0; i < all.Count; i++)
        {
            var s = all[i];
            for (int j = 0; j < s.neighbors.Count; j++)
            {
                var n = s.neighbors[j];
                var headHead = (s.relaxHead - n.relaxHead).sqrMagnitude;
                if (headHead > rejectSq) continue;// quick reject
                if (
                     headHead < scanDistSq ||
                    (s.relaxHead - n.relaxTail).sqrMagnitude < scanDistSq ||
                    (s.relaxTail - n.relaxHead).sqrMagnitude < scanDistSq ||
                    (s.relaxTail - n.relaxTail).sqrMagnitude < scanDistSq
                    )
                {
                    pairs.Add(s);
                    pairs.Add(n);
                }
            }

        }

        // separate each pair
        for (int r = 0; r < 10; r++)
            for (int i = 0; i < pairs.Count; i+=2)
                Separate2D(pairs[i], pairs[i+1], Sheep.separationDist);
    }
    private static void Separate2D(Sheep sheep, Sheep neighbor, float hardDist)
    {

        var npos = neighbor.relaxTail.To2D();
        var ndir = neighbor.relaxHead.To2D() * 1.5f - neighbor.relaxTail.To2D() * .5f - npos;
        var t1 = Vector2.Dot(sheep.relaxHead.To2D() * 1.5f - sheep.relaxTail.To2D() * .5f - npos, ndir) / ndir.sqrMagnitude;
        t1 = Mathf.Clamp01(t1);
        var projected = npos + ndir * t1;


        var offset1 = sheep.relaxHead.To2D() * 1.5f - sheep.relaxTail.To2D() * .5f - projected;


        var pushMag = hardDist - offset1.magnitude;
        if (pushMag > 0)
        {
            var push = offset1.normalized * pushMag * .2f;
            sheep.RelaxMoveHead((push / 2).To3D());
            neighbor.RelaxMoveFull((-push / 2).To3D());
        }


        var t2 = Vector2.Dot(sheep.relaxTail.To2D() - npos, ndir) / ndir.sqrMagnitude;
        t2 = Mathf.Clamp01(t2);
        projected = npos + ndir * t2;

        var offset2 = sheep.relaxTail.To2D() - projected;

        pushMag = hardDist - offset2.magnitude;
        if (pushMag > 0)
        {
            var push = offset2.normalized * pushMag * .2f;

            if (t1 > 0 && t1 < 1 && t2 > 0 && t2 < 1 && Vector3.Dot(offset1, offset2) < 0) // front and end on opposite sides of same neighbor 
                push = -push;

            sheep.RelaxMoveTail((push / 2).To3D());
            neighbor.RelaxMoveFull((-push / 2).To3D());
        }

    }
}
