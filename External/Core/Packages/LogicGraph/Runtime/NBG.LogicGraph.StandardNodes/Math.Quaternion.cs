using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Quaternion")]
    public static class MathQuaternion
    {
        [NodeAPI("Combine")]
        public static Quaternion Combine(Quaternion a, Quaternion b)
        {
            return a * b;
        }

        [NodeAPI("Components")]
        public static void Components(Quaternion value, out float x, out float y, out float z, out float w)
        {
            x = value.x;
            y = value.y;
            z = value.z;
            w = value.w;
        }

        [NodeAPI("Euler Angles")]
        public static void EulerAngles(Quaternion value, out float xRoll, out float yPitch, out float zYaw)
        {
            var angles = value.eulerAngles;
            xRoll = angles.x;
            yPitch = angles.y;
            zYaw = angles.z;
        }

        [NodeAPI("Euler Angles vector")]
        public static void EulerAnglesVector(Quaternion value, out Vector3 angles)
        {
            angles = value.eulerAngles;
        }

        [NodeAPI("Invert")]
        public static Quaternion Invert(Quaternion value)
        {
            return Quaternion.Inverse(value);
        }

        [NodeAPI("Lerp")]
        public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.Lerp(a, b, t);
        }

        [NodeAPI("Make from Euler Angles")]
        public static Quaternion MakeFromEulerAngles(float x, float y, float z)
        {
            return Quaternion.Euler(x, y, z);
        }

        [NodeAPI("Make from Euler Angles vector")]
        public static Quaternion MakeFromEulerAnglesVector(Vector3 value)
        {
            return Quaternion.Euler(value.x, value.y, value.z);
        }

        [NodeAPI("Slerp")]
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
        {
            return Quaternion.Slerp(a, b, t);
        }
    }
}
