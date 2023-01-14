using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Boolean")]
    public static class UtilityBoolean
    {
        [NodeAPI("Invert")]
        public static bool Invert(bool a)
        {
            return !a;
        }

        [NodeAPI("AND")]
        public static bool AND(bool a, bool b)
        {
            return (a && b);
        }

        [NodeAPI("Equal")]
        public static bool Equal(bool a, bool b)
        {
            return (a == b);
        }

        [NodeAPI("NAND")]
        public static bool NAND(bool a, bool b)
        {
            return !(a && b);
        }

        [NodeAPI("NOR")]
        public static bool NOR(bool a, bool b)
        {
            return !(a || b);
        }

        [NodeAPI("NOT")]
        public static bool NOT(bool a)
        {
            return !a;
        }

        [NodeAPI("NotEqual")]
        public static bool NotEqual(bool a, bool b)
        {
            return (a != b);
        }

        [NodeAPI("OR")]
        public static bool OR(bool a, bool b)
        {
            return (a || b);
        }

        [NodeAPI("XOR")]
        public static bool XOR(bool a, bool b)
        {
            return (a ^ b);
        }
    }
}
