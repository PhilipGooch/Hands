using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using VR.System;

public class SheepShaderPreprocessor : IPreprocessShaders
{
    // We get invalid shader quality defines, instead of valid to avoid stripping shaders that have no quality defines at all.
    private List<ShaderKeyword> invalidShaderQualityKeywords = null;

    private Dictionary<ShaderKeyword, int> debugReportCounter = new Dictionary<ShaderKeyword, int>();

    public int callbackOrder { get { return 100; } }

    private List<ShaderKeyword> GetInvalidPlatformShaderKeywords ()
    {
        HashSet<string> invalidPlatformShaderKeywordsAsString = ShaderKeywordUtils.CopyAllVRPlatformQualityKeywords();

        VRPlatform currentVRPlatform = DefinesManager.GetCurrentVRPlatformFromDefines(EditorUserBuildSettings.activeScriptCompilationDefines);

        if (ShaderKeywordUtils.TryGetVRPlatformQualityKeyword(currentVRPlatform, out string shaderDefine))
        {
            invalidPlatformShaderKeywordsAsString.Remove(shaderDefine);
        }
        else
        {
            Debug.LogError($"[{GetType()}] Building for: {EditorUserBuildSettings.activeBuildTarget} " +
                $"unidentified platform: no scripting defines found that map to shader quality defines. Defaulting to: {ShaderKeywordUtils.VR_PLATFORM_DEFAULT}");

            invalidPlatformShaderKeywordsAsString.Remove(ShaderKeywordUtils.VR_PLATFORM_DEFAULT);
        }

        List<ShaderKeyword> invalidShaderKeywords = new List<ShaderKeyword>();
        foreach(string shaderPlatformDefine in invalidPlatformShaderKeywordsAsString)
        {
            invalidShaderKeywords.Add(new ShaderKeyword(shaderPlatformDefine));
        }

        return invalidShaderKeywords;
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        // EditorUserBuildSettings are still wrong in constructor of this, so we use this in first shader processor
        if (invalidShaderQualityKeywords == null)
        {
            invalidShaderQualityKeywords = GetInvalidPlatformShaderKeywords();
        }

        debugReportCounter.Clear();

        for (int i = 0; i<invalidShaderQualityKeywords.Count; i++)
        {
            debugReportCounter.Add(invalidShaderQualityKeywords[i], 0);

            for (int i2 = data.Count - 1; i2 >= 0; --i2)
            {
                if (data[i2].shaderKeywordSet.IsEnabled(invalidShaderQualityKeywords[i]))
                {
                    data.RemoveAt(i2);
                    debugReportCounter[invalidShaderQualityKeywords[i]]++;
                }
            }
        }

        PrintResultsForShaderStripping(shader.name, snippet.passName);
    }

    private void PrintResultsForShaderStripping(string shaderName, string passName)
    {
        StringBuilder variantsRemoved = new StringBuilder($"Stripping of {shaderName} ({passName}) REPORT:");
        int tolatDiscarded = 0;
        foreach (ShaderKeyword key in debugReportCounter.Keys)
        {
            if (debugReportCounter[key] > 0)
            {
                tolatDiscarded += debugReportCounter[key];
                variantsRemoved.Append($"\nVarinats with keyword {ShaderKeyword.GetGlobalKeywordName(key)} discarded: {debugReportCounter[key]}");
            }

        }
        if (tolatDiscarded > 0)
        {
            Debug.Log(variantsRemoved.ToString());
        }
    }
}
