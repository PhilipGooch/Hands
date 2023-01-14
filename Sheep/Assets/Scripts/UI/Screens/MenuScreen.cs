using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuScreen : MonoBehaviour, IUIStackElement
{
    PlaceInfrontOfPlayer placeInfrontOfPlayer;

    PlayerUIManager uiManager;

    bool initialized = false;
    public virtual bool Blocker => false;
    public virtual bool DoFadeOut => false;

    UIStack uiStack;

    public virtual void Init()
    {
        initialized = true;

        placeInfrontOfPlayer = GetComponent<PlaceInfrontOfPlayer>();

        uiManager = PlayerUIManager.Instance;

        uiStack = UIStack.Instance;

        if (uiManager == null)
            Debug.LogWarning("<color=red>Player not found, either Player.instance doesnt exist</color>", this);

    }

    public void Toggle()
    {
        if (!initialized)
            Init();

        if (uiStack.AlreadyAtTop(this))
            uiStack.Pop();
        else
            uiStack.Push(this);
    }

    public virtual void OnBecomeActive()
    {
        gameObject.SetActive(true);

        uiManager.IncrementInteractivityCounter();
        if (placeInfrontOfPlayer != null)
        {
            transform.position = new Vector3(0, 100, 0);
            placeInfrontOfPlayer.PlaceInFrontOfPlayer();
        }

        if (DoFadeOut)
            UICamera.Instance.FadeInUI();
    }

    public virtual void OnBecomeInactive()
    {
        if (DoFadeOut)
            UICamera.Instance.FadeOutUI();

        uiManager.DecrementInteractivityCounter();

        gameObject.SetActive(false);
    }
}
