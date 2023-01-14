namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Int")]
    public static class MathInt
    {
        [NodeAPI("Absolute")]
        public static int Absolute(int a)
        {
            return System.Math.Abs(a);
        }

        [NodeAPI("Add")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [NodeAPI("Clamp")]
        public static int Clamp(int x, int min, int max)
        {
            return UnityEngine.Mathf.Clamp(x, min, max);
        }

        [NodeAPI("Divide (a / b)")]
        public static int Divide(int a, int b)
        {
            return a / b;
        }

        [NodeAPI("Equal")]
        public static bool Equal(int a, int b)
        {
            return a == b;
        }

        [NodeAPI("Greater (a > b)")]
        public static bool Greater(int a, int b)
        {
            return (a > b);
        }

        [NodeAPI("Greater or Equal (a >= b)")]
        public static bool GreaterOrEqual(int a, int b)
        {
            return (a >= b);
        }

        [NodeAPI("If (a OP b)")]
        public static int If(int a, int b, int equal, int greater, int less)
        {
            if (a > b)
            {
                return greater;
            }
            else if (a == b)
            {
                return equal;
            }
            else
            {
                return less;
            }
        }

        [NodeAPI("Less (a < b)")]
        public static bool Less(int a, int b)
        {
            return (a < b);
        }

        [NodeAPI("Less or Equal (a <= b)")]
        public static bool LessOrEqual(int a, int b)
        {
            return (a <= b);
        }

        [NodeAPI("Max")]
        public static int Max(int a, int b)
        {
            return System.Math.Max(a, b);
        }

        [NodeAPI("Min")]
        public static int Min(int a, int b)
        {
            return System.Math.Min(a, b);
        }

        [NodeAPI("Multiply)")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [NodeAPI("Not equal")]
        public static bool NotEqual(int a, int b)
        {
            return a != b;
        }

        [NodeAPI("Select")]
        public static int Select(int a, int b, bool pickA)
        {
            return (pickA ? a : b);
        }

        [NodeAPI("Sign")]
        public static int Sign(int x)
        {
            return System.Math.Sign(x);
        }

        [NodeAPI("Substract (a - b)")]
        public static int Substract(int a, int b)
        {
            return a - b;
        }

    }
}
