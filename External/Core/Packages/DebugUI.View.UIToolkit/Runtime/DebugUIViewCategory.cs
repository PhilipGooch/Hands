using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NBG.DebugUI.View.UIToolkit
{
    internal class DebugUIViewCategory
    {

        public VisualElement component;
        TextElement catText;

        const string kSelectedColorCode = "#F4C7AB";
        const string kNotSelectedColorCode = "#FFF5EB";
        const string kNonInteractableColorCode = "#DEEDF0";

        int selected;

        Vector3 origin;
        StringBuilder text = new StringBuilder();
        public DebugUIViewCategory(VisualElement root, bool active)
        {
            VisualTreeAsset visualTreeAsset = Resources.Load<VisualTreeAsset>("NBG.DebugUI/DebugUICategory");
            component = visualTreeAsset.Instantiate();
            catText = component.Q<TextElement>("category");
            root.Add(component);
            origin = component.transform.position;

            if (!active)
                Hide();
        }

        public void RewriteText(IEnumerable<IDebugItem> items)
        {
            text.Clear();

            int id = 0;
            foreach (var item in items)
            {
                if (id == selected)
                    text.Append("<b>");

                text.Append("<color=");
                if (id == selected)
                {
                    text.Append(kSelectedColorCode);
                }
                else
                {
                    if (item.HasActivation || item.HasSwitching)
                        text.Append(kNotSelectedColorCode);
                    else
                        text.Append(kNonInteractableColorCode);
                }
                text.Append(">");

                text.Append(item.Label);

                if (item.HasSwitching)
                {
                    text.Append(" <<");
                    text.Append(item.DisplayValue);
                    text.Append(">>");

                }
                else if (item.DisplayValue != null)
                {
                    text.Append(" : ");
                    text.Append(item.DisplayValue);
                }

                text.Append("</color>");

                if (id == selected)
                    text.Append("</b>");

                if (id < items.Count() - 1)
                    text.AppendLine();
                id++;
            }

            catText.text = text.ToString();
        }


        public void UpdateSelection(int itemID)
        {
            string[] lines = text.ToString().Split('\n');
            text.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == selected)
                    lines[i] = lines[i].Replace(kSelectedColorCode, kNotSelectedColorCode);

                if (i == itemID)
                    lines[i] = lines[i].Replace(kNotSelectedColorCode, kSelectedColorCode);

                text.AppendLine(lines[i]);
            }

            catText.text = text.ToString();
            selected = itemID;

        }

        public void Hide()
        {
            component.transform.position = new Vector3(-10000, -10000, -10000);

        }

        public void Show()
        {
            component.transform.position = origin;

        }
    }
}
