using UnityEngine;
using System.Collections;

public static class SheepMath2D
{
    public static bool SegmentSegmentIntersection(out Vector2 i1, out Vector2 i2, Vector2 p1, Vector2 v1, Vector2 p2, Vector2 v2)
    {
        var res = SegmentSegmentIntersection(out float t1, out float t2, p1, v1, p2, v2);
        i1 = p1 + v1 * t1;
        i2 = p2 + v2 * t2;
        return res;
    }
    //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
    public static bool SegmentSegmentIntersection(out float t1, out float t2, Vector2 p1, Vector2 v1, Vector2 p2, Vector2 v2)
    {
        var p12 = p2 - p1;

        var delta = v1.x * v2.y - v2.x * v1.y;
        if (delta > -0.000001f && delta < 0.000001f) // paralel
        {

            var tp2 = Vector2.Dot(v1, p2 - p1) / v1.sqrMagnitude;
            var tv2 = Vector2.Dot(v1, p2 + v2 - p1) / v1.sqrMagnitude;

            if (tp2 <= 0) // start before beginning of segment 1
            {
                if (tv2 <= 0) // end before beginning of segment 1
                {
                    t1 = 0; // s1 start
                    if (tp2 < tv2)
                        t2 = 1; // s2 end
                    else
                        t2 = 0; // s2 start
                }
                else if (tv2 >= 1) // end after end of segment 1
                {
                    t1 = .5f; // s1 middle
                    t2 = (t1 - tp2) / (tv2 - tp2);// s1 point projected to s2
                }
                else // end inside segment 1
                {
                    t1 = tv2 / 2;
                    t2 = (t1 - tp2) / (tv2 - tp2);// s1 point projected to s2
                }
            }
            else if (tp2 >= 1)  // start after end of segment 1
            {
                if (tv2 >= 1) // end after end of segment 1
                {
                    t1 = 1;
                    if (tp2 < tv2)
                        t2 = 0;
                    else
                        t2 = 1;
                }
                else if (tv2 >= 1) // start before s1
                {
                    t1 = .5f; // s1 middle
                    t2 = (t1 - tp2) / (tv2 - tp2);// s1 point projected to s2
                }
                else // start inside
                {
                    t1 = .5f + tv2 / 2; // midpoint between end and 1
                    t2 = (t1 - tp2) / (tv2 - tp2);// s1 point projected to s2
                }
            }
            else // start inside s1
            {
                if (tv2 <= 0) // end before segment
                    t1 = tp2 / 2; // midpoint between 0 and tp2
                else if (tv2 >= 1)
                    t1 = .5f + tp2 / 2; // midpoint between 1 and tp2
                else
                    t1 = (tp2 + tv2) / 2;// midpoint between tp2 and tv2
                t2 = (t1 - tp2) / (tv2 - tp2);// s1 point projected to s2
            }
            return false;// parralel don't intersect
        }
        else
        {
            t1 = (p12.x * v2.y - v2.x * p12.y) / delta;
            t2 = (p12.x * v1.y - v1.x * p12.y) / delta;

            // intersect
            if (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1)
                return true;

            // do not intersect - many possible cases

            var dot = v1.x * v2.x + v1.y * v2.y;
            if (dot >= -0.000001f && dot <= 0.000001f) // perpendicular, just clamp t
            {
                if (t1 < 0) t1 = 0;
                else if (t1 > 1) t1 = 1;
                if (t2 < 0) t2 = 0;
                else if (t2 > 1) t2 = 1;
            }
            else if (dot > 0) // going same direction
            {
                var tp1 = Vector2.Dot(v2, p1 - p2) / v2.sqrMagnitude;
                var tv1 = Vector2.Dot(v2, p1 + v1 - p2) / v2.sqrMagnitude;
                var tp2 = Vector2.Dot(v1, p2 - p1) / v1.sqrMagnitude;
                var tv2 = Vector2.Dot(v1, p2 + v2 - p1) / v1.sqrMagnitude;

                if (t1 <= 0) // intesection before segment1
                {
                    if (tp2 <= 0) // start of seg2 projects before seg1
                    {
                        t1 = 0;
                        t2 = Mathf.Clamp01(tp1);
                    }
                    else
                    {
                        t2 = 0;
                        t1 = Mathf.Clamp01(tp2);
                    }
                }
                else if (t1 >= 1) // intersection after segment 1
                {
                    if (tv2 >= 1) // end 2 projects after seg 1
                    {
                        t1 = 1;
                        t2 = Mathf.Clamp01(tv1);
                    }
                    else
                    {
                        t2 = 1;
                        t1 = Mathf.Clamp01(tv2);
                    }
                }
                else // intersection at segment 1
                {
                    if (t2 <= 0) // but before seg 2
                    {
                        t2 = 0;
                        t1 = Mathf.Clamp01(tp2);
                    }
                    else // after seg2
                    {
                        t2 = 1;
                        t1 = Mathf.Clamp01(tv2);
                    }
                }
            }
            else // going opposite directions
            {
                var tp1 = Vector2.Dot(v2, p1 - p2) / v2.sqrMagnitude;
                var tv1 = Vector2.Dot(v2, p1 + v1 - p2) / v2.sqrMagnitude;
                var tp2 = Vector2.Dot(v1, p2 - p1) / v1.sqrMagnitude;
                var tv2 = Vector2.Dot(v1, p2 + v2 - p1) / v1.sqrMagnitude;

                if (t1 <= 0) // intesection before segment1
                {
                    if (tv2 <= 0) // end of seg2 projects before seg1
                    {
                        t1 = 0;
                        t2 = Mathf.Clamp01(tp1);
                    }
                    else
                    {
                        t2 = 1;
                        t1 = Mathf.Clamp01(tv2);
                    }
                }
                else if (t1 >= 1) // intersection after segment 1
                {
                    if (tp2 >= 1) // start 2 projects after seg 1
                    {
                        t1 = 1;
                        t2 = Mathf.Clamp01(tv1);
                    }
                    else
                    {
                        t2 = 0;
                        t1 = Mathf.Clamp01(tp2);
                    }
                }
                else // intersection at segment 1
                {
                    if (t2 <= 0) // but before seg 2
                    {
                        t2 = 0;
                        t1 = Mathf.Clamp01(tp2);
                    }
                    else // after seg2
                    {
                        t2 = 1;
                        t1 = Mathf.Clamp01(tv2);
                    }
                }

            }
            return false;
        }

    }

}


