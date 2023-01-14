using CliWrap;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using NiceIO;
using System.Linq;
using System.Diagnostics;

namespace Automation.Core
{
    public static class Steam
    {
        static string _steamCmdDir;
        static string _steamCmd;

        static string _steamDir;
        static string _steam;

        const string kProcessName = "steam";

        public enum Target
        {
            SteamCMD,
            Steam
        }

        static void EnsureInitialized(AutoEnv env)
        {
            if (_steamCmd != null)
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _steamCmdDir = Path.Combine(env.CoreModuleDir, "Publishing", "Steam", "bin");
                _steamCmd = Path.Combine(_steamCmdDir, "steamcmd.exe");

                _steamDir = "C:/Program Files (x86)/Steam";
                _steam = Path.Combine(_steamDir, "steam.exe");
            }
            else
                throw new NotImplementedException("Current platform is not supported.");

            Utils.Log.Message($"SteamCMD path: {_steamCmd}");
            Utils.Log.Message($"Steam path: {_steam}");
        }

        public static void DeploySteamSentryFiles(AutoEnv env, Target target, string steamSentryFiles, string username)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(steamSentryFiles));
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(username));

            EnsureInitialized(env);
            Utils.Log.PushBlock("Deploying Steam Sentry files");

            var destDir = (target == Target.Steam) ? _steamDir : _steamCmdDir;

            var dir = new NPath(steamSentryFiles).Combine(username);
            Utils.Log.Message($"Looking for Steam Sentry files in '{dir}'.");
            if (dir.Exists())
            {
                Utils.Log.Message($"Found {dir}");
                foreach (var f in dir.CopyFiles(new NPath(destDir), false))
                    Utils.Log.Message($"Copied {f}");
            }

            Utils.Log.PopBlock();
        }

        public static async Task LoginSteam(AutoEnv env, Target target, string username, string password)
        {
            EnsureInitialized(env);
            Utils.Log.PushBlock("Logging in to Steam");

            Process steamProcess;
            if (IsSteamProcessReady(out steamProcess))
            {
                Utils.Log.PopBlock();
                return; // Use existing Steam user
            }

            if (steamProcess != null)
            {
                // Stop Steam client if it is running without an active user (login window)
                Utils.StopProcess(steamProcess);
                steamProcess = null;
            }

            Command cmd;
            if (target == Target.Steam)
            {
                cmd = Cli.Wrap(_steam)
                    .WithArguments(a => a
                        .Add("-login").Add(username).Add(password)
                        .Add("-silent")
                    )
                    .WithEnvironmentVariables(env.Config.EnvironmentVariables);
            }
            else
            {
                cmd = Cli.Wrap(_steamCmd)
                    .WithArguments(a => a
                        .Add("+logon").Add(username).Add(password)
                        .Add("+quit")
                    )
                    .WithEnvironmentVariables(env.Config.EnvironmentVariables);
            }
            cmd = cmd | (Utils.Log.Message, Utils.Log.Message);

            //Utils.Log.Message($"Executing: {cmd}"); // Don't print password
            var op = cmd.ExecuteAsync();

            if (target == Target.Steam)
            {
                const int kTimeoutSeconds = 60;
                int seconds = 0;

                while (!IsSteamProcessReady(out _))
                {
                    ++seconds;
                    if (seconds == kTimeoutSeconds)
                        throw new Exception("Steam failed to launch.");

                    Utils.Log.Message($"Waiting for Steam to launch: {seconds}/{kTimeoutSeconds}...");
                    await Task.Delay(1000);
                }
            }
            else
            {
                await op;
            }
            
            Utils.Log.PopBlock();
        }

        private static bool IsSteamProcessReady(out Process runningProcess)
        {
            runningProcess = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam\ActiveProcess"))
                {
                    if (key == null)
                        return false;

                    bool ready = true;
                    if (key.GetValue("pid") is int pid)
                    {
                        if (pid != 0)
                        {
                            try
                            {
                                runningProcess = Process.GetProcessById(pid);
                                ready &= (runningProcess.ProcessName.ToLower() == kProcessName);
                            }
                            catch
                            {
                                Utils.Log.Message($"Steam process (pid = {key.GetValue("pid")}) is not running. ");
                                ready = false;
                            }
                        }
                        else
                        {
                            ready = false;
                        }
                        
                        ready &= (pid != 0); // Process is running
                    }

                    if (!ready)
                        runningProcess = null;

                    if (key.GetValue("ActiveUser") is int userid)
                        ready &= (userid != 0); // User is signed in

                    if (ready)
                        Utils.Log.Message($"Steam process (pid = {key.GetValue("pid")}) has user (id = {key.GetValue("ActiveUser")}) logged in. ");

                    return ready;
                }
            }
            else
            {
                throw new NotImplementedException("Steam process detection is not implemented on current platform.");
            }
        }

        public static async Task<int> Deploy(AutoEnv env, string username, string password, string buildTemplateVdfPath, string steamSentryFiles, string buildDescSuffix, string setLiveBranch)
        {
            EnsureInitialized(env);

            // Deploy Steam sentry files to bypass Steam Guard
            if (string.IsNullOrWhiteSpace(steamSentryFiles))
            {
                Utils.Log.Message($"Assuming Steam Guard has been activated.");
            }
            else
            {
                DeploySteamSentryFiles(env, Target.SteamCMD, steamSentryFiles, username);
            }

            // Login to Steam, collect updates
            await LoginSteam(env, Target.SteamCMD, username, password);

            // Patch build template
            var generatedVdf = new NPath(buildTemplateVdfPath).Parent.Combine("build.generated.vdf");
            {
                var vdf = File.ReadAllText(buildTemplateVdfPath);

                // Build description
                string desc = string.Empty;

                try
                {
                    var versions = new NPath(env.BuildDependencyDir).Contents("Version.json", true);
                    if (versions.Count() == 0)
                        throw new InvalidDataException("Version.json not found");
                    else if (versions.Count() > 1)
                        throw new InvalidDataException("Multiple Version.json found");
                    var path = versions.First();
                    Utils.Log.Message($"Reading {path}");

                    var opts = new JsonSerializerOptions();
                    opts.IncludeFields = true;
                    var data = JsonSerializer.Deserialize<NBG.Core.BuildVersionData>(File.ReadAllText(path), opts);
                    desc = $"{data.branch} ({data.hash})";
                    if (!string.IsNullOrWhiteSpace(buildDescSuffix))
                        desc = $"{desc} {buildDescSuffix}";
                }
                catch (Exception e)
                {
                    Utils.Log.Message($"Failed to read Version.json: {e.Message}");
                    desc = "No description";
                }

                vdf = vdf.Replace("REPLACE_ME_DESC", desc);
                Utils.Log.Message($"Setting build description to: {desc}");

                // Live branch
                string liveBranch = setLiveBranch ?? string.Empty;
                vdf = vdf.Replace("REPLACE_ME_SETLIVE", liveBranch);
                Utils.Log.Message($"Setting live branch to: {liveBranch}");

                File.WriteAllText(generatedVdf, vdf);
                Utils.Log.PublishArtifacts(generatedVdf);
            }

            // Publish
            Utils.Log.PushBlock("Publishing to Steam");
            {
                var cmd = Cli.Wrap(_steamCmd)
                        .WithArguments(a => a
                            .Add("+logon").Add(username).Add(password)
                            .Add("+run_app_build").Add(generatedVdf)
                            .Add("+quit")
                            )
                        .WithEnvironmentVariables(env.Config.EnvironmentVariables)
                        | (Utils.Log.Message, Utils.Log.Message);

                Utils.Log.Message($"Executing: {cmd}");
                var op = cmd.ExecuteAsync();
                await op;
            }
            Utils.Log.PopBlock();

            return 0;
        }
    }
}
