using UnityEngine;

namespace NBG.NodeGraph
{

    [CustomNodeRectDrawer(typeof(SignalInt))]
    public class SignalIntDrawer : NodeRect
    {
        public SignalIntDrawer(NodeWindow win, Node node) : base(win, node) { }
        string strInput;
        int lastInput;
        string id = "";
        protected override string Title
        {
            get
            {
                return string.IsNullOrEmpty(node.renamed) ? "Int" : node.renamed;
            }
        }
        public override void Deselect()
        {
            base.Deselect();
            var selected = GUI.GetNameOfFocusedControl();
            if (selected == id)
                GUI.FocusControl("");
        }

        public override void Render(string id)
        {
            this.id = id;
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUI.color = Color.white;

            var output = (NodeOutputInt)sockets[0].socket;

            var v = Application.isPlaying ? output.value : output.initialValue;
            var rect = sockets[0].localRect;
            rect.x = rect.width - 50;
            rect.width = 32;
            string focus = GUI.GetNameOfFocusedControl();
            if (string.IsNullOrEmpty(strInput) && focus != id)
                strInput = (Application.isPlaying ? output.value : output.initialValue) + "";

            //var o = Application.isPlaying ? output.value : output.initialValue;
            //if (o != lastInput && focus != id) //in case value was changed in the inspector
            //    strInput = o + "";
            GUI.SetNextControlName(id);
            strInput = GUI.TextField(rect, strInput);
            if ((lastInput + "" != strInput && focus != id))
            {
                int.TryParse(strInput, out lastInput);
                strInput = lastInput + "";
                if (Application.isPlaying)
                    output.SetValue(lastInput);
                else
                    output.initialValue = lastInput;
            }
            rect.x = sockets[0].localRect.x - 20;
            rect.width = sockets[0].localRect.width - 35;
            GUI.Label(rect, output.name);

            var circleRect = sockets[0].hitRect;
            circleRect.x = sockets[0].localRect.width - 15;
            circleRect.y += 3;
            circleRect.width = 10;
            circleRect.height = 10;
            NodeWindow.DrawTextureGUI(circleRect, NodeWindow.circle);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Rect xPos = new Rect(88, 0, 14, 14);
            if (GUI.Button(xPos, ""))
                GameObject.DestroyImmediate(node);
            GUI.Label(new Rect(90, -1f, 15, 15), "x");
            GUI.DragWindow();
        }

        public override void UpdateLayout()
        {
            GUIStyle s = new GUIStyle("Toggle");
            var size = s.CalcSize(new GUIContent(sockets[0].name));
            float width = size.x + 50;
            rect = new Rect(nodePos.x, nodePos.y, width, NodeWindow.headerHeight + NodeWindow.footerHeight + sockets.Count * NodeWindow.signalHeight + 5);
            var y = NodeWindow.headerHeight;
            for (int i = 0; i < sockets.Count; i++)
            {
                sockets[i].UpdateLayout(new Rect(0, y + 3, width, NodeWindow.signalHeight));
                y += NodeWindow.signalHeight;
            }
        }
    }
}
