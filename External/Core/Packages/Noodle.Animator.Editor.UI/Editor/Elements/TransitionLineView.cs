using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NBG.Core;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Line which displays that a transition between keys exists
    /// </summary>
    public class TransitionLineView : VisualElement
    {
        private const string k_UXMLGUID = "dda428c2d4e43e44aaec9a8ca9e5ff6d";
        public new class UxmlFactory : UxmlFactory<TransitionLineView, UxmlTraits> { }

        internal const float defaultRight = NoodleAnimatorParameters.keyWidth - NoodleAnimatorParameters.additionalTransitionLineWidth / 2;
        bool darkMode;

        public TransitionLineView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            usageHints = UsageHints.DynamicTransform;
            pickingMode = PickingMode.Ignore;
           
            style.right = defaultRight;
            style.top = NoodleAnimatorParameters.keyHeight / 2 - NoodleAnimatorParameters.transitionLineHeight / 2;

            darkMode = EditorGUIUtility.isProSkin;
        }

        internal void SetData(int columnStart, int columnEnd, bool isLastNLooped, int timelineEnd)
        {
            this.SetVisibility(true);
            var startX = TimelineUtils.GetXFromColumnID(columnStart) + NoodleAnimatorParameters.keyWidth;
            var endX = TimelineUtils.GetXFromColumnID(isLastNLooped ? Mathf.Max(timelineEnd, columnEnd) : columnEnd);
            var width = endX - startX + NoodleAnimatorParameters.additionalTransitionLineWidth;

            if (isLastNLooped)
                style.right = TimelineUtils.GetXFromColumnID(columnEnd) + NoodleAnimatorParameters.keyWidth - endX;
            else
                style.right = defaultRight;

            style.width = width;
        }

        public void SelectVisuals()
        {
            style.backgroundColor = NoodleAnimatorParameters.GetSelectedColor(darkMode);
        }

        public void DeselectVisuals()
        {
            style.backgroundColor = NoodleAnimatorParameters.GetNotSelectedColor(darkMode);
        }
    }
}
