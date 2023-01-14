using System;
using UnityEngine;

namespace NBG.NodeGraph
{
    [Serializable]
    public class NodeInputFloat : NodeInput<float>
    {
        public override bool CanConnect(NodeOutput output)
        {
            return output is NodeOutput<float>;
        }

#if UNITY_EDITOR
        [HideInInspector]
        public string visualInput;
        private bool isEditing;

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
                string focus = GUI.GetNameOfFocusedControl();
                var hash = GetHashCode().ToString();
                bool isFocused = focus == hash;
                if (isFocused && isEditing == false) //Just started editting input field
                {
                    isEditing = true;
                }
                else if (!isFocused && isEditing) //Finished editing
                {
                    isEditing = false;
                    float parsed;
                    float.TryParse(visualInput, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed);
                    if (Application.isPlaying)
                    {
                        ConnectedOutput_onValueChanged(parsed);
                    }
                    else
                    {
                        initialValue = parsed;
                        UnityEditor.EditorUtility.SetDirty(node);
                    }
                }

                float actualValue = Application.isPlaying ? value : initialValue;
                GUI.SetNextControlName(hash);
                if (isEditing)
                    visualInput = GUI.TextField(labelRect, visualInput);
                else
                    visualInput = GUI.TextField(labelRect, actualValue + "");

                labelRect.x += 35;
                labelRect.width = localPos.width;
                GUI.Label(labelRect, name);
            }
            else
            {
                labelRect.width = localPos.width;
                GUI.Label(labelRect, name + ":" + value);
            }
        }
#endif
    }
}
