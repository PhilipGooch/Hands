using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class WindowResizeManipulator : MouseManipulator
    {
        private bool active;
        private Action<Vector2> onMove;

        public WindowResizeManipulator(Action<Vector2> onMove)
        {
            this.onMove = onMove;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            active = false;
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
            if (active)
            {
                e.StopImmediatePropagation();
                return;
            }
            else
            {
                if (target != null && CanStartManipulation(e))
                {
                    active = true;
                    target.CaptureMouse();
                    e.StopImmediatePropagation();
                }
            }
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (active)
            {
                onMove?.Invoke(e.mousePosition);
            }

            e.StopPropagation();
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (active)
            {
                active = false;
                target.ReleaseMouse();
            }
            e.StopPropagation();
        }
    }
}
