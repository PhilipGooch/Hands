using CliWrap;
using NBG.Core;
using NiceIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBG.Automation.RuntimeTests;

namespace Automation.Core
{
    class RuntimeTestPlatformSwitch : IRuntimeTestPlatform
    {
        const int kDefaultTimeoutSeconds = 300;
        public int? RunReturnCode => null; // Switch does not have a process return code, expect OnReportTestRunResult.

        public int RunTimeout { get; set; } = kDefaultTimeoutSeconds;

        AutoEnv _env;
        BuildPlatform _platform;
        string _target;
        string _appId;

        const string kNINTENDO_SDK_ROOT = "NINTENDO_SDK_ROOT";
        const string kNINTENDO_SWITCH_APPID = "NINTENDO_SWITCH_APPID";
        const string kSnapshotDumpPrefix = "[SnapShotDumper] Start dumping to";
        const string kFailurePattern = "Result: Failure, ExitKind:";
        const string kSuccessPattern = "Result: Success, ExitKind:";

        string nintendoRoot;
        string cliToolsDir;
        string controlTargetPath;
        string runOnTargetPath;
        string targetShellPath;
        string targetOutputPath;

        string snapshotPath;

        internal RuntimeTestPlatformSwitch(AutoEnv env, BuildPlatform platform, string target)
        {
            _env = env;
            _platform = platform;
            _target = target;

            _appId = _env.GetEnvironmentVariable(kNINTENDO_SWITCH_APPID);
            if (string.IsNullOrWhiteSpace(_appId))
                throw new Exception($"{kNINTENDO_SWITCH_APPID} is not set.");

            nintendoRoot = _env.GetEnvironmentVariable(kNINTENDO_SDK_ROOT);
            cliToolsDir = Path.Combine(nintendoRoot, "Tools", "CommandLineTools");
            controlTargetPath = Path.Combine(cliToolsDir, "ControlTarget.exe");
            runOnTargetPath = Path.Combine(cliToolsDir, "RunOnTarget.exe");
            targetShellPath = Path.Combine(cliToolsDir, "TargetShell.exe");
            targetOutputPath = Path.Combine(env.ArtifactsDir, "sdcard");

            Utils.Log.Message($"ControlTarget.exe @ {controlTargetPath}");
            Utils.Log.Message($"RunOnTarget.exe @ {runOnTargetPath}");
            Utils.Log.Message($"TargetShell.exe @ {targetShellPath}");
            Utils.Log.Message($"Target output @ {targetOutputPath}");
        }

        public async Task Prepare()
        {
            Utils.Log.PushBlock("Preparing Switch devkit");

            Utils.Log.PushBlock("Unregister all targets");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments("unregister-all")
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PushBlock("Register target");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                        .Add("register")
                        .Add($"--target").Add(_target)
                        )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PushBlock("Check/Set htc version");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments("set-target-htc-generation --generation 2")
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            await ConnectToTarget();

            Utils.Log.PushBlock("Delete save games");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments("delete-all-savedata")
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PushBlock("Format SD card");
            {
                var devMenuDir = Path.Combine(nintendoRoot, "TargetTools", "NX-NXFP2-a64", "DevMenuCommand", "Release");
                var devMenuPath = Path.Combine(devMenuDir, "DevMenuCommand.nsp");
                var cmd = Cli.Wrap(runOnTargetPath)
                    .WithArguments($"{devMenuPath} -- sdcard format")
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PushBlock("Reset Switch target");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                     .WithArguments(a => a
                        .Add("reset")
                        .Add($"--target").Add(_target)
                        )
                     .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                     | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PushBlock("Waiting for target to reset");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                    .Add("wait-detect")
                    .Add($"--target").Add(_target)
                    )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            await ConnectToTarget();

            await TerminateAnyRunningApp();
            await UninstallApp();

            /*Utils.Log.PushBlock("Delete save data");
            {
                var cmd = Cli.Wrap(targetShellPath)
                    .WithArguments(a => a
                        .Add("delete-savedata")
                        .Add($"--application-id").Add(_appId)
                        )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }*///TODO: figure out why this hangs, eventually

            // Utils.Log.PushBlock("Export settings to artifacts");
            // {
            // var outputPath = Path.Combine(_env.ArtifactsDir, "DeviceSettings");

            // var cmd = Cli.Wrap(targetShellPath)
            // .WithArguments(a => a
            // .Add("export-setting")
            // .Add("--directory-path").Add(outputPath)
            // )
            // .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
            // | (Utils.Log.Message, Utils.Log.Message);
            // Utils.Log.Message($"Executing: {cmd}");
            // var result = await cmd.ExecuteAsync();
            // }
            // Utils.Log.PopBlock();

            Utils.Log.PopBlock(); // root
        }

        public async Task Deploy()
        {
            Utils.Log.PushBlock("Deploying the game to a Switch devkit");

            var devMenuDir = Path.Combine(nintendoRoot, "TargetTools", "NX-NXFP2-a64", "DevMenuCommand", "Release");
            var devMenuPath = Path.Combine(devMenuDir, "DevMenuCommand.nsp");
            Utils.Log.Message($"DevMenuCommand.nsp @ {devMenuPath}");

            var nspPath = FindNSPFile(new NPath(_env.BuildDependencyDir));
            Utils.Log.Message($"nsp: {nspPath}");

            var cmd = Cli.Wrap(controlTargetPath)
                .WithArguments(a => a
                        .Add($"install-application {nspPath}", false)
                        )
                | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            var result = await cmd.ExecuteAsync();

            Utils.Log.PopBlock();

            await Task.Yield();
        }

        public async Task Run(string testServerAddress, Action onTimeout)
        {
            Utils.Log.PushBlock("Running the game on a Switch devkit");

            var automationCompleted = false;
            var cmd = Cli.Wrap(runOnTargetPath)
               .WithArguments(a => a
                       .Add($"{_appId}", false)
                       .Add("--pattern-failure-exit").Add(kFailurePattern)
                       .Add("--pattern-success-exit").Add(kSuccessPattern)
                       .Add($"-- --automation={testServerAddress}", false)
                       )
               .WithValidation(CommandResultValidation.None)
               | ((string line) =>
               {
                   Utils.Log.Message(line);
                   if (line.Contains(NBG.Automation.RuntimeTests.Settings.AutomationCompleteLogMessage))
                   {
                       Utils.Log.Message("Detected automation completion signal.");
                       automationCompleted = true;
                   }
                   else if (line.Contains(kSnapshotDumpPrefix))
                   {
                       snapshotPath = line.Substring(kSnapshotDumpPrefix.Length).Trim();
                       Utils.Log.Message($"Detected snapshot dump at: {snapshotPath}");
                       Utils.Log.Error("Crash!");
                   }
                   else if (line.Contains(kFailurePattern))
                   {
                       automationCompleted = true;
                   }
                   else if (line.Contains(kSuccessPattern))
                   {
                       automationCompleted = true;
                   }

               }, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            var op = cmd.ExecuteAsync();

            const int kStepSeconds = 5;
            int seconds = 0;
            while (!op.Task.IsCompleted || !automationCompleted)
            {
                seconds += kStepSeconds;
                if (seconds >= RunTimeout)
                {
                    Utils.Log.Error("Timeout reached.");
                    onTimeout?.Invoke();
                    break;
                }

                Utils.Log.Message($"Waiting for game process to exit: {seconds}/{RunTimeout}...");
                await Task.Delay(kStepSeconds * 1000);
            }

            try
            {
                var p = Process.GetProcessById(op.ProcessId);
                if (p != null && !p.HasExited)
                {
                    Utils.Log.Warning($"Killing process {op.ProcessId}.");
                    p.Kill(true);
                }
            }
            catch
            {
            }

            await op;
            var retCode = op.Task.Result.ExitCode;
            Utils.Log.Message($"Monitoring process has exited with code: {retCode}");
            Utils.Log.Message($"Automation completed: {automationCompleted}");

            Utils.Log.PopBlock();
        }

        public async Task Cleanup()
        {
            Utils.Log.PushBlock("Cleaning up Switch devkit");

            Utils.Log.PushBlock("Extracting target output Data");
            {
                var devMenuDir = Path.Combine(nintendoRoot, "TargetTools", "NX-NXFP2-a64", "DevMenuCommand", "Release");
                var devMenuPath = Path.Combine(devMenuDir, "DevMenuCommand.nsp");
                var cmd = Cli.Wrap(runOnTargetPath)
                    .WithArguments($"{devMenuPath} -- debug copy --source sdcard:/{Settings.AutomationDirName} --destination {targetOutputPath}/")
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    .WithValidation(CommandResultValidation.None)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            if (snapshotPath != null)
            {
                Utils.Log.PushBlock("Trying to collect a snapshot dump");
                try
                {
                    Utils.Log.Message($"Collecting {snapshotPath}");
                    Utils.Log.PublishArtifacts(snapshotPath);
                }
                catch (Exception e)
                {
                    Utils.Log.Message($"Could not collect the snapshot dump: {e.Message}");
                }
                Utils.Log.PopBlock();
            }

            await TerminateAnyRunningApp();
            await UninstallApp();

            Utils.Log.PushBlock("Disconnect target");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                        .Add("disconnect")
                        .Add($"--target").Add(_target)
                        )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    .WithValidation(CommandResultValidation.None)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();

            Utils.Log.PopBlock(); // root
        }

        async Task ConnectToTarget()
        {
            Utils.Log.PushBlock("Connect to Switch target");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                    .Add("connect")
                    .Add($"--target").Add(_target)
                    .Add($"--force")
                    )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();
        }

        async Task TerminateAnyRunningApp()
        {
            Utils.Log.PushBlock("Terminate any running application");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                        .Add("terminate")
                        )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    .WithValidation(CommandResultValidation.None)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();  
        }

        async Task UninstallApp()
        {
            Utils.Log.PushBlock("Uninstall previous build");
            {
                var cmd = Cli.Wrap(controlTargetPath)
                    .WithArguments(a => a
                        .Add("uninstall-application")
                        .Add($"--target").Add(_target)
                        .Add(_appId)
                        )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    .WithValidation(CommandResultValidation.None)
                    | (Utils.Log.Message, Utils.Log.Message);
                Utils.Log.Message($"Executing: {cmd}");
                var result = await cmd.ExecuteAsync();
            }
            Utils.Log.PopBlock();
        }

        static NPath FindNSPFile(NPath root)
        {
            var execs = root.Files("*.nsp", true);

            var count = execs.Count();
            if (count == 0)
                throw new System.Exception("Failed to find an nsp file.");
            else if (count > 1)
                throw new System.Exception($"Found {count} nsp files!.");

            return execs.First();
        }

        public async Task TakeSystemScreenshot(string path)
        {
            Utils.Log.PushBlock("Taking screenshot");
            Utils.Log.Message($"Screenshot path: {path}");

            var cmd = Cli.Wrap(controlTargetPath)
                .WithArguments(a => a
                        .Add("take-screenshot")
                        .Add("--full-path").Add(path)
                        .Add("--verbose")
                        )
                .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            var result = await cmd.ExecuteAsync();

            Utils.Log.PopBlock();
        }
    }
}
