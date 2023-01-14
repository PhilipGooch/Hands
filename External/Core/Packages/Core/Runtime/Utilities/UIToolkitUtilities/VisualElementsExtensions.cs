using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core
{
    /// <summary>
    /// UI Toolkit Elements extensions
    /// </summary>
    public static class VisualElementsExtensions
    {
        public static VisualElement GetTopElement(this VisualElement element)
        {
            if (element.parent != null)
                return GetTopElement(element.parent);
            else
                return element;
        }

        public static void RemoveIfContains(this VisualElement parent, VisualElement element)
        {
            if (parent.Contains(element))
                parent.Remove(element);
        }

        public static bool IsChildOf(this VisualElement element, VisualElement parent)
        {
            if (parent.Contains(element) || parent == element)
                return true;

            foreach (var child in parent.Children())
            {
                if (element.IsChildOf(child))
                    return true;
            }

            return false;
        }

        public static VisualElement QInputField<T>(this VisualElement element)
        {
            return element.Q(TextInputBaseField<T>.textInputUssName);
        }

        public static void SetVisibility(this VisualElement element, bool visibility)
        {
            if (visibility)
            {
                element.style.display = DisplayStyle.Flex;
            }
            else
            {
                element.style.display = DisplayStyle.None;
            }

            element.visible = visibility;
        }

        public static bool IsVisible(this VisualElement element)
        {
            return element.style.display == DisplayStyle.Flex;
        }

        public static void SetBorderColor(this VisualElement element, Color color)
        {
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
        }

        public static void SetBorderRadius(this VisualElement element, int radius)
        {
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
        }

        public static void SetBorderWidth(this VisualElement element, int width)
        {
            element.style.borderBottomWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
        }

        public static void SetMarginSize(this VisualElement element, int size)
        {
            element.style.marginBottom = size;
            element.style.marginTop = size;
            element.style.marginLeft = size;
            element.style.marginRight = size;
        }

        public static void SetMarginSize(this VisualElement element, int left, int right, int top, int bottom)
        {
            element.style.marginLeft = left;
            element.style.marginRight = right;
            element.style.marginTop = top;
            element.style.marginBottom = bottom;

        }

        public static void SetPaddingSize(this VisualElement element, int size)
        {
            element.style.paddingBottom = size;
            element.style.paddingTop = size;
            element.style.paddingLeft = size;
            element.style.paddingRight = size;
        }

        public static void SetPaddingSize(this VisualElement element, int left, int right, int top, int bottom)
        {
            element.style.paddingLeft = left;
            element.style.paddingRight = right;
            element.style.paddingTop = top;
            element.style.paddingBottom = bottom;
        }
    }
}