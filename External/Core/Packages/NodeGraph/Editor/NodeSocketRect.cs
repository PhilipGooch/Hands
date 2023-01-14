using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    public class NodeSocketRect
    {
        public Rect rect;
        public NodeSocket socket;
        public NodeRect nodeRect;
        public bool allowEdit = true;
        public Rect localRect;
        public Rect hitRect;

        public string name { get { return socket.name; } set { socket.name = value; } }

        public virtual void Deselect()
        {
            var selected = GUI.GetNameOfFocusedControl();
            if (selected == id)
                GUI.FocusControl("");
        }

        public void UpdateLayout(Rect rect)
        {
            this.localRect = rect;
            hitRect = localRect;
            if (socket is NodeOutput)
                hitRect.x = hitRect.width - NodeWindow.signalHeight;
            hitRect.width = NodeWindow.signalHeight;

            rect.x += nodeRect.rect.x;
            rect.y += nodeRect.rect.y;
            this.rect = rect;
        }

        public Vector2 connectPoint
        {
            get
            {
                if (socket is NodeInput)
                    return new Vector2(rect.xMin, rect.center.y);
                else
                    return new Vector2(rect.xMax, rect.center.y);
            }
        }
        protected string id = "";

        public bool HitTest(Vector2 localPos)
        {
            return hitRect.Contains(localPos);
        }

        public void Render(string id)
        {
            this.id = id;
            var labelRect = localRect;
            var circleRect = hitRect;
            circleRect.width = 10;
            circleRect.height = 10;
            circleRect.x = 5;
            circleRect.y += 4;

            GUI.SetNextControlName(id);

            if (allowEdit && Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1 && labelRect.Contains(Event.current.mousePosition))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Rename"), false, BeginRename);
                    menu.ShowAsContext();
                    Event.current.Use();
                    GUIUtility.hotControl = 0;
                }
            }

            if (socket is NodeOutput || socket.GetType().IsSubclassOf(typeof(NodeOutput)))
            {
                GUI.skin.label.alignment = TextAnchor.UpperRight;
                circleRect.x = NodeWindow.boxWidth - 15;
            }
            if (rename)
                Rename(labelRect);
            else
                socket.Render(localRect);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            NodeWindow.DrawTextureGUI(circleRect, NodeWindow.circle);
            GUI.color = Color.white;
        }

        bool rename = false;
        private void BeginRename()
        {
            rename = true;
        }

        void Rename(Rect labelRect)
        {
            var style = new GUIStyle();
            style.onFocused = style.normal = new GUIStyleState() { textColor = Color.white };
            GUI.SetNextControlName("rename");
            socket.name = GUI.TextField(labelRect, socket.name, style);

            if (GUI.GetNameOfFocusedControl() != "rename")
                rename = false;
        }
    }
}
