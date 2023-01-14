using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class SearchersInspector : VisualElement
    {
        private const string k_UXMLGUID = "50f3e1bced9d4ec48b97ddcdbab12c6a";
        public new class UxmlFactory : UxmlFactory<SearchersInspector, VisualElement.UxmlTraits> { }

        private LogicGraphPlayerEditor activeGraph;

        private SearcherContainerView hierarchySearcher;
        private SearcherContainerView builtinSearcher;

        private VariableDragView dragView;
        private Action<ClickContext> onClick;

        public SearchersInspector()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            hierarchySearcher = this.Q<SearcherContainerView>("hierarchySearcher");
            builtinSearcher = this.Q<SearcherContainerView>("builtinSearcher");
            UpdateVisibility();

            this.FixTabBackgroundColor();
        }

        internal void Initialize(LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;

            IEnumerable<NodeEntry> nodeTypes = activeGraph.NodesController.NodeTypes;
            hierarchySearcher.Initialize(null, CreateNode, SearcherType.AutoRefresh, dragView, true);
            builtinSearcher.Initialize(null, CreateNode, SearcherType.AutoRefresh, dragView, false);
            hierarchySearcher.SetNewData(new List<SearcherData>() { SearcherUtils.GetHierarchyNodesSearcherData(nodeTypes, activeGraph, SearcherVisualType.Hierarchy) });
            builtinSearcher.SetNewData(new List<SearcherData>() { SearcherUtils.GetBuiltInNodesSearcherData(nodeTypes) });
        }

        internal void Update()
        {
            if (activeGraph != null)
            {
                IEnumerable<NodeEntry> nodeTypes = activeGraph.NodesController.NodeTypes;
                hierarchySearcher.Update(new List<SearcherData>() { SearcherUtils.GetHierarchyNodesSearcherData(nodeTypes, activeGraph, SearcherVisualType.Hierarchy) });
            }
        }

        internal void AddDragView(VariableDragView dragView)
        {
            this.dragView = dragView;
        }

        internal void AddOnClick(Action<ClickContext> onClick)
        {
            this.onClick = onClick;
        }

        internal void ClearAll()
        {
            builtinSearcher.ClearAll();
            hierarchySearcher.ClearAll();
        }

        internal void CreateNode(ClickContext ctx)
        {
            onClick?.Invoke(ctx);
        }

        internal void UpdateVisibility()
        {
            builtinSearcher.SetVisibility(EditorPrefsManager.BuiltinSearcherVisible);
            hierarchySearcher.SetVisibility(EditorPrefsManager.HierarchySearcherVisible);

            if (builtinSearcher.IsVisible() || hierarchySearcher.IsVisible())
                this.SetVisibility(true);
            else
                this.SetVisibility(false);
        }
    }
}
