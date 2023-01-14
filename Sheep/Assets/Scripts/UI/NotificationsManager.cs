using System;
using System.Collections.Generic;
using UnityEngine;

public class NotificationsManager : MonoBehaviour
{
    [SerializeField]
    NotificationScreen notificationPanelPrefab;
    NotificationScreen notificationPanel;

    Queue<Notification> notificationQueue = new Queue<Notification>();

    bool open;

    NotificationScreen GetNotificationPanel()
    {
        if (notificationPanel == null)
            notificationPanel = Instantiate(notificationPanelPrefab, transform);

        return notificationPanel;
    }

    void ReleaseNotificationPanel()
    {
        open = false;

        if (notificationQueue.Count > 0)
            DisplayNotification();

    }


    public void DisplayNotification(
        string title,
        string content,
        string yesBtnTxt,
        Action yesClickedAction = null,
        string noBtnTxt = "",
        Action noClickedAction = null
        )
    {
        notificationQueue.Enqueue(new Notification(ReleaseNotificationPanel, title, content, yesBtnTxt, yesClickedAction, noBtnTxt, noClickedAction));

        if (!open)
            DisplayNotification();

    }

    void DisplayNotification()
    {
        NotificationScreen notificationPanel = GetNotificationPanel();
        notificationPanel.SetNotificationData(notificationQueue.Dequeue());
        notificationPanel.Toggle();
        open = true;

    }
}

public struct Notification
{
    public string titleLocalizationKey;
    public string contentLocalizationKey;
    public string yesBtnLocalizationKey;
    public string noBtnLocalizationKey;
    public Action yesClickedAction;
    public Action noClickedAction;
    //to allow showing a new notification 
    public Action releaseNotificationAction;
    public Notification(Action releaseNotificationAction, string title, string content, string yesBtnTxt, Action yesClickedAction, string noBtnTxt, Action noClickedAction)
    {
        this.titleLocalizationKey = title;
        this.contentLocalizationKey = content;
        this.yesBtnLocalizationKey = yesBtnTxt;
        this.noBtnLocalizationKey = noBtnTxt;
        this.yesClickedAction = yesClickedAction;
        this.noClickedAction = noClickedAction;
        this.releaseNotificationAction = releaseNotificationAction;
    }
}
