using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartingScreen : MenuScreen
{
    [SerializeField]
    Button3D campaignsButton;
    [SerializeField]
    Button3D settingsButton;
    [SerializeField]
    Button3D quitButton;

    public void Initialize(Action ShowChapterSelectScreen)
    {
        campaignsButton.SetPrimaryText(ScriptTerms.campaign);
        campaignsButton.SetSecondaryText(ScriptTerms.progress);
        campaignsButton.SetParameterInSecondaryText("COMPLETION_PERCENT", ((int)(DataManager.Instance.GetCampaignCompletion() * 100)).ToString());
        campaignsButton.onClick += ShowChapterSelectScreen;
        settingsButton.onClick += () => PlayerUIManager.Instance.ShowPanel(MenuState.SETTINGS);
        quitButton.onClick += PlayerUIManager.Instance.QuitGameNotification;

    }
}
