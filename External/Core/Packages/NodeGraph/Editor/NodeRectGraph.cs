using System.Collections.Generic;
using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomNodeRectDrawer(typeof(NodeGraph))]
    public class NodeRectGraph : NodeRect
    {
        protected override Color NodeColor => new Color(0.9f, 0.3f, 0.3f);
        protected override string Title => "Graph";

        //two rects, one for inputs one for outputs
        readonly bool output;

        public NodeRectGraph(NodeWindow win, Node node) : base(win, node)
        {
            if (win.activeGraph == node)
                if ((node as NodeGraph).outputs.Count > 0)
                    win.AddNodeRect(new NodeRectGraph(win, node, true));
        }

        public NodeRectGraph(NodeWindow win, Node node, bool output) : base(win, node)
        {
            this.output = output;
        }

        public override Vector2 nodePos
        {
            get
            {
                if (parentWindow.activeGraph != node)
                    return base.nodePos;

                if (output)
                    return ((NodeGraph)node).outputsPos;
                else
                    return ((NodeGraph)node).inputsPos;
            }
            set
            {
                if (parentWindow.activeGraph == node)
                {
                    Vector2 val = value;
                    val.x = Mathf.Clamp(val.x, 0, 1500);
                    val.y = Mathf.Clamp(val.y, 0, 1000);
                    if (output)
                        ((NodeGraph)node).outputsPos = val;
                    else
                        ((NodeGraph)node).inputsPos = val;
                }
                else
                    base.nodePos = value;
            }
        }

        public override void Initialize()
        {
            sockets.Clear();
            List<NodeSocket> nodeSockets = new List<NodeSocket>();

            var graph = (NodeGraph)node;
            //
            if (parentWindow.activeGraph == node)
            {
                if (output)
                {
                    for (int i = 0; i < graph.outputs.Count; i++)
                        nodeSockets.Add(graph.outputs[i].input);
                }
                else
                {
                    for (int i = 0; i < graph.inputs.Count; i++)
                        nodeSockets.Add(graph.inputs[i].output);
                }
            }
            else
            {
                for (int i = 0; i < graph.inputs.Count; i++)
                    nodeSockets.Add(graph.inputs[i].input);
                for (int i = 0; i < graph.outputs.Count; i++)
                    nodeSockets.Add(graph.outputs[i].output);
            }

            for (int i = 0; i < nodeSockets.Count; i++)
                sockets.Add(new NodeSocketRect() { nodeRect = this, socket = nodeSockets[i], name = nodeSockets[i].name });
        }

        public override void UpdateLayout()
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

        public override void Render(string id)
        {
            RenderClassRect(id);
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            for (int i = 0; i < sockets.Count; i++)
                sockets[i].Render(id + i);
            //Event evt = Event.current;
            //if (evt.type == EventType.MouseDown && evt.button == 1)
            //{
            //    Debug.Log("popup");
            //    //EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0),, "Assets/", null);
            //    if (type == NodeRectType.GraphInputs)
            //        GraphInputsContextMenu().ShowAsContext();


            //    evt.Use();
            //}

            GUI.DragWindow();
        }

    }
}
