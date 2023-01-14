using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NBG.Core;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Displays animation data (keys, tracks, columns, transition lines, etc) and allows editing of keys
    /// </summary>
    public class TimelineView : VisualElement
    {
        private const string k_UXMLGUID = "fda2c7f8182900346803406079199456";
        public new class UxmlFactory : UxmlFactory<TimelineView, UxmlTraits> { }

        private readonly List<ColumnView> columnViews = new List<ColumnView>();
        private readonly List<KeyView> keyViews = new List<KeyView>();
        private readonly List<RowView> rowViews = new List<RowView>();

        private readonly DragSelectionView dragSelectionView;

        private readonly ScrollView elementsScrollView;
        //visible checks doesnt work since its disabled using uss
        internal bool HorizontalScrollBarVisible => !elementsScrollView.horizontalScroller.ClassListContains("unity-disabled");

        private readonly VisualElement topBarParent;
        private readonly VisualElement horizontalLinesParent;
        private readonly VisualElement columnsParent;
        private readonly VisualElement keysParent;

        private CursorView playModeCursor;
        private CursorView editModeCursor;

        private PivotView pivotView;
        private UtilityBarView utilityBarView;

        private AnimatorWindowAnimationData data;

        private int timelineLength = 40;

        private int rowCount = 40;
        private int keysCount = 0;

        private int rowHeight = 20;
        private int columnWidth = 30;

        internal event Action<float> onScroll;

        private readonly TimelineMouseManipulator mouseManipuliator;

        int lastWidth;
        int lastHeight;

        public TimelineView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            topBarParent = this.Q<VisualElement>("topBarParent");
            horizontalLinesParent = this.Q<VisualElement>("horizontalLinesParent");
            columnsParent = this.Q<VisualElement>("columnsParent");
            keysParent = this.Q<VisualElement>("keysParent");

            horizontalLinesParent.Clear();
            columnsParent.Clear();
            keysParent.Clear();

            utilityBarView = topBarParent.Q<UtilityBarView>();
            utilityBarView.TimelineView = this;

            editModeCursor = this.Q<CursorView>("editModeCursor");
            playModeCursor = this.Q<CursorView>("playModeCursor");

            editModeCursor.SetColor(Color.black);
            playModeCursor.SetColor(Color.green);

            elementsScrollView = this.Q<ScrollView>();
            elementsScrollView.contentContainer.AddToClassList("timeline-scroll-content");

            dragSelectionView = this.Q<DragSelectionView>("dragSelectionView");

            mouseManipuliator = new TimelineMouseManipulator(dragSelectionView, this);
            this.AddManipulator(mouseManipuliator);

            elementsScrollView.verticalScroller.valueChanged += OnScroll;

            UpdateEditModeCursor();
            UpdatePlayModeCursor();
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            //first time getting data 
            if (this.data == null)
                AddToHorizontalScrollValue(AnimatorWindowAnimationData.TotalNegativeColumnCount * NoodleAnimatorParameters.columnWidth);

            this.data = data;

            mouseManipuliator.SetNewDataFile(data);

            foreach (var key in keyViews)
            {
                key.SetNewDataFile(data);
            }

            foreach (var rowView in rowViews)
            {
                rowView.SetNewDataFile(data);
            }

            utilityBarView.SetNewDataFile(data);

            Update();
        }

        internal void Update()
        {
            if (data != null)
            {
                timelineLength = data.FramesCount;
                rowCount = data.RowsCount;
                keysCount = data.KeyCount;
            }

            rowHeight = NoodleAnimatorParameters.rowHeight;
            columnWidth = NoodleAnimatorParameters.columnWidth;

            UpdateScrollerContainer();

            UpdateColumnViews();
            UpdateRowViews();
            UpdateKeyViews();
            UpdatePivotView();
            UpdateEditModeCursor();
            UpdateUtilityBarView();

            DisableUnused();
        }

        void UpdateScrollerContainer()
        {
            var width = AnimatorWindowAnimationData.TotalColumnCount * columnWidth;
            var height = rowCount * rowHeight;

            if (width != lastWidth)
            {
                elementsScrollView.contentContainer.style.maxWidth = width;
                elementsScrollView.contentContainer.style.width = width;
            }
            if (height != lastHeight)
            {
                elementsScrollView.contentContainer.style.maxHeight = height;
                elementsScrollView.contentContainer.style.height = height;
            }

            lastHeight = height;
            lastWidth = width;
        }

        internal void UpdatePlayMode()
        {
            UpdatePlayModeCursor();
        }

        #region Scroll
        internal void OnScroll(float totalValue)
        {
            utilityBarView.style.top = -elementsScrollView.contentContainer.worldBound.y + worldBound.y;
            onScroll?.Invoke(totalValue);
        }

        internal void ScrollVertical(float toAdd)
        {
            utilityBarView.style.top = -elementsScrollView.contentContainer.worldBound.y + worldBound.y;

            AddToVerticalScrollValue(toAdd);
            onScroll?.Invoke(elementsScrollView.verticalScroller.value);
        }

        internal void SetVerticalScrollValue(float value)
        {
            elementsScrollView.verticalScroller.value = value;
        }

        internal void AddToVerticalScrollValue(float value)
        {
            elementsScrollView.verticalScroller.value += value;
        }

        internal void AddToHorizontalScrollValue(float value)
        {
            elementsScrollView.horizontalScroller.value += value;
        }

        internal void ArrowKeysHorizontalScroll(float change)
        {
            if (IsColumnHidden(data.noodleAnimatorData.currentFrame) != 0)
            {
                AddToHorizontalScrollValue(change * NoodleAnimatorParameters.columnWidth);
            }
        }

        #endregion

        private void UpdateColumnViews()
        {
            for (int i = 0; i < AnimatorWindowAnimationData.TotalColumnCount; i++)
            {
                GetGraphColumnView(i);
            }
        }

        private void UpdateRowViews()
        {
            for (int i = 0; i < rowCount; i++)
            {
                GetRowView(i);
            }
        }

        private void UpdatePivotView()
        {
            if (pivotView == null)
            {
                pivotView = new PivotView();
            }

            if (data.noodleAnimatorData.selection.Count == 0)
            {
                if (pivotView.visible)
                {
                    pivotView.Hide();
                    pivotView.RemoveFromHierarchy();
                }
                return;
            }

            pivotView.Show();
            int rowId = data.rowToTrackMap.FirstOrDefault(x => x.Value == data.noodleAnimatorData.selectionPivot.y).Key;
            keysParent.Add(pivotView);
            pivotView.SetData(rowId, TimelineUtils.TimeToColumn(data.noodleAnimatorData.selectionPivot.x));
        }

        #region Keys

        private void UpdateKeyViews()
        {
            if (data == null)
                return;

            int keyId = 0;
            int rowId = NoodleAnimatorParameters.startRowsAt;

            foreach (var group in data.groupedTracks.Where(x => !x.Value.FullyHidden))
            {
                foreach (var track in group.Value.tracks)
                {
                    if (!track.hidden)
                    {
                        var frames = track.animationTrack.frames;

                        rowId++;

                        for (int j = 0; j < frames.Count; j++)
                        {
                            var column = TimelineUtils.TimeToColumn(frames[j].time);

                            if (column >= 0)
                            {
                                CreateKeyView(
                                    column,
                                    rowId,
                                    keyId,
                                    data.rowToTrackMap[rowId],
                                    j + 1 == frames.Count,
                                    frames[j].easeType,
                                    j > 0 ? TimelineUtils.TimeToColumn(frames[j - 1].time) : -1
                                );
                                keyId++;
                            }
                        }
                    }
                }
                rowId++;
            }
        }

        private void CreateKeyView(int columnID, int rowID, int totalId, int trackId, bool isLast, EasingType easingType, int prevKeyTime)
        {
            var x = TimelineUtils.GetXFromColumnID(columnID);
            var y = TimelineUtils.GetYFromRowID(rowID);

            var keyView = GetKeyView(totalId);
            keyView.SetPosition(x, y);
            keyView.SetData(
                new Vector2Int(columnID, trackId),
                prevKeyTime,
                isLast,
                easingType
                );

        }

        private void UpdateUtilityBarView()
        {
            utilityBarView.SetData(AnimatorWindowAnimationData.TotalColumnCount, AnimatorWindowAnimationData.TotalNegativeColumnCount, timelineLength);
        }
        #endregion

        #region Cursors
        private void UpdatePlayModeCursor()
        {
            var column = TimelineUtils.TimeToColumn(data != null ? data.noodleAnimatorData.playCurrentFrame : 0);
            UpdateCursorView(ref playModeCursor, column);
        }

        private void UpdateEditModeCursor()
        {
            var column = TimelineUtils.TimeToColumn(data != null ? data.noodleAnimatorData.currentFrame : 0);
            UpdateCursorView(ref editModeCursor, column);
        }

        private void UpdateCursorView(ref CursorView cursorView, int position)
        {
            if (!cursorView.visible)
                cursorView.SetVisibility(true);

            cursorView.SetData(rowCount, position);
        }
        #endregion

        #region Pooled timeline elements creation

        private ColumnView GetGraphColumnView(int id)
        {
            ColumnView column;

            if (columnViews.Count > id)
            {
                column = columnViews[id];
            }
            else
            {
                column = new ColumnView();

                columnViews.Add(column);
                columnsParent.Add(column);
            }

            column.SetData(id, id >= timelineLength + AnimatorWindowAnimationData.TotalNegativeColumnCount || id < AnimatorWindowAnimationData.TotalNegativeColumnCount);
            if (!column.visible)
                column.SetVisibility(true);

            return column;
        }

        private KeyView GetKeyView(int id)
        {
            KeyView keyView;

            if (keyViews.Count > id)
            {
                keyView = keyViews[id];
            }
            else
            {
                keyView = new KeyView();
                keyView.SetNewDataFile(data);
                keyViews.Add(keyView);
            }

            keyView.SetVisibilityState(true);
            
            if (!keysParent.Contains(keyView))
                keysParent.Add(keyView);

            return keyView;
        }

        private RowView GetRowView(int id)
        {
            RowView rowView;
            if (rowViews.Count > id)
            {
                rowView = rowViews[id];
            }
            else
            {
                rowView = new RowView();
                rowView.SetNewDataFile(data);
                rowViews.Add(rowView);
                horizontalLinesParent.Add(rowView);
            }

            if (!rowView.visible)
                rowView.SetVisibility(true);

            rowView.SetData(id, AnimatorWindowAnimationData.TotalColumnCount);

            return rowView;
        }

        #endregion

        private void DisableUnused()
        {
            for (int i = AnimatorWindowAnimationData.TotalColumnCount; i < columnViews.Count; i++)
            {
                if (columnViews[i].visible)
                    columnViews[i].SetVisibility(false);
            }

            for (int i = keysCount; i < keyViews.Count; i++)
            {
                if (keyViews[i].visible)
                    keyViews[i].SetVisibilityState(false);
            }

            for (int i = rowCount; i < rowViews.Count; i++)
            {
                if (rowViews[i].visible)
                    rowViews[i].SetVisibility(false);
            }

            utilityBarView.DisableUnused();

            if ((data.noodleAnimatorData.playbackMode != PlayBackMode.Play || !Application.isPlaying) && playModeCursor.visible)
                playModeCursor.SetVisibility(false);
        }

        //check if track is hidden and get scroll value to unhide
        internal (int direction, int amountToUnhide) IsTrackHidden(int trackId)
        {
            int rowId = data.rowToTrackMap.FirstOrDefault(x => x.Value == trackId).Key;

            var adjustedUp = rowViews[rowId].layout.position.y - elementsScrollView.scrollOffset.y;
            var adjustedDown = adjustedUp + NoodleAnimatorParameters.rowHeight;

            if (adjustedUp <= elementsScrollView.worldBound.position.y)
            {
                //scroll up
                return (1, (int)(adjustedUp - elementsScrollView.worldBound.position.y));
            }
            else if (adjustedDown >= elementsScrollView.worldBound.height - elementsScrollView.worldBound.position.y)
            {
                //scroll down
                return (-1, (int)(adjustedDown - (elementsScrollView.worldBound.height - elementsScrollView.worldBound.position.y)));
            }
            return (0, 0);
        }

        internal int IsColumnHidden(int columnId)
        {
            var posFromLeft = columnId * NoodleAnimatorParameters.columnWidth;
            var posFromRight = posFromLeft + NoodleAnimatorParameters.columnWidth;

            if (posFromRight >= elementsScrollView.contentViewport.worldBound.width + elementsScrollView.scrollOffset.x)
            {
                return 1;
            }
            else if (posFromLeft <= elementsScrollView.scrollOffset.x)
            {
                return -1;
            }

            return 0;
        }

        internal void ScrollTimelineHorizontalyIfColumnNearEdge(Vector2Int coords, int delta)
        {
            if (delta > 0)
            {
                //move right
                if (IsColumnHidden(coords.x) > 0)
                {
                    AddToHorizontalScrollValue(NoodleAnimatorParameters.columnWidth);
                }
            }
            else
            {
                //move left
                if (IsColumnHidden(coords.x) < 0)
                {
                    AddToHorizontalScrollValue(-NoodleAnimatorParameters.columnWidth);
                }
            }
        }

        internal Vector2 GetLocalMousePosition(Vector2 mousePos)
        {
            return mousePos - worldBound.position + elementsScrollView.scrollOffset;
        }

        internal Vector2Int GetCoordinatesFromWorldMouse(Vector2 mousePos)
        {
            Vector2 localPos = GetLocalMousePosition(mousePos);

            var row = (int)localPos.y / rowHeight;
            var column = (int)localPos.x / columnWidth;

            if (data.rowToTrackMap.ContainsKey(row))
            {
                return new Vector2Int(column, data.rowToTrackMap[row]);
            }
            //no row found, but column always exists
            else
            {
                return new Vector2Int(column, -1);
            }
        }
    }

    internal static class TimelineUtils
    {
        internal static int GetYFromRowID(int rowID)
        {
            return rowID * NoodleAnimatorParameters.rowHeight + (NoodleAnimatorParameters.rowHeight - NoodleAnimatorParameters.keyHeight) / 2;
        }

        internal static int GetXFromColumnID(int columnID)
        {
            return columnID * NoodleAnimatorParameters.columnWidth + (NoodleAnimatorParameters.columnWidth - NoodleAnimatorParameters.keyWidth) / 2;
        }

        public static int TimeToColumn(int time)
        {
            return time + AnimatorWindowAnimationData.TotalNegativeColumnCount;
        }

        public static int ColumnToTime(int column)
        {
            return column - AnimatorWindowAnimationData.TotalNegativeColumnCount;
        }

        public static Vector2Int CoordsToTimeAndTrack(Vector2Int coords)
        {
            return new Vector2Int(ColumnToTime(coords.x), coords.y);
        }
    }
}
