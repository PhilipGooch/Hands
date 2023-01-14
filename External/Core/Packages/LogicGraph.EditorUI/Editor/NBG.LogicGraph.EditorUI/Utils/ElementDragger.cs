using NBG.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    internal class ElementDragger<T> : MouseManipulator where T : VisualElement
    {
        protected bool active;
        internal bool Active => active;
        private VariableDragView dragView;
        private SerializableGuid? currentId = null;
        private ClickContext? currentEntry = null;

        public ElementDragger(VariableDragView dragView)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            active = false;
            this.dragView = dragView;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void RegisterDragData(VisualElement target)
        {
            T source = (T)target;
            Debug.Assert(source != null, $"Drag didnt start from set type of view {target.name} {target.GetType()}");

            var blackboardVariable = source as BlackboardVariable;
            var searcherLeafView = source as SearcherLeafView;

            if (blackboardVariable != null)
                currentId = blackboardVariable.variable.ID;
            else if (searcherLeafView != null)
                currentEntry = new ClickContext(searcherLeafView.Node.entry, searcherLeafView.Node.Reference);
        }

        protected void OnMouseMove(MouseMoveEvent e)
        {
            //start drag
            if (!active && !target.HasMouseCapture() && CanStartManipulation(e) && e.imguiEvent.type == EventType.MouseDrag)
            {
                RegisterDragData(target);
                
                if (dragView != null)
                {
                    dragView.visible = true;
                    dragView.transform.position = e.mousePosition;
                    dragView.DragStarted(target);
                }

                active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
            //continue drag
            else if (active && target.HasMouseCapture())
            {
                if (dragView != null)
                    dragView.transform.position = e.mousePosition;
                e.StopPropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent e)
        {
            if (!active || !target.HasMouseCapture() || !CanStopManipulation(e))
                return;

            if (dragView != null)
                dragView.visible = false;

            active = false;
            target.ReleaseMouse();
            e.StopPropagation();

            if (currentId != null)
                dragView.DraggingStopped(e.mousePosition, (SerializableGuid)currentId);
            else if (currentEntry != null)
                dragView.DraggingStopped(e.mousePosition, (ClickContext)currentEntry);
        }
    }
}