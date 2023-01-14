using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeOutputFloat : NodeOutput<float>
    {
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width -= 32;
            float v = Application.isPlaying ? value : initialValue;
            GUI.Label(labelRect, name + ":" + v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
