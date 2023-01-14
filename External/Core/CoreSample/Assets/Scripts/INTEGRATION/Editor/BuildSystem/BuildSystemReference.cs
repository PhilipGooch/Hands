using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBG.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildSystemReference : IBuildSystem
{
    public bool Prebuild(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting)
    {
        return false;
    }

    public void Build(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting)
    {
        var opts = new BuildPlayerOptions();
        
        if (platform == BuildPlatform.Windows)
        {
            var path = Path.Combine(BuildSystem.BuildOutputDirectory, "CoreSample.exe");
            opts.locationPathName = path;
            opts.targetGroup = BuildTargetGroup.Standalone;
            opts.target = BuildTarget.StandaloneWindows64;
        }
        else if (platform == BuildPlatform.Switch)
        {
            EditorUserBuildSettings.switchCreateRomFile = true;

            var path = BuildSystem.BuildOutputDirectory;
            if (EditorUserBuildSettings.switchCreateRomFile)
                path = Path.Combine(path, "CoreSample.nsp");
            else
                path = Path.Combine(path, "CoreSample.nspd");
            opts.locationPathName = path;
            opts.targetGroup = BuildTargetGroup.Switch;
            opts.target = BuildTarget.Switch;
        }
        else
        {
            throw new NotSupportedException($"BuildSystemCoreSample does not support building {platform}-{config}");
        }

        SetScriptingBackend(opts.targetGroup, scripting);

        // Build all scenes enabled in the build settings
        {
            var enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled);
            var scenes = new List<string>();
            foreach (var scene in enabledScenes)
                scenes.Add(scene.path);
            opts.scenes = scenes.ToArray();
        }

        switch (config)
        {
            case BuildConfiguration.Development:
                opts.options = BuildOptions.Development;
                break;

            case BuildConfiguration.Release:
                opts.options = BuildOptions.None;
                break;

            default:
                throw new NotImplementedException();
        }

        var report = BuildPipeline.BuildPlayer(opts);
        SaveReport(BuildSystem.BuildOutputDirectory, report);
        CheckReportAndThrow(report);
    }

    static void SaveReport(string targetPath, BuildReport report)
    {
        var outputPath = targetPath + "/BuildReport.json";
        var reportContents = EditorJsonUtility.ToJson(report);
        File.WriteAllText(outputPath, reportContents);
    }

    static void CheckReportAndThrow(BuildReport r)
    {
        UnityEngine.Debug.Log($"\nNBG build report:\nWarnings: {r.summary.totalWarnings}\nErrors: {r.summary.totalErrors}\nTime: {r.summary.totalTime.TotalSeconds} seconds\nResult: {r.summary.result}\n");

        foreach (var step in r.steps)
        {
            foreach (var msg in step.messages)
            {
                if (msg.type == LogType.Log || msg.type == LogType.Warning)
                    continue;
                UnityEngine.Debug.LogWarning($"[{step.name}][{msg.type}] {msg.content}");
            }
        }

        if (r.summary.result != BuildResult.Succeeded)
            throw new Exception($"NBG build failed.");
    }

    static void SetScriptingBackend(BuildTargetGroup buildTargetGroup, BuildScripting scripting)
    {
        if (scripting == BuildScripting.Auto)
            return; // Leave the setting alone

        ScriptingImplementation backend;
        switch (scripting)
        {
            case BuildScripting.Mono:
                backend = ScriptingImplementation.Mono2x;
                break;
            case BuildScripting.IL2CPP:
                backend = ScriptingImplementation.IL2CPP;
                PlayerSettings.SetIl2CppCompilerConfiguration(buildTargetGroup, Il2CppCompilerConfiguration.Release);
                break;
            default:
                throw new NotImplementedException();
        }

        PlayerSettings.SetScriptingBackend(buildTargetGroup, backend);
    }
}
