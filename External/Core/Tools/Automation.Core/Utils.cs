using CliWrap;
using NBG.Core;
using NiceIO;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Automation.Core
{
    public static class Utils
    {
        public static ILog Log { get; private set; } = new LogTeamCity();

        static int ThreadCount7zip
        {
            get
            {
                int cpus = Environment.ProcessorCount;
                cpus = Math.Max(cpus - 1, 2); // All threads but one. Min 2 as per 7z defaults.
                return cpus;
            }
        }

        public enum CompressionLevel
        {
            Store = 0,
            Fastest = 1,
            Fast = 3,
            Normal = 5,
            Maximum = 7,
            Ultra = 9,
        }

        public static async Task CompressDirectory(string sourceDir, string targetPath, string archiveType = "zip", CompressionLevel compressionLevel = CompressionLevel.Normal)
        {
            var cpus = ThreadCount7zip;
            var source = sourceDir + Path.DirectorySeparatorChar + "*"; // Collect contents, not the root folder itself


            var cmd = Cli.Wrap("7z")
                .WithArguments(a => a
                    .Add("a")
                    .Add($"-mmt{cpus}")
                    .Add($"-mx{(int)compressionLevel}")
                    .Add($"-t{archiveType}")
                    .Add(targetPath)
                    .Add(source))
                | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            await cmd.ExecuteAsync();
        }

        public static async Task<string> GetGitBranch(string workingDir)
        {
            var stdout = new StringBuilder();

            var cmd = Cli.Wrap("git")
                .WithArguments(a => a
                    .Add("branch")
                    .Add("--show-current")
                    )
                .WithWorkingDirectory(workingDir)
                | stdout;

            Utils.Log.Message($"Executing: {cmd}");
            await cmd.ExecuteAsync();


            return RemoveEOL(stdout.ToString());
        }

        public static async Task<string> GetGitCommit(string workingDir)
        {
            var stdout = new StringBuilder();

            var cmd = Cli.Wrap("git")
                .WithArguments(a => a
                    .Add("rev-parse")
                    .Add("--short").Add("HEAD")
                    )
                .WithWorkingDirectory(workingDir)
                | stdout;

            Utils.Log.Message($"Executing: {cmd}");
            await cmd.ExecuteAsync();

            return RemoveEOL(stdout.ToString());
        }

        public static string RemoveEOL(string s)
        {
            return s.Replace("\r", string.Empty).Replace("\n", string.Empty);
        }

        public static async Task<int> WriteBuildVersion(AutoEnv env, int buildNumber)
        {
            try
            {
                var version = new BuildVersionData();
                version.branch = await GetGitBranch(env.CheckoutDir);
                version.hash = await GetGitCommit(env.CheckoutDir);
                version.buildNumber = buildNumber;
                version.versionName = env.Config.GameVersion;

                var versionDir = new NPath(env.UnityProjectDir).Combine("Assets", "StreamingAssets");
                versionDir.EnsureDirectoryExists();
                var versionPath = versionDir.Combine("Version.json");
                Utils.Log.Message($"Writing {versionPath}");
                var opts = new JsonSerializerOptions();
                opts.WriteIndented = true;
                opts.IncludeFields = true;
                File.WriteAllText(versionPath, JsonSerializer.Serialize(version, opts));

                return 0;
            }
            catch (Exception e)
            {
                Utils.Log.Message($"Failed to write Version.json: {e.Message}");

                return -1;
            }
        }

        public static string PlatformToBuildTarget(BuildPlatform platform)
        {
            switch (platform)
            {
                case BuildPlatform.Windows:
                    return "StandaloneWindows";
                case BuildPlatform.MacOS:
                    return "StandaloneOSX";
                case BuildPlatform.Linux:
                    return "StandaloneLinux64";
                case BuildPlatform.Android:
                    return "Android";
                case BuildPlatform.Switch:
                    return "Switch";
                default:
                    throw new NotImplementedException();
            }
        }

        public static CompressionLevel PlatformBuildCompressionLevel(BuildPlatform platform)
        {
            switch (platform)
            {
                case BuildPlatform.Switch:
                    return CompressionLevel.Store; // NSP is already compressed
                default:
                    return CompressionLevel.Normal;
            }
        }

        public static void CleanArtifacts(AutoEnv env)
        {
            Utils.Log.PushBlock("Cleaning Artifacts");

            var artifactsDir = new NiceIO.NPath(env.ArtifactsDir);
            artifactsDir.DeleteIfExists();
            artifactsDir.EnsureDirectoryExists();

            Utils.Log.PopBlock();
        }

        public static void PublishArtifacts(AutoEnv env)
        {
            Utils.Log.PushBlock("Publishing all Artifacts");

            Utils.Log.PublishArtifacts(env.ArtifactsDir);

            Utils.Log.PopBlock();
        }

        public static bool IsInteractiveShell()
        {
            return Environment.UserInteractive;
        }

        public static void StopProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    Utils.Log.Warning($"Killing process {process.Id}.");
                    process.Kill(true);
                }
            }
            catch
            {
            }
            
            process.WaitForExit(10 * 1000);
            Utils.Log.Message($"Process '{process.ProcessName}' has been killed.");
        }
    }
}
