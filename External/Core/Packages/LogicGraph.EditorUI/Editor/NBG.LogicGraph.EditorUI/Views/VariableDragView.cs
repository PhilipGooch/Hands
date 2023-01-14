using NBG.Core;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class VariableDragView : VisualElement
    {
        private const string k_UXMLGUID = "5262fe6359971b843b69c68413c53495";
        public new class UxmlFactory : UxmlFactory<VariableDragView, VisualElement.UxmlTraits> { }

        internal event Action<VariableDragView, Vector3, SerializableGuid> onVariableDraggingStopped;
        internal event Action<VariableDragView, Vector3, ClickContext> onNodeDraggingStopped;

        private VisualElement dragStartElement;
        internal VisualElement DragStartElement => dragStartElement;

        public VariableDragView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);
        }

        internal void DragStarted(VisualElement dragStartElement)
        {
            this.dragStartElement = dragStartElement;
        }

        internal void DraggingStopped(Vector3 mousePosition, SerializableGuid id)
        {
            onVariableDraggingStopped?.Invoke(this, mousePosition, id);
            dragStartElement = null;
        }

        internal void DraggingStopped(Vector3 mousePosition, ClickContext id)
        {
            onNodeDraggingStopped?.Invoke(this, mousePosition, id);
            dragStartElement = null;
        }
    }
}