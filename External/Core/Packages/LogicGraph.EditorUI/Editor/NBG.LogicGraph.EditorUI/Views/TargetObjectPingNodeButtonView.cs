using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class TargetObjectPingNodeButtonView : VisualElement
    {
        private const string k_UXMLGUID = "115abdc22086c0a4db84cf2387e96f1c";
        public new class UxmlFactory : UxmlFactory<PortsDividerView, VisualElement.UxmlTraits> { }

        Button button;

        public TargetObjectPingNodeButtonView(Action clickAction)
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            button = this.Q<Button>();
            button.clickable.clicked += clickAction;
        }

        internal void SetBackgroundPicture(Texture2D picture)
        {
            button.style.backgroundImage = picture;
        }
    }
}
