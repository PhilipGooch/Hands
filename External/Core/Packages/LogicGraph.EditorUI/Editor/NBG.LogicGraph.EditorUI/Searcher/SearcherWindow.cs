using NBG.LogicGraph.EditorInterface;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Context of dragged link origin
    /// </summary>
    internal struct ClickContext
    {
        public NodeEntry entry;

        public INodeObjectReference reference;

        public ClickContext(NodeEntry entry, INodeObjectReference reference)
        {
            this.entry = entry;
            this.reference = reference;
        }
    }

    /// <summary>
    /// Node creation menu window
    /// </summary>
    internal class SearcherWindow : EditorWindow
    {
        private const string k_NodeGraphUXMLGUID = "c733df85d9b44e74281ca8ad39bf46c0";

        private VisualElement root;
        private VisualElement resizeDragger;

        private Action<ClickContext> onNodeClick;

        private List<SearcherData> data;
        private SearcherContainerView searcherView;

        public static void Show(EditorWindow host, List<SearcherData> data, Vector2 clickPosition, Action<ClickContext> onNodeClick)
        {
            var window = CreateInstance<SearcherWindow>();
            var pos = clickPosition + host.position.position;

            window.position = new Rect(pos, EditorStateManager.SearcherSize);
            window.onNodeClick += onNodeClick;
            window.onNodeClick += (_) => window.Close();
            window.data = data;

            window.ShowPopup();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_NodeGraphUXMLGUID));
            visualTree.CloneTree(root);

            searcherView = root.Q<SearcherContainerView>();
            searcherView.Initialize(onNodeClick, onNodeClick, SearcherType.Temporary, null, true);
            searcherView.SetNewData(data);

            resizeDragger = root.Q<VisualElement>("resizeDragger");
            resizeDragger.AddManipulator(new WindowResizeManipulator(OnResizerMoved));
        }

        private void OnResizerMoved(Vector2 size)
        {
            position = new Rect(position.position, size);
            EditorStateManager.SearcherSize = size;
        }

        private void OnLostFocus()
        {
            Close();
        }
    }

    /// <summary>
    /// Selectable searcher elements - foldouts and leafs
    /// </summary>
    interface ISearcherSelectable
    {
        bool IsVissible { get; }
        string UniqueID { get; }
        bool Selected { get; }
        bool Hovered { get; }
        int OrderIndex { get; set; }

        void Select();

        void Deselect();

        void OnHoverStart();

        void OnHoverEnd();
    }
}