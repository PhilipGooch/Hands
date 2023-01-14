using UnityEngine;
using System;
using I2.Loc;

public class PauseScreen : MenuScreen
{
    [Tooltip("Assign variants to not have identical [Resume] and [Quit Level] buttons")]
    [SerializeField]
    Button3D buttonVariant1;
    [Tooltip("Assign variants to not have identical [Settings] and [Quit Game] buttons")]
    [SerializeField]
    Button3D buttonVariant2;
    [SerializeField]
    UIText label;
    [SerializeField]
    Transform buttonGroup;
    public override bool DoFadeOut => true;

    public override void Init()
    {
        base.Init();

        label.SetText(ScriptTerms.pause);

        buttonVariant1.Create(
            transform.position,
            ScriptTerms.resume,
            Toggle,
            buttonGroup
            );

        buttonVariant2.Create(
            transform.position,
            ScriptTerms.settings,
            () => PlayerUIManager.Instance.ShowPanel(MenuState.SETTINGS),
            buttonGroup
            );

        buttonVariant1.Create(
            transform.position,
            ScriptTerms.quitLevel,
            PlayerUIManager.Instance.QuitLevelNotification,
            buttonGroup
            );

        buttonVariant2.Create(
            transform.position,
            ScriptTerms.quitGame,
            PlayerUIManager.Instance.QuitGameNotification,
            buttonGroup
            );

    }
}
