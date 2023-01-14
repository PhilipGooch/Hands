using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.Core.DataMining
{
    public class Tooltip
    {
        Box element;
        TextElement text;
        public Tooltip(VisualElement root)
        {
            element = new Box();
            element.AddToClassList("tooltip");


            if (EditorGUIUtility.isProSkin) //dark mode
            {
                element.AddToClassList("dark-background");
            }
            else //light mode
            {
                element.AddToClassList("light-background");
            }

            text = new TextElement();
            element.Add(text);
            element.style.visibility = Visibility.Hidden;
            element.pickingMode = PickingMode.Ignore;
            text.pickingMode = PickingMode.Ignore;

            root.Add(element);
            element.BringToFront();
        }

        public void Move(Vector2 pos, VisualElement from)
        {
            pos = from.ChangeCoordinatesTo(element.parent, pos);

            element.style.left = Mathf.Clamp(pos.x ,
                0, element.parent.contentRect.width - element.contentRect.width);
            element.style.top = Mathf.Clamp(pos.y - element.contentRect.height,
                0, element.parent.contentRect.height - element.contentRect.height);

            element.style.visibility = Visibility.Visible;
       
        }

        public void Hide()
        {
            element.style.visibility = Visibility.Hidden;

        }

        public void Update(string text)
        {
            this.text.text = text;
        }
    }
}
