using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    /// <summary>
    /// Base or default rect drawing
    /// </summary>
    public class NodeRect
    {
        public Rect rect;
        public Node node;
        public List<NodeSocketRect> sockets = new List<NodeSocketRect>();

        public NodeWindow parentWindow;

        public static List<NodeRect> selectedNodes = new List<NodeRect>();

        public bool isSelected => selectedNodes.Contains(this);

        protected virtual Color NodeColor
        {
            get
            {
                return node.nodeColour;
            }
        }

        protected virtual string Title
        {
            get
            {
                return node.Title;
            }
        }

        protected virtual string ClassName
        {
            get
            {
                if (string.IsNullOrEmpty(node.renamed) == false || renaming)
                    return node.renamed;
                return node.name;
            }
        }

        public virtual Vector2 nodePos
        {
            get
            {
                return node.pos;
            }
            set
            {
                Vector2 val = value;
                val.x = Mathf.Clamp(val.x, 0, 1500);
                val.y = Mathf.Clamp(val.y, 0, 1000);
                node.pos = val;
            }
        }

        public NodeRect(NodeWindow win, Node node)
        {
            this.node = node;
            parentWindow = win;
        }

        public virtual void Initialize()
        {
            sockets.Clear();
            var nodeSockets = node.ListAllSockets();
            for (int i = 0; i < nodeSockets.Count; i++)
                sockets.Add(new NodeSocketRect() { nodeRect = this, socket = nodeSockets[i] });
        }
        GenericMenu rightClickMenu;
        static Rect rightClickRect;

        protected bool renaming = false;

        void RightClickMenuCallback(object data)
        {
            if ((int)data == 0) //rename
            {
                DeselectNodes();
                SelectNode(this);
                renaming = true;
            }
        }

        protected virtual void ProcessInput()
        {
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    rightClickRect = GUILayoutUtility.GetLastRect();
                }
                if (rightClickMenu == null)
                {
                    rightClickMenu = new GenericMenu();
                    rightClickMenu.AddItem(new GUIContent("Rename"), false, RightClickMenuCallback, 0);
                }
                rightClickMenu.DropDown(rightClickRect);
                evt.Use();
            }

            if ((evt.button == 0) && (evt.type == EventType.MouseDown))
            {
                var additive = evt.control || evt.shift;
                if (!additive && !isSelected)
                    DeselectNodes();
                SelectNode(this);
                renaming = false;
            }
            if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.Return)
            {
                GUI.FocusControl("");
                renaming = false;
            }
        }

        void SelectNode(NodeRect node)
        {
            if (!selectedNodes.Contains(node))
                selectedNodes.Add(node);
        }

        public static void DeselectNodes()
        {
            foreach (var oldNode in selectedNodes)
                oldNode.Deselect();
            selectedNodes.Clear();
        }


        public virtual void UpdateLayout()
        {
            float width = NodeWindow.boxWidth;
            float classHeight = NodeWindow.classHeight + 3;
            rect = new Rect(nodePos.x, nodePos.y, width, NodeWindow.headerHeight + classHeight + NodeWindow.footerHeight + sockets.Count * NodeWindow.signalHeight);
            var y = NodeWindow.headerHeight + classHeight;
            for (int i = 0; i < sockets.Count; i++)
            {
                sockets[i].UpdateLayout(new Rect(0, y, width, NodeWindow.signalHeight));
                y += NodeWindow.signalHeight;
            }
        }

        public bool RenderWindow(int id, bool drawHighlight)
        {
            if (node == null)
            {
                return false;
            }
            GUI.backgroundColor = NodeColor;
            GUIContent content = new GUIContent(Title);

            rect = GUI.Window(id, rect, UpdateRect, content);


            if (nodePos != rect.position)
            {
                var delta = rect.position - nodePos;
                foreach (var node in selectedNodes)
                {
                    node.UpdatePosition(delta);
                }
                Undo.RecordObject(node, "Move"); //TODO: not recording
                nodePos = rect.position;
                UpdateLayout();

            }
            if (isSelected)
                DrawRing(rect);
            return true;
        }
        public void UpdatePosition(Vector2 offset)
        {
            nodePos += offset;
            UpdateLayout();
        }
        void UpdateRect(int id)
        {
            ProcessInput();
            Render(id + "");
        }
        public virtual void RenderClassRect(string id)
        {
            GUI.color = new Color(.7f, .7f, .7f, 1);

            Rect classRect = new Rect(6, NodeWindow.headerHeight + 2, NodeWindow.boxWidth - 40, NodeWindow.classHeight);
            if (GUI.Button(new Rect(classRect.x, classRect.y, 20, NodeWindow.classHeight), "»"))
            {
                Selection.activeGameObject = node.gameObject;
                if (parentWindow != null)
                    parentWindow.ChangeSelectedNode(this);
            }

            Rect classNameRect = new Rect(30, NodeWindow.headerHeight + 2, NodeWindow.boxWidth - 40, NodeWindow.classHeight);
            if (renaming)
            {
                GUI.SetNextControlName(id + "renaming");
                node.renamed = GUI.TextField(classNameRect, ClassName);
                GUI.FocusControl(id + "renaming");
            }
            else
            {
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
                GUI.Label(classNameRect, ClassName);
            }
        }

        public virtual void Render(string id)
        {
            RenderClassRect(id);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            for (int i = 0; i < sockets.Count; i++)
                sockets[i].Render(id + i);
            GUI.color = new Color(0.9f, 0.9f, 0.9f);

            Rect xPos = new Rect(NodeWindow.boxWidth - 16, 0, 14, 14);
            if (GUI.Button(xPos, ""))
                GameObject.DestroyImmediate(node);
            GUI.Label(new Rect(NodeWindow.boxWidth - 13, -1f, 15, 15), "x");
            GUI.DragWindow();
        }

        public NodeSocketRect HitTest(Vector2 pos)
        {
            pos.x -= rect.x;
            pos.y -= rect.y;
            for (int i = 0; i < sockets.Count; i++)
                if (sockets[i].HitTest(pos))
                    return sockets[i];
            return null;
        }

        protected void DrawRing(Rect box)
        {
            Rect inner = ExpandBox(box, 4.0f, 4.0f);
            Rect outer = ExpandBox(inner, 4.0f, 4.0f);
            Color col = new Color(1.0f, 0.5f, 0.3f, 0.5f);
            EditorGUI.DrawRect(Rect.MinMaxRect(outer.xMin, outer.yMin, inner.xMin, outer.yMax), col);
            EditorGUI.DrawRect(Rect.MinMaxRect(inner.xMax, outer.yMin, outer.xMax, outer.yMax), col);
            EditorGUI.DrawRect(Rect.MinMaxRect(inner.xMin, outer.yMin, inner.xMax, inner.yMin), col);
            EditorGUI.DrawRect(Rect.MinMaxRect(inner.xMin, inner.yMax, inner.xMax, outer.yMax), col);
        }

        Rect ExpandBox(Rect input, float sizeX, float sizeY)
        {
            input.x -= sizeX;
            input.y -= sizeY;
            input.width += sizeX * 2.0f;
            input.height += sizeY * 2.0f;
            return input;
        }

        public virtual void Deselect()
        {
            renaming = false;
            foreach (var socket in sockets)
                socket.Deselect();
        }

        public virtual void OnSelect()
        {

        }
    }
}
