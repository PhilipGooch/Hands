using UnityEngine;

namespace NBG.LogicGraph.StandardNodes
{
    [NodeCategoryPath("Type Operations/Color")]
    public static class UtilityColor
    {
        [NodeAPI("RGB to HSV")]
        public static Vector3 RGB2HSV(Color rgb)
        {
            float H, S, V;
            Color.RGBToHSV(rgb, out H, out S, out V);
            return new Vector3(H, S, V);
        }

        [NodeAPI("HSV to RGB")]
        public static Color HSV2RGB(Vector3 hsv, bool hdr)
        {
            return Color.HSVToRGB(hsv.x, hsv.y, hsv.z, hdr);
        }

        [NodeAPI("Lerp")]
        public static Color Lerp(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }

        [NodeAPI("LerpUnclamped")]
        public static Color LerpUnclamped(Color a, Color b, float t)
        {
            return Color.LerpUnclamped(a, b, t);
        }
    }
}
