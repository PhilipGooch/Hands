using System.Collections.Generic;
using UnityEditor;
using VR.System;

public class DefinesManager
{
    const string kCheatsDefine = "CHEATS_ENABLED";
    const string kSteamVRDefine = "STEAMVR";
    const string kOculusVRDefine = "OCULUSVR";
    const string kDemoDefine = "DEMO_BUILD";

    public static string GetVRPlatformDefine(VRPlatform platform)
    {
        switch (platform)
        {
            case VRPlatform.SteamVR:
                return kSteamVRDefine;
            case VRPlatform.Oculus:
                return kOculusVRDefine;
            case VRPlatform.Undefined:
                return "";
            default:
                throw new System.Exception($"Unexpected platform {platform}!");
        }
    }
    public static string GetVRPlatformDefine(BuildTargetGroup buildTargetGroup)
    {
        switch (buildTargetGroup)
        {
            case BuildTargetGroup.Standalone:
                return kSteamVRDefine;
            case BuildTargetGroup.Android:
                return kOculusVRDefine;
            default:
                throw new System.Exception($"Unexpected platform {buildTargetGroup}!");
        }
    }
    public static HashSet<string> GetDefines(BuildTargetGroup buildTargetGroup)
    {
        HashSet<string> currentDefines = new HashSet<string>();
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(';');

        foreach (var define in defines)
        {
            currentDefines.Add(define);
        }

        return currentDefines;
    }

    public static void SetDefines(HashSet<string> newDefines, BuildTargetGroup buildTargetGroup)
    {
        var combinedDefines = string.Join(";", newDefines);
        UnityEngine.Debug.Log($"Setting '{buildTargetGroup}' scripting defines to: {combinedDefines}");

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, combinedDefines);
    }

    public static void RemoveVRPlatformDefines(HashSet<string> currentDefines)
    {
        foreach (VRPlatform platform in System.Enum.GetValues(typeof(VRPlatform)))
        {
            var platformDefine = GetVRPlatformDefine(platform);
            currentDefines.Remove(platformDefine);
        }
    }

    public static VRPlatform GetCurrentVRPlatformFromDefines(ICollection<string> currentDefines)
    {
        foreach (VRPlatform platform in System.Enum.GetValues(typeof(VRPlatform)))
        {
            var platformDefine = GetVRPlatformDefine(platform);
            if (currentDefines.Contains(platformDefine))
            {
                return platform;
            }
        }

        return VRPlatform.Undefined;
    }

    public static bool GetCheatsEnabled(HashSet<string> currentDefines)
    {
        return GetDefineEnabled(currentDefines, kCheatsDefine);
    }

    public static void SetCheatsEnabled(HashSet<string> currentDefines, bool enabled)
    {
        SetDefineEnabled(currentDefines, kCheatsDefine, enabled);
    }

    public static bool GetDemoEnabled(HashSet<string> currentDefines)
    {
        return GetDefineEnabled(currentDefines, kDemoDefine);
    }

    public static void SetDemoEnabled(HashSet<string> currentDefines, bool enabled)
    {
        SetDefineEnabled(currentDefines, kDemoDefine, enabled);
    }

    static bool GetDefineEnabled(HashSet<string> currentDefines, string targetDefine)
    {
        return currentDefines.Contains(targetDefine);
    }

    static void SetDefineEnabled(HashSet<string> currentDefines, string targetDefine, bool enabled)
    {
        if (enabled)
        {
            currentDefines.Add(targetDefine);
        }
        else
        {
            currentDefines.Remove(targetDefine);
        }
    }
}
