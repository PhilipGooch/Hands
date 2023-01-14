using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NBG.Core;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Displays frame numbers above TimelineView
    /// </summary>
    public class UtilityBarView : VisualElement
    {
        private const string k_UXMLGUID = "9783619a91a134548aa64cf12f40480a";
        public new class UxmlFactory : UxmlFactory<UtilityBarView, UxmlTraits> { }

        private CursorDragManipuliator cursorDragManipuliator;

        private readonly List<TextElement> frameNumViews = new List<TextElement>();

        int columnCount;
        int timelineLength;
        int negativeColumnCount;

        public TimelineView TimelineView
        {
            set { cursorDragManipuliator.TimelineView = value; }
        }

        public UtilityBarView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            usageHints = UsageHints.DynamicTransform;

            cursorDragManipuliator = new CursorDragManipuliator();
            this.AddManipulator(cursorDragManipuliator);
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            cursorDragManipuliator.SetNewDataFile(data);
        }

        internal void SetData(int columnCount, int negativeColumnCount, int timelineLength)
        {
            this.columnCount = columnCount;
            this.timelineLength = timelineLength;
            this.negativeColumnCount = negativeColumnCount;

            style.height = NoodleAnimatorParameters.rowHeight;
            style.minWidth = columnCount * NoodleAnimatorParameters.columnWidth;

            CreateFrameNumberViews();
        }

        private void CreateFrameNumberViews()
        {
            for (int k = 0; k < negativeColumnCount; k++)
            {
                var frameNumView = GetFrameNumView(k, k - negativeColumnCount);
                frameNumView.style.backgroundColor = new Color(256 / 256, 82 / 256, 82 / 256, 0.5f);
            }

            for (int i = negativeColumnCount; i < timelineLength + negativeColumnCount; i++)
            {
                var frameNumView = GetFrameNumView(i, i - negativeColumnCount);
                frameNumView.style.backgroundColor = new Color(0, 0, 0, 0);
            }

            for (int j = timelineLength + negativeColumnCount; j < columnCount; j++)
            {
                var frameNumView = GetFrameNumView(j, j - negativeColumnCount);
                frameNumView.style.backgroundColor = new Color(256 / 256, 82 / 256, 82 / 256, 0.5f);
            }
        }

        //TODO? remake as unique visual element?
        private TextElement GetFrameNumView(int id, int displayNumber)
        {
            TextElement frameNum;

            if (frameNumViews.Count > id)
            {
                frameNum = frameNumViews[id];
            }
            else
            {
                frameNum = new TextElement();
                frameNum.AddToClassList("timeline-top-text-view");

                frameNum.style.width = NoodleAnimatorParameters.columnWidth;
                frameNum.style.height = NoodleAnimatorParameters.rowHeight;
                frameNum.style.left = id * NoodleAnimatorParameters.columnWidth;
                frameNum.pickingMode = PickingMode.Ignore;

                frameNumViews.Add(frameNum);
                Add(frameNum);
            }

            frameNum.text = displayNumber.ToString();

            if (!frameNum.visible)
                frameNum.SetVisibility(true);

            return frameNum;
        }

        internal void DisableUnused()
        {
            for (int i = columnCount; i < frameNumViews.Count; i++)
            {
                if (frameNumViews[i].visible)
                    frameNumViews[i].SetVisibility(false);
            }
        }
    }
}
