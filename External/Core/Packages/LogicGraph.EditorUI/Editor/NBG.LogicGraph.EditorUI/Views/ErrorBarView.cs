using NBG.Core;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    /// <summary>
    /// Node error message
    /// </summary>
    public class ErrorBarView : VisualElement
    {
        private const string k_UXMLGUID = "d9c0dfa362fa84847968350d85b7c54e";
        public new class UxmlFactory : UxmlFactory<ErrorBarView, VisualElement.UxmlTraits> { }

        private Button actionButton;
        private VisualElement icon;

        public ErrorBarView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            icon = this.Q<VisualElement>("icon");
            icon.style.backgroundImage = EditorGUIUtility.IconContent("console.infoicon").image as Texture2D;
            actionButton = this.Q<Button>();
        }

        internal void RegisterButtonAction(Action onButtonClick)
        {
            actionButton.clickable.clicked += onButtonClick;
        }

        internal void SetVisibility(bool isVisible, bool isButtonVisible)
        {
            this.SetVisibility(isVisible);
            actionButton.SetVisibility(isButtonVisible);
        }
    }
}