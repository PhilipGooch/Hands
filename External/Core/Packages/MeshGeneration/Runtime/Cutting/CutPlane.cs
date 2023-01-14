using Unity.Mathematics;

namespace NBG.MeshGeneration
{
    public struct CutPlane
    {
        public float3 position, normal, positivePosition, negativePosition;
        public float width;

        public enum Side
        {
            Positive,
            Negative,
            InsideCut
        }

        public CutPlane(float3 pos, float3 nor, float width = 0.0f)
        {
            position = pos;
            normal = math.normalize(nor);
            this.width = width;
            positivePosition = 0;
            negativePosition = 0;

            positivePosition = CalculateCutPositionWithWidth(Side.Positive);
            negativePosition = CalculateCutPositionWithWidth(Side.Negative);
        }
        public bool Sign(float3 pos)
        {
            return math.dot(pos - position, normal) > 0;
        }

        private float3 CalculateCutPositionWithWidth(Side side)
        {
            return position + (side == Side.Positive ? 1f : -1f) * normal * width * 0.5f;
        }

        public Side CalculateSide(float3 pos)
        {
            if (math.dot(pos - positivePosition, normal) > 0)
                return Side.Positive;
            else if (math.dot(pos - negativePosition, normal) <= 0)
                return Side.Negative;
            else
                return Side.InsideCut;
        }

        public bool IntersectionPoint(in Side side, out float3 output, out float3 outNormal, in float3 p1, in float3 p2, in float3 n1, in float3 n2)
        {
            float3 PoRo = (side == Side.Positive ? positivePosition : negativePosition) - p1;
            float3 transformedNormal = side == Side.Positive ? normal : -normal;
            float distanceBetweenPoints = math.distance(p1, p2);
            float3 rayDir = (p2 - p1) / distanceBetweenPoints;
            float distanceToPlane = math.abs(math.dot(PoRo, transformedNormal));
            float incrementInPlaneNormalDirection = math.abs(math.dot(rayDir, transformedNormal));
            float increment = (distanceToPlane / incrementInPlaneNormalDirection);
            float3 targetPosition = increment * rayDir + p1;

            outNormal = math.lerp(n1, n2, increment / distanceBetweenPoints);
            output = targetPosition;
            return (incrementInPlaneNormalDirection != 0.0f);
        }

        public bool LinePlaneIntersectionPoints(out float3 intersection, float3 linePoint, float3 p2)
        {
            return LinePlaneIntersection(out intersection, linePoint, (p2 - linePoint));
        }

        public bool LinePlaneIntersection(out float3 intersection, float3 linePoint, float3 lineVec)
        {

            float length;
            float dotNumerator;
            float dotDenominator;
            float3 vector;
            intersection = float3.zero;

            //calculate the distance between the linePoint and the line-plane intersection point
            dotNumerator = math.dot((position - linePoint), normal);
            dotDenominator = math.dot(lineVec, normal);

            //line and plane are not parallel
            if (dotDenominator != 0.0f)
            {
                length = dotNumerator / dotDenominator;

                //create a vector from the linePoint to the intersection point
                vector = math.normalize(lineVec) * length;

                //get the coordinates of the line-plane intersection point
                intersection = linePoint + vector;

                return true;
            }

            //output not valid
            else
            {
                return false;
            }
        }
    }
}
