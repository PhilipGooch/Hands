using NBG.Core;
using NBG.LogicGraph.EditorInterface;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    internal class ToastNotificationsManager
    {
        internal ToastNotificationsManager(VisualElement root)
        {
            SetupToastNotifications(root);
        }

        VisualElement toastNotificationsContainer;

        List<ToastNotificationView> activeNotifications = new List<ToastNotificationView>();

        void SetupToastNotifications(VisualElement root)
        {
            toastNotificationsContainer = new VisualElement();
            toastNotificationsContainer.style.position = Position.Absolute;
            toastNotificationsContainer.style.right = 2;
            toastNotificationsContainer.style.bottom = 2;
            root.Add(toastNotificationsContainer);
        }

        internal void AddToastNotification(ToastNotification notification)
        {
            ToastNotificationView toast = new ToastNotificationView();

            toast.Initialize(notification, RemoveToastNotificatin);
            toastNotificationsContainer.Add(toast);
            activeNotifications.Add(toast);
        }

        internal void Update()
        {
            foreach (var item in activeNotifications)
                item.Update();

            //remove all closed notification
            activeNotifications.RemoveAll(x => !toastNotificationsContainer.Contains(x));
        }

        void RemoveToastNotificatin(ToastNotificationView toast)
        {
            toastNotificationsContainer.RemoveIfContains(toast);
        }
    }
}
