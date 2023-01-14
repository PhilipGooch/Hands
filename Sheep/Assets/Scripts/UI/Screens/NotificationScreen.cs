using UnityEngine;
using TMPro;
using System;

public class NotificationScreen : MenuScreen
{
    [SerializeField]
    UIText titleText;
    [SerializeField]
    UIText contentText;
    [SerializeField]
    Button3D yesButton;
    [SerializeField]
    Button3D noButton;

    Notification notification;
    public override bool Blocker => true;
    public override bool DoFadeOut => true;

    public void SetNotificationData(Notification notification)
    {
        this.notification = notification;
    }

    public override void OnBecomeActive()
    {
        base.OnBecomeActive();

        titleText.SetText(notification.titleLocalizationKey);
        contentText.SetText(notification.contentLocalizationKey);

        ButtonSetup(yesButton, notification.yesBtnLocalizationKey, notification.yesClickedAction, notification.releaseNotificationAction);
        yesButton.gameObject.SetActive(true);

        //Checking this instead of action == null because often you want "no" action to just close the panel
        if (!string.IsNullOrEmpty(notification.noBtnLocalizationKey))
        {
            ButtonSetup(noButton, notification.noBtnLocalizationKey, notification.noClickedAction, notification.releaseNotificationAction);
            noButton.gameObject.SetActive(true);
        }
        else
        {
            noButton.gameObject.SetActive(false);
        }
    }

    void ButtonSetup(Button3D btn, string localizationKey, Action onClick, Action releaseNotificationAction)
    {
        btn.ClearOnClick();

        btn.onClick += Toggle;
        btn.onClick += onClick;
        btn.onClick += releaseNotificationAction;

        btn.SetPrimaryText(localizationKey);

    }

}
