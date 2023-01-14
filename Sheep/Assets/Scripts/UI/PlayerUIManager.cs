using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using I2.Loc;

public enum MenuState
{
    PAUSE,
    LEVEL_DONE,
    SETTINGS
}
public class PlayerUIManager : MonoBehaviour
{
    [SerializeField]
    public List<Panel> panelPrefs;
    NotificationsManager notificationsManager;

    public event System.Action onUIInteractionStarted;
    public event System.Action onUIInteractionEnded;
    public bool InteractingWithUI => uiInteractionCount > 0;

    int uiInteractionCount = 0;

    Dictionary<MenuState, MenuScreen> panels;

    public static PlayerUIManager Instance { get; private set; }
    public PlayerUIManager Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        panels = new Dictionary<MenuState, MenuScreen>();
        notificationsManager = GetComponent<NotificationsManager>();

        return Instance;
    }

    public MenuScreen GetPanel(MenuState type)
    {
        if (!panels.ContainsKey(type))
        {
            MenuScreen newPanel = CreatePanel(type);
            panels.Add(type, newPanel);
            return newPanel;
        }
        else
        {
            if (panels[type] == null)
            {
                panels[type] = CreatePanel(type);
            }

            return panels[type];
        }
    }

    MenuScreen CreatePanel(MenuState type)
    {
        var panel = Instantiate(panelPrefs.Find(x => x.type == type).panel, transform);
        panel.gameObject.SetActive(false);
        return panel;
    }

    public MenuScreen ShowPanel(MenuState type)
    {
        var panel = GetPanel(type);
        panel.Toggle();
        return panel;
    }

    public void DisplayNotification(string title, string content, string yesBtnTxt, Action yesClickedAction = null, string noBtnTxt = "", Action noClickedAction = null)
    {
        notificationsManager.DisplayNotification(title, content, yesBtnTxt, yesClickedAction, noBtnTxt, noClickedAction);
    }

    public void QuitLevelNotification()
    {
        DisplayNotification(
         	ScriptTerms.Notifications.quitToLvlSelection,
        	ScriptTerms.Notifications.genericConfirmation,
            ScriptTerms.yes,
            () => QuitToMainMenuAsync(true),
            ScriptTerms.no
    	);
    }

    public void QuitGameNotification()
    {
        DisplayNotification(
           ScriptTerms.Notifications.quitTheGame,
           ScriptTerms.Notifications.quitConfirmation,
           ScriptTerms.yes,
           Application.Quit,
           ScriptTerms.no
        );
    }

    public async void PlayLevel(Level level)
    {
        await UICamera.Instance.FadeToBlack();
        UIStack.Instance.PopAll();
        await LevelManager.Instance.LoadLevelAsync(level.scene);
        await UICamera.Instance.FadeFromBlack();
    }

    public async void RestartLevel()
    {
        await UICamera.Instance.FadeToBlack();
        UIStack.Instance.PopAll();
        await LevelManager.Instance.RestartLevelAsync();
        await UICamera.Instance.FadeFromBlack();
    }

    public async void QuitToMainMenuAsync(bool saveLastChapter = true)
    {
        await UICamera.Instance.FadeToBlack();
        UIStack.Instance.PopAll();
        await LevelManager.Instance.LoadMainMenuAsync(saveLastChapter);
        await UICamera.Instance.FadeFromBlack();
    }

    public void IncrementInteractivityCounter()
    {
        if (uiInteractionCount == 0)
        {
            onUIInteractionStarted?.Invoke();
        }
        uiInteractionCount++;
    }

    public void DecrementInteractivityCounter()
    {
        if (uiInteractionCount == 1)
        {
            onUIInteractionEnded?.Invoke();
        }
        uiInteractionCount--;
    }
}

[System.Serializable]
public class Panel
{
    [SerializeField]
    public MenuState type;
    [SerializeField]
    public MenuScreen panel;

    public Panel(MenuState type, MenuScreen panel)
    {
        this.type = type;
        this.panel = panel;
    }
}
