using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeInputInt : NodeInput<int>
    {
        public override bool CanConnect(NodeOutput output)
        {
            return output is NodeOutput<int>;
        }

#if UNITY_EDITOR
        string temp;
        int parsed;
        /// <summary>
        /// Used for graphical display of socket
        /// </summary>
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width = 32;
            if (connectedNode == null)
            {
                temp = GUI.TextField(labelRect, temp);
                int.TryParse(temp, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out parsed);
                temp = parsed + "";
                if (Application.isPlaying)
                    value = parsed;
                else
                {
                    initialValue = parsed;
                    UnityEditor.EditorUtility.SetDirty(node);
                }
                labelRect.x += 35;
                labelRect.width = localPos.width - 35;
                GUI.Label(labelRect, name);
            }
            else
            {
                GUI.Label(labelRect, name + ":" + value);
            }
        }
#endif
    }
}
