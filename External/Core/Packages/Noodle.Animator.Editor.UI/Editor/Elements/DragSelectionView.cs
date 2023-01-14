using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NBG.Core;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Drag selection box visual
    /// </summary>
    public class DragSelectionView : VisualElement
    {
        private const string k_UXMLGUID = "8697664a6fb2dee4cb36ecfdf8c33d1e";
        public new class UxmlFactory : UxmlFactory<DragSelectionView, UxmlTraits> { }

        private Vector2 startPos;

        public DragSelectionView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            this.pickingMode = PickingMode.Ignore;
        }

        public void StartDrag(Vector2 mousePos)
        {
            this.SetVisibility(true);
            transform.position = startPos = mousePos - parent.worldBound.position;
        }

        public void EndDrag()
        {
            this.SetVisibility(false);
        }

        public void Drag(Vector2 mousePos)
        {
            var adjustedPos = mousePos - parent.worldBound.position;

            var size = adjustedPos - startPos;
            var absSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));

            if (size.x < 0 && size.y < 0)//left up
                transform.position = adjustedPos;
            else if (size.x > 0 && size.y < 0) // right up
                transform.position = new Vector2(startPos.x, adjustedPos.y);
            else if (size.x < 0 && size.y > 0) // right down
                transform.position = new Vector2(adjustedPos.x, startPos.y);
            else //right down 
                transform.position = startPos;

            style.width = absSize.x;
            style.height = absSize.y;
        }
    }
}
