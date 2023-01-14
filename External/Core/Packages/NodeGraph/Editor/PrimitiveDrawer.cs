using UnityEngine;

namespace NBG.NodeGraph
{
    public abstract class PrimitiveDrawer : NodeRect
    {
        public PrimitiveDrawer(NodeWindow win, Node node) : base(win, node) { }

        protected abstract string primitiveTitle { get; }

        protected string id = "";

        protected override string Title
        {
            get
            {
                return string.IsNullOrEmpty(node.renamed) ? primitiveTitle : node.renamed;
            }
        }


        public override void Render(string id)
        {
            this.id = id;
            HandleRenaming();
            RenderPrimitive();
            DrawCircle();
            DrawX();
            GUI.DragWindow();
        }

        void HandleRenaming()
        {
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUI.color = Color.white;

            if (renaming)
            {
                GUI.SetNextControlName(id + "renaming");
                node.renamed = GUI.TextField(new Rect(2, 2, 80, 13), node.renamed);
                GUI.FocusControl(id + "renaming");
            }
        }

        protected abstract void RenderPrimitive();

        void DrawCircle()
        {
            var circleRect = sockets[0].hitRect;
            circleRect.x = sockets[0].localRect.width - 15;
            circleRect.y += 3;
            circleRect.width = 10;
            circleRect.height = 10;
            NodeWindow.DrawTextureGUI(circleRect, NodeWindow.circle);
        }

        void DrawX()
        {
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            Rect xPos = new Rect(88, 0, 14, 14);
            if (GUI.Button(xPos, ""))
                GameObject.DestroyImmediate(node);
            GUI.Label(new Rect(90, -1f, 15, 15), "x");

        }

        public override void Deselect()
        {
            base.Deselect();
            var selected = GUI.GetNameOfFocusedControl();
            if (selected == id)
                GUI.FocusControl("");
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
