namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Float")]
    public static class MathFloat
    {
        [NodeAPI("Absolute")]
        public static float Absolute(float a)
        {
            return UnityEngine.Mathf.Abs(a);
        }

        [NodeAPI("Add")]
        public static float Add(float a, float b)
        {
            return a + b;
        }

        [NodeAPI("Arccosine")]
        public static float Arccosine(float x)
        {
            return UnityEngine.Mathf.Acos(x);
        }

        [NodeAPI("Arcsine")]
        public static float Arcsine(float x)
        {
            return UnityEngine.Mathf.Asin(x);
        }

        [NodeAPI("Arctangent")]
        public static float Arctangent(float x)
        {
            return UnityEngine.Mathf.Atan(x);
        }

        [NodeAPI("Arctangent2 (y/x)")]
        public static float Arctangent2(float x, float y)
        {
            return UnityEngine.Mathf.Atan2(y, x);
        }

        [NodeAPI("Ceil")]
        public static float Ceil(float x)
        {
            return UnityEngine.Mathf.Ceil(x);
        }

        [NodeAPI("CeilToInt")]
        public static int CeilToInt(float x)
        {
            return UnityEngine.Mathf.CeilToInt(x);
        }

        [NodeAPI("Clamp")]
        public static float Clamp(float x, float min, float max)
        {
            return UnityEngine.Mathf.Clamp(x, min, max);
        }

        [NodeAPI("Cosine")]
        public static float Cosine(float f)
        {
            return UnityEngine.Mathf.Cos(f);
        }

        [NodeAPI("Divide (a / b)")]
        public static float Divide(float a, float b)
        {
            return a / b;
        }

        [NodeAPI("Floor")]
        public static float Floor(float x)
        {
            return UnityEngine.Mathf.Floor(x);
        }

        [NodeAPI("FloorToInt")]
        public static int FloorToInt(float x)
        {
            return UnityEngine.Mathf.FloorToInt(x);
        }

        [NodeAPI("Fmod (a % b)")]
        public static float Fmod(float a, float b)
        {
            return a % b;
        }

        [NodeAPI("Frac")]
        public static float Frac(float x)
        {
            return x - UnityEngine.Mathf.Floor(x);
        }

        [NodeAPI("Approximately")]
        public static bool Approximately(float a, float b)
        {
            return UnityEngine.Mathf.Approximately(a, b);
        }

        [NodeAPI("Greater (a > b)")]
        public static bool Greater(float a, float b)
        {
            return (a > b);
        }

        [NodeAPI("Greater or Equal (a >= b)")]
        public static bool GreaterOrEqual(float a, float b)
        {
            return (a >= b);
        }

        [NodeAPI("If (a OP b)")]
        public static float If(float a, float b, float equal, float greater, float less)
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
        public static bool Less(float a, float b)
        {
            return (a < b);
        }

        [NodeAPI("Less or Equal (a <= b)")]
        public static bool LessOrEqual(float a, float b)
        {
            return (a <= b);
        }

        [NodeAPI("LinearInterpolate")]
        public static float LinearInterpolate(float a, float b, float t)
        {
            return UnityEngine.Mathf.Lerp(a, b, t);
        }

        [NodeAPI("Logarithm10")]
        public static float Logarithm10(float x)
        {
            return UnityEngine.Mathf.Log10(x);
        }

        [NodeAPI("Logarithm2")]
        public static float Logarithm2(float x)
        {
            return UnityEngine.Mathf.Log(x, 2.0f);
        }

        [NodeAPI("LogarithmE")]
        public static float LogarithmE(float x)
        {
            return UnityEngine.Mathf.Log(x);
        }

        [NodeAPI("Max")]
        public static float Max(float a, float b)
        {
            return System.Math.Max(a, b);
        }

        [NodeAPI("Min")]
        public static float Min(float a, float b)
        {
            return System.Math.Min(a, b);
        }

        [NodeAPI("Multiply")]
        public static float Multiply(float a, float b)
        {
            return a * b;
        }

        [NodeAPI("OneMinus (1 - x)")]
        public static float OneMinus(float x)
        {
            return 1.0f - x;
        }

        [NodeAPI("Power (base ^ exp)")]
        public static float Power(float @base, float exp)
        {
            return UnityEngine.Mathf.Pow(@base, exp);
        }

        [NodeAPI("Round")]
        public static float Round(float x)
        {
            return UnityEngine.Mathf.Round(x);
        }

        [NodeAPI("Saturate")]
        public static float Saturate(float x)
        {
            return UnityEngine.Mathf.Clamp01(x);
        }

        [NodeAPI("Select")]
        public static float Select(float a, float b, bool pickA)
        {
            return (pickA ? a : b);
        }

        [NodeAPI("Sign")]
        public static float Sign(float x)
        {
            return UnityEngine.Mathf.Sign(x);
        }

        [NodeAPI("Sine")]
        public static float Sine(float x)
        {
            return UnityEngine.Mathf.Sin(x);
        }

        [NodeAPI("SquareRoot")]
        public static float SquareRoot(float x)
        {
            return UnityEngine.Mathf.Sqrt(x);
        }

        [NodeAPI("Substract (a - b)")]
        public static float Substract(float a, float b)
        {
            return a - b;
        }

        [NodeAPI("Tangent")]
        public static float Tangent(float x)
        {
            return UnityEngine.Mathf.Tan(x);
        }

        [NodeAPI("Truncate")]
        public static float Truncate(float x)
        {
            if (x > 0.0f)
            {
                return UnityEngine.Mathf.Floor(x);
            }
            else
            {
                return UnityEngine.Mathf.Ceil(x);
            }
        }
    }
}
