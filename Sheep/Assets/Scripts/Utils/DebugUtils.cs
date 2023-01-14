using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugUtils 
{

    // Draw a circular sector (pie piece) in 3D space.
    public static void DrawArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, Color color)
    {
        var count = 60;
        from.Normalize();
        Quaternion quaternion = Quaternion.AngleAxis(angle / (float)(count - 1), normal);
        Vector3 v = from * radius;
        for (int index = 0; index < count; ++index)
        {
            var old = v;
            v = quaternion * v;
            if(index%2==0 || index>count/2)
            Debug.DrawLine(center + old, center + v, color);
           
        }

    }

}
