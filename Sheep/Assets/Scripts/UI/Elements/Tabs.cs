using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tabs : MonoBehaviour
{
    [SerializeField]
    TabButton tabButton;

    Dictionary<TabButton, List<UIElement>> tabs = new Dictionary<TabButton, List<UIElement>>();
    TabButton active;

    public void AddTab(string name, List<UIElement> elements, bool defaultTab)
    {
        var tab = tabButton.Create(
            transform.position,
            name,
            transform
        ) as TabButton;

        tab.onClick += () => ChangeTab(tab);

        tabs.Add(tab, elements);

        if (defaultTab)
            ChangeTab(tab);
        else
            SetElementsState(elements, false);

    }

    void TabClicked(TabButton tabButton)
    {
        ChangeTab(tabButton);
    }

    void ChangeTab(TabButton tabButton)
    {
        var clickedTabButton = tabButton as TabButton;

        if (active != clickedTabButton)
        {
            if (active != null)
            {
                active.SetActive(false);
                SetElementsState(tabs[active], false);
            }

            active = clickedTabButton;

            clickedTabButton.SetActive(true);
            SetElementsState(tabs[active], true);
        }

    }

    void SetElementsState(List<UIElement> elements, bool state)
    {
        foreach (var item in elements)
        {
            if (state)
                item.Show();
            else
                item.Hide();
        }
    }
}
