using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NiceIO;
using NBG.Core;

namespace Automation
{
    static class Program
    {
        const string kDefaultConfig = "automation.json";

        static string AutomationConfigFile;
        static string CheckoutDir;

        static async Task<int> Main(string[] args)
        {
            var root = new RootCommand();
            root.Description = "No Brakes Games automation.";

            // Command: checkEnvironment
            {
                var cmd = new Command("checkEnvironment");
                cmd.Handler = CommandHandler.Create(Test);
                root.Add(cmd);
            }
            // Command: inspectLog
            {
                var cmd = new Command("inspectLog");
                cmd.AddArgument(new Argument<string>("filePath", $"Log file path (absolute or relative to BuildSystem/Dependencies)"));
                cmd.AddOption(new Option<string>("--root", "Path prefix to remove when inspecting log entries"));
                cmd.AddOption(new Option<string>("--rules", "Path of the inspection rule configuration file (json)."));
                cmd.Handler = CommandHandler.Create<string, string, string>(InspectLog);
                root.Add(cmd);
            }
            // Command: initializeUnityProject
            {
                var cmd = new Command("initializeUnityProject", "Perform cleanups and workarounds as needed");
                cmd.Handler = CommandHandler.Create(InitializeUnityProject);
                root.Add(cmd);
            }
            // Command: writeBuildVersion
            {
                var cmd = new Command("writeBuildVersion");
                cmd.AddArgument(new Argument<int>("buildNumber", "Reference number of the build system."));
                cmd.Handler = CommandHandler.Create<int>(WriteBuildVersion);
                root.Add(cmd);
            }
            // Command: buildUnityProject
            {
                var cmd = new Command("buildUnityProject");
                cmd.AddArgument(new Argument<BuildPlatform>("platform"));
                cmd.AddArgument(new Argument<BuildConfiguration>("config"));
                cmd.AddOption(new Option<BuildScripting>("--scripting", () => BuildScripting.Auto));
                cmd.AddOption(new Option<bool>("--cleanArtifacts", () => true));
                cmd.AddOption(new Option<string>("--extraUnityEditorArgs"));
                cmd.AddOption(new Option<string>("--buildVersion"));
                cmd.Handler = CommandHandler.Create<BuildPlatform, BuildConfiguration, BuildScripting, bool, string, string>(BuildUnityProject);
                root.Add(cmd);
            }
            // Command: installUnityEditor
            {
                var cmd = new Command("installUnityEditor");
                cmd.AddArgument(new Argument<string>("version"));
                cmd.AddArgument(new Argument<string>("changeset"));
                cmd.AddOption(new Option<bool>("--hub", "Use Unity Hub (danger: very unreliable!)"));
                cmd.AddOption(new Option<string>("--modules", "Comma-separated list of modules to install"));
                cmd.Handler = CommandHandler.Create<string, string, bool, string>(InstallUnityEditor);
                root.Add(cmd);
            }
            // Command: runUnityEditor
            {
                var cmd = new Command("runUnityEditor");
                cmd.AddArgument(new Argument<string>("extraUnityEditorArgs"));
                cmd.AddOption(new Option<string>("--logFileName"));
                cmd.AddOption(new Option<bool>("--cleanArtifacts", () => false));
                cmd.AddOption(new Option<bool>("--publishArtifacts", () => false));
                cmd.AddOption(new Option<bool>("--disableGraphics", () => true));
                cmd.Handler = CommandHandler.Create<string, string, bool, bool, bool>(RunUnityEditor);
                root.Add(cmd);
            }
            // Command: runUnityTests
            {
                var cmd = new Command("runUnityTests");
                cmd.AddArgument(new Argument<EditorTestsPlatform>("testPlatform"));
                cmd.AddOption(new Option<bool>("--cleanArtifacts", () => true));
                cmd.Handler = CommandHandler.Create<EditorTestsPlatform, bool>(RunUnityTests);
                root.Add(cmd);
            }
            // Command: deployToSteam
            {
                var cmd = new Command("deployToSteam");
                cmd.AddArgument(new Argument<string>("username"));
                cmd.AddArgument(new Argument<string>("password"));
                cmd.AddArgument(new Argument<string>("buildTemplateVdfPath"));
                cmd.AddOption(new Option<string>("--steamSentryFiles", "To bypass Steam Guard, copy Steam Sentry files to '/steamSentryFiles/username' and set this option."));
                cmd.AddOption(new Option<string>("--buildDescSuffix", "Extra information to add to build description."));
                cmd.AddOption(new Option<string>("--setLiveBranch", "Publish build to the specified branch."));
                cmd.Handler = CommandHandler.Create<string, string, string, string, string, string>(DeployToSteam);
                root.Add(cmd);
            }
            //Command: toggleUnityAccelerator
            {
                var cmd = new Command("toggleUnityAccelerator");
                cmd.AddArgument(new Argument<CacheServerConnectionState>("state"));
                cmd.AddOption(new Option<string>("--ip", "ip"));
                cmd.Handler = CommandHandler.Create<CacheServerConnectionState, string>(ToggleAccelerator);
                root.Add(cmd);
            }
            // Command: deployBuild
            {
                var cmd = new Command("deployBuild");
                cmd.AddArgument(new Argument<BuildPlatform>("platform"));
                cmd.AddArgument(new Argument<string>("target"));
                cmd.Handler = CommandHandler.Create<BuildPlatform, string>(DeployBuild);
                root.Add(cmd);
            }
            // Command: runRuntimeTests
            {
                var cmd = new Command("runRuntimeTests");
                cmd.AddArgument(new Argument<BuildPlatform>("platform"));
                cmd.AddArgument(new Argument<string>("target", () => { return string.Empty; }));
                cmd.Handler = CommandHandler.Create<BuildPlatform, string>(RunRuntimeTests);
                root.Add(cmd);
            }
            // Command: runSteamRuntimeTests
            {
                var cmd = new Command("runSteamRuntimeTests");
                cmd.AddArgument(new Argument<BuildPlatform>("platform"));
                cmd.AddArgument(new Argument<string>("username"));
                cmd.AddArgument(new Argument<string>("password"));
                cmd.AddOption(new Option<string>("--steamSentryFiles", "To bypass Steam Guard, copy Steam Sentry files to '/steamSentryFiles/username' and set this option."));
                cmd.Handler = CommandHandler.Create<BuildPlatform, string, string, string>(RunSteamRuntimeTests);
                root.Add(cmd);
            }
            // Command: testRuntimeTestServer
            {
                var cmd = new Command("testRuntimeTestServer");
                cmd.AddArgument(new Argument<int>("durationSeconds"));
                cmd.Handler = CommandHandler.Create<int>(TestRuntimeTestServer);
                root.Add(cmd);
            }
            // Command: signAPK
            {
                var cmd = new Command("signApk");
                cmd.AddArgument(new Argument<string>("keystorePassword"));
                cmd.AddArgument(new Argument<string>("aliasName"));
                cmd.AddArgument(new Argument<string>("aliasPassword"));
                cmd.Handler = CommandHandler.Create<string, string, string>(SignAPK);
                root.Add(cmd);
            }

            // Global: automation config file name
            {
                var opt = new Option<string>("--config", () => kDefaultConfig);
                opt.Description = "Override the global automation configuration file (json).";
                root.AddGlobalOption(opt);

                AutomationConfigFile = root.Parse(args).ValueForOption(opt);
                if (string.IsNullOrEmpty(AutomationConfigFile))
                {
                    Core.Utils.Log.Message($"--config is not set. Defaulting to {kDefaultConfig}");
                    AutomationConfigFile = kDefaultConfig;
                }
                else
                {
                    Core.Utils.Log.Message($"Expecting config in: {AutomationConfigFile}");
                }
            }

            // Global: checkout directory path
            {
                var opt = new Option<string>("--checkoutDir");
                opt.Description = "Override the checkout directory path";
                root.AddGlobalOption(opt);

                CheckoutDir = root.Parse(args).ValueForOption(opt);
                if (string.IsNullOrEmpty(CheckoutDir))
                {
                    try
                    {
                        CheckoutDir = NPath.CurrentDirectory.ParentContaining(AutomationConfigFile);
                        Core.Utils.Log.Message($"Checkout directory: {CheckoutDir}");
                    }
                    catch
                    {
                        Core.Utils.Log.Message($"Checkout directory can't be determined.");
                    }
                }
            }

            // Global: interactive shell
            {
                var opt = new Option<bool>("--requireInteractiveShell", () => false);
                opt.Description = "Require interactive shell";
                root.AddGlobalOption(opt);

                var require = root.Parse(args).ValueForOption(opt);
                if (require)
                {
                    if (Core.Utils.IsInteractiveShell())
                    {
                        Core.Utils.Log.Message($"Passed interactive shell requirement.");
                    }
                    else
                    {
                        Core.Utils.Log.Error($"Failed interactive shell requirement.");
                        return -200;
                    }
                }
            }

            return await root.InvokeAsync(args);
        }

        static RepoConfig ParseAutomationConfig()
        {
            try
            {
                var configFile = Path.Combine(CheckoutDir, AutomationConfigFile);
                var opts = new JsonSerializerOptions();
                opts.IncludeFields = true;
                var config = JsonSerializer.Deserialize<RepoConfig>(File.ReadAllText(configFile), opts);
                return config;
            }
            catch (Exception e)
            {
                Core.Utils.Log.Message($"Failed to parse automation config file: {e.Message}");
                return null;
            }
        }

        static void Test()
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();
        }

        static int InspectLog(string filePath, string root, string rules)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            if (!Path.IsPathFullyQualified(filePath))
                filePath = Path.Combine(env.DependenciesDir, filePath);

            return Core.Inspection.ParseLogFile(filePath, root, rules);
        }

        static async Task<int> RunUnityTests(EditorTestsPlatform testPlatform, bool cleanArtifacts)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Unity.RunTests(env, testPlatform, cleanArtifacts);
        }

        static async Task<int> InitializeUnityProject()
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Unity.InitializeProject(env);
        }

        static async Task<int> WriteBuildVersion(int buildNumber)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Utils.WriteBuildVersion(env, buildNumber);
        }

        static async Task<int> BuildUnityProject(BuildPlatform platform, BuildConfiguration config, BuildScripting scripting, bool cleanArtifacts, string extraUnityEditorArgs, string buildVersion)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            if (cleanArtifacts)
                Core.Utils.CleanArtifacts(env);

            return await Core.Unity.BuildProject(env, platform, config, scripting, extraUnityEditorArgs, buildVersion);
        }

        static async Task<int> RunUnityEditor(string extraUnityEditorArgs, string logFileName, bool cleanArtifacts, bool publishArtifacts, bool disableGraphics)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            if (cleanArtifacts)
                Core.Utils.CleanArtifacts(env);

            int ret = await Core.Unity.RunEditor(env, extraUnityEditorArgs, logFileName, disableGraphics, true);

            if (publishArtifacts)
                Core.Utils.PublishArtifacts(env);

            return ret;
        }

        static async Task<int> InstallUnityEditor(string version, string changeset, bool hub, string modules)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Unity.InstallEditor(env, hub, version, changeset, modules);
        }

        static async Task<int> DeployToSteam(string username, string password, string buildTemplateVdfPath, string steamSentryFiles, string buildDescSuffix, string setLiveBranch)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Steam.Deploy(env, username, password, buildTemplateVdfPath, steamSentryFiles, buildDescSuffix, setLiveBranch);
        }

        static async Task<int> ToggleAccelerator(CacheServerConnectionState state, string ip)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Unity.ToggleAccelerator(env, state, ip);
        }

        static async Task<int> DeployBuild(BuildPlatform platform, string target)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.RuntimeTests.Deploy(env, platform, target);
        }

        static async Task<int> RunRuntimeTests(BuildPlatform platform, string target)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.RuntimeTests.Run(env, platform, target);
        }

        static async Task<int> RunSteamRuntimeTests(BuildPlatform platform, string username, string password, string steamSentryFiles)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.RuntimeTests.RunSteam(env, platform, username, password, steamSentryFiles);
        }

        static async Task<int> TestRuntimeTestServer(int durationSeconds)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.RuntimeTests.RunTest(env, durationSeconds);
        }

        static async Task<int> SignAPK(string keystorePassword, string aliasName, string aliasPassword)
        {
            var automationConfig = ParseAutomationConfig();
            if (automationConfig == null)
                return -1;

            var env = new Core.AutoEnv(CheckoutDir, automationConfig);
            env.Dump();

            return await Core.Android.SignAPK(env, keystorePassword, aliasName, aliasPassword);
        }
    }
}
