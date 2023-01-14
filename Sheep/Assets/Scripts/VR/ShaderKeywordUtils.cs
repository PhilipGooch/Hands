using System.Collections.Generic;
using UnityEngine;

namespace VR.System
{
    public static class ShaderKeywordUtils
    {
        public const string VR_PLATFORM_DEFAULT = VR_PLATFORM_QUALITY_LOW;


        private const string VR_PLATFORM_QUALITY_LOW = "VR_PLATFORM_QUALITY_LOW";
        private const string VR_PLATFORM_QUALITY_HIGH = "VR_PLATFORM_QUALITY_HIGH";
        private static readonly HashSet<string> QUALITY_KEYWORDS = new HashSet<string> { VR_PLATFORM_QUALITY_LOW, VR_PLATFORM_QUALITY_HIGH };

        private static readonly Dictionary<VRPlatform, string> VRPlatformScriptDefineToShaderKeyword = new Dictionary<VRPlatform, string>
    {
        {  VRPlatform.SteamVR, VR_PLATFORM_QUALITY_HIGH },
        {  VRPlatform.Oculus, VR_PLATFORM_QUALITY_LOW }
        // Undefined is deliberately left out, to be able to take different code paths and print errors.
    };


        public static bool TryGetVRPlatformQualityKeyword(VRPlatform vrPlatform, out string shaderQualityKeyword)
        {
            return VRPlatformScriptDefineToShaderKeyword.TryGetValue(vrPlatform, out shaderQualityKeyword);
        }

        public static void ClearVRPlatformQualityKeywords()
        {
            foreach (string shaderQualityOption in QUALITY_KEYWORDS)
            {
                Shader.DisableKeyword(shaderQualityOption);
            }
        }

        public static HashSet<string> CopyAllVRPlatformQualityKeywords()
        {
            return new HashSet<string>(QUALITY_KEYWORDS);
        }
    }
}
