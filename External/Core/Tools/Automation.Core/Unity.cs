#define ENABLE_LOG_INSPECTIONS
using CliWrap;
using NBG.Core;
using NiceIO;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using CliWrap.Builders;

namespace Automation.Core
{
    public static class Unity
    {
        internal static string GetUnityEditorVersion(string[] lines)
        {
            const string kHint = "m_EditorVersion: ";

            var result = lines.Single(l => l.StartsWith(kHint)).Substring(kHint.Length);
            return result;
        }

        internal static string GetUnityEditorRevision(string[] lines)
        {
            const string kHint = "m_EditorVersionWithRevision: ";

            var result = lines.Single(l => l.StartsWith(kHint));
            var start = result.IndexOf('(');
            var end = result.IndexOf(')');
            return result.Substring(start + 1, end - start - 1);
        }

        [Conditional("ENABLE_LOG_INSPECTIONS")]
        static void InspectUnityLogFile(string logPath, AutoEnv env)
        {
            if (env.Config.UnityLogInspection == false)
            {
                Utils.Log.Message("Will not inspect log output because UnityLogInspection is false.");
                return;
            }
            Utils.Log.PushBlock("Inspecting log output", logPath);
            var rulesFilePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(env.Config.UnityLogInspectionRulesPath))
                rulesFilePath = Path.Combine(env.CheckoutDir, env.Config.UnityLogInspectionRulesPath);
            Inspection.ParseLogFile(logPath, env.CheckoutDir, rulesFilePath);
            Utils.Log.PopBlock();
        }

        public static async Task<int> InstallEditor(AutoEnv env, bool hub, string version, string changeset, string modules)
        {
            IUnityInstallationTasks tasks = hub ? new UnityInstallationTasksHub(env) : new UnityInstallationTasksManual(env);

            var failures = 0;

            // Editor
            var editorInstalled = false;
            var path = await tasks.GetUnityEditorExecutable(version);
            if (path != null)
            {
                if (File.Exists(path))
                {
                    editorInstalled = true;
                    Utils.Log.Message($"Unity {version} is already installed at {path}.");
                }
            }

            if (!editorInstalled)
            {
                Utils.Log.Message($"Installing Unity: {version}");
                await tasks.InstallUnityEditor(version, changeset);
            }

            // Modules
            if (modules != null)
            {
                var mods = modules.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var mod in mods)
                {
                    await tasks.InstallUnityEditorModule(version, changeset, mod);
                }
            }

            return failures;
        }

        static async Task<(int, string)> ExecuteUnityEditorTests(AutoEnv env, string editorPath, NPath artifactsDir, EditorTestsPlatform platform)
        {
            Debug.Assert(platform != EditorTestsPlatform.All);

            const string testResultsType = "xml";
            var testsResultsFileName = $"unity.tests.{platform}";
            var testsResultsFile = $"{testsResultsFileName}.{testResultsType}";
            var testsResultsPath = artifactsDir.Combine(testsResultsFile);

            var logPath = artifactsDir.Combine("Logs", $"Unity.editor.tests-{platform}.log");
            var logHandler = new UnityLogHandler(Utils.Log, logPath);

            Utils.Log.PushBlock($"Running Unity {platform} tests");
            var cmd = Cli.Wrap(editorPath)
                    .WithArguments(a => a
                        .Add("-runTests")
                        .Add("-batchmode")
                        .Add("-projectPath").Add(env.UnityProjectDir)
                        .Add("-forgetProjectPath")
                        .Add("-testResults").Add(testsResultsPath)
                        .Add("-testPlatform").Add(platform)
                        .Add("-logFile").Add("-")
                        )
                    .WithEnvironmentVariables(env.Config.EnvironmentVariables)
                    .WithValidation(CommandResultValidation.None) // Handle process failures manually
                    | (logHandler.OnStdOut, logHandler.OnStdErr);

            Utils.Log.Message($"Executing: {cmd}");
            var op = cmd.ExecuteAsync();
            await op;
            logHandler.Close(); //TODO: IDisposable
            Utils.Log.PopBlock();

            InspectUnityLogFile(logPath, env);

            var ret = op.Task.Result.ExitCode;
            if (ret != 0)
            {
                Utils.Log.Error($"Unity reported a non-zero exit code ({op.Task.Result.ExitCode})");
            }

            return (ret, testsResultsPath);
        }

        public static async Task<int> RunTests(AutoEnv env, EditorTestsPlatform platform, bool cleanArtifacts)
        {
            var path = await FindUnityEditorExecutable(env);
            if (path == null)
            {
                Utils.Log.Message($"Could not find Unity {env.UnityEditorVersion} installed.");
                return -1;
            }

            Utils.Log.Message($"Unity editor path: {path}");

            var artifactsDir = new NPath(env.ArtifactsDir);
            if (cleanArtifacts)
                artifactsDir.DeleteIfExists();

            var failures = 0;

            foreach (var value in Enum.GetValues(typeof(EditorTestsPlatform)).Cast<EditorTestsPlatform>())
            {
                if (value == EditorTestsPlatform.All)
                    continue;

                if ((platform & value) == value)
                {
                    // Build
                    var (ret, resultsPath) = await ExecuteUnityEditorTests(env, path, artifactsDir, value);
                    if (ret != 0)
                        failures++;

                    // Publish
                    Utils.Log.PublishArtifacts(resultsPath);
                }
            }

            return failures;
        }

        public static async Task<int> BuildProject(AutoEnv env, BuildPlatform platform, BuildConfiguration config, BuildScripting scripting, string extraUnityEditorArgs, string buildVersion)
        {
            if (extraUnityEditorArgs == null)
                extraUnityEditorArgs = string.Empty;
            if (buildVersion != null && buildVersion.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new InvalidDataException($"BuildVersion parameter has invalid characters: {buildVersion}");

            var buildTarget = Utils.PlatformToBuildTarget(platform);
            var extraArgs = $"-buildTarget {buildTarget} -executeMethod NBG.Core.BuildSystem.Build {BuildSystemCommandLineArgs.Platform}{platform} {BuildSystemCommandLineArgs.Configuration}{config} {BuildSystemCommandLineArgs.Scripting}{scripting} {extraUnityEditorArgs}";
            var quit = false; // NBG.Core.BuildSystem.Build quits manually
            var ret = await RunEditor(env, extraArgs, "Unity.editor.build.log", true, quit);
            if (ret != 0)
                return ret;

            // Package
            Utils.Log.PushBlock("Compressing and uploading the build");
            {
                if (string.IsNullOrWhiteSpace(env.Config.GameName))
                    throw new InvalidDataException("Repository configuration is missing property 'GameName'");

                const string kArchiveType = "zip";
                var artifactsDir = new NiceIO.NPath(env.ArtifactsDir);
                var buildDir = artifactsDir.Combine("Build");
                var packageName = $"{env.Config.GameName}-{platform}-{config}";
                if (scripting != BuildScripting.Auto)
                    packageName += $"-{scripting}";
                if (buildVersion != null)
                    packageName += $".{buildVersion}";               

                // IL2CPP symbols
                {
                    var symbolsDir = FindDirectoryWithSuffix(buildDir, "BackUpThisFolder_ButDontShipItWithYourGame");
                    if (symbolsDir != null)
                    {
                        var symbolsFile = $"Symbols.{packageName}.{kArchiveType}";
                        var symbolsPath = artifactsDir.Combine(symbolsFile);
                        await Utils.CompressDirectory(symbolsDir, symbolsPath, kArchiveType);
                        Utils.Log.PublishArtifacts(symbolsPath, "Build");

                        symbolsDir.DeleteIfExists();
                    }
                }

                // Burst debug information
                {
                    var debugDir = FindDirectoryWithSuffix(buildDir, "BurstDebugInformation_DoNotShip");
                    if (debugDir != null)
                    {
                        var outputFile = $"Burst.Debug.{packageName}.{kArchiveType}";
                        var outputPath = artifactsDir.Combine(outputFile);
                        await Utils.CompressDirectory(debugDir, outputPath, kArchiveType);
                        Utils.Log.PublishArtifacts(outputPath, "Build");

                        debugDir.DeleteIfExists();
                    }
                }

                // Actual build
                {
                    var packageFile = $"{packageName}.{kArchiveType}";
                    var packagePath = artifactsDir.Combine(packageFile);
                    var compressionLevel = Utils.PlatformBuildCompressionLevel(platform);
                    await Utils.CompressDirectory(buildDir, packagePath, kArchiveType, compressionLevel);
                    Utils.Log.PublishArtifacts(packagePath, "Build");
                }
            }
            Utils.Log.PopBlock();

            return 0;
        }

        public static async Task<int> InitializeProject(AutoEnv env)
        {
            Utils.Log.PushBlock("Generate a dummy shader to workaround bug 1372139");
            try
            {
                var fileName = "NBG_Automation_Generated_Workaround_1372139.shader";
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Unity", "NBG_Automation_Generated_Workaround_1372139.shader");
                var assetsDir = Path.Combine(env.UnityProjectDir, "Assets");
                var targetPath = Path.Combine(assetsDir, fileName);

                Utils.Log.Message($"Generating {filePath} to {targetPath}");

                var text = File.ReadAllText(filePath);
                var rnd = new Random((int)DateTimeOffset.Now.ToUnixTimeMilliseconds());
                text = text.Replace("NBG_REPLACE_TAG", $"{rnd.NextDouble()}, {rnd.NextDouble()}, {rnd.NextDouble()}, {rnd.NextDouble()}");
                File.WriteAllText(targetPath, text);
            }
            catch (Exception e)
            {
                Utils.Log.Error($"Workaround failed: {e.Message}");
            }
            Utils.Log.PopBlock();

            await Task.Yield();//
            return 0;
        }

        static NPath FindDirectoryWithSuffix(NPath root, string suffix)
        {
            var dirs = root.Directories($"*{suffix}", true);
            var count = dirs.Count();
            if (count > 1)
                throw new System.Exception($"Found {count} folders suffixed '{suffix}'");
            return dirs.Count() == 0 ? null : dirs.First();
        }

        public static async Task<int> RunEditor(AutoEnv env, string extraUnityEditorArgs, string logFileName, bool disableGraphics, bool quit)
        {
            var path = await FindUnityEditorExecutable(env);
            if (path == null)
            {
                Utils.Log.Message($"Could not find Unity {env.UnityEditorVersion} installed.");
                return -1;
            }

            var ret = 0;

            if (string.IsNullOrEmpty(logFileName))
                logFileName = "Unity.editor.run.log";

            Utils.Log.Message($"Unity editor path: {path}");

            // Run
            Utils.Log.PushBlock("Running Unity editor");
            {
                var artifactsDir = new NiceIO.NPath(env.ArtifactsDir);
                var logPath = artifactsDir.Combine("Logs", logFileName);
                var logHandler = new UnityLogHandler(Utils.Log, logPath);

                var args = new ArgumentsBuilder()
                    .Add("-batchmode")
                    .Add("-projectPath").Add(env.UnityProjectDir)
                    .Add("-logFile").Add("-")
                    .Add(extraUnityEditorArgs, false);
                if (disableGraphics)
                    args = args.Add("-nographics");
                if (quit)
                    args = args.Add("-quit");

                var cmd = Cli.Wrap(path)
                        .WithArguments(args.Build())
                        .WithEnvironmentVariables(env.Config.EnvironmentVariables)
                        .WithValidation(CommandResultValidation.None) // Handle process failures manually
                        | (logHandler.OnStdOut, logHandler.OnStdErr);

                Utils.Log.Message($"Executing: {cmd}");
                var op = cmd.ExecuteAsync();
                await op;

                logHandler.Close(); //TODO: IDisposable

                InspectUnityLogFile(logPath, env);

                if (op.Task.Result.ExitCode != 0)
                {
                    ret = op.Task.Result.ExitCode;
                    Utils.Log.Error($"Unity reported a non-zero exit code ({op.Task.Result.ExitCode})");
                }
            }
            Utils.Log.PopBlock();

            return ret;
        }

        public static async Task<int> ToggleAccelerator(AutoEnv env, CacheServerConnectionState state, string ip)
        {
            var projectDir = new NiceIO.NPath(env.UnityProjectDir);

            const string kEditorSettingsFile = "EditorSettings.asset";

            string editorSettingsPath = projectDir.Combine("ProjectSettings").Combine(kEditorSettingsFile);
            if (!File.Exists(editorSettingsPath))
            {
                Utils.Log.Message($"Could not find {editorSettingsPath}");
                return -1;
            }

            const string kCacheServerModeLine = "m_CacheServerMode";
            const string kCacheServerEndpointLine = "m_CacheServerEndpoint";

            List<string> readText = (await File.ReadAllLinesAsync(editorSettingsPath)).ToList();

            int serverModeLineId = readText.FindIndex(x => x.Contains(kCacheServerModeLine));
            int endpointLineId = readText.FindIndex(x => x.Contains(kCacheServerEndpointLine));

            if (serverModeLineId != -1)
            {
                readText[serverModeLineId] = ($"  {kCacheServerModeLine}: {(int)state}");
            }
            else
            {
                Utils.Log.Message($"variable {kCacheServerModeLine} not found");
                return -2;
            }

            if (endpointLineId != -1)
            {
                if (!string.IsNullOrEmpty(ip))
                    readText[endpointLineId] = ($"  {kCacheServerEndpointLine}: {ip}");
            }
            else
            {
                Utils.Log.Message($"variable {kCacheServerEndpointLine} not found");
                return -3;
            }

            await File.WriteAllLinesAsync(editorSettingsPath, readText);

            return 0;
        }

        internal static async Task<string> FindUnityEditorExecutable(AutoEnv env)
        {
            // Try Unity Hub
            var path = await UnityInstallationTasksHub.GetUnityEditorExecutableInternal(env.UnityEditorVersion);
            if (path != null)
            {
                if (File.Exists(path))
                    return path;
            }

            // Try manual (offline) installation location
            path = UnityInstallationTasksManual.GetUnityEditorExecutableInternal(env.UnityEditorVersion);
            if (path != null)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
