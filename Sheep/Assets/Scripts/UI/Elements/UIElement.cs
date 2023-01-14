using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIElement : MonoBehaviour
{
    List<UIElement> otherUIElements;

    protected Color originalColor = new Color32(255, 255, 255, 255);

    protected bool interactable = true;
    public event Action<bool> onInteractableChange;

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void SetInteractable(bool interactable, bool changeChildren = true)
    {
        if (interactable != this.interactable)
            onInteractableChange?.Invoke(interactable);

        this.interactable = interactable;

        if (changeChildren)
        {
            if (otherUIElements == null)
                CollectOtherUIElements();

            SetOtherUIElementsInteractivity(interactable);
        }
    }

    void CollectOtherUIElements()
    {
        otherUIElements = new List<UIElement>();
        GetComponentsInChildren(otherUIElements);
        otherUIElements.Remove(this);
    }

    void SetOtherUIElementsInteractivity(bool interactable)
    {
        foreach (var element in otherUIElements)
        {
            //this already changes all existing elements so no need to check for children again
            element.SetInteractable(interactable, false);
        }
    }
}
