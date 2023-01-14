using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Manipulator which handles all TimelineView mouse actions as well as context menu:
    /// Single click - select key
    /// Double click - create and select key
    /// Dragging - box selection and automatic scrolling when near edges
    /// Dragging Key - moves selected keys horizontally, while scolling view when near the edge
    /// </summary>
    public class TimelineMouseManipulator : MouseManipulator
    {
        private AnimatorWindowAnimationData data;
        private readonly DragSelectionView dragSelectionView;
        private bool dragSelectionActive;

        private Vector2Int mouseCoordinates;
        private Vector2Int lastMouseCoordinates;
        private Vector2Int mouseCoordinatesDelta => mouseCoordinates - lastMouseCoordinates;

        private Vector2Int mouseTimeAndTrack;

        private Vector2Int oldClickCoordinates;

        private double lastClickTime = 0;

        private Vector2 dragSelectionStartLocalMousePosition;
        private Vector2Int dragSelectionStartTrackAndTime;

        int undoStep;
        private bool keyDragStarted;
        private bool wasDraggingKey;
        private int keyDragStartColumn;

        private ContextualMenuManipulator contextualMenuManipulator;
        private TimelineView timelineView;

        public TimelineMouseManipulator(DragSelectionView dragSelectionView, TimelineView timelineView)
        {
            this.timelineView = timelineView;
            this.dragSelectionView = dragSelectionView;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });

            dragSelectionActive = false;

            oldClickCoordinates = Vector2Int.zero;
        }

        internal void SetNewDataFile(AnimatorWindowAnimationData data)
        {
            this.data = data;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        private void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (dragSelectionActive)
            {
                dragSelectionActive = false;
            }
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (dragSelectionActive || keyDragStarted)
            {
                evt.StopImmediatePropagation();
                return;
            }

            SetClickCoords(evt.mousePosition, timelineView);

            wasDraggingKey = false;

            if (evt.button == 0)
            {
                //lets see if this gives any issues
                if (timelineView != null && evt.shiftKey)
                {
                    StartDragSelection(evt);
                }
                else if (CanStartManipulation(evt))
                {
                    StartKeyDrag();
                }
            }
            else if (evt.button == 1)
            {
                if (mouseTimeAndTrack.y != -1)
                {
                    Select(true);
                    ResetKeyRightClickMenu(data.DeleteSelection, data.InvertSelection, data.SetEasing, data.GetEasing(mouseTimeAndTrack));
                }
            }
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            SetClickCoords(evt.mousePosition, timelineView);

            if (dragSelectionActive)
            {
                UpdateDragSelection(evt);
            }
            else if (keyDragStarted)
            {
                UpdateKeyDrag(evt);
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            SetClickCoords(evt.mousePosition, timelineView);

            bool dragSelectionEnded = false;

            if (dragSelectionActive)
            {
                if (timelineView == null || !CanStopManipulation(evt))
                    return;

                dragSelectionEnded = true;

                EndDragSelection(evt);
            }
            else if (keyDragStarted)
            {
                if (evt.button != 0 || !CanStopManipulation(evt))
                    return;

                EndKeyDrag();
            }

            if (!wasDraggingKey && !dragSelectionEnded)
                HandleSimpleClicks(evt);
        }

        #region Key Drag

        void StartKeyDrag()
        {
            if (data.KeyExists(mouseTimeAndTrack))
            {
                data.refreshSource = RefreshSource.KeyDrag;

                if (!data.IsSelected(mouseTimeAndTrack))
                {
                    Select(false);
                }

                keyDragStartColumn = mouseCoordinates.x;
                data.undoController.RecordUndo();
                undoStep = 0;
                keyDragStarted = true;
                target.CaptureMouse();
            }
        }

        void UpdateKeyDrag(MouseMoveEvent evt)
        {
            var totalOffset = mouseCoordinates.x - keyDragStartColumn;

            if (mouseCoordinatesDelta.x != 0)
            {
                wasDraggingKey = true;

                //exposing undo system index would work more reliably
                if (undoStep > 0)
                {
                    data.refreshSource = RefreshSource.KeyDrag;
                    data.undoController.Undo();
                    undoStep--;
                }
                if (totalOffset != 0)
                {
                    data.refreshSource = RefreshSource.KeyDrag;
                    data.operationsController.MoveSelection(totalOffset);
                    undoStep++;
                }
                data.selectionController.CalculatePivot();
                data.StateChanged();
            }

            if (mouseCoordinatesDelta.x > 0)
            {
                //move right
                timelineView.ScrollTimelineHorizontalyIfColumnNearEdge(new Vector2Int(TimelineUtils.TimeToColumn(data.rightMostSelectedKey) + mouseCoordinatesDelta.x, mouseCoordinates.y), mouseCoordinatesDelta.x);
            }
            else
            {
                //move left
                //its working correctly, but looks different from scrolling to the left because of how scroll view behaves
                timelineView.ScrollTimelineHorizontalyIfColumnNearEdge(new Vector2Int(TimelineUtils.TimeToColumn(data.leftMostSelectedKey) + mouseCoordinatesDelta.x, mouseCoordinates.y), mouseCoordinatesDelta.x);
            }

            evt.StopPropagation();
        }

        void EndKeyDrag()
        {
            keyDragStarted = false;
            target.ReleaseMouse();
        }

        #endregion

        #region Drag Selection

        void StartDragSelection(MouseDownEvent evt)
        {
            dragSelectionView.StartDrag(evt.mousePosition);
            dragSelectionStartLocalMousePosition = evt.localMousePosition;
            dragSelectionStartTrackAndTime = TimelineUtils.CoordsToTimeAndTrack(mouseCoordinates);
            dragSelectionActive = true;
            target.CaptureMouse();
            evt.StopImmediatePropagation();
        }
        void UpdateDragSelection(MouseMoveEvent evt)
        {
            ScrollTimelineIfNearEdge(evt.localMousePosition, dragSelectionStartLocalMousePosition);

            dragSelectionView.Drag(evt.mousePosition);

            evt.StopPropagation();
        }

        void EndDragSelection(MouseUpEvent evt)
        {
            dragSelectionActive = false;
            dragSelectionView.EndDrag();
            DragSelectionDone();
            target.ReleaseMouse();
            evt.StopPropagation();
        }

        internal void DragSelectionDone()
        {
            var tracksCount = data.noodleAnimatorData.animation.allTracks.Count;

            int startRow = Mathf.Clamp(dragSelectionStartTrackAndTime.y, 0, tracksCount);
            int endRow = Mathf.Clamp(mouseTimeAndTrack.y, 0, tracksCount);

            int startColumn = dragSelectionStartTrackAndTime.x;
            int endColumn = mouseTimeAndTrack.x;

            data.ClearSelection();

            if (endColumn < startColumn)
                SwapInt(ref endColumn, ref startColumn);

            if (endRow < startRow)
                SwapInt(ref endRow, ref startRow);

            for (int trackID = startRow; trackID < tracksCount; trackID++)
            {
                var track = data.noodleAnimatorData.animation.allTracks[trackID];
                for (int frameID = 0; frameID < track.frames.Count; frameID++)
                {
                    var key = track.frames[frameID];
                    if (InSelectionRange(startRow, endRow, startColumn, endColumn, key.time, trackID))
                    {
                        data.SelectAdditive(new Vector2Int(key.time, trackID), false);
                    }
                }
            }

            bool InSelectionRange(int rowIdStart, int rowIdEnd, int timeStart, int timeEnd, int keyTime, int keyTrack)
            {
                return rowIdStart <= keyTrack && rowIdEnd >= keyTrack && timeStart <= keyTime && timeEnd >= keyTime;
            }

            data.StateChanged();
            data.clickType = ClickType.Single;
        }

        private void SwapInt(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        #endregion

        internal void HandleSimpleClicks(MouseUpEvent evt)
        {
            if (evt.button == 0)
            {
                var thisClickTime = EditorApplication.timeSinceStartup;
                if (thisClickTime - lastClickTime <= NoodleAnimatorParameters.doubleClickThresh && oldClickCoordinates == mouseCoordinates)
                {
                    if (!data.KeyExists(mouseTimeAndTrack))
                    {
                        data.clickType = ClickType.Double;

                        data.CreateKey(mouseTimeAndTrack);
                    }
                    else
                    {
                        data.clickType = ClickType.Double;

                        HandleKeyClick(evt);
                        data.StateChanged();
                    }
                }
                else
                {
                    data.clickType = ClickType.Single;

                    HandleKeyClick(evt);
                    data.StateChanged();
                }

                lastClickTime = thisClickTime;
            }

            oldClickCoordinates = mouseCoordinates;
        }

        internal void HandleKeyClick(MouseUpEvent evt)
        {
            bool selected = data.IsSelected(mouseTimeAndTrack);

            if (evt.ctrlKey)
            {
                if (!selected)
                    SelectAdditive(true);
                else
                    Deselect(false);
            }
            else
            {
                //single clink not a key == deselect all
                if (data.clickType == ClickType.Single && !data.KeyExists(mouseTimeAndTrack))
                {
                    data.ClearSelection();
                    data.MoveTrackAndCursor(mouseTimeAndTrack);
                }
                else
                    Select(true);
            }
        }

        public void Select(bool moveCursor)
        {
            data.Select(mouseTimeAndTrack, moveCursor);
        }

        public void SelectAdditive(bool moveCursor)
        {
            data.SelectAdditive(mouseTimeAndTrack, moveCursor);
        }

        public void Deselect(bool moveCursor)
        {
            data.Deselect(mouseTimeAndTrack, moveCursor);
        }

        #region TimelineView sroll
        //used for drag select
        private void ScrollTimelineIfNearEdge(Vector2 currPos, Vector2 startPos)
        {
            //move right
            if (startPos.x < currPos.x && currPos.x >= timelineView.worldBound.width - NoodleAnimatorParameters.distanceFromEdgeToScroll && timelineView.HorizontalScrollBarVisible)
            {
                timelineView.AddToHorizontalScrollValue(NoodleAnimatorParameters.columnWidth);
            }

            //move left
            else if (startPos.x > currPos.x && currPos.x <= NoodleAnimatorParameters.distanceFromEdgeToScroll && timelineView.HorizontalScrollBarVisible)
            {
                timelineView.AddToHorizontalScrollValue(-NoodleAnimatorParameters.columnWidth);
            }

            //move up
            if (startPos.y < currPos.y && currPos.y >= timelineView.worldBound.height - NoodleAnimatorParameters.distanceFromEdgeToScroll)
            {
                timelineView.ScrollVertical(NoodleAnimatorParameters.rowHeight);
            }
            //move down
            else if (startPos.y > currPos.y && currPos.y <= NoodleAnimatorParameters.distanceFromEdgeToScroll)
            {
                timelineView.ScrollVertical(-NoodleAnimatorParameters.rowHeight);
            }
        }
        #endregion

        private void SetClickCoords(Vector2 mousePosition, TimelineView timelineView)
        {
            lastMouseCoordinates = mouseCoordinates;
            mouseCoordinates = timelineView.GetCoordinatesFromWorldMouse(mousePosition);

            //mouse is between tracks == use last track id
            if (mouseCoordinates.y == -1)
                mouseCoordinates.y = lastMouseCoordinates.y;

            mouseTimeAndTrack = TimelineUtils.CoordsToTimeAndTrack(mouseCoordinates);
        }

        void ResetKeyRightClickMenu(Action onDeleteKey, Action onInvertKey, Action<EasingType> onSetEasing, EasingType selected)
        {
            timelineView.RemoveManipulator(contextualMenuManipulator);

            contextualMenuManipulator = new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.StopPropagation();

                bool keyExists = data.KeyExists(mouseTimeAndTrack);

                //standard key manipulation
                if (!keyExists)
                {
                    evt.menu.AppendAction("Add key", (_) => data.CreateKey(mouseTimeAndTrack));
                }
                else
                {
                    evt.menu.AppendAction("Delete key", (_) => onDeleteKey?.Invoke());
                    evt.menu.AppendAction("Invert key", (_) => onInvertKey?.Invoke());
                }

                evt.menu.AppendAction("Paste here", (_) => data.PasteHere(mouseTimeAndTrack));
                evt.menu.AppendSeparator();

                //IK - FK options
                var ikFkSyncLabel = NoodleAnimationLayout.GetLabelForIKSync(mouseTimeAndTrack.y);
                if (!string.IsNullOrEmpty(ikFkSyncLabel))
                {
                    bool isPlaymode = Application.isPlaying;
                    var status = isPlaymode ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    var additionalText = isPlaymode ? "" : "(PlayMode only)";
                    evt.menu.AppendAction($"{ikFkSyncLabel} <= FK {additionalText}", (_) => data.SyncIKFK(mouseTimeAndTrack, SyncIKFKMode.FK), status);
                    evt.menu.AppendAction($"{ikFkSyncLabel} <= IK {additionalText}", (_) => data.SyncIKFK(mouseTimeAndTrack, SyncIKFKMode.IK), status);
                    evt.menu.AppendAction($"{ikFkSyncLabel} <= IK Relative {additionalText}", (_) => data.SyncIKFK(mouseTimeAndTrack, SyncIKFKMode.IKRelative), status);
                }

                //easing options
                if (keyExists)
                {
                    foreach (var easingType in Enum.GetValues(typeof(EasingType)))
                    {
                        evt.menu.AppendAction($"Set ease to: {easingType}", (_) => onSetEasing?.Invoke((EasingType)easingType)
                        , (EasingType)easingType == selected ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                        );
                    }
                }
            });

            timelineView.AddManipulator(contextualMenuManipulator);
        }
    }
}
