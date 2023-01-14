using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VR.System;

public class PlatformSettingsWindow : EditorWindow
{
    HashSet<string> currentDefines = new HashSet<string>();

    [MenuItem("Window/Platform Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow(typeof(PlatformSettingsWindow));
        window.Show();
    }
    static BuildTargetGroup GetCurrentBuildTargetGroup()
    {
        var currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
        return BuildPipeline.GetBuildTargetGroup(currentBuildTarget);
    }
    void OnEnable()
    {
        currentDefines = DefinesManager.GetDefines(GetCurrentBuildTargetGroup());
    }

    private void OnGUI()
    {
        var currentPlatform = DefinesManager.GetCurrentVRPlatformFromDefines(currentDefines);
        var newPlatform = (VRPlatform)EditorGUILayout.EnumPopup("Current VR Platform:", currentPlatform);

        if (newPlatform != currentPlatform)
        {
            DefinesManager.RemoveVRPlatformDefines(currentDefines);
            currentDefines.Add(DefinesManager.GetVRPlatformDefine(newPlatform));
        }

        var currentDemoState = DefinesManager.GetDemoEnabled(currentDefines);
        var newDemoState = EditorGUILayout.Toggle("Demo Mode", currentDemoState);

        if (currentDemoState != newDemoState)
        {
            DefinesManager.SetDemoEnabled(currentDefines, newDemoState);
        }

        var currentCheatsState = DefinesManager.GetCheatsEnabled(currentDefines);
        var newCheatsState = EditorGUILayout.Toggle("Enable Cheats", currentCheatsState);

        if (newCheatsState != currentCheatsState)
        {
            DefinesManager.SetCheatsEnabled(currentDefines, newCheatsState);
        }


        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Apply"))
        {
            DefinesManager.SetDefines(currentDefines, GetCurrentBuildTargetGroup());
        }

        GUILayout.Space(10);
    }
}
