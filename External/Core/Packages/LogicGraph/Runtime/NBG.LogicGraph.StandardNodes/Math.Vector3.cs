using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Vector3")]
    public static class MathVector3
    {
        [NodeAPI("Add float")]
        public static Vector3 AddFloat(Vector3 vector, float value)
        {
            vector.x += value;
            vector.y += value;
            vector.z += value;
            return vector;
        }

        [NodeAPI("Add int")]
        public static Vector3 AddInt(Vector3 vector, int value)
        {
            vector.x += value;
            vector.y += value;
            vector.z += value;
            return vector;
        }

        [NodeAPI("Components")]
        public static void Components(Vector3 value, out float x, out float y, out float z)
        {
            x = value.x;
            y = value.y;
            z = value.z;
        }

        [NodeAPI("Clamp magnitude")]
        public static Vector3 ClampMagnitude(Vector3 value, float max)
        {
            return Vector3.ClampMagnitude(value, max);
        }

        [NodeAPI("Cross")]
        public static Vector3 CrossProduct(Vector3 a, Vector3 b)
        {
            return Vector3.Cross(a, b);
        }

        [NodeAPI("Distance")]
        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        [NodeAPI("Divide by float")]
        public static Vector3 DivideFloat(Vector3 vector, float value)
        {
            vector.x /= value;
            vector.y /= value;
            vector.z /= value;
            return vector;
        }

        [NodeAPI("Divide by int")]
        public static Vector3 DivideInt(Vector3 vector, int value)
        {
            vector.x /= value;
            vector.y /= value;
            vector.z /= value;
            return vector;
        }

        [NodeAPI("Dot")]
        public static float DotProduct(Vector3 a, Vector3 b)
        {
            return Vector3.Dot(a, b);
        }

        [NodeAPI("Equal")]
        public static bool Equal(Vector3 a, Vector3 b, float errorTolerance)
        {

            if (System.Math.Abs(a.x - b.x) > errorTolerance)
                return false;
            if (System.Math.Abs(a.y - b.y) > errorTolerance)
                return false;
            if (System.Math.Abs(a.z - b.z) > errorTolerance)
                return false;
            return true;
        }

        [NodeAPI("EqualExactly")]
        public static bool EqualExactly(Vector3 a, Vector3 b, float errorTolerance)
        {
            return a.Equals(b);
        }

        [NodeAPI("Length")]
        public static float Length(Vector3 a)
        {
            return a.magnitude;
        }

        [NodeAPI("Length Squared")]
        public static float LengthSquared(Vector3 a)
        {
            return a.sqrMagnitude;
        }

        [NodeAPI("Lerp")]
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }

        [NodeAPI("Make")]
        public static Vector3 Make(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        [NodeAPI("Multiply by float")]
        public static Vector3 MultiplyFloat(Vector3 vector, float value)
        {
            vector.x *= value;
            vector.y *= value;
            vector.z *= value;
            return vector;
        }

        [NodeAPI("Multiply by int")]
        public static Vector3 MultiplyInt(Vector3 vector, int value)
        {
            vector.x *= value;
            vector.y *= value;
            vector.z *= value;
            return vector;
        }

        [NodeAPI("Normalize")]
        public static Vector3 Normalize(Vector3 value)
        {
            return Vector3.Normalize(value);
        }

        [NodeAPI("Normalize")]
        public static Vector3 Negate(Vector3 value)
        {
            return -value;
        }

        [NodeAPI("Reflect")]
        public static Vector3 Reflect(Vector3 value, Vector3 normal)
        {
            return Vector3.Reflect(value, normal);
        }

        [NodeAPI("Substract float")]
        public static Vector3 SubstractFloat(Vector3 vector, float value)
        {
            vector.x -= value;
            vector.y -= value;
            vector.z -= value;
            return vector;
        }

        [NodeAPI("Substract int")]
        public static Vector3 SubstractInt(Vector3 vector, int value)
        {
            vector.x -= value;
            vector.y -= value;
            vector.z -= value;
            return vector;
        }
    }
}
