using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core
{
    /// <summary>
    /// Add to visual element to get click on the visual element of Type
    /// Main advantage - clicks are not obstructed by other elements, even if they are clickable as well
    /// </summary>
    /// <typeparam name="T">Type should match type of the object manipulator is added to </typeparam>
    public class SelectionManipulator<T> : MouseManipulator where T : VisualElement
    {
        Action<T> onClick;

        public SelectionManipulator(Action<T> onClick)
        {
            this.onClick = onClick;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e))
            {
                T source = (T)target;

                Debug.Assert(source != null, $"SelectionManipulator didnt click {typeof(T)} {target.name} {target.GetType()}");

                if (source != null)
                {
                    onClick?.Invoke(source);
                }
            }
        }

    }
}
