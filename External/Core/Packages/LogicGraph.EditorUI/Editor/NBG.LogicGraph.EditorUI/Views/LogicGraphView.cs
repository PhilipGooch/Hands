using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Root element of all Logic Graph nodes, links, groups
    /// </summary>
    internal class LogicGraphView : GraphView, Searcher
    {
        private const string k_NodeGraphUSSGUID = "dd3962010488fe644b5a8d8dda3b8bee";

        private LogicGraphPlayerEditor activeGraph;
        private VariableDragView dragView;
        private MiniMap minimap;
        private InspectorView inspectorView;

        private List<ISerializedNode> serializedNodeViews = new List<ISerializedNode>();
        internal List<ISerializedNode> SerializedNodeViews => serializedNodeViews;

        private List<INodeContainer> nodeContainers = new List<INodeContainer>();

        private List<SerializableGuid> nodesToRemoveIds = new List<SerializableGuid>();

        internal Vector2 LastClickPosition { get; private set; }
        internal Vector2 MousePosition { get; private set; }

        private EditorWindow host;

        private const int kFrameBorder = 30;
        internal bool GraphValid => activeGraph != null && activeGraph.logicGraphPlayer != null;

        public new class UxmlFactory : UxmlFactory<LogicGraphView, GraphView.UxmlTraits> { }

        public LogicGraphView()
        {
            Insert(0, new GridBackground());

            viewTransformChanged += ViewTransformChangedCallback;

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ContentZoomer());

            RegisterCallback<MouseUpEvent>((evt) =>
            {
                if (evt.button == 1 && GraphValid && evt.currentTarget is LogicGraphView)
                {
                    UpdateLastClickPosition(evt.currentTarget as VisualElement, evt.localMousePosition);

                    ShowSearcher(evt.mousePosition);
                }
            });

            RegisterCallback<MouseDownEvent>((evt) =>
            {
                if (evt.button == 1 && GraphValid && evt.currentTarget is LogicGraphView)
                {
                    UpdateLastClickPosition(evt.currentTarget as VisualElement, evt.localMousePosition);
                }
            });

            RegisterCallback<MouseOverEvent>((evt) =>
            {
                if (evt.button == 0 && GraphValid && evt.currentTarget is LogicGraphView)
                {
                    MousePosition = GetLocalMousePosition(evt.currentTarget as VisualElement, evt.localMousePosition);
                }
            });

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(k_NodeGraphUSSGUID));
            styleSheets.Add(styleSheet);

            GenerateMinimap();
            SetMinimapVisibility(EditorPrefsManager.MinimapVisible);
        }

        internal void Initialize(EditorWindow host, LogicGraphPlayerEditor activeGraph, InspectorView inspectorView)
        {
            this.host = host;
            this.inspectorView = inspectorView;
            this.activeGraph = activeGraph;

            ClearAll();
        }

        internal void Update(bool fullUpdate)
        {
            graphViewChanged -= OnGraphViewChanged;

            nodeContainers = activeGraph.NodesController.GetNodes().ToList();
            nodesToRemoveIds = serializedNodeViews.Select(x => x.Container.ID).ToList();

            UpdateNodeViews(fullUpdate);
            UpdateStickyNoteViews(fullUpdate);
            UpdateGroupViews(fullUpdate);

            foreach (var id in nodesToRemoveIds)
            {
                RemoveNodeView(id);
            }

            UpdateEdgeViews(fullUpdate);

            graphViewChanged += OnGraphViewChanged;
        }

        internal void ClearAll()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            serializedNodeViews.Clear();
            graphViewChanged += OnGraphViewChanged;
        }

        #region Viewport values update
        public bool TryUpdateViewportPosition(bool updateOnlyIfExists)
        {
            if (activeGraph == null)
                return false;

            (bool exists, Vector3 position, Vector3 scale) = EditorStateManager.GetGraphViewViewportValues(activeGraph.graphObj);
            if (exists)
            {
                UpdateViewTransform(position, scale);
                return true;
            }
            else if (!updateOnlyIfExists)
            {
                var rectToFit = CalculateRectToFitAll(contentViewContainer);
                CalculateFrameTransform(rectToFit, layout, kFrameBorder, out Vector3 frameTranslation, out Vector3 frameScaling);

                UpdateViewTransform(frameTranslation, frameScaling);
                contentViewContainer.MarkDirtyRepaint();
                return true;
            }
            return false;
        }

        private void ViewTransformChangedCallback(GraphView graphView)
        {
            if (activeGraph != null)
            {
                EditorStateManager.SetGraphViewViewportValues(activeGraph.graphObj, viewTransform.position, viewTransform.scale);
            }
        }
        #endregion

        #region Minimap
        private void GenerateMinimap()
        {
            minimap = new MiniMap() { anchored = true };
            minimap.SetPosition(new Rect(3, 0, 140, 140));
            minimap.maxHeight = 140;
            minimap.maxWidth = 140;
            Add(minimap);
        }

        public void SetMinimapVisibility(bool state)
        {
            minimap.visible = state;
        }
        #endregion

        #region Blackboard
        internal void AddDragView(VariableDragView dragView)
        {
            this.dragView = dragView;
            this.dragView.onVariableDraggingStopped += VariableDraggedIn;
            this.dragView.onNodeDraggingStopped += NodeDraggedIn;
        }

        internal void VariableDraggedIn(VisualElement dragView, Vector3 mousePosition, SerializableGuid id)
        {
            if (MousePositionOverGraph(mousePosition))
            {
                UpdateLastClickPosition(dragView.parent, mousePosition);
                var node = activeGraph.NodesController.CreateVariableNode(id);
                var ui = (IContainerUI)node;
                ui.Rect = new Rect(LastClickPosition.x, LastClickPosition.y, 100, 100);
                activeGraph.StateChanged();
            }
            /* else
             {
                 Debug.LogWarning($"Graph does not contain mouse position {mousePosition} xMin {worldBound.xMin} xMax {worldBound.xMax}  yMin {worldBound.yMin}  yMax {worldBound.yMax} ");
             }*/
        }

        private bool MousePositionOverGraph(Vector3 mousePosition)
        {
            //non existent world bound, must be a test
            if (float.IsNaN(worldBound.xMin) || float.IsNaN(worldBound.xMax) || float.IsNaN(worldBound.yMin) || float.IsNaN(worldBound.yMax))
                return true;
            else
                return worldBound.Contains(mousePosition);
        }

        #endregion

        #region Searcher

        public void ShowSearcher(Vector2 position)
        {
            GetSearcherData();
            SearcherWindow.Show(host, GetSearcherData(), position, CreateNode);
        }

        public List<SearcherData> GetSearcherData()
        {
            IEnumerable<NodeEntry> nodeTypes = activeGraph.NodesController.NodeTypes;
            return new List<SearcherData>()
            {
                SearcherUtils.GetHierarchyNodesSearcherData(nodeTypes, activeGraph, SearcherVisualType.Hierarchy),
                SearcherUtils.GetBuiltInNodesSearcherData(nodeTypes)
            };
        }

        private void NodeDraggedIn(VisualElement dragView, Vector3 mousePosition, ClickContext entry)
        {
            if (MousePositionOverGraph(mousePosition))
            {
                UpdateLastClickPosition(dragView.parent, mousePosition);
                CreateNode(entry);
            }
        }

        #endregion

        private (Port output, Port input) GetConnectedPorts(IComponent outputComponent, IComponent inputComponent)
        {
            Port output = null;
            Port input = null;

            foreach (var nodeView in serializedNodeViews.Where(x => x.Container.ContainerType == ContainerType.Generic))
            {
                int id = 0;
                foreach (var port in (nodeView as NodeView).ports)
                {
                    if (port.Key == outputComponent.InstanceId)
                        output = port.Value;
                    else if (port.Key == inputComponent.InstanceId)
                        input = port.Value;
                    id++;
                }
            }

            return (output, input);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(
                endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node &&
                activeGraph.NodesController.CanConnectPorts(((PortView)endPort).Component, ((PortView)startPort).Component)
                ).ToList();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            RemoveElements(graphViewChange.elementsToRemove);
            MoveElements(graphViewChange.movedElements);
            CreateEdges(graphViewChange.edgesToCreate);
            activeGraph.StateChanged();

            return graphViewChange;
        }

        #region Backend actions

        internal void RemoveElements(List<GraphElement> elementsToRemove)
        {
            if (elementsToRemove != null)
            {
                foreach (var elem in elementsToRemove)
                {
                    ISerializedNode nodeView = elem as ISerializedNode;
                    if (nodeView != null)
                    {
                        serializedNodeViews.Remove(nodeView);
                        activeGraph.NodesController.RemoveNode(nodeView.Container);

                        continue;
                    }

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        NodeView outputNode = edge.output.node as NodeView;
                        NodeView inputNode = edge.input.node as NodeView;

                        activeGraph.NodesController.DisconnectNodes(
                            outputNode.GetPortComponent(edge.output),
                            inputNode.GetPortComponent(edge.input)
                            );
                    }
                }
                activeGraph.StateChanged();
            }
        }

        private void MoveElements(List<GraphElement> elementsToMove)
        {
            if (elementsToMove != null)
            {
                //FIRST update all groups
                foreach (var elem in elementsToMove)
                {
                    GroupView groupView = elem as GroupView;
                    if (groupView != null)
                    {
                        groupView.UpdateContentRects();
                    }
                }
                //THEN update all others
                foreach (var elem in elementsToMove)
                {
                    var serializedNode = elem as ISerializedNode;
                    if (serializedNode != null)
                    {
                        activeGraph.NodesController.UpdateRect(serializedNode.Container, elem.GetPosition());
                    }
                }
            }
        }

        private void CreateEdges(List<Edge> elementsToCreate)
        {
            if (elementsToCreate != null)
            {
                foreach (var elem in elementsToCreate)
                {
                    NodeView outputNode = elem.output.node as NodeView;
                    NodeView inputNode = elem.input.node as NodeView;

                    activeGraph.NodesController.ConnectNodes(
                        outputNode.GetPortComponent(elem.output),
                        inputNode.GetPortComponent(elem.input)
                        );
                }
                activeGraph.StateChanged();
            }
        }

        private void CreateNode(ClickContext ctx)
        {
            CreateAndGetNode(ctx, LastClickPosition);
        }

        internal void CreateNodeAtMousePos(ClickContext ctx)
        {
            CreateAndGetNode(ctx, MousePosition);
        }

        internal INodeContainer CreateAndGetNode(ClickContext ctx, Vector2 pos)
        {
            INodeContainer node = activeGraph.NodesController.CreateNode(ctx.entry, ctx.reference);
            var ui = (IContainerUI)node;
            ui.Rect = new Rect(pos.x, pos.y, 100, 100);

            if (node.ContainerType == ContainerType.Group)
            {
                ui.Color = Parameters.defaultGroupColor;
                //default group header
                activeGraph.NodesController.SetGroupName(node, "Group");
            }

            activeGraph.StateChanged();

            return node;
        }

        #endregion

        #region Views
        //this feels inefficient
        private void UpdateEdgeViews(bool fullUpdate)
        {
            if (!fullUpdate)
                return;

            List<Edge> toDelete = new List<Edge>(edges.ToList());
            foreach (var node in nodeContainers.Where(x => x.ContainerType == ContainerType.Generic))
            {
                foreach (var port in node.Components)
                {
                    if (port.Target != null)
                    {
                        (Port output, Port input) = GetConnectedPorts(port, port.Target);

                        if (output != null && input != null)
                        {
                            Debug.Assert(port.DataType == port.Target.DataType, "Port type mismatch!");
                            var existingConnectionOutput = output.connections.FirstOrDefault(x => x.output == output && x.input == input);
                            var existingConnectionOutputReverse = output.connections.FirstOrDefault(x => x.output == input && x.input == output);
                            toDelete.Remove(existingConnectionOutput);
                            toDelete.Remove(existingConnectionOutputReverse);

                            if (existingConnectionOutput == null && existingConnectionOutputReverse == null)
                            {
                                Edge edge = output.ConnectTo(input);
                                AddElement(edge);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"COULD NOT FIND CONNECTED PORTS output is null: {output == null} input is null: {input == null}");
                        }
                    }
                }
            }

            foreach (var item in toDelete)
            {
                foreach (var node in serializedNodeViews.Where(x => x is NodeView))
                {
                    NodeView nodeView = node as NodeView;
                    nodeView.DisconnectEdge(item);
                }
                RemoveElement(item);
            }
        }

        private void UpdateNodeViews(bool fullUpdate)
        {
            foreach (var item in nodeContainers)
            {
                if (item.ContainerType == ContainerType.Generic)
                {
                    NodeView nodeView = GetSerializedNode<NodeView>(item) as NodeView;
                    nodeView.Update(item, fullUpdate, this);
                }
            }
        }

        private void UpdateStickyNoteViews(bool fullUpdate)
        {
            foreach (var item in nodeContainers)
            {
                if (item.ContainerType == ContainerType.Comment)
                {
                    CommentView commentView = GetSerializedNode<CommentView>(item) as CommentView;
                    commentView.Update(item, fullUpdate);
                }
            }
        }

        private void UpdateGroupViews(bool fullUpdate)
        {
            foreach (var item in nodeContainers)
            {
                if (item.ContainerType == ContainerType.Group)
                {
                    GroupView groupView = GetSerializedNode<GroupView>(item) as GroupView;
                    groupView.Update(item, fullUpdate, serializedNodeViews, this);
                }
            }
        }

        private ISerializedNode GetSerializedNode<T>(INodeContainer container) where T : new()
        {
            ISerializedNode node = null;

            foreach (var item in serializedNodeViews)
            {
                if (item.Container.ID == container.ID)
                {
                    node = item;
                    break;
                }
            }

            if (node == null)
            {
                node = new T() as ISerializedNode;
                node.Initialize(activeGraph, OnNodeSelected, OnNodeDeselected);

                serializedNodeViews.Add(node);
                AddElement(node as GraphElement);
            }

            nodesToRemoveIds.Remove(container.ID);
            return node;
        }

        private void RemoveNodeView(SerializableGuid id)
        {
            var nodeView = serializedNodeViews.FirstOrDefault(x => x.Container.ID == id);
            if (nodeView != null)
            {
                serializedNodeViews.Remove(nodeView);
                RemoveElement(nodeView as GraphElement);
            }
        }

        #endregion

        internal void SelectAllViews()
        {
            foreach (var view in serializedNodeViews)
            {
                (view as GraphElement).Select(this, true);
            }
        }

        private void UpdateLastClickPosition(VisualElement clickTarget, Vector2 localMousePos)
        {
            LastClickPosition = GetLocalMousePosition(clickTarget, localMousePos);
        }

        Vector2 GetLocalMousePosition(VisualElement clickTarget, Vector2 localMousePos)
        {
            return clickTarget.ChangeCoordinatesTo(contentViewContainer, localMousePos);
        }

        //needed to override default context menu
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            //TODO only show context menu only if noting selected?
            UpdateLastClickPosition(evt.currentTarget as VisualElement, evt.localMousePosition);
        }

        private void OnNodeSelected(INodeContainer container)
        {
            inspectorView.NodeSelected(container);
        }

        private void OnNodeDeselected(INodeContainer container)
        {
            inspectorView.NodeDeselected(container);
        }

        internal List<INodeContainer> GetSelectedNodes()
        {
            List<INodeContainer> selected = new List<INodeContainer>();
            foreach (var item in selection)
            {
                var node = item as ISerializedNode;
                if (node != null)
                {
                    selected.Add(node.Container);
                }
            }
            return selected;
        }
    }

    /// <summary>
    /// Interface which unites all serializable nodes 
    /// Needed because GroupView and CommentView use different base classes from generic Logic Nodes
    /// </summary>
    internal interface ISerializedNode
    {
        INodeContainer Container { set; get; }
        Action<INodeContainer> onSelected { set; get; }
        Action<INodeContainer> onDeselected { set; get; }
        void Initialize(LogicGraphPlayerEditor activeGraph, Action<INodeContainer> onSelected, Action<INodeContainer> onDeselected);
    }
}

