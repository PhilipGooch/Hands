using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NBG.Core.Editor
{
    internal class ValidationTestsWindow : EditorWindow
    {
        [MenuItem("No Brakes Games/Validation Tests...", priority = 100)]
        static void ShowWindow()
        {
            var w = EditorWindow.GetWindow<ValidationTestsWindow>();
            w.Initialize();
            w.Show();
        }

        string[] _levelBaseScenes;
        string[] _levelNames;
        string _levelBaseToLoad = null;

        private void Initialize()
        {
            this.titleContent = new GUIContent("Level Validation");

            _levelBaseScenes = ValidationTests.Indexer.LevelBaseScenes;
            _levelNames = ValidationTests.Indexer.LevelNames;
        }



        // GUI settings
        enum Scope
        {
            Scenes,
            Project,
        }
        enum Strictness
        {
            Strict,
            Helper,
        }

        bool _guiInitialized = false;
        GUIStyle _labelOk;
        GUIStyle _labelError;
        string[] _testStrictnessNames = new string[] {
            "Strict",
            "Helper",
        };
        string[] _testScopeNames = new string[] {
            "Level Specific",
            "Project Wide",
        };

        // GUI state
        Scope _selectedTestScope;
        Strictness _selectedTestStrictness;
        int _selectedLevelIndex;
        Vector2 _testListScrollPosition;
        bool _levelLoadErrorCurrent;

        private void InitializeGUI()
        {
            if (_guiInitialized)
                return;
            Initialize();
            _guiInitialized = true;

            _labelOk = new GUIStyle(GUI.skin.label);
            _labelOk.normal.textColor = Color.green;

            _labelError = new GUIStyle(GUI.skin.label);
            _labelError.normal.textColor = Color.red;

            _selectedTestScope = Scope.Scenes;
            _selectedTestStrictness = Strictness.Strict;

            try
            {
                var loaded = ValidationTests.Indexer.CurrentLevel;
                _selectedLevelIndex = Array.FindIndex(_levelBaseScenes, (s) => s == loaded.BaseScene.path);
            }
            catch
            {
            }
        }

        const int kStatusColumnWidth = 128;
        const int kButtonColumnWidth = 64;

        private GUIStyle GetStyleForStatusLabel(ValidationTestStatus status)
        {
            switch (status)
            {
                case ValidationTestStatus.Unknown:
                    return GUI.skin.label;
                case ValidationTestStatus.OK:
                    return _labelOk;
                case ValidationTestStatus.Failure:
                    return _labelError;
                default:
                    throw new System.NotImplementedException();
            }
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                GUILayout.Label("Disabled in play mode.");
                return;
            }

            InitializeGUI();

            // Test scope
            EditorGUI.BeginChangeCheck();
            _selectedTestScope = (Scope)GUILayout.Toolbar((int)_selectedTestScope, _testScopeNames);
            if (EditorGUI.EndChangeCheck())
                ValidationTests.ResetAllTests();
            GUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(!string.IsNullOrWhiteSpace(_levelBaseToLoad));
            {
                bool levelLoadError = false;

                if (_selectedTestScope == Scope.Scenes)
                {
                    // Level selection
                    GUILayout.BeginHorizontal();
                    _selectedLevelIndex = EditorGUILayout.Popup("Level", _selectedLevelIndex, _levelNames);
                    if (GUILayout.Button("Open", GUILayout.Width(128)))
                    {
                        _levelBaseToLoad = _selectedLevelIndex == -1 ? null : _levelBaseScenes[_selectedLevelIndex];
                    }
                    if (GUILayout.Button("Refresh List", GUILayout.Width(128)))
                    {
                        ValidationTests.Indexer.Refresh();
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                    GUILayout.EndHorizontal();

                    // Level status
                    var levelLoadStatusText = ValidationTests.Indexer.GetLevelLoadStatus(out levelLoadError);
                    GUILayout.BeginVertical();
                    GUILayout.Label(levelLoadStatusText, levelLoadError ? _labelError : _labelOk);
                    if (levelLoadError)
                        GUILayout.Label("Only tests that can operate on loose scenes are enabled", _labelError);
                    GUILayout.EndVertical();
                    if (_levelLoadErrorCurrent != levelLoadError)
                    {
                        _levelLoadErrorCurrent = levelLoadError;
                        ValidationTests.ResetAllTests();
                    }

                    GUILayout.Space(5);
                }

                _selectedTestStrictness = (Strictness)GUILayout.Toolbar((int)_selectedTestStrictness, _testStrictnessNames);

                // Determine test to run based on caps
                ILevel context = null;
                ValidationTestCaps requireCaps = ValidationTestCaps.None;
                ValidationTestCaps excludeCaps = ValidationTestCaps.None;

                if (_selectedTestScope == Scope.Scenes)
                {
                    requireCaps |= ValidationTestCaps.ChecksScenes;
                    if (levelLoadError)
                    {
                        excludeCaps = ValidationTestCaps.RequiresCompleteLevels;
                        context = ValidationTests.LooseScenesProxyLevel;
                    }
                    else
                    {
                        context = ValidationTests.Indexer.CurrentLevel;
                    }

                    if (_selectedTestStrictness == Strictness.Strict)
                        requireCaps |= ValidationTestCaps.StrictScenesScope;
                    else if (_selectedTestStrictness == Strictness.Helper)
                        excludeCaps |= ValidationTestCaps.StrictScenesScope;
                }
                else if (_selectedTestScope == Scope.Project)
                {
                    requireCaps |= ValidationTestCaps.ChecksProject;

                    if (_selectedTestStrictness == Strictness.Strict)
                        requireCaps |= ValidationTestCaps.StrictProjectScope;
                    else if (_selectedTestStrictness == Strictness.Helper)
                        excludeCaps |= ValidationTestCaps.StrictProjectScope;
                }

                // Tests
                OnGUI_ValidationTests(requireCaps, excludeCaps, context);
            }
            EditorGUI.EndDisabledGroup();
        }

        // Runs/Fixes all tests that have <requireCaps> and don't have <excludeCaps>
        private void OnGUI_ValidationTests(ValidationTestCaps requireCaps, ValidationTestCaps excludeCaps, ILevel context)
        {
            _testListScrollPosition = GUILayout.BeginScrollView(_testListScrollPosition);
            GUILayout.BeginVertical();
            {
                // Header
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Description", EditorStyles.boldLabel);
                    GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(kStatusColumnWidth));
                    if (GUILayout.Button("Run All", GUILayout.Width(kButtonColumnWidth)))
                    {
                        if (ValidationTestsSettingsProvider.GetClearBeforeRun())
                            ClearConsoleWindow();
                        ValidationTests.RunAllTests(requireCaps, excludeCaps, context);
                    }
                    GUILayout.Space(kButtonColumnWidth);
                    if (GUILayout.Button("Fix All", GUILayout.Width(kButtonColumnWidth)))
                    {
                        if (ValidationTestsSettingsProvider.GetClearBeforeFix())
                            ClearConsoleWindow();
                        ValidationTests.FixAllTests(requireCaps, excludeCaps, context);
                    }
                }
                GUILayout.EndHorizontal();

                // Special case when want to see level tests, but don't have a full level loaded.
                // Show disabled tests which require full levels loaded.
                var onlyLooseScenes = (excludeCaps & ValidationTestCaps.RequiresCompleteLevels) == ValidationTestCaps.RequiresCompleteLevels;
                if (onlyLooseScenes)
                    excludeCaps &= ~ValidationTestCaps.RequiresCompleteLevels;

                // Items
                var categories = ValidationTests.Tests.Select(x => x.Category).Distinct();
                foreach (var category in categories)
                {
                    var tests = ValidationTests.Tests.Where(t => t.Category == category && t.HasCaps(requireCaps) && t.DoesntHaveCaps(excludeCaps));
                    if (!tests.Any())
                        continue;

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"[{category}]", EditorStyles.boldLabel);
                    GUILayout.EndHorizontal();

                    foreach (var test in tests)
                    {
                        EditorGUI.BeginDisabledGroup(onlyLooseScenes && test.HasCaps(ValidationTestCaps.RequiresCompleteLevels));
                        OnTestRowGUI(test, context);
                        EditorGUI.EndDisabledGroup();
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void OnTestRowGUI(ValidationTest test, ILevel context)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(test.Name);
                var statusText = test.StatusCount == 0 ? test.Status.ToString() : $"{test.Status} ({test.StatusCount})";
                GUILayout.Label(statusText, GetStyleForStatusLabel(test.Status), GUILayout.Width(kStatusColumnWidth));
                if (GUILayout.Button("Run", GUILayout.Width(kButtonColumnWidth)))
                {
                    if (ValidationTestsSettingsProvider.GetClearBeforeRun())
                        ClearConsoleWindow();
                    test.Reset();
                    test.Run(context);
                }
                EditorGUI.BeginDisabledGroup(!test.CanAssist);
                {
                    var content = new GUIContent("Assist", test.CanAssist ? test.AssistTooltip : string.Empty);
                    if (GUILayout.Button(content, GUILayout.Width(kButtonColumnWidth)))
                    {
                        if (ValidationTestsSettingsProvider.GetClearBeforeAssist())
                            ClearConsoleWindow();
                        test.Assist(context);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUI.BeginDisabledGroup(!test.CanFix);
                {
                    var content = new GUIContent("Fix", test.CanFix ? test.FixTooltip : string.Empty);
                    if (GUILayout.Button(content, GUILayout.Width(kButtonColumnWidth)))
                    {
                        if (ValidationTestsSettingsProvider.GetClearBeforeFix())
                            ClearConsoleWindow();
                        test.Fix(context);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndHorizontal();
        }

        [ClearOnReload]
        static System.Reflection.MethodInfo _clearConsoleWindowMi = null;
        private static void ClearConsoleWindow()
        {
            if (_clearConsoleWindowMi == null)
            {
                var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
                var type = assembly.GetType("UnityEditor.LogEntries");
                _clearConsoleWindowMi = type.GetMethod("Clear");
                if (_clearConsoleWindowMi == null)
                    Debug.LogWarning("Could not find UnityEditor.LogEntries.Clear");
            }

            _clearConsoleWindowMi?.Invoke(new object(), null);
        }

        private void Update()
        {
            if (!string.IsNullOrWhiteSpace(_levelBaseToLoad))
            {
                var levelBasetoLoad = _levelBaseToLoad;
                _levelBaseToLoad = null;
                bool wantsOpen = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                if (wantsOpen)
                {
                    ValidationTests.Indexer.OpenLevel(levelBasetoLoad);
                    ValidationTests.ResetAllTests();
                }
            }
        }
    }
}
