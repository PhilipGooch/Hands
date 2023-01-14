using UnityEditor;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Displays an icon where current selection top left corner is
    /// </summary>
    public class PivotView : VisualElement
    {
        private const string k_UXMLGUID = "afee725c2f124e74aa474431d3fc863c";
        public new class UxmlFactory : UxmlFactory<PivotView, UxmlTraits> { }

        public PivotView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            style.width = NoodleAnimatorParameters.keyWidth;
            style.height = NoodleAnimatorParameters.keyWidth;

            pickingMode = PickingMode.Ignore;
        }

        internal void SetData(int rowId, int columnId)
        {
            var x = TimelineUtils.GetXFromColumnID(columnId);
            var y = TimelineUtils.GetYFromRowID(rowId);

            style.left = x;
            style.top = y;
        }

        internal void Show()
        {
            if (!visible)
                visible = true;
        }

        internal void Hide()
        {
            if (visible)
                visible = false;
        }
    }
}

