using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeOutputBool : NodeOutput<bool>
    {
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width -= 32;
            var v = Application.isPlaying ? value : initialValue;
            GUI.Label(labelRect, name + ":" + v.ToString());
        }
    }
}
