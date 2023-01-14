using UnityEditor;
using UnityEngine;
using VR.System;

// Called on: Platform switch, script recompile, editor project open and play mode enter.
[InitializeOnLoad]
public static class PlatformShaderToggle
{
    static PlatformShaderToggle()
    {
        /*
         * This is an editor script that also executes when entering play mode.
         * So we want to avoid setting any shader parameters whene entering playmode
         * to simulate built application environment situation as closely as possible
         * Otherwise we could be getting good shader variants in editor playtests, but something could go broken and unnoticed in builds.
         */

        ShaderKeywordUtils.ClearVRPlatformQualityKeywords(); // These persist through playtests, so constantly clear them in editor.

        if (EditorApplication.isPlayingOrWillChangePlaymode) // NOTE: Application.isPlaying always returns false at this stage.
        {
            // If this was triggered because we're entering playmode, leave it up to player scripts to handle shader variant setting.
            return;
        }

        string shaderQuality;
        if (!ShaderKeywordUtils.TryGetVRPlatformQualityKeyword(VRSystem.CurrentVRPlatform, out shaderQuality))
        {
            shaderQuality = ShaderKeywordUtils.VR_PLATFORM_DEFAULT;
            Debug.LogError($"[{typeof(PlatformShaderToggle)}] VR platform {VRSystem.CurrentVRPlatform} not recognized. Shader quality set to default." +
                $" Please amend the platform to quality list inside \"{typeof(ShaderKeywordUtils)}\" class");
        }

        Shader.EnableKeyword(shaderQuality);
        Debug.Log($"[{typeof(PlatformShaderToggle)}] Set platform based shader quality to: {shaderQuality}, because activeBuildTarget is: {EditorUserBuildSettings.activeBuildTarget}");
    }
}
