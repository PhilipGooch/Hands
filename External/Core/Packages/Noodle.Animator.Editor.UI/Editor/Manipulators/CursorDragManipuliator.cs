using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Noodles.Animation.Editor.UI
{
    /// <summary>
    /// Manipulator which controls key dragging in the TimelineView
    /// </summary>
    public class CursorDragManipuliator : MouseManipulator
    {
        private bool active;
        private int previousTime;

        private AnimatorWindowAnimationData data;

        private Vector2Int lastMouseCoordinates;
        private Vector2Int mouseCoordinates;

        internal TimelineView TimelineView { get; set; }

        public CursorDragManipuliator()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            active = false;
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
            if (active)
            {
                active = false;
            }
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (data == null)
                return;

            SetClickCoords(e.mousePosition, TimelineView);

            if (active)
            {
                e.StopImmediatePropagation();
                return;
            }
            else
            {
                if (target != null && CanStartManipulation(e))
                {
                    previousTime = TimelineUtils.ColumnToTime(mouseCoordinates.x);
                    data.SetCursorPosition(previousTime);

                    active = true;
                    target.CaptureMouse();
                    e.StopImmediatePropagation();
                }
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (data == null)
                return;

            SetClickCoords(e.mousePosition, TimelineView);

            if (active)
            {
                var time = TimelineUtils.ColumnToTime(mouseCoordinates.x);

                if (time - previousTime != 0)
                {
                    TimelineView.ScrollTimelineHorizontalyIfColumnNearEdge(mouseCoordinates, mouseCoordinates.x - lastMouseCoordinates.x);

                    previousTime = time;
                    data.SetCursorPosition(previousTime);
                }
            }

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (data == null)
                return;

            if (active)
            {
                if (target == null || !CanStopManipulation(e))
                    return;

                active = false;
                target.ReleaseMouse();
            }
            e.StopPropagation();
        }

        private void SetClickCoords(Vector2 mousePosition, TimelineView timelineView)
        {
            lastMouseCoordinates = mouseCoordinates;
            mouseCoordinates = timelineView.GetCoordinatesFromWorldMouse(mousePosition);
        }
    }
}
