using CliWrap;
using NBG.Core;
using NiceIO;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Automation.Core
{
    internal class RuntimeTestPlatformDesktop : IRuntimeTestPlatform, IRuntimeTestPlatformUsesCompanyAndProductNames
    {
        const int kDefaultTimeoutSeconds = 120;
        public int? RunReturnCode { get; private set; } = null;

        public int RunTimeout { get; set; } = kDefaultTimeoutSeconds;

        public string CompanyName { get; set; }
        public string ProductName { get; set; }

        AutoEnv _env;
        BuildPlatform _platform;
        NPath _executable;

        internal RuntimeTestPlatformDesktop(AutoEnv env, BuildPlatform platform)
        {
            _env = env;
            _platform = platform;
        }

        public async Task Prepare()
        {
            Utils.Log.PushBlock("Preparing the game on Desktop");

            // Nothing to do

            Utils.Log.PopBlock();

            await Task.Yield();//
        }

        public async Task Deploy()
        {
            Utils.Log.PushBlock("Deploying the game on Desktop");

            var buildDir = new NPath(_env.BuildDependencyDir);
            _executable = FindExecutable(_platform, buildDir);
            Utils.Log.Message($"Found executable: {_executable}");

            Utils.Log.PopBlock();

            await Task.Yield();//
        }

        public async Task Run(string testServerAddress, Action onTimeout)
        {
            Utils.Log.PushBlock("Running the game on Desktop");

            var workingDir = _executable.Parent;
            var cmd = Cli.Wrap(_executable)
                    .WithWorkingDirectory(workingDir)
                    .WithValidation(CommandResultValidation.None)
                    .WithArguments(a => a
                        .Add($"--automation={testServerAddress}")
                        .Add("-screen-fullscreen").Add("0") //TODO: expose configuration
                        .Add("-screen-width").Add("1920") //TODO: expose configuration
                        .Add("-screen-height").Add("1080") //TODO: expose configuration
                    )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd} in {workingDir}");
            var op = cmd.ExecuteAsync();

            const int kStepSeconds = 5;
            int seconds = 0;
            while (!op.Task.IsCompleted)
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
            RunReturnCode = op.Task.Result.ExitCode;
            var dword = string.Format("0x{0:X8}", RunReturnCode);
            Utils.Log.Message($"Game process has exited with code: {RunReturnCode} ({dword})");

            Utils.Log.PopBlock();
        }

        public async Task TakeSystemScreenshot(string path)
        {
            await GrabSystemScreenshot(path);
        }

        public static async Task GrabSystemScreenshot(string path)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Utils.Log.Warning($"{nameof(GrabSystemScreenshot)} is not implemented on this platform.");
                return;
            }

            Utils.Log.PushBlock("Taking screenshot");
            Utils.Log.Message($"Screenshot path: {path}");

            try
            {
                int width = 1920;
                int height = 1080;
                using (var screen = Graphics.FromHwnd(IntPtr.Zero))
                {
                    width = (int)screen.VisibleClipBounds.Width;
                    height = (int)screen.VisibleClipBounds.Height;
                }

                using (var b = new Bitmap(width, height))
                using (var g = Graphics.FromImage(b))
                {
                    g.CopyFromScreen(0, 0, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
                    b.Save(path, ImageFormat.Png);
                }
            }
            catch (Exception e)
            {
                Utils.Log.Message($"Error: {e.Message}");
            }

            Utils.Log.PopBlock();

            await Task.Yield();//
        }

        public async Task Cleanup()
        {
            Utils.Log.PushBlock("Cleaning up the game on Desktop");

            await WaitForUnityCrashHandler64ToComplete();

            if (CompanyName != null && ProductName != null)
            {
                Utils.Log.Message("Company and Product names have been set. Performing extra steps.");
                await TryToCollectRelevantCrashDump(CompanyName, ProductName);
            }

            Utils.Log.PopBlock();
        }

        static NPath FindExecutable(BuildPlatform platform, NPath root)
        {
            if (platform != BuildPlatform.Windows)
                throw new System.NotImplementedException(); //TODO: support other platforms

            var execs = root.Files("*.exe", true)
                .Where(x => !x.FileName.StartsWith("UnityCrashHandler"));

            var count = execs.Count();
            if (count == 0)
                throw new System.Exception("Failed to find an executable to run.");
            else if (count > 1)
                throw new System.Exception($"Found {count} executables.");

            return execs.First();
        }

        static async Task WaitForUnityCrashHandler64ToComplete()
        {
            Utils.Log.PushBlock("Waiting for UnityCrashHandler64 process(es) to complete");
            try
            {
                var handlers = Process.GetProcessesByName("UnityCrashHandler64");
                if (handlers.Length > 0)
                {
                    Console.WriteLine($"Found {handlers.Length} process(es) running.");

                    const int kStepSeconds = 5;
                    const int kStepLimit = 12;
                    int steps = 0;
                    while (handlers.Any(p => !p.HasExited))
                    {
                        ++steps;
                        if (steps == kStepLimit)
                        {
                            Utils.Log.Warning($"Timeout reached with {handlers.Count(p => !p.HasExited)} process(es) still running.");
                            break;
                        }

                        Utils.Log.Message($"Waiting for process(es) to exit: {steps}/{kStepLimit}...");
                        await Task.Delay(kStepSeconds * 1000);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Log.Warning($"Trouble while waiting: {e.Message}");
            }
            Utils.Log.PopBlock();
        }

        static async Task<bool> TryToCollectRelevantCrashDump(string companyName, string productName)
        {
            Debug.Assert(companyName != null);
            Debug.Assert(productName != null);

            bool ret = false;

            Utils.Log.PushBlock("Trying to collect a relevant crash dump");
            try
            {
                const string kPlayerLog = "Player.log";
                const string kMarker = "A crash has been intercepted by the crash handler";

                var baseDir = NPath.HomeDirectory.Combine("AppData", "LocalLow", companyName, productName);
                var playerLog = baseDir.Combine(kPlayerLog);
                if (!playerLog.Exists())
                    throw new Exception($"Could not find {playerLog}");

                Utils.Log.Message($"Analysing {playerLog}");
                var lines = await File.ReadAllLinesAsync(playerLog);
                var markerLineIndex = -1;
                for (int i = 0; i < lines.Length; ++i)
                {
                    if (lines[i].Contains(kMarker))
                    {
                        markerLineIndex = i;
                        break;
                    }
                }
                if (markerLineIndex == -1)
                    throw new Exception($"Could not find crash handler marker in {kPlayerLog}");

                Utils.Log.Error("Crash!");

                var pathLineIndex = markerLineIndex + 1;
                var path = lines[pathLineIndex];
                path = path.Substring(path.IndexOf('*') + 1).Trim();
                var crashesDir = new NPath(path);
                if (!crashesDir.Exists())
                    throw new Exception($"Could not find {crashesDir}");

                var dirs = crashesDir.Directories();
                if (!dirs.Any())
                    throw new Exception($"No crash dumps found in {crashesDir}");

                var crashPath = dirs.OrderByDescending(x => Directory.GetCreationTime(x)).First();
                Utils.Log.Message($"Collecting {crashPath}");
                Utils.Log.PublishArtifacts(crashPath, crashPath.FileName);
                ret = true;
            }
            catch (Exception e)
            {
                Utils.Log.Message($"Could not collect crash dump: {e.Message}");
            }
            Utils.Log.PopBlock();

            return ret;
        }
    }
}
