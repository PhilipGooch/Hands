using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Recoil
{
    public static partial class re
    {
        //Get the intersection between a line and a plane. 
        //If the line and plane are not parallel, the function outputs true, otherwise false.
        public static bool LinePlaneIntersection(out float3 intersection, float3 linePoint, float3 lineVec, float3 planeNormal, float3 planePoint)
        {

            float length;
            float dotNumerator;
            float dotDenominator;
            //float3 vector;
            intersection = float3.zero;

            //calculate the distance between the linePoint and the line-plane intersection point
            dotNumerator = math.dot((planePoint - linePoint), planeNormal);
            dotDenominator = math.dot(lineVec, planeNormal);

            //line and plane are not parallel
            if (dotDenominator != 0.0f)
            {
                length = dotNumerator / dotDenominator;
                intersection = linePoint + math.normalize(lineVec) * length;
                return true;
            }

            //output not valid
            else
            {
                return false;
            }
        }

        //https://arrowinmyknee.com/2021/03/15/some-math-about-capsule-collision/
        // Computes closest points C1 and C2 of S1(s)=P1+s*(Q1-P1) and
        // S2(t)=P2+t*(Q2-P2), returning s and t. Function result is squared
        // distance between between S1(s) and S2(t)
        public static void ClosestPtSegmentSegment(float3 p1, float3 q1, float3 p2, float3 q2,
            out float s, out float t, out float3 c1, out float3 c2)
        {
            var d1 = q1 - p1; // Direction vector of segment S1
            var d2 = q2 - p2; // Direction vector of segment S2
            var r = p1 - p2;
            float a = math.dot(d1, d1); // Squared length of segment S1, always nonnegative
            float e = math.dot(d2, d2); // Squared length of segment S2, always nonnegative
            float f = math.dot(d2, r);
            // Check if either or both segments degenerate into points
            if (a <= FLT_EPSILON && e <= FLT_EPSILON)
            {
                // Both segments degenerate into points
                s = t = 0.0f;
                c1 = p1;
                c2 = p2;
                return;
            }
            if (a <= FLT_EPSILON)
            {
                // First segment degenerates into a point
                s = 0.0f;
                t = f / e; // s = 0 => t = (b*s + f) / e = f / e
                t = math.saturate(t);
            }
            else
            {
                float c = math.dot(d1, r);
                if (e <= FLT_EPSILON)
                {
                    // Second segment degenerates into a point
                    t = 0.0f;
                    s = math.saturate(-c / a); // t = 0 => s = (b*t - c) / a = -c / a
                }
                else
                {
                    // The general nondegenerate case starts here
                    float b = math.dot(d1, d2);
                    float denom = a * e - b * b; // Always nonnegative
                                                 // If segments not parallel, compute closest point on L1 to L2 and
                                                 // clamp to segment S1. Else pick arbitrary s (here 0)
                    if (denom != 0.0f)
                    {
                        s = math.saturate((b * f - c * e) / denom);
                    }
                    else s = 0.0f;
                    // Compute point on L2 closest to S1(s) using
                    // t = Dot((P1 + D1*s) - P2,D2) / Dot(D2,D2) = (b*s + f) / e
                    t = (b * s + f) / e;
                    // If t in [0,1] done. Else clamp t, recompute s for the new value
                    // of t using s = Dot((P2 + D2*t) - P1,D1) / Dot(D1,D1)= (t*b - c) / a
                    // and clamp s to [0, 1]
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = math.saturate(-c / a);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = math.saturate((b - c) / a);
                    }
                }
            }
            c1 = p1 + d1 * s;
            c2 = p2 + d2 * t;
        }
    }
}

