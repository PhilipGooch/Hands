using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Cursor - vertical line which represents which animation frame is selected
    /// </summary>
    public class CursorView : VisualElement
    {
        private const string k_UXMLGUID = "40c2a5d17ccec954b81dd39773d5fd25";
        public new class UxmlFactory : UxmlFactory<CursorView, UxmlTraits> { }

        public CursorView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            usageHints = UsageHints.DynamicTransform;
            pickingMode = PickingMode.Ignore;
            style.width = NoodleAnimatorParameters.cursorWidth;
            style.top = NoodleAnimatorParameters.startRowsAt * NoodleAnimatorParameters.rowHeight;
        }

        internal void SetColor(Color color)
        {
            style.backgroundColor = color;
        }

        internal void SetData(int rowCount, int frame)
        {
            style.height = (rowCount - NoodleAnimatorParameters.startRowsAt) * NoodleAnimatorParameters.rowHeight;
            style.left = NoodleAnimatorParameters.columnWidth * frame + NoodleAnimatorParameters.columnWidth / 2 - NoodleAnimatorParameters.cursorWidth / 2;
        }

        internal void UpdateTopPosition(float top)
        {
            style.top = top;
        }
    }
}
