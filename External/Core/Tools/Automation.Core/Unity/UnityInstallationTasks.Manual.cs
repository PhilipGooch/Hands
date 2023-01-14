using CliWrap;
using NiceIO;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Automation.Core
{
    class UnityInstallationTasksManual : IUnityInstallationTasks
    {
        static string _installPath;
        static UnityInstallationTasksManual()
        {
            _installPath = GetDefaultUnityEditorsInstallPath();
        }

        AutoEnv _env;
        public UnityInstallationTasksManual(AutoEnv env)
        {
            _env = env;
        }

        public Task<string> GetUnityEditorExecutable(string version)
        {
            return Task.FromResult(GetUnityEditorExecutableInternal(version));
        }

        public async Task<int> InstallUnityEditor(string version, string changeset)
        {
            var artifactsDir = new NPath(_env.ArtifactsDir);
            artifactsDir.EnsureDirectoryExists();

            var installerRemote = $"https://download.unity3d.com/download_unity/{changeset}/Windows64EditorInstaller/UnitySetup64.exe";
            var installerLocal = Path.Combine(artifactsDir, "UnitySetup64.exe");
            var finalPath = Path.Combine(_installPath, version);

            Utils.Log.Message($"Downloading {installerRemote} into {installerLocal}");
            var net = new System.Net.WebClient();
            await net.DownloadFileTaskAsync(installerRemote, installerLocal);

            var cmd = Cli.Wrap(installerLocal)
                    .WithArguments(a => a
                        .Add("/S")
                        .Add($"/D={finalPath}", false)
                    )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
            Utils.Log.Message($"Executing: {cmd}");
            await cmd.ExecuteAsync();

            return 0;
        }

        public async Task<int> InstallUnityEditorModule(string version, string changeset, string module)
        {
            var artifactsDir = new NPath(_env.ArtifactsDir);
            artifactsDir.EnsureDirectoryExists();

            var target = GetTargetSupportInstallerPrefixFromHubModuleName(module);
            var installerRemote = $"https://download.unity3d.com/download_unity/{changeset}/TargetSupportInstaller/UnitySetup-{target}-Support-for-Editor-{version}.exe";
            var installerLocal = Path.Combine(artifactsDir, $"UnitySetup-{target}-Support-for-Editor-{version}.exe");
            var finalPath = Path.Combine(_installPath, version);

            Utils.Log.Message($"Downloading {installerRemote} into {installerLocal}");
            var net = new System.Net.WebClient();
            await net.DownloadFileTaskAsync(installerRemote, installerLocal);

            var cmd = Cli.Wrap(installerLocal)
                    .WithArguments(a => a
                        .Add("/S")
                        .Add($"/D={finalPath}", false)
                    )
                    .WithEnvironmentVariables(_env.Config.EnvironmentVariables)
                    | (Utils.Log.Message, Utils.Log.Message);
            Utils.Log.Message($"Executing: {cmd}");
            await cmd.ExecuteAsync();

            return 0;
        }

        static string GetTargetSupportInstallerPrefixFromHubModuleName(string module)
        {
            module = module.ToLowerInvariant();
            switch (module)
            {
                case "windows-il2cpp":
                    return "Windows-IL2CPP";
                case "android":
                    return "Android";
                default:
                    throw new NotSupportedException($"Can't translate module '{module}' to target support installer prefix.");
            }
        }



        static string GetDefaultUnityEditorsInstallPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const string kDefaultEditorInstallPath = @"C:\Program Files\Unity\Hub\Editor";
                return kDefaultEditorInstallPath;
            }
            else
            {
                throw new NotImplementedException("Don't know how to locate default Unity editors install path on the current platform.");
            }
        }

        public static string GetUnityEditorExecutableInternal(string version)
        {
            return Path.Combine(_installPath, version, "Editor", "Unity.exe");
        }
    }
}
