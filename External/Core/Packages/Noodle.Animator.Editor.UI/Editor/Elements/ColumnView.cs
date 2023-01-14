using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Visual representation of a column (frame)
    /// </summary>
    public class ColumnView : VisualElement
    {
        private const string k_UXMLGUID = "272f02f603b586049a166da0ab77c0e3";
        public new class UxmlFactory : UxmlFactory<ColumnView, UxmlTraits> { }

        public ColumnView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            style.width = NoodleAnimatorParameters.columnWidth;
            style.minWidth = NoodleAnimatorParameters.columnWidth;

            pickingMode = PickingMode.Ignore;
        }

        internal void SetData(int columnId, bool isBeyondAnimationLength)
        {
            style.left = columnId * NoodleAnimatorParameters.columnWidth;

            if (!isBeyondAnimationLength)
                style.backgroundColor = columnId % 2 == 0 ? new Color(0.45f, 0.45f, 0.45f, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            else
                style.backgroundColor = columnId % 2 == 0 ? new Color(0.90f, 0.45f, 0.45f, 0.5f) : new Color(0.90f, 0.5f, 0.5f, 0.5f);
        }
    }
}
