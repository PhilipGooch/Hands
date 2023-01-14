using NBG.Core;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class AddPortsView : VisualElement
    {
        private const string k_UXMLGUID = "0d00a67ecdc2e91459e0aed9b0340397";
        public new class UxmlFactory : UxmlFactory<AddPortsView, VisualElement.UxmlTraits> { }

        private Button plusButton;
        private Button minusButton;

        public AddPortsView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            plusButton = this.Q<Button>("plusButton");
            minusButton = this.Q<Button>("minusButton");
        }

        internal void SetRemovePortsButtonVisibility(bool visible)
        {
            minusButton.SetVisibility(visible);
        }

        internal void RegisterButtonActions(Action plusAction, Action minusAction)
        {
            plusButton.clickable.clicked += plusAction;
            minusButton.clickable.clicked += minusAction;
        }
    }
}