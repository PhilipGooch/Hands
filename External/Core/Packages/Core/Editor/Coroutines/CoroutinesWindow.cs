using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor
{
    internal class CoroutinesWindow : EditorWindow
    {
        [MenuItem("No Brakes Games/Coroutine Viewer...", priority = 2)]
        static void ShowWindow()
        {
            var w = EditorWindow.GetWindow<CoroutinesWindow>();
            w.Initialize();
            w.Show();
        }

        private void Initialize()
        {
            this.titleContent = new GUIContent("Coroutine Viewer");
            this.minSize = new Vector2(450, 300);
        }

        // GUI settings
        bool _guiInitialized = false;
        GUIStyle _labelName;
        GUIStyle _labelOk;
        GUIStyle _labelWarning;
        GUIStyle _labelError;

        // GUI state
        Vector2 _scrollPosition;

        private void InitializeGUI()
        {
            if (_guiInitialized)
                return;
            Initialize();
            _guiInitialized = true;

            _labelName = new GUIStyle(EditorStyles.boldLabel);
            _labelName.clipping = TextClipping.Clip;

            _labelOk = new GUIStyle(GUI.skin.label);
            _labelOk.normal.textColor = Color.green;

            _labelWarning = new GUIStyle(GUI.skin.label);
            _labelWarning.normal.textColor = Color.yellow;

            _labelError = new GUIStyle(GUI.skin.label);
            _labelError.normal.textColor = Color.red;
        }

        const int kNameColumnFudge = 32; // Prevents table distortion
        const int kStatusColumnWidth = 80;
        const int kDurationColumnWidth = 80;

        private GUIStyle GetStyleForStatusLabel(CoroutineStatus status)
        {
            switch (status)
            {
                case CoroutineStatus.Running:
                    return GUI.skin.label;
                case CoroutineStatus.Completed:
                    return _labelOk;
                case CoroutineStatus.Interrupted:
                    return _labelWarning;
                case CoroutineStatus.Exception:
                    return _labelError;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private void OnGUI()
        {
            InitializeGUI();

            // Info
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label($"Coroutines.LingerSeconds = {Coroutines.LingerSeconds}");
            }
            GUILayout.EndHorizontal();

            // Header
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Name", EditorStyles.boldLabel);
                GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(kStatusColumnWidth));
                GUILayout.Label("Duration (s)", EditorStyles.boldLabel, GUILayout.Width(kDurationColumnWidth));
            }
            GUILayout.EndHorizontal();

            // Items
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.BeginVertical();
            {
                var coroutines = Coroutines.All;
                foreach (var coroutine in coroutines)
                {
                    OnRowGUI(coroutine);
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void OnRowGUI(ICoroutine co)
        {
            GUILayout.BeginHorizontal();
            {
                var name = co.Name;
                var nameSize = EditorStyles.boldLabel.CalcSize(new GUIContent(name));
                var rect = GUILayoutUtility.GetRect(this.position.width - kStatusColumnWidth - kDurationColumnWidth - kNameColumnFudge, nameSize.y, EditorStyles.boldLabel);
                GUI.Label(rect, name, _labelName);

                var style = GetStyleForStatusLabel(co.Status);
                GUILayout.Label(co.Status.ToString(), style, GUILayout.Width(kStatusColumnWidth));

                var durationText = co.DurationSeconds.ToString("0.###");
                GUILayout.Label(durationText, GUILayout.Width(kDurationColumnWidth));
            }
            GUILayout.EndHorizontal();
        }

        private void Update()
        {
            this.Repaint();
        }
    }
}
