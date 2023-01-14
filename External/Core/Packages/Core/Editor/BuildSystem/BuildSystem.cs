using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace NBG.Core
{
    public interface IBuildSystem
    {
        // Prebuild pass can be used to change defines.
        // Return <true> to reload assemblies before continuing with the build process.
        bool Prebuild(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting);

        void Build(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting);
    }

    // Automation entry point into game specific build systems
    public static class BuildSystem
    {
        const string kRequireProLicensePrefix = "--requireProLicense=";

        static IBuildSystem _buildSystemInstance;
        static IBuildSystem Instance
        {
            get
            {
                if (_buildSystemInstance == null)
                {
                    var list = AssemblyUtilities.GetAllDerivedClasses(typeof(IBuildSystem));
                    if (list.Count == 0)
                        throw new InvalidOperationException("Could not find an IBuildSystem.");
                    else if (list.Count > 1)
                        throw new InvalidOperationException($"Found {list.Count} IBuildSystem.");

                    _buildSystemInstance = (IBuildSystem)Activator.CreateInstance(list[0]);
                }

                return _buildSystemInstance;
            }
        }

        // Standard build output directory. Automation Tools expect the output there.
        public static string BuildOutputDirectory
        {
            get
            {
                return Path.Combine(Application.dataPath, "..", "..", "BuildSystem", "Artifacts", "Build");
            }
        }

        /// <summary>
        /// Automation entry point.
        /// </summary>
        public static void Build()
        {
            try
            {
                // Batch mode check
                if (!Application.isBatchMode)
                {
                    throw new Exception("Build() should be called in batch mode.");
                }

                // License check
                var hasProLicense = Application.HasProLicense();
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Unity has Pro license: {hasProLicense}");

                var requireProLicense = TryGetPrefixedCommandLineArg<Bool>(kRequireProLicensePrefix, Bool.False).ToBoolean();
                if (requireProLicense && !hasProLicense)
                {
                    throw new Exception("Unity Pro license is required.");
                }

                // Build
                var platform = GetPrefixedCommandLineArg<BuildPlatform>(BuildSystemCommandLineArgs.Platform);
                var configuration = GetPrefixedCommandLineArg<BuildConfiguration>(BuildSystemCommandLineArgs.Configuration);
                var scripting = TryGetPrefixedCommandLineArg<BuildScripting>(BuildSystemCommandLineArgs.Scripting, BuildScripting.Auto);

                BuildDirect(platform, configuration, scripting, true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(2);
            }            
        }

        public static void BuildDirect(BuildPlatform platform, BuildConfiguration configuration, BuildScripting scripting, bool quitEditor)
        {
            // Check if active build target is correct
            var expectedactiveBuildTargetGroup = PlatformToBuildTargetGroup(platform);
            if (EditorUserBuildSettings.selectedBuildTargetGroup != expectedactiveBuildTargetGroup)
                throw new Exception($"BuildSystem requires editor build target group to be '{expectedactiveBuildTargetGroup}' to build for platform '{platform}'");

            var useRelay = Instance.Prebuild(platform, configuration, scripting);
            if (useRelay)
            {
                if (Environment.GetCommandLineArgs().Contains("-quit"))
                    Debug.LogWarning("BuildSystemRelay can't be used when editor is launched with '-quit' as it will quit on assembly reload.");

                // Setup for domain reload
                SessionState.SetBool(BuildSystemSessionState.Build, true);
                SessionState.SetBool(BuildSystemSessionState.QuitEditor, quitEditor);
                SessionState.SetInt(BuildSystemSessionState.Platform, (int)platform);
                SessionState.SetInt(BuildSystemSessionState.Configuration, (int)configuration);
                SessionState.SetInt(BuildSystemSessionState.Scripting, (int)scripting);

                // Recompile scripts. Will reload domain.
                CompilationPipeline.RequestScriptCompilation();
            }
            else
            {
                BuildInternal(platform, configuration, scripting, quitEditor);
            }
        }

        static void BuildInternal(BuildPlatform platform, BuildConfiguration configuration, BuildScripting scripting, bool quitEditor)
        {
            int retCode = 0;

            try
            {
                Instance.Build(platform, configuration, scripting);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                retCode = 3;
            }

            if (quitEditor)
                EditorApplication.Exit(retCode);
        }

        [InitializeOnLoadMethod]
        static void CheckBuildSystemRelay()
        {
            AssemblyReloadEvents.afterAssemblyReload += delegate ()
            {
                var isBuilding = SessionState.GetBool(BuildSystemSessionState.Build, false);
                if (!isBuilding)
                    return;

                Debug.Log($"[BuildSystemRelay] Queueing build after assembly reload.");
                SessionState.SetBool(BuildSystemSessionState.Build, false);

                EditorApplication.update += BuildViaRelay;
            };

            /*EditorApplication.wantsToQuit += delegate ()
            {
                var relay = BuildSystemRelay.Instance;
                if (!relay.build)
                    return true;

                Debug.Log($"[BuildSystemRelay] Deferring quit due to a pending build.");
                relay.quitEditor = true;
                return false;
            };*/ // Reference code. Does not seem to be required.
        }

        static void BuildViaRelay()
        {
            EditorApplication.update -= BuildViaRelay;

            Debug.Log($"[BuildSystemRelay] Building after assembly reload.");
            var quitEditor = SessionState.GetBool(BuildSystemSessionState.QuitEditor, true);
            var platform = (BuildPlatform)SessionState.GetInt(BuildSystemSessionState.Platform, 0);
            var configuration = (BuildConfiguration)SessionState.GetInt(BuildSystemSessionState.Configuration, 0);
            var scripting = (BuildScripting)SessionState.GetInt(BuildSystemSessionState.Scripting, 0);
            BuildInternal(platform, configuration, scripting, quitEditor);
        }

        public enum Bool
        {
            False,
            True
        }
        public static bool ToBoolean(this Bool value)
        {
            return value != Bool.False;
        }

        /// <summary>
        /// Extract value of a command line arg based on prefix. e.g., for --name=value specific --name= as prefix.
        /// Case insensitive.
        /// </summary>
        /// <typeparam name="TEnum">Enum of allowed values.</typeparam>
        /// <param name="prefix">Prefix to look for.</param>
        /// <exception cref="System.Exception">Error finding or parsing the argument.</exception>
        /// <returns>Enum value.</returns>
        public static TEnum GetPrefixedCommandLineArg<TEnum>(string prefix) where TEnum : struct
        {
            prefix = prefix.ToLowerInvariant();
            var args = Environment.GetCommandLineArgs();
            
            try
            {
                var modeStr = args
                    .First(a => a.ToLowerInvariant().StartsWith(prefix))
                    .Substring(prefix.Length);
                var finalValue = (TEnum)Enum.Parse(typeof(TEnum), modeStr, true);
                return finalValue;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse {prefix}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tries to extract value of a command line arg based on prefix. e.g., for --name=value specific --name= as prefix.
        /// </summary>
        /// <typeparam name="TEnum">Enum of allowed values.</typeparam>
        /// <param name="prefix">Prefix to look for.</param>
        /// <param name="defaultValue">Default value to use in case of an error.</param>
        /// <returns>Enum value.</returns>
        public static TEnum TryGetPrefixedCommandLineArg<TEnum>(string prefix, TEnum defaultValue) where TEnum : struct
        {
            prefix = prefix.ToLowerInvariant();
            var finalValue = defaultValue;

            try
            {
                finalValue = GetPrefixedCommandLineArg<TEnum>(prefix);
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Parsed {prefix} as {typeof(TEnum)}");
            }
            catch (Exception ex)
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Falling back {prefix} to default value: {ex.Message}");
            }

            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Requested {prefix}{finalValue} as {typeof(TEnum)}");
            return finalValue;
        }

        static BuildTargetGroup PlatformToBuildTargetGroup(BuildPlatform platform)
        {
            switch (platform)
            {
                case BuildPlatform.Windows:
                    return BuildTargetGroup.Standalone;
                case BuildPlatform.MacOS:
                    return BuildTargetGroup.Standalone;
                case BuildPlatform.Linux:
                    return BuildTargetGroup.Standalone;
                case BuildPlatform.Android:
                    return BuildTargetGroup.Android;
                case BuildPlatform.Switch:
                    return BuildTargetGroup.Switch;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    static class BuildSystemSessionState
    {
        public const string Build = "BuildSystemRelay.Build";
        public const string QuitEditor = "BuildSystemRelay.QuitEditor";
        public const string Platform = "BuildSystemRelay.Platform";
        public const string Configuration = "BuildSystemRelay.Configuration";
        public const string Scripting = "BuildSystemRelay.Scripting";
    }
}
