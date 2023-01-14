using System;
using UnityEngine;
using TMPro;
using I2.Loc;

public class LevelDoneScreen : MenuScreen
{
    [Tooltip("Assign variants to not have identical [Continue] button")]
    [SerializeField]
    Button3D buttonVariant1;
    [Tooltip("Assign variants to not have identical [Restart] button")]
    [SerializeField]
    Button3D buttonVariant2;

    [SerializeField]
    Transform buttonGroup;

    [SerializeField]
    TMP_Text timeSpentText;

    [SerializeField]
    UIText titleText;
    [SerializeField]
    UIText timeSpentLabelText;

    public override bool DoFadeOut => true;

    public override void Init()
    {
        base.Init();

        titleText.SetText(ScriptTerms.levelComplete);
        timeSpentLabelText.SetText(ScriptTerms.timeSpent);

        buttonVariant1.Create(
            transform.position,
            ScriptTerms.@continue,
            () => PlayerUIManager.Instance.QuitToMainMenuAsync(true),
            buttonGroup
            );

        buttonVariant2.Create(
            transform.position,
            ScriptTerms.restart,
            PlayerUIManager.Instance.RestartLevel,
            buttonGroup
            );

    }

    public void SetTimeSpent(float timeSpent)
    {
        int s = (int)timeSpent;
        int m = s / 60;
        s = s % 60;

        timeSpentText.SetText($"{m}:{s}");
    }
}
