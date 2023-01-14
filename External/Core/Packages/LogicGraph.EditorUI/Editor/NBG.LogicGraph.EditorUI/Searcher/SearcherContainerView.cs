using NBG.Core;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    enum SearcherType
    {
        Temporary,
        AutoRefresh,
    }

    public class SearcherContainerView : VisualElement
    {
        private const string k_UXMLGUID = "7eb80ae4520ef0b409c998b33ee4785c";
        public new class UxmlFactory : UxmlFactory<SearcherContainerView, VisualElement.UxmlTraits> { }

        Action<ClickContext> onClick;
        Action<ClickContext> onEnter;
        private SearcherItemView treeView;

        private ScrollView scrollView;
        private TextField filterField;
        private VariableDragView dragView;
        private Label searcherNotificationView;

        private List<ISearcherSelectable> allElements = new List<ISearcherSelectable>();
        private List<ISearcherSelectable> onlyLeaves = new List<ISearcherSelectable>();
        private List<ISearcherSelectable> toDelete;
        private ISearcherSelectable selectedElement;
        private bool IsFiltering => filterField.text.Length > 0;
        private SearcherType searcherType;
        private bool preventDefocus;

        private string query;
        private bool openRootFoldout;

        public SearcherContainerView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            filterField = this.Q<TextField>("filterField");
            searcherNotificationView = this.Q<Label>("searcherNotificationView");

            scrollView = this.Q<ScrollView>();

            filterField.RegisterValueChangedCallback((evt) =>
            {
                query = evt.newValue.ReplaceWhitespace("").ToLower();
                treeView.Filter(query);
            });

            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<FocusOutEvent>((_) => { OnDefocus(); });
        }

        internal void Initialize(Action<ClickContext> onClick, Action<ClickContext> onEnter, SearcherType searcherType, VariableDragView dragView, bool openRootFoldout)
        {
            this.dragView = dragView;
            this.onClick = onClick;
            this.onEnter = onEnter;
            this.searcherType = searcherType;
            this.openRootFoldout = openRootFoldout;
        }

        internal void Update(List<SearcherData> data)
        {
            if (data.Count == 0 || data.Find(x => x.searcherEndNodes.Count > 0) == null)
            {
                ClearAll();
                treeView = new SearcherItemView(scrollView, openRootFoldout, onClick, SelectNodeView, OnElementAdded);
            }
            else
            {
                PopulateMenu(data);
            }
            UpdateNotificationLabel(data);
        }

        private void OnDefocus()
        {
            if (preventDefocus)
            {
                preventDefocus = false;
                return;
            }

            EditorStateManager.ClearSearcherSelectionID(name);
            DeSelect();
        }

        internal void SetNewData(List<SearcherData> data)
        {
            ClearAll();
            treeView = new SearcherItemView(scrollView, openRootFoldout, onClick, SelectNodeView, OnElementAdded);
            PopulateMenu(data);
            UpdateNotificationLabel(data);

            switch (searcherType)
            {
                case SearcherType.Temporary:
                    RegisterCallback<GeometryChangedEvent>(TemporarySearcherInitliazed);
                    ReselectPrevioslySelectedElement();
                    break;
                case SearcherType.AutoRefresh:
                    ReselectPrevioslySelectedElement();
                    ReapplyFilter();
                    break;
                default:
                    break;
            }

            preventDefocus = false;
        }

        void UpdateNotificationLabel(List<SearcherData> data)
        {
            int combinedDisabledCount = 0;
            foreach (var item in data)
            {
                combinedDisabledCount += item.disabledTypesCount;
            }

            searcherNotificationView.text = combinedDisabledCount > 0 ? $"Hidden Node Types Count: {combinedDisabledCount}" : "";
        }

        internal void ClearAll()
        {
            scrollView.contentContainer.Clear();
            allElements.Clear();
            onlyLeaves.Clear();
        }

        private void TemporarySearcherInitliazed(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(TemporarySearcherInitliazed);
            filterField.QInputField<string>().Focus();
            UnhideSelected();
        }

        void ReapplyFilter()
        {
            if (!string.IsNullOrEmpty(query))
                treeView.Filter(query);
        }

        void ReselectPrevioslySelectedElement()
        {
            var idToFind = EditorStateManager.GetSearcherSelectionID(name);
            var previouslySelected = allElements.Find(x => x.UniqueID == idToFind);
            if (previouslySelected != null)
            {
                SelectNodeView(previouslySelected, false);
                FocusFilter();
            }
        }

        void FocusFilter()
        {
            filterField.QInputField<string>().Focus();
        }

        internal void SetupDrag(VariableDragView dragView)
        {
            foreach (var item in allElements)
            {
                var leaf = item as SearcherLeafView;
                if (leaf != null)
                {
                    leaf.SetupDrag(dragView);
                }
            }
            dragView.onNodeDraggingStopped -= OnDraggingStopped;
            dragView.onNodeDraggingStopped += OnDraggingStopped;
        }

        private void OnDraggingStopped(VariableDragView DragView, Vector3 mousePosition, ClickContext id)
        {
            if (!MousePositionOverGraph(mousePosition) && DragView.DragStartElement != null && DragView.DragStartElement.IsChildOf(this))
            {
                preventDefocus = false;
                //force defocus
                DragView.Focus();
            }
        }

        private bool MousePositionOverGraph(Vector3 mousePosition)
        {
            //non existent world bound, must be a test
            if (float.IsNaN(worldBound.xMin) || float.IsNaN(worldBound.xMax) || float.IsNaN(worldBound.yMin) || float.IsNaN(worldBound.yMax))
                return true;
            else
                return worldBound.Contains(mousePosition);
        }

        private void OnElementAdded(ISearcherSelectable element)
        {
            allElements.Add(element);
            if (element is SearcherLeafView leaf)
                onlyLeaves.Add(leaf);
        }

        //to change selection via keyboard
        private void OnKeyDown(KeyDownEvent ev)
        {
            switch (ev.keyCode)
            {
                case KeyCode.UpArrow:
                    SelectionChange(-1, true);
                    break;
                case KeyCode.DownArrow:
                    SelectionChange(1, true);
                    break;
                case KeyCode.LeftArrow:
                    var foldout = selectedElement as SearcherFoldoutView;
                    if (foldout != null)
                    {
                        foldout.value = false;
                    }
                    break;
                case KeyCode.RightArrow:
                    foldout = selectedElement as SearcherFoldoutView;
                    if (foldout != null)
                    {
                        foldout.value = true;
                    }
                    break;
                case KeyCode.Return:
                    if (selectedElement != null)
                    {
                        preventDefocus = true;

                        var leaf = selectedElement as SearcherLeafView;
                        if (leaf != null)
                        {
                            onEnter?.Invoke(new ClickContext(leaf.Node.entry, leaf.Node.Reference));
                        }
                        else
                        {
                            FocusFilter();
                        }
                    }

                    break;
            }
        }

        private void SelectionChange(int change, bool keyboardChange)
        {
            int currentSelection = selectedElement != null ? selectedElement.OrderIndex : 0;
            int newSelection = currentSelection;

            ISearcherSelectable newlySelectedElement;

            do
            {
                newSelection += change;

                if (newSelection < 0)
                    newSelection = allElements.Count - 1;
                else if (newSelection == allElements.Count)
                    newSelection = 0;

                newlySelectedElement = allElements.Find(x => x.OrderIndex == newSelection);
                if (newlySelectedElement != null)
                {
                    if (IsFiltering) //only select nodes
                    {
                        var leaf = newlySelectedElement as SearcherLeafView;
                        if (leaf == null || leaf.FilteredOut)
                            continue;
                    }
                    else //select everything
                    {
                        if (!newlySelectedElement.IsVissible)
                            continue;
                    }
                }

                break;
            }
            while (newSelection != currentSelection);
            SelectNodeView(newlySelectedElement, keyboardChange);
        }

        void SelectNodeView(ISearcherSelectable newSelection, bool keyboardChange)
        {
            preventDefocus = true;
            FocusFilter();

            DeSelect();
            Select(newSelection);

            if (keyboardChange)
                UnhideSelected();
        }

        void DeSelect()
        {
            if (selectedElement != null)
                selectedElement.Deselect();
        }

        void Select(ISearcherSelectable newSelection)
        {
            selectedElement = newSelection;
            selectedElement.Select();

            EditorStateManager.SetSearcherSelectionID(newSelection.UniqueID, name);
        }

        void UnhideSelected()
        {
            if (selectedElement != null)
            {
                scrollView.verticalScroller.value += UnhideElement(selectedElement as VisualElement);
            }
        }

        internal float UnhideElement(VisualElement element)
        {
            var viewportOffset = scrollView.contentViewport.worldBound.y;
            var viewportHeight = scrollView.contentViewport.worldBound.height;

            var vieportTop = viewportOffset + scrollView.scrollOffset.y;
            var vieportBot = viewportHeight + vieportTop;

            var elementPosition = element.worldBound.y + scrollView.scrollOffset.y;

            const float lineHeight = 20f;
            //scroll up (dir => 1)
            if (elementPosition <= vieportTop)
                return elementPosition - vieportTop;
            //scroll down (dir => -1)
            //need to add line height because object position starts at TOP left corner
            else if ((elementPosition + lineHeight) >= vieportBot)
                return (elementPosition + lineHeight) - vieportBot;

            return 0;
        }

        private void PopulateMenu(List<SearcherData> data)
        {
            toDelete = new List<ISearcherSelectable>(onlyLeaves);

            //update existing searcher
            if (data != null)
            {
                foreach (var item in data)
                {
                    foreach (SearcherEndNode leaf in item.searcherEndNodes)
                    {
                        //path does not contain leaves, so need to add them without changing original path list
                        var pathCopy = new List<(string segment, UnityEngine.Object relativeObj)>(leaf.path);

                        pathCopy.Add((leaf.name, leaf.Reference?.Target));
                        leaf.ID = SearcherUtils.PathToID(pathCopy, pathCopy.Count - 1);

                        var exstingElement = allElements.Find(x => x.UniqueID == leaf.ID);
                        if (exstingElement == null)
                            treeView.Add(leaf, 0);
                        else
                            toDelete.Remove(exstingElement);
                    }
                }
            }

            //deleted removed data visuals
            foreach (var delete in toDelete)
            {
                var leaf = delete as SearcherLeafView;
                if (leaf != null)
                {
                    leaf.Remove((id) => { allElements.Remove(allElements.Find(x => x.UniqueID == id)); });
                    allElements.Remove(delete);
                    onlyLeaves.Remove(delete);
                }
            }

            //update order index of all elements
            int index = 0;
            treeView.IterateDown((branch) =>
            {
                AssignOrderIndex(ref index, branch);
            });

            if (dragView != null)
                SetupDrag(dragView);
        }

        private void AssignOrderIndex(ref int index, ISearcherSelectable searcherSelectable)
        {
            searcherSelectable.OrderIndex = index;
            index++;
        }
    }
}
