using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeConceptualType(NodeConceptualType.TypeConverter)]
    [NodeCategoryPath("Type Conversions")]
    public static class UtilityConversions
    {
        [NodeAPI("Float to Vector(x,y,z)")]
        public static Vector3 FloatToVector(float x)
        {
            return new Vector3(x, x, x);
        }

        [NodeAPI("Float to int")]
        public static int FloatToInt(float x)
        {
            return (int)x;
        }

        [NodeAPI("Int to float")]
        public static float IntToFloat(int x)
        {
            return (float)x;
        }

        [NodeAPI("Bool to float")]
        public static float BoolToFloat(bool value, float whenTrue, float whenFalse)
        {
            return value ? whenTrue : whenFalse;
        }

        [NodeAPI("Bool to int")]
        public static int BoolToInt(bool value, int whenTrue, int whenFalse)
        {
            return value ? whenTrue : whenFalse;
        }
    }
}
