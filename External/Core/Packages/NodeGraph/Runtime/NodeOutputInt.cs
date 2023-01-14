using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeOutputInt : NodeOutput<int>
    {
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width -= 32;
            int v = Application.isPlaying ? value : initialValue;
            GUI.Label(labelRect, name + ":" + v.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        //public static implicit operator NodeOutputFloat(NodeOutputInt rhs)
        //{
        //    var result = new NodeOutputFloat();
        //    result.name = rhs.name;
        //    result.node = rhs.node;
        //    result.initialValue = rhs.initialValue;
        //    result.value = rhs.value;
        //    result.onValueChanged = rhs.onValueChanged;
        //    return result;
        //}
    }
}
