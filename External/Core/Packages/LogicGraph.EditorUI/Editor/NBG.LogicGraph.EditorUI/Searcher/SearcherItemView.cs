using NBG.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Display of SearcherData tree
    /// </summary>
    internal class SearcherItemView
    {
        internal const int level1FoldoutOffset = 20;

        internal SearcherItemView parent;
        internal SearcherEndNode node;

        internal VisualElement ContentContainer { get; private set; }

        internal Dictionary<string, SearcherItemView> branchViews;
        internal List<SearcherLeafView> nodeViews;

        private Action<ClickContext> onNodeClick;
        private Action<ISearcherSelectable, bool> onSelect;
        private Action<ISearcherSelectable> onElementAdded;

        internal bool isRoot => depth == 0;
        internal string branchID { get; private set; }

        internal int depth;
        private bool openRootFoldout;

        internal SearcherItemView(
            VisualElement parentElement,
            SearcherEndNode node,
            int depth,
            bool openRootFoldout,
            SearcherItemView parent,
            Action<ClickContext> onNodeClick,
            Action<ISearcherSelectable, bool> onSelect,
            Action<ISearcherSelectable> onElementAdded)
        {
            this.parent = parent;
            this.onNodeClick = onNodeClick;
            this.depth = depth;
            this.node = node;
            this.onSelect = onSelect;
            this.onElementAdded = onElementAdded;
            this.openRootFoldout = openRootFoldout;

            branchID = SearcherUtils.PathToID(node.path, depth);

            AddBranchView(parentElement, node.path, depth);

            branchViews = new Dictionary<string, SearcherItemView>();
            nodeViews = new List<SearcherLeafView>();
        }

        //root constructor
        internal SearcherItemView(
            VisualElement parent,
            bool openRootFoldout,
            Action<ClickContext> onNodeClick,
            Action<ISearcherSelectable, bool> onSelect,
            Action<ISearcherSelectable> onElementAdded)
        {
            this.parent = null;
            this.onNodeClick = onNodeClick;
            this.onSelect = onSelect;
            this.depth = 0;
            this.node = null;
            this.onElementAdded = onElementAdded;
            this.openRootFoldout = openRootFoldout;

            ContentContainer = parent;
            branchViews = new Dictionary<string, SearcherItemView>();
            nodeViews = new List<SearcherLeafView>();
        }

        internal void Add(SearcherEndNode leaf, int depth)
        {
            if (leaf.path.Count == depth)
            {
                AddNodeView(leaf, depth);
                return;
            }

            var key = SearcherUtils.PathToID(leaf.path, depth);

            if (!branchViews.ContainsKey(key))
            {
                branchViews.Add(
                   key,
                    new SearcherItemView(
                        ContentContainer,
                        leaf,
                        depth,
                        openRootFoldout,
                        this,
                        onNodeClick,
                        onSelect,
                        onElementAdded
                       ));
            }

            branchViews[key].Add(leaf, ++depth);
        }

        private void AddBranchView(VisualElement parent, List<(string segment, UnityEngine.Object relativeObj)> path, int depth)
        {
            var foldout = new SearcherFoldoutView();
            bool defaultFoldoutState = openRootFoldout && isRoot;
            parent.Add(foldout);
            ContentContainer = foldout;
            foldout.Initialize(parent, this, path, depth, defaultFoldoutState, onSelect);
            onElementAdded?.Invoke(foldout);
        }

        internal void IterateDown(Action<ISearcherSelectable> onIteration)
        {
            foreach (var child in ContentContainer.Children())
            {
                if (child is SearcherFoldoutView branch)
                {
                    onIteration?.Invoke(branch);
                    branch.ParentItemView.IterateDown(onIteration);
                }

                if (child is SearcherLeafView leaf)
                {
                    onIteration?.Invoke(leaf);
                }
            }
        }

        public void RemoveLeaf(SearcherLeafView toRemove, Action<string> onBranchColapse)
        {
            ContentContainer.Remove(toRemove);
            nodeViews.Remove(toRemove);

            if (nodeViews.Count == 0 && branchViews.Count == 0)
            {
                parent.Colapse(branchID, onBranchColapse);
            }
        }

        void Colapse(string branchToRemoveID, Action<string> onBranchColapse)
        {
            onBranchColapse?.Invoke(branchToRemoveID);
            ContentContainer.Remove(branchViews[branchToRemoveID].ContentContainer);
            branchViews.Remove(branchToRemoveID);
            if (nodeViews.Count == 0 && branchViews.Count == 0)
            {
                parent.Colapse(branchID, onBranchColapse);
            }
        }

        private void AddNodeView(SearcherEndNode leaf, int depth)
        {
            var leafView = new SearcherLeafView();

            ContentContainer.Add(leafView);
            nodeViews.Add(leafView);

            leafView.Initialize(leaf, this, onNodeClick, onSelect, depth);
            onElementAdded?.Invoke(leafView);
        }

        internal bool Filter(string query)
        {
            bool anyVisible = false;
            var foldout = ContentContainer as SearcherFoldoutView;

            foreach (var branch in branchViews)
            {
                anyVisible |= branch.Value.Filter(query);
            }

            //only Component itemViews have nodes
            if (nodeViews.Count > 0)
            {
                if (foldout != null)
                {
                    anyVisible |= foldout.Filter(query);
                }
            }

            if (!anyVisible)//if this branch does not contain query, check nodes
                foreach (var node in nodeViews)
                {
                    anyVisible |= node.Filter(query);
                }
            else //if branch name contains query, enable all nodes
                foreach (var node in nodeViews)
                {
                    node.SetFilterOutState(false);
                }

            if (foldout != null)
            {
                ContentContainer.SetVisibility(anyVisible);

                if (anyVisible)
                    foldout.value = true;
            }

            return anyVisible;
        }
    }
}