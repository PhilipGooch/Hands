using UnityEngine;

namespace NBG.Core
{
    public static class Math2d
    {
        public static float SignedAngle(Vector2 from, Vector2 to)
        {
            //Now calculate the dot product between the perpendicular vector (perpVector) and the other input vector
            var angle = Vector2.Angle(from, to);

            Vector3 cross = Vector3.Cross(from, to);
            if (cross.z < 0)
                angle = -angle;


            return angle;
        }
        // limits angle to +-PI
        public static float NormalizeAngle(float angle)
        {
            while (angle < -Mathf.PI)
                angle += 2 * Mathf.PI;
            while (angle > Mathf.PI)
                angle -= 2 * Mathf.PI;
            return angle;

        }
        public static float NormalizeAngleDeg(float angle)
        {
            while (angle < -180)
                angle += 360;
            while (angle > 180)
                angle -= 360;
            return angle;

        }

        public static float NormalizeAnglePositive(float angle)
        {
            while (angle > 2 * Mathf.PI)
                angle -= 2 * Mathf.PI;
            while (angle < 0)
                angle += 2 * Mathf.PI;
            return angle;

        }
        public static float NormalizeAngleDegPositive(float angle)
        {
            //while (angle > 2 * Mathf.PI)
            //    angle -= 2 * Mathf.PI;
            while (angle < 0)
                angle += 360;
            return angle % 360;

        }
        public static float CalculateDistToBisector(Vector2 edgeVector, Vector2 normal, Vector2 startBisector, Vector2 endBisector)
        {
            // calculate bisector intersection
            var delta = (startBisector.x * endBisector.y - endBisector.x * startBisector.y);
            if (delta != 0)
            {
                // calculate time
                var tLeft = (edgeVector.x * endBisector.y - endBisector.x * edgeVector.y) / delta;
                // multiply by b1 projected to normal (dot)
                return (startBisector.x * normal.x + startBisector.y * normal.y) * tLeft;
            }
            else
                return -10000; // pretend points move a bit apart
        }
        public static float CalculateTimeToBisector(Vector2 edgeVector, Vector2 normal, Vector2 startBisector, Vector2 endBisector)
        {
            // handle "vertical" bisectors
            if (startBisector == Vector2.zero)
            {
                if (endBisector == Vector2.zero)
                    return -10000; // parallel
                else
                    return Vector2.Dot(endBisector, -edgeVector) / endBisector.sqrMagnitude; // calculate time needed to cross the edge
            }
            else if (endBisector == Vector2.zero)
            {
                return Vector2.Dot(startBisector, edgeVector) / startBisector.sqrMagnitude; // calculate time needed to cross the edge
            }


            // calculate bisector intersection
            var delta = (startBisector.x * endBisector.y - endBisector.x * startBisector.y);
            if (delta != 0)
            {
                // calculate time
                var tLeft = (edgeVector.x * endBisector.y - endBisector.x * edgeVector.y) / delta;
                return tLeft;
                //// multiply by b1 projected to normal (dot)
                //return (startBisector.x * normal.x + startBisector.y * normal.y) * tLeft;
            }
            else
                return -10000; // pretend points move a bit apart
        }
        // calculate angle bisector for unit normal vectors
        public static Vector2 CalculateUnitBisector(Vector2 a, Vector2 b)
        {
            var sum = a + b;
            var dot = (sum.x * b.x + sum.y * b.y);
            //// store
            //if (Mathf.Abs( dot)<0.0000001f)
            //    return sum / dot;
            //else // opposing normals, assume inner angle of 0 degrees
            //{
            //    Debug.Log("Opposing normals"); // very high speed
            //    return a.RotateCW90()*100000000;
            //}

            if (dot != 0)
                return sum / dot;
            else // opposing normals, assume inner angle of 0 degrees
            {
                //Debug.Log("Opposing normals");
                return a.RotateCW90();
            }

        }

        // calculate angle bisector with non unit normals
        // distance from corner to intersections of edge offsets
        public static Vector2 CalculateOffsetBisector(Vector2 a, Vector2 b)
        {
            var cross = a.x * b.y - b.x * a.y;
            if (cross != 0)
            {
                var ab = a - b;
                var dot2 = ab.x * b.x + ab.y * b.y;
                return a + dot2 / cross * a.RotateCW90();
            }
            else
                return a.RotateCW90();
        }

        public static bool LineLineIntersection(out Vector2 intersection, Vector2 linePoint1, Vector2 lineVec1, Vector2 linePoint2, Vector2 lineVec2, float parallel_cross_epsilon = 0.000001f)
        {
            if (linePoint1 == linePoint2)
            {
                intersection = linePoint1;
                return true;
            }
            var A1 = lineVec1.y;
            var B1 = -lineVec1.x;
            var C1 = A1 * linePoint1.x + B1 * linePoint1.y;

            var A2 = lineVec2.y;
            var B2 = -lineVec2.x;
            var C2 = A2 * linePoint2.x + B2 * linePoint2.y;

            float delta = A1 * B2 - A2 * B1;
            if (Mathf.Abs(delta) < parallel_cross_epsilon)
            {
                intersection = Vector3.zero;
                return false;
            }
            else
            {
                intersection = new Vector2(
                    (B2 * C1 - B1 * C2) / delta,
                    (A1 * C2 - A2 * C1) / delta
                );
                return true;
            }
        }
        //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
        //public static bool LineLineIntersection(out Vector2 intersection, Vector2 linePoint1, Vector2 lineVec1, Vector2 linePoint2, Vector2 lineVec2)
        //{
        //    if (linePoint1 == linePoint2)
        //    {
        //        intersection = linePoint1;
        //        return true;
        //    }
        //    var A1 = lineVec1.y;
        //    var B1 = -lineVec1.x;
        //    var C1 = A1 * linePoint1.x + B1 * linePoint1.y;

        //    var A2 = lineVec2.y;
        //    var B2 = -lineVec2.x;
        //    var C2 = A2 * linePoint2.x + B2 * linePoint2.y;

        //    float delta = A1 * B2 - A2 * B1;
        //    if (Mathf.Abs(delta)<0.000001f)
        //    {
        //        intersection = Vector3.zero;
        //        return false;
        //    }
        //    else
        //    {
        //        intersection = new Vector2(
        //            (B2 * C1 - B1 * C2) / delta,
        //            (A1 * C2 - A2 * C1) / delta
        //        );
        //        return true;
        //    }
        //}
        public static bool LineLineIntersection(out Vector2 intersection, Vector2 n1, float C1, Vector2 n2, float C2)
        {
            var A1 = n1.x;
            var B1 = n1.y;
            var A2 = n2.x;
            var B2 = n2.y;
            float delta = A1 * B2 - A2 * B1;
            if (Mathf.Abs(delta) < 0.000001f)
            {
                intersection = Vector3.zero;
                return false;
            }
            else
            {
                intersection = new Vector2(
                    (B2 * C1 - B1 * C2) / delta,
                    (A1 * C2 - A2 * C1) / delta
                );
                return true;
            }

        }

        public static float Cross(Vector2 v1, Vector2 v2)
        {
            // equivalent to cross(v1,v2).z in 3d;
            return v1.x * v2.y - v1.y * v2.x;
        }
        //Calculate the intersection point of two lines. Returns true if lines intersect, otherwise false.
        public static bool LineLineIntersection(out float t1, out float t2, Vector2 p1, Vector2 v1, Vector2 p2, Vector2 v2)
        {
            var p12 = p2 - p1;

            //var delta = v2.x*v1.y-v2.y*v1.x;
            //if (delta == 0)
            //{
            //    // alternative: calculate time to meet at line perpendicular to both lines
            //    var v = (v1 - v2).magnitude;
            //    var t = v != 0 ? p12.magnitude / v : 0;
            //    t1 = t2 = t;
            //    return false;
            //}
            //else
            //{
            //    t1 = (p12.y * v2.x - p12.x * v2.y) / delta;
            //    t2 = (p12.y * v1.x - p12.x * v1.y) / delta;
            //    return true;
            //}

            var delta = v1.x * v2.y - v2.x * v1.y;
            if (delta == 0)
            {
                // alternative: calculate time to meet at line perpendicular to both lines
                var v = (v1 - v2).magnitude;
                var t = v != 0 ? p12.magnitude / v : 0;
                t1 = t2 = t;
                return false;
            }
            else
            {
                t1 = (p12.x * v2.y - v2.x * p12.y) / delta;
                t2 = (p12.x * v1.y - v1.x * p12.y) / delta;
                return true;
            }

        }

        //This function finds out on which side of a line segment the point is located.
        //The point is assumed to be on a line created by linePoint1 and linePoint2. If the point is not on
        //the line segment, project it on the line using ProjectPointOnLine() first.
        //Returns 0 if point is on the line segment.
        //Returns 1 if point is outside of the line segment and located on the side of linePoint1.
        //Returns 2 if point is outside of the line segment and located on the side of linePoint2.
        public static int PointOnWhichSideOfLineSegment(Vector2 linePoint1, Vector2 linePoint2, Vector2 point)
        {

            Vector2 lineVec = linePoint2 - linePoint1;
            Vector2 pointVec = point - linePoint1;

            float dot = Vector2.Dot(pointVec, lineVec);

            //point is on side of linePoint2, compared to linePoint1
            if (dot > 0)
            {

                //point is on the line segment
                if (pointVec.sqrMagnitude <= lineVec.sqrMagnitude)
                {

                    return 0;
                }

                //point is not on the line segment and it is on the side of linePoint2
                else
                {

                    return 2;
                }
            }

            //Point is not on side of linePoint2, compared to linePoint1.
            //Point is not on the line segment and it is on the side of linePoint1.
            else
            {

                return 1;
            }

        }
        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        // also returns the parameter
        public static Vector2 ProjectPointOnLine(Vector2 linePoint, Vector2 lineVec, Vector2 point, out float t)
        {

            //get vector from point on line to point in space
            Vector2 linePointToPoint = point - linePoint;

            t = Vector2.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec * t;
        }

        //This function returns a point which is a projection from a point to a line.
        //The line is regarded infinite. If the line is finite, use ProjectPointOnLineSegment() instead.
        public static Vector2 ProjectPointOnLine(Vector2 linePoint, Vector2 lineVec, Vector2 point)
        {

            //get vector from point on line to point in space
            Vector2 linePointToPoint = point - linePoint;

            float t = Vector2.Dot(linePointToPoint, lineVec);

            return linePoint + lineVec * t;
        }

        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will 
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        public static Vector2 ProjectPointOnLineSegment(Vector2 linePoint1, Vector2 linePoint2, Vector2 point)
        {

            Vector2 vector = linePoint2 - linePoint1;

            Vector2 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

            int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

            //The projected point is on the line segment
            if (side == 0)
            {

                return projectedPoint;
            }

            if (side == 1)
            {

                return linePoint1;
            }

            if (side == 2)
            {

                return linePoint2;
            }

            //output is invalid
            return Vector2.zero;
        }

        //This function returns a point which is a projection from a point to a line segment.
        //If the projected point lies outside of the line segment, the projected point will 
        //be clamped to the appropriate line edge.
        //If the line is infinite instead of a segment, use ProjectPointOnLine() instead.
        // also returns parameter
        public static Vector2 ProjectPointOnLineSegment(Vector2 linePoint1, Vector2 linePoint2, Vector2 point, out float t)
        {

            Vector2 vector = linePoint2 - linePoint1;

            Vector2 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point, out t);

            t /= vector.magnitude;
            //int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

            //The projected point is on the line segment
            if (t < 0)
            {
                //t = 0;
                return linePoint1;
            }
            if (t > 1)
            {
                //t = 1;
                return linePoint2;
            }
            return projectedPoint;

        }

        public static float PointDistanceToSegment(Vector2 linePoint1, Vector2 linePoint2, Vector2 point)
        {

            var projectedPoint = ProjectPointOnLineSegment(linePoint1, linePoint2, point);

            Vector3 vector = projectedPoint - point;
            return vector.magnitude;
        }

        public static float LerpAngle(float from, float to, float t)
        {
            var diff = NormalizeAngle(to - from);
            return NormalizeAngle(Mathf.Lerp(from, from + diff, t));
        }

        // Cosine law
        public static float GetAngle(float a, float b, float c)
        {
            var cos = (a * a + b * b - c * c) / (2 * a * b);
            //Debug.LogFormat("{0} {1} {2} {3} ",  cos, a,b,c);
            if (cos > .99f) return 0;
            if (cos < -.99f) return 180;
            return Mathf.Acos(cos) * Mathf.Rad2Deg;
        }

    }
}