using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomNodeRectDrawer(typeof(NodeComment))]
    public class NodeRectComment : NodeRect
    {
        protected override Color NodeColor => new Color(0.4f, 1, 0.4f);

        protected override string Title => "Comment";

        public NodeRectComment(NodeWindow win, Node node) : base(win, node) { }

        public override void Initialize()
        {
        }

        public override void UpdateLayout()
        {
            float width = NodeWindow.boxWidth;

            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(((NodeComment)node).comment));
            width = Mathf.Max(width, 32 + size.x);
            float classHeight = size.y + 20;

            rect = new Rect(nodePos.x, nodePos.y, width, NodeWindow.headerHeight + classHeight + NodeWindow.footerHeight);
        }

        public override void Render(string id)
        {
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.color = NodeColor;
            GUI.contentColor = Color.white;
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.border = new RectOffset(0, 0, 0, 0);
            var stateStyle = new GUIStyleState();

            stateStyle.textColor = Color.white;
            style.active =
            style.normal = stateStyle;
            GUI.SetNextControlName(GetHashCode().ToString());
            string res = GUI.TextArea(new Rect(9, NodeWindow.headerHeight + 10, rect.width - 16, rect.height - 14), ((NodeComment)node).comment, style);
            if (res != ((NodeComment)node).comment)
            {
                ((NodeComment)node).comment = res;
                UpdateLayout();
            }


            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;

            Rect xPos = new Rect(NodeWindow.boxWidth - 16, 0, 14, 14);
            if (GUI.Button(xPos, ""))
                GameObject.DestroyImmediate(node);
            GUI.Label(new Rect(NodeWindow.boxWidth - 14, -1f, 15, 15), "x");

            GUI.color = Color.white;
            GUI.DragWindow();
        }

        public override void Deselect()
        {
            base.Deselect();
            if (GUI.GetNameOfFocusedControl() == GetHashCode().ToString())
            {
                GUI.FocusControl("");
            }
        }
    }
}
