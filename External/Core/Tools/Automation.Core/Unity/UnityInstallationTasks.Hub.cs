using CliWrap;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Automation.Core
{
    class UnityInstallationTasksHub : IUnityInstallationTasks
    {
        static string _hubPath;
        static string _hubExecutable;
        static UnityInstallationTasksHub()
        {
            _hubPath = GetUnityHubPath();
            _hubExecutable = Path.Combine(_hubPath, "Unity Hub.exe");
            ValidateUnityHubVersion();
        }

        AutoEnv env;
        public UnityInstallationTasksHub(AutoEnv env)
        {
            this.env = env;
        }

        public Task<string> GetUnityEditorExecutable(string version)
        {
            return GetUnityEditorExecutableInternal(version);
        }

        public Task<int> InstallUnityEditor(string version, string changeset)
        {
            return InstallUnityEditorInternal(version, changeset);
        }

        public Task<int> InstallUnityEditorModule(string version, string changeset, string module)
        {
            return InstallUnityEditorModuleInternal(version, module);
        }



        static string GetUnityHubPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Unity Technologies\Hub"))
                {
                    if (key?.GetValue("InstallLocation") is string location)
                    {
                        return location;
                    }
                }
            }
            else
            {
                throw new NotImplementedException("Don't know how to locate Unity Hub on the current platform.");
            }

            throw new Exception("Could not determine the location of Unity Hub.");
        }

        static void ValidateUnityHubVersion()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(_hubExecutable);
            Utils.Log.Message($"UnityHub version: {versionInfo.FileVersion}");

            bool ok = (versionInfo.FileMajorPart >= 3);
            ok &= (versionInfo.FileMinorPart >= 1);
            ok &= (versionInfo.FileMinorPart >= 1);
            if (!ok)
                throw new Exception("Please upgrade Unity Hub to version 3.1.1 or later.");
        }

        public static async Task<string> GetUnityEditorExecutableInternal(string version)
        {
            string path = null;

            var cmd = Cli.Wrap(_hubExecutable)
                        .WithArguments(a => a
                            .Add("--")
                            .Add("--headless")
                            .Add("editors"))
                        .WithValidation(CommandResultValidation.None) // Unity Hub always returns 1
                        | ((string line) =>
                        {
                            if (path == null)
                                path = TryGetUnityEditorExecutableFromHubOutput(line, version);
                        });

            Utils.Log.Message($"Executing: {cmd}");
            var op = cmd.ExecuteAsync();
            await op;

            return path;
        }

        static string TryGetUnityEditorExecutableFromHubOutput(string line, string version)
        {
            const string kHint = "installed at ";

            try
            {
                if (line.StartsWith(version))
                {
                    var idx = line.IndexOf(kHint);
                    return line.Substring(idx + kHint.Length);
                }
            }
            catch
            {
            }

            return null;
        }

        static async Task<int> InstallUnityEditorInternal(string version, string changeset)
        {
            var cmd = Cli.Wrap(_hubExecutable)
                        .WithArguments(a => a
                            .Add("--")
                            .Add("--headless")
                            .Add("install")
                            .Add("--version").Add(version)
                            .Add("--changeset").Add(changeset)
                            )
                        .WithValidation(CommandResultValidation.None) // Unity Hub always returns 1
                        | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            var op = cmd.ExecuteAsync();
            await op;

            return 0;
        }

        static async Task<int> InstallUnityEditorModuleInternal(string version, string module)
        {
            var cmd = Cli.Wrap(_hubExecutable)
                        .WithArguments(a => a
                            .Add("--")
                            .Add("--headless")
                            .Add("install-modules")
                            .Add("--version").Add(version)
                            .Add("--module").Add(module)
                            .Add("--childModules")
                            )
                        .WithValidation(CommandResultValidation.None) // Unity Hub always returns 1
                        | (Utils.Log.Message, Utils.Log.Message);

            Utils.Log.Message($"Executing: {cmd}");
            var op = cmd.ExecuteAsync();
            await op;

            return 0;
        }
    }
}
