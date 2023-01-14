using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Milkshake
{
    /// <summary>
    /// Adds git branch and checkout path to Editor title bar
    /// See https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorApplication.cs
    /// See https://github.com/mob-sakai/MainWindowTitleModifierForUnity/blob/master/Assets/Editor/Solution2.InternalsVisibleToAttribute/Solution2.InternalsVisibleToAttribute.cs
    /// </summary>
    [InitializeOnLoad]
    public class EditorTitleBar
    {
        const string PrefsKeyEnabled = "NoBrakesGames.EditorTitleBar.Enabled";
        const string MenuItemToggle = "No Brakes Games/Utilities/Advanced Editor Title Bar";
        const bool DefaultState = false;

        static EditorTitleBar()
        {
            var state = EditorPrefs.GetBool(PrefsKeyEnabled, DefaultState);
            Enable(state);
        }

        [MenuItem(MenuItemToggle)]
        static void Toggle()
        {
            var state = EditorPrefs.GetBool(PrefsKeyEnabled, DefaultState);
            Enable(!state);
            EditorApplication.UpdateMainWindowTitle();
        }

        static void Enable(bool state)
        {
            EditorPrefs.SetBool(PrefsKeyEnabled, state);
            Menu.SetChecked(MenuItemToggle, state);

            EditorApplication.updateMainWindowTitle -= UpdateMainWindowTitle;
            if (state)
                EditorApplication.updateMainWindowTitle += UpdateMainWindowTitle;
        }

        static void UpdateMainWindowTitle(ApplicationTitleDescriptor desc)
        {
            var title = EditorApplication.GetDefaultMainWindowTitle(desc);
            title = $"{ProjectPath}{title} - GIT:{BranchName}";
            desc.title = title;
        }

        static string ProjectPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../../"));
        static string BranchName
        {
            get
            {
                try
                {
                    string headDir = $"{ProjectPath}/.git";
                    if (File.Exists(headDir))
                    {
                        // Worktree
                        string data = File.ReadAllLines(headDir)[0];
                        data = data.Replace("gitdir:", "").Trim();
                        headDir = data;
                    }

                    string branchName = File.ReadAllLines($"{headDir}/HEAD")[0];
                    branchName = branchName.Replace("ref: refs/heads", "").Trim();
                    return branchName;
                }
                catch
                {
                    return "?";
                }
            }
        }
    }
}
