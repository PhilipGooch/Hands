using UnityEditor;
using UnityEngine;

namespace NBG.Core
{
    public class BuildWindow : EditorWindow
    {
        BuildPlatform _platform = BuildPlatform.Windows;
        BuildConfiguration _config = BuildConfiguration.Development;
        BuildScripting _scripting = BuildScripting.Auto;

        [MenuItem("No Brakes Games/Automation/Show Automation Build Window...")]
        public static void Init()
        {
            var window = EditorWindow.GetWindow(typeof(BuildWindow), false, "NBG Automation Build Window", true) as BuildWindow;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);
            window.Focus();
        }

        private void OnGUI()
        {
            var isBuilding = SessionState.GetBool(BuildSystemSessionState.Build, false);
            EditorGUI.BeginDisabledGroup(isBuilding);

            _platform = (BuildPlatform)EditorGUILayout.EnumPopup(_platform, GUILayout.MinWidth(50), GUILayout.Width(100), GUILayout.ExpandWidth(true));
            _config = (BuildConfiguration)EditorGUILayout.EnumPopup(_config, GUILayout.MinWidth(50), GUILayout.Width(100), GUILayout.ExpandWidth(true));
            _scripting = (BuildScripting)EditorGUILayout.EnumPopup(_scripting, GUILayout.MinWidth(50), GUILayout.Width(100), GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Build", GUILayout.MaxWidth(150), GUILayout.ExpandWidth(false)))
            {
                Build();
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void Build()
        {
            BuildSystem.BuildDirect(_platform, _config, _scripting, false);
        }
    }
}
