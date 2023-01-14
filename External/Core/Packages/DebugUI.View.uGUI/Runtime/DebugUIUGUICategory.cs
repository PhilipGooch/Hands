using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using System.Linq;

namespace NBG.DebugUI.View.uGUI
{
    public class DebugUIUGUICategory : MonoBehaviour
    {
        const string k_selectedColorCode = "#F4C7AB";
        const string k_notSelectedColorCode = "#FFF5EB";
        const string k_nonInteractableColorCode = "#DEEDF0";

        StringBuilder text = new StringBuilder();
        int selected;

        public TMP_Text catText;

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
                    text.Append(k_selectedColorCode);
                }
                else
                {
                    if (item.HasActivation || item.HasSwitching)
                        text.Append(k_notSelectedColorCode);
                    else
                        text.Append(k_nonInteractableColorCode);
                }
                text.Append(">");

                text.Append(item.Label);

                if (item.HasSwitching)
                {
                    text.Append(" <noparse><<");
                    text.Append(item.DisplayValue);
                    text.Append(">></noparse>");

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
                    lines[i] = lines[i].Replace(k_selectedColorCode, k_notSelectedColorCode);

                if (i == itemID)
                    lines[i] = lines[i].Replace(k_notSelectedColorCode, k_selectedColorCode);

                text.AppendLine(lines[i]);
            }

            catText.text = text.ToString();
            selected = itemID;

        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);

        }
    }
}
