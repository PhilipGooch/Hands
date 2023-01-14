using NBG.Core.Editor;
using NBG.LogicGraph.EditorInterface;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace NBG.LogicGraph.EditorUI
{
    public class ToastNotificationView : VisualElement
    {
        private const string k_UXMLGUID = "e1a1fad13cd87cc44b168809f0579f95";
        public new class UxmlFactory : UxmlFactory<ToastNotificationView, VisualElement.UxmlTraits> { }

        VisualElement container;
        VisualElement icon;
        VisualElement closeIcon;
        VisualElement closeButton;

        TextElement message;
        TextElement header;

        Action onClick;
        Action<ToastNotificationView> onClose;

        const double durationUntilAutoClose = 7;
        double creationTimestamp;

        public ToastNotificationView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(k_UXMLGUID));
            visualTree.CloneTree(this);

            container = this.Q<VisualElement>("toastContainer");
            icon = this.Q<VisualElement>("icon");
            closeIcon = this.Q<VisualElement>("closeIcon");
            closeButton = this.Q<VisualElement>("closeButton");

            message = this.Q<TextElement>("message");
            header = this.Q<TextElement>("header");

            closeIcon.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("CrossIcon");
        }

        internal void Initialize(ToastNotification notification, Action<ToastNotificationView> onClose)
        {
            var mainManipulator = new PointerClickable();
            mainManipulator.onMouseDown += OnClick;
            this.AddManipulator(mainManipulator);

            var closeButtonManipulator = new PointerClickable();
            closeButtonManipulator.onMouseDown += Close;
            closeButton.AddManipulator(closeButtonManipulator);

            this.onClick = notification.onClick;
            this.onClose = onClose;

            container.style.backgroundColor = Parameters.ToastNotificationColors[notification.severity];
            
            message.text = notification.message;
            header.text = notification.header;

            creationTimestamp = EditorApplication.timeSinceStartup;

            switch (notification.severity)
            {
                case Severity.Info:
                    icon.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("console.infoicon");
                    break;
                case Severity.Warning:
                    icon.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("console.warnicon");
                    break;
                case Severity.Error:
                    icon.style.backgroundImage = VisualElementsEditorExtensions.GetUnityBuiltinIcon("console.erroricon");
                    break;
                default:
                    break;
            }
        }

        void OnClick()
        {
            onClick?.Invoke();
            Close();
        }

        void Close()
        {
            onClose?.Invoke(this);
        }

        internal void Update()
        {
            if (EditorApplication.timeSinceStartup - creationTimestamp >= durationUntilAutoClose)
                Close();
        }
    }
}
