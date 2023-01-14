using UnityEngine;

namespace NBG.NodeGraph
{

    [CustomNodeRectDrawer(typeof(SignalBool))]
    public class SignalBoolDrawer : NodeRect
    {
        protected override string Title
        {
            get
            {
                return string.IsNullOrEmpty(node.renamed) ? "Bool" : node.renamed;
            }
        }

        public SignalBoolDrawer(NodeWindow win, Node node) : base(win, node) { }

        public override void Render(string id)
        {
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUI.color = Color.white;

            if (renaming)
            {
                GUI.SetNextControlName(id + "renaming");
                node.renamed = GUI.TextField(new Rect(2, 2, 80, 13), node.renamed);
                GUI.FocusControl(id + "renaming");
            }

            var output = (NodeOutputBool)sockets[0].socket;

            var rect = sockets[0].localRect;
            rect.x = rect.width - 33;
            if (Application.isPlaying)
                output.SetValue(GUI.Toggle(rect, output.value, ""));
            else
                output.initialValue = GUI.Toggle(rect, output.initialValue, "");
            rect.x = sockets[0].localRect.x - 35;
            GUI.Label(rect, sockets[0].name);
            var circleRect = sockets[0].hitRect;
            circleRect.x = sockets[0].localRect.width - 15;
            circleRect.y += 3;
            circleRect.width = 10;
            circleRect.height = 10;
            NodeWindow.DrawTextureGUI(circleRect, NodeWindow.circle);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            GUI.color = new Color(0.9f, 0.9f, 0.9f);

            Rect xPos = new Rect(74, 0, 14, 14);
            if (GUI.Button(xPos, ""))
                GameObject.DestroyImmediate(node);
            GUI.Label(new Rect(76, -1f, 15, 15), "x");

            GUI.DragWindow();
        }

        public override void UpdateLayout()
        {
            float width = 90;
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
