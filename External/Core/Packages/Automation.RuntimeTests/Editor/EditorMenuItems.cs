using UnityEditor;

namespace NBG.Automation.RuntimeTests.Editor
{
    internal static class EditorMenuItems
    {
        //public const string EnableToggleItemName = "No Brakes Games/Automation/Runtime Tests/Enable in Play Mode";
        //public const string OfflineToggleItemName = "No Brakes Games/Automation/Runtime Tests/Enable Offline Mode";
        public const string OfflineAddressOverride = "offline";

        [MenuItem(TestClient.EnableToggleEditorMenuItemName, true)]
        static bool EnableToggleValidate() => (!EditorApplication.isPlaying);
        [MenuItem(TestClient.EnableToggleEditorMenuItemName, priority = 120)]
        static void EnableToggle()
        {
            var on = Menu.GetChecked(TestClient.EnableToggleEditorMenuItemName);
            Menu.SetChecked(TestClient.EnableToggleEditorMenuItemName, !on);
        }

        [MenuItem(TestClient.OfflineToggleEditorMenuItemName, true)]
        static bool OfflineToggleValidate() => (!EditorApplication.isPlaying);
        [MenuItem(TestClient.OfflineToggleEditorMenuItemName, priority = 121)]
        static void OfflineToggle()
        {
            var on = Menu.GetChecked(TestClient.OfflineToggleEditorMenuItemName);
            Menu.SetChecked(TestClient.OfflineToggleEditorMenuItemName, !on);
        }
    }
}
