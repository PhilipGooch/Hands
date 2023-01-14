using NBG.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class SettingsView : VisualElement
    {
        private const string k_UXMLGUID = "59c69ea9d904d224eab23ba1b1d99b16";
        public new class UxmlFactory : UxmlFactory<SettingsView, VisualElement.UxmlTraits> { }

        VisualElement container;

        SearchersInspector searchersInspector;
        LogicGraphView nodeGraphView;
        LogicGraphPlayerEditor activeGraph;
        ScrollView searcherVisibleTypesScrollView;

        Toggle minimapToggle;
        Toggle toggleHierarchySearcher;
        Toggle toggleBuiltInSearcher;
        Dictionary<string, Toggle> usedTypes = new Dictionary<string, Toggle>();

        public SettingsView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            container = this.Q("rootContainer");
            minimapToggle = this.Q<Toggle>("toggleMinimap");
            toggleHierarchySearcher = this.Q<Toggle>("toggleHierarchySearcher");
            toggleBuiltInSearcher = this.Q<Toggle>("toggleBuiltInSearcher");
            searcherVisibleTypesScrollView = this.Q<ScrollView>("searcherVisibleTypesScrollView");
            searcherVisibleTypesScrollView.style.maxHeight = 200;

            minimapToggle.RegisterCallback<ChangeEvent<bool>>(OnMinimapToggle);
            minimapToggle.SetValueWithoutNotify(EditorPrefsManager.MinimapVisible);

            toggleHierarchySearcher.RegisterCallback<ChangeEvent<bool>>(OnHierarchySearcherToggle);
            toggleHierarchySearcher.SetValueWithoutNotify(EditorPrefsManager.HierarchySearcherVisible);

            toggleBuiltInSearcher.RegisterCallback<ChangeEvent<bool>>(OnBuiltInSearcherToggle);
            toggleBuiltInSearcher.SetValueWithoutNotify(EditorPrefsManager.BuiltinSearcherVisible);

            this.SetVisibility(EditorStateManager.SettingsViewVisibility);
        }

        internal void Initialize(SearchersInspector searchersInspector, LogicGraphView nodeGraphView)
        {
            this.searchersInspector = searchersInspector;
            this.nodeGraphView = nodeGraphView;

        }

        internal void SetNewActiveGraph(LogicGraphPlayerEditor activeGraph)
        {
            this.activeGraph = activeGraph;
            ClearAll();
            PopulateVisibleTypes();
        }

        internal void Update()
        {
            PopulateVisibleTypes();
        }

        void PopulateVisibleTypes()
        {
            var toDelete = usedTypes.Keys.ToList();
            foreach (var item in activeGraph.NodesController.NodeTypes)
            {
                if (item.reference != null && item.bindingType != null)
                {
                    var key = item.bindingType.FullName;
                    if (!usedTypes.ContainsKey(key))
                    {
                        Toggle toggle = new Toggle();
                        toggle.text = item.bindingType.Name;
                        toggle.SetValueWithoutNotify(EditorStateManager.GetSearcherTypeVisibility(key));
                        toggle.RegisterValueChangedCallback((evt) => OnSearcherTypeVisibilityStateChanged(evt, item.bindingType));
                        searcherVisibleTypesScrollView.contentContainer.Add(toggle);
                        usedTypes.Add(key, toggle);
                        toDelete.Remove(key);
                    }
                    else
                    {
                        toDelete.Remove(key);
                    }
                }
            }

            foreach (var item in toDelete)
            {
                searcherVisibleTypesScrollView.contentContainer.Remove(usedTypes[item]);
                usedTypes.Remove(item);
            }
        }

        private void OnSearcherTypeVisibilityStateChanged(ChangeEvent<bool> evt, Type bindingType)
        {
            EditorStateManager.SetSearcherTypeVisibility(bindingType.FullName, evt.newValue);
            activeGraph.StateChanged();
        }

        private void OnBuiltInSearcherToggle(ChangeEvent<bool> evt)
        {
            EditorPrefsManager.BuiltinSearcherVisible = evt.newValue;
            searchersInspector.UpdateVisibility();
        }

        private void OnHierarchySearcherToggle(ChangeEvent<bool> evt)
        {
            EditorPrefsManager.HierarchySearcherVisible = evt.newValue;
            searchersInspector.UpdateVisibility();
        }

        private void OnMinimapToggle(ChangeEvent<bool> evt)
        {
            EditorPrefsManager.MinimapVisible = evt.newValue;
            nodeGraphView.SetMinimapVisibility(evt.newValue);
        }

        internal void ClearAll()
        {
            searcherVisibleTypesScrollView.contentContainer.Clear();
            usedTypes.Clear();
        }
    }
}