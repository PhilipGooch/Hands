using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{

    [CustomNodeRectDrawer(typeof(SignalFloat))]
    public class SignalFloatDrawer : PrimitiveDrawer
    {
        public SignalFloatDrawer(NodeWindow win, Node node) : base(win, node) { }

        string temp;
        float parsed;

        protected override string primitiveTitle => "Float";

        protected override void RenderPrimitive()
        {
            var output = (NodeOutputFloat)sockets[0].socket;
            var rect = sockets[0].localRect;
            rect.x = rect.width - 50;
            rect.width = 32;
            GUI.SetNextControlName(id);
            temp = GUI.TextField(rect, temp);
            string focus = GUI.GetNameOfFocusedControl();
            if (string.IsNullOrEmpty(temp) && focus != id)
                temp = (Application.isPlaying ? output.value : output.initialValue) + "";

            var o = Application.isPlaying ? output.value : output.initialValue;
            if (o != parsed && focus != id)//in case value was changed in the inspector
                temp = o + "";
            if ((parsed + "" != temp && focus != id))
            {
                float.TryParse(temp, out parsed);
                temp = parsed + "";
                Undo.RecordObject(output.node, "Float Changed");
                if (Application.isPlaying)
                    output.SetValue(parsed);
                else
                    output.initialValue = parsed;
                EditorUtility.SetDirty(output.node);
            }
            rect.x = sockets[0].localRect.x - 20;
            rect.width = sockets[0].localRect.width - 35;
            GUI.Label(rect, output.name);
        }
    }
}
