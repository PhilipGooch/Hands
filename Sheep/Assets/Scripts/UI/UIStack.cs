using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIStack
{
    static UIStack instance;
    public static UIStack Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UIStack();
            }

            return instance;
        }
    }

    Stack<IUIStackElement> elements = new Stack<IUIStackElement>();

    public void Push(IUIStackElement toAdd)
    {
        if (elements.Count > 0)
        {
            var element = elements.Peek();
            //cannot override blocker elements (e.g. notification panel)
            if (element.Blocker)
            {
                return;
            }
            //this element exists in the stack BUT its not the latest one -
            //trying to open pause when settings are open would add another pause on top of settings,
            //so closing the new pause would lead back to settings which would lead back to the first pause.
            else if (elements.Contains(toAdd))
            {
                return;
            }
            else
            {
                element.OnBecomeInactive();
            }
        }

        elements.Push(toAdd);
        toAdd.OnBecomeActive();
    }

    public void Pop()
    {
        if (elements.Count == 0)
        {
            Debug.LogWarning("<color=red>Trying to pop an empty UIStack</color>");
            return;
        }

        var element = elements.Pop();
        element.OnBecomeInactive();

        if (elements.Count > 0)
            elements.Peek().OnBecomeActive();
    }

    //check if element is already active (e.g. pause meniu toggle).
    public bool AlreadyAtTop(IUIStackElement toRemove)
    {
        if (elements.Count > 0)
        {
            if (elements.Peek() == toRemove)
            {
                return true;
            }
        }

        return false;
    }

    public void PopAll()
    {
        while (elements.Count > 0)
        {
            Pop();
        }
    }

    bool HasOverlay()
    {
        return elements.Any(x => x.DoFadeOut);
    }
}
