using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeInputBool : NodeInput<bool>
    {
        public override bool CanConnect(NodeOutput output)
        {
            return output is NodeOutput<bool>;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Used for graphical display of socket.
        /// </summary>
        public override void Render(Rect localPos)
        {
            var labelRect = localPos;
            labelRect.x += 16;
            labelRect.width -= 32;

            if (connectedNode == null)
            {
                if (Application.isPlaying)
                {
                    var v = GUI.Toggle(labelRect, value, name);
                    ConnectedOutput_onValueChanged(v);
                }
                else
                {
                    var value = GUI.Toggle(labelRect, initialValue, name);
                    if (value != initialValue)
                    {
                        initialValue = value;
                        UnityEditor.EditorUtility.SetDirty(node);
                    }
                }
            }
            else
            {
                GUI.Label(labelRect, name + ":" + value);
            }
        }
#endif
    }
}
