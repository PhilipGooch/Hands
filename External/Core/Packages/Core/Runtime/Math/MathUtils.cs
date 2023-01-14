using UnityEngine;
using System.Text;

namespace NBG.Core
{
    public static class MathUtils
    {
        public static Vector3 WrapSigned(Vector3 value, Vector3 size)
        {
            return new Vector3(
                WrapSigned(value.x, size.x),
                WrapSigned(value.y, size.y),
                WrapSigned(value.z, size.z)
                );
        }
        public static float WrapSigned(float value, float size)
        {
            return value - Mathf.Floor(value / size + 0.5f) * size;
        }
        public static Vector3 Wrap(Vector3 value, Vector3 size)
        {
            return new Vector3(
                Wrap(value.x, size.x),
                Wrap(value.y, size.y),
                Wrap(value.z, size.z)
                );
        }
        public static float Wrap(float value, float size)
        {
            return value - Mathf.Floor(value / size) * size;
        }

        public static float LogMap(float value, float sourceLow, float sourceMid, float sourceHigh, float targetFrom, float targetCenter, float targetTo)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Pitch: " + value + "\n");
            var mid = (sourceMid - sourceLow) / (sourceHigh - sourceLow);
            sb.Append("Low/High Ratio: " + mid + "\n");
            var pow = Mathf.Log((targetCenter - targetFrom) / (targetTo - targetFrom), mid);
            sb.Append("Power: " + pow + "\n");
            var pitchFactor = Mathf.InverseLerp(sourceLow, sourceHigh, value);
            sb.Append("PitchFactor: " + pitchFactor + "\n");
            var valuePower = Mathf.Pow(pitchFactor, pow);
            sb.Append("LerpValue: " + valuePower + "\n");
            var ret = Mathf.Lerp(targetFrom, targetTo, valuePower);
            sb.Append("Result: " + ret);
            //DebugUI.QuickPrint = sb.ToString();
            return ret;
        }
        public static float MoveTowards(float from, float to, float speedUp, float speedDown)
        {
            return Mathf.MoveTowards(from, to, to > from ? speedUp : speedDown);
        }
    }
}
