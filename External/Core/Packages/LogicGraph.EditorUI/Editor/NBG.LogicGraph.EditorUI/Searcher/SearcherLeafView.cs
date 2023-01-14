using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Searcher tree end element, each represents a creatable node
    /// </summary>
    internal class SearcherLeafView : VisualElement, ISearcherSelectable
    {
        private const string k_UXMLGUID = "806042db8ab07fb42ba7532250269ac0";
        public new class UxmlFactory : UxmlFactory<SearcherLeafView, VisualElement.UxmlTraits> { }

        private Label label;
        internal SearcherEndNode Node { get; private set; }

        private VisualElement icon;
        private SearcherItemView parentItemView;
        new private VisualElement contentContainer;
        private ElementDragger<SearcherLeafView> elementDragger;

        internal VisualElement SelectionIndicator { get; private set; }

        internal bool FilteredOut { get; private set; }
        public string UniqueID { get; private set; }
        public bool Selected { get; private set; }
        public bool Hovered { get; private set; }
        public int OrderIndex { get; set; }

        string nameForFiltering;

        public bool IsVissible
        {
            get
            {
                var foldout = parentItemView.ContentContainer as SearcherFoldoutView;
                if (foldout != null)
                {
                    if (!foldout.value)
                    {
                        return false;
                    }
                    else
                    {
                        return foldout.IsVissible;
                    }
                }

                return true;
            }
        }

        public SearcherLeafView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            label = this.Q<Label>();
            icon = this.Q<VisualElement>("icon");
            contentContainer = this.Q<VisualElement>("contentContainer");
            SelectionIndicator = this.Q<VisualElement>("selectionIndicator");

            SelectionIndicator.RegisterCallback<MouseEnterEvent>((_) => OnHoverStart());
            SelectionIndicator.RegisterCallback<MouseLeaveEvent>((_) => OnHoverEnd());

            //needed because of GraphView bug which blocks light theme uss
            if (!EditorGUIUtility.isProSkin)
                label.FixLightSkinLabel();
        }

        internal void Initialize(
            SearcherEndNode node,
            SearcherItemView parent,
            Action<ClickContext> onNodeClick,
            Action<ISearcherSelectable, bool> onSelect,
            int depth)
        {
            this.Node = node;
            focusable = true;

            parentItemView = parent;
            label.text = node.name;
            nameForFiltering = node.name.ReplaceWhitespace("").ToLower();
            UniqueID = node.ID;

            RegisterCallback<MouseUpEvent>((evt) =>
            {
                if (evt.button == 0 && (elementDragger == null || !elementDragger.Active))
                {
                    onNodeClick?.Invoke(new ClickContext(node.entry, node.Reference));
                }
                onSelect?.Invoke(this, false);
            });

            AddIcon(Parameters.functionIcon);

            if (node.Reference != null)
            {
                var referenceTarget = node.Reference.Target;
            }

            RegisterCallback<GeometryChangedEvent>(OnInitialized);
        }

        internal void Remove(Action<string> onBranchColapse)
        {
            parentItemView.RemoveLeaf(this, onBranchColapse);
        }

        private void OnInitialized(GeometryChangedEvent evt)
        {
            SetupSelector();
        }

        internal void SetupDrag(VariableDragView dragView)
        {
            elementDragger = new ElementDragger<SearcherLeafView>(dragView);
            this.AddManipulator(elementDragger);
        }

        private void SetupSelector()
        {
            int depth = (parentItemView.depth + 2);

            style.left = depth * -Parameters.foldoutLayerWidth;

            //needed hanks to unity bug
            if (parentItemView.depth == 0)
                depth++;

            contentContainer.style.left = depth * Parameters.foldoutLayerWidth;
            SelectionIndicator.style.width = depth * Parameters.foldoutLayerWidth + resolvedStyle.width;
        }

        internal void AddIcon(Texture2D image)
        {
            if (icon == null)
                return;

            if (!icon.ClassListContains("searcher-node-group-icon"))
                icon.AddToClassList("searcher-node-group-icon");

            icon.style.backgroundImage = image;
            icon.style.unityBackgroundImageTintColor = Parameters.HighContrastNodeColors[Node.entry.conceptualType];
        }

        internal bool Filter(string query)
        {
            if (nameForFiltering.Contains(query))
            {
                SetFilterOutState(false);
                return true;
            }
            else
            {
                SetFilterOutState(true);
                return false;
            }
        }

        internal void SetFilterOutState(bool state)
        {
            this.SetVisibility(!state);
            FilteredOut = state;
        }

        public void Select()
        {
            Selected = true;
            SelectionIndicator.style.backgroundColor = Parameters.seacherSelectableSelectedColor;
            LogicGraphUtils.Ping(Node.Reference);
        }

        public void Deselect()
        {
            Selected = false;
            if (!Hovered)
                SelectionIndicator.style.backgroundColor = Color.clear;
            else
                SelectionIndicator.style.backgroundColor = Parameters.seacherSelectableHoverOnColor;
        }

        public void OnHoverStart()
        {
            Hovered = true;
            if (!Selected)
            {
                SelectionIndicator.style.backgroundColor = Parameters.seacherSelectableHoverOnColor;
            }
        }

        public void OnHoverEnd()
        {
            Hovered = false;
            if (!Selected)
                SelectionIndicator.style.backgroundColor = Color.clear;
        }
    }
}
