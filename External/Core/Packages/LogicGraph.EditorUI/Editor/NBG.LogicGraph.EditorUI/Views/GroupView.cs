using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Groups nodes
    /// </summary>
    internal class GroupView : Group, ISerializedNode
    {
        private const string k_USSGUID = "dd3962010488fe644b5a8d8dda3b8bee";
        private ColorField colorField;

        private LogicGraphPlayerEditor activeGraph;

        private Searcher searcher;

        public INodeContainer Container { set; get; }
        public Action<INodeContainer> onSelected { get; set; }
        public Action<INodeContainer> onDeselected { get; set; }

        private int elementsCount;

        public GroupView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(k_USSGUID));
            styleSheets.Add(styleSheet);

            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.Q<TextField>().RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                activeGraph.NodesController.SetGroupName(Container, evt.newValue);
            });

            headerContainer.Q<VisualElement>("titleContainer").style.flexGrow = 1;
            headerContainer.Q<Label>("titleLabel").style.unityTextAlign = TextAnchor.MiddleLeft;

            colorField = new ColorField { value = Parameters.defaultGroupColor, name = "groupColorPicker" };
            colorField.RegisterValueChangedCallback(e =>
            {
                SetBackgroundColor(e.newValue);
                activeGraph.NodesController.UpdateColor(Container, e.newValue);
            });
            headerContainer.Add(colorField);

            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <summary>
        /// Stops GraphView right click from executing and shows searcher menu
        /// </summary>
        /// <param name="evt"></param>
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 1)
            {
                searcher?.ShowSearcher(evt.mousePosition);

                evt.StopPropagation();
            }
        }

        public void Initialize(LogicGraphPlayerEditor activeGraph, Action<INodeContainer> onSelected, Action<INodeContainer> onDeselected)
        {
            this.activeGraph = activeGraph;
            this.onSelected = onSelected;
            this.onDeselected = onDeselected;
        }

        //need to update nodes inside this group
        internal void UpdateContentRects()
        {
            var children = activeGraph.NodesController.GetGroupChildren(Container).ToList();
            foreach (var elem in containedElements)
            {
                ISerializedNode nodeView = elem as ISerializedNode;
                if (nodeView != null)
                {
                    var node = children.Find(x => x.ID == nodeView.Container.ID);
                    activeGraph.NodesController.UpdateRect(node, (nodeView as GraphElement).GetPosition());
                }
            }
        }

        internal void Update(INodeContainer groupContainer, bool fullUpdate, List<ISerializedNode> nodes, Searcher searcher)
        {
            if (!fullUpdate)
                return;

            this.searcher = searcher;
            Container = groupContainer;

            title = activeGraph.NodesController.GetGroupName(groupContainer);

            SetBackgroundColor(((IContainerUI)groupContainer).Color);

            InitializeInnerNodes(nodes);

            if (elementsCount == 0)
                SetPosition(((IContainerUI)groupContainer).Rect);
        }

        private void InitializeInnerNodes(IEnumerable<ISerializedNode> nodes)
        {
            elementsCount = 0;
            foreach (var item in activeGraph.NodesController.GetGroupChildren(Container))
            {
                GraphElement node = nodes.FirstOrDefault(x => x.Container.ID == item.ID) as GraphElement;

                if (node != null)
                {
                    if (!containedElements.Contains(node))
                        AddElement(node);

                    elementsCount++;
                }
            }
        }

        private void SetBackgroundColor(Color newColor)
        {
            style.backgroundColor = newColor;
        }

        internal void OnElementAdded(GraphElement element)
        {
            OnElementsAdded(new List<GraphElement> { element });
        }

        protected override void OnElementsAdded(IEnumerable<GraphElement> elements)
        {
            foreach (var item in elements)
            {
                ISerializedNode nodeView = item as ISerializedNode;

                if (nodeView != null && !activeGraph.NodesController.GetGroupChildren(Container).Contains(nodeView.Container))
                {
                    ElementsAdded(nodeView.Container);
                }
            }

            base.OnElementsAdded(elements);
        }

        private void ElementsAdded(INodeContainer toAdd)
        {
            activeGraph.NodesController.AddNodeToGroup(Container, toAdd);
            activeGraph.NodesController.UpdateRect(Container, GetPosition());
            activeGraph.StateChanged();
        }

        internal void OnElementRemoved(GraphElement element)
        {
            OnElementsRemoved(new List<GraphElement> { element });
        }

        protected override void OnElementsRemoved(IEnumerable<GraphElement> elements)
        {
            if (parent != null)
            {
                foreach (var item in elements)
                {
                    ISerializedNode nodeView = item as ISerializedNode;
                    
                    //check if node exists and if its not already deleted (guid is empty)
                    if (nodeView != null && nodeView.Container.ID != Core.SerializableGuid.empty)
                    {
                        activeGraph.NodesController.RemoveNodeFromGroup(Container, nodeView.Container);
                        activeGraph.NodesController.UpdateRect(Container, GetPosition());
                        activeGraph.StateChanged();
                    }
                }
            }

            base.OnElementsRemoved(elements);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            onSelected?.Invoke(Container);
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            onDeselected?.Invoke(Container);
        }
    }
}