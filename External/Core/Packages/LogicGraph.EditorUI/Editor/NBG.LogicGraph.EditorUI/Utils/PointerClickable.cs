using System;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    //very basic manipulator which lets reliably get mouse events on elements
    public class PointerClickable : MouseManipulator
    {
        internal event Action onMouseUp;
        internal event Action onMouseDown;
        internal event Action onMouseMove;
        internal event Action onMouseCaptureOutEvent;

        public PointerClickable()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
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

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (CanStopManipulation(e) && onMouseDown != null)
            {
                onMouseDown.Invoke();
                e.StopImmediatePropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (CanStartManipulation(e) && onMouseUp != null)
            {
                onMouseUp.Invoke();
                e.StopImmediatePropagation();
            }
        }

        private void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            onMouseCaptureOutEvent?.Invoke();
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            onMouseMove?.Invoke();
        }
    }
}



