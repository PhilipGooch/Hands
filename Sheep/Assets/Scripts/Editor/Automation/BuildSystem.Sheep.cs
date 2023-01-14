using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBG.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public enum BuildType
{
    Full,
    Demo,
}

public enum TrueFalse // oy vey
{
    True,
    False,
}

public class BuildSystemSheep : IBuildSystem
{
    const string kScriptDebugging = "--scriptDebugging=";
    public const TrueFalse DefaultScriptDebugging = TrueFalse.False;

    public const string kBuildType = "--buildType=";
    public const BuildType DefaultBuildType = BuildType.Full;

    public bool Prebuild(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting)
    {
        BuildTargetGroup group;
        switch (platform)
        {
            case BuildPlatform.Windows:
                group = BuildTargetGroup.Standalone;
                break;
            case BuildPlatform.Android:
                group = BuildTargetGroup.Android;
                break;
            default:
                throw new NotSupportedException($"BuildSystemSheep does not support building for {platform}");
        }

        // Apply defines
        var currentDefines = DefinesManager.GetDefines(group);
        DefinesManager.RemoveVRPlatformDefines(currentDefines);
        currentDefines.Add(DefinesManager.GetVRPlatformDefine(group));

        var buildType = GetPrefixedCommandLineArg(kBuildType, DefaultBuildType);
        Debug.Log($"[BuildSystem] {kBuildType}{buildType}");
        if (buildType == BuildType.Demo)
        {
            DefinesManager.SetDemoEnabled(currentDefines, true);
        }

        switch (config)
        {
            case BuildConfiguration.Development:
                DefinesManager.SetCheatsEnabled(currentDefines, true);
                break;
            case BuildConfiguration.Release:
                DefinesManager.SetCheatsEnabled(currentDefines, false);
                break;
            default:
                throw new NotImplementedException();
        }

        DefinesManager.SetDefines(currentDefines, group);

        return true;
    }

    public void Build(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting = BuildScripting.Auto)
    {
        var opts = new BuildPlayerOptions();

        string path;
        switch (platform)
        {
            case BuildPlatform.Windows:
                path = Path.Combine(GetBuildDir(), "Sheep.exe");
                opts.target = BuildTarget.StandaloneWindows64;
                break;
            case BuildPlatform.Android:
                path = Path.Combine(GetBuildDir(), "Sheep.apk");
                opts.target = BuildTarget.Android;
                break;
            default:
                throw new NotSupportedException($"BuildSystemSheep does not support building for {platform}");
        }
        opts.locationPathName = path;
        opts.targetGroup = BuildPipeline.GetBuildTargetGroup(opts.target);
        var targetGroup = opts.targetGroup;
        BuildOptions buildOptions = BuildOptions.None;

        switch (config)
        {
            case BuildConfiguration.Development:
                buildOptions |= BuildOptions.Development;
                break;
            case BuildConfiguration.Release:
                buildOptions |= BuildOptions.None;
                break;
            default:
                throw new NotImplementedException();
        }

        var scriptDebugging = GetPrefixedCommandLineArg(kScriptDebugging, DefaultScriptDebugging);
        Debug.Log($"[BuildSystem] {kScriptDebugging}{scriptDebugging}");
        if (scriptDebugging == TrueFalse.True)
        {
            opts.options |= BuildOptions.AllowDebugging;
        }

        opts.options = buildOptions;

        var targetBackend = PlayerSettings.GetScriptingBackend(targetGroup);
        switch (scripting)
        {
            case BuildScripting.Mono:
                targetBackend = ScriptingImplementation.Mono2x;
                break;
            case BuildScripting.IL2CPP:
                targetBackend = ScriptingImplementation.IL2CPP;
                PlayerSettings.SetIl2CppCompilerConfiguration(targetGroup, Il2CppCompilerConfiguration.Release);
                break;
        }
        PlayerSettings.SetScriptingBackend(targetGroup, targetBackend);

        // Build all scenes enabled in the build settings
        {
            var levelHolder = LevelManager.LoadLevelHolder();
            var scenes = new List<string>();
            scenes.Add(levelHolder.MainMenu.ScenePath);
            scenes.Add(levelHolder.BoostrapScene.ScenePath);
            foreach(var level in levelHolder.GetAllLevels())
            {
                scenes.Add(level.scene.ScenePath);
            }
            opts.scenes = scenes.ToArray();
        }
        // Build
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"Build report ({report.summary.result}):\nWarnings: {report.summary.totalWarnings}\nErrors: {report.summary.totalErrors}\nTime: {report.summary.totalTime.TotalSeconds} seconds");
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Build did not succeed.");
    }



    static string GetBuildDir()
    {
        return Path.Combine(Application.dataPath, "..", "..", "BuildSystem", "Artifacts", "Build");
    }

    static string GetBuildName(BuildPlatform platform, BuildConfiguration config)
    {
        return $"Sheep-{platform}-{config}";
    }

    public static TEnum GetPrefixedCommandLineArg<TEnum>(string prefix, TEnum defaultValue) where TEnum : struct
    {
        prefix = prefix.ToLowerInvariant();

        var args = Environment.GetCommandLineArgs();
        var finalValue = defaultValue;

        try
        {
            var modeStr = args
                .First(a => a.ToLowerInvariant().StartsWith(prefix))
                .Substring(prefix.Length);
            Enum.TryParse(modeStr, out finalValue);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log($"Failed to parse {prefix}, will use '{defaultValue}': {ex.Message}");
        }
        finally
        {
            UnityEngine.Debug.Log($"Requested {typeof(TEnum)} = {finalValue}");
        }

        return finalValue;
    }
}
