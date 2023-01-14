using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CliWrap.Builders;
using CliWrap;
using NBG.Core;

namespace Automation.Core
{
    public static class Android
    {
        public static async Task<int> SignAPK(AutoEnv env, string keystorePassword, string aliasName, string aliasPassword)
        {
            var unityPath = await Unity.FindUnityEditorExecutable(env);
            if (unityPath == null)
            {
                Utils.Log.Message($"Could not find Unity {env.UnityEditorVersion} installed.");
                return -1;
            }

            var editorFolder = Path.GetDirectoryName(unityPath);

            var apkSignerPath = GetApkSignerPath(editorFolder);
            if (apkSignerPath == null)
            {
                Utils.Log.Error($"Could not find apksigner.bat for {env.UnityEditorVersion}. Make sure Android support is installed.");
                return -1;
            }

            var apkPath = FindExecutable(env.BuildDependencyDir);
            if (apkPath == null)
            {
                Utils.Log.Error($"Could not find .apk file to sign in {env.BuildDependencyDir}.");
                return -1;
            }

            var keystorePath = GetKeystorePath(env);
            if (keystorePath == null)
            {
                Utils.Log.Error($"Could not find .keystore file in the android publishing folder.");
                return -1;
            }

            var openJDKFolder = GetJavaHomePath(editorFolder);
            if (!Directory.Exists(openJDKFolder))
            {
                Utils.Log.Message($"Could not find Open JDK directory {openJDKFolder}. Make sure Android support is installed.");
                return -1;
            }

            Utils.Log.PushBlock($"Trying to sign {apkPath} with signer {apkSignerPath} and keystore {keystorePath}");

            var environmentVariables = new Dictionary<string, string>(env.Config.EnvironmentVariables);
            environmentVariables["JAVA_HOME"] = openJDKFolder;

            var args = new ArgumentsBuilder()
                    .Add("sign")
                    .Add("-ks").Add(keystorePath)
                    .Add("-ks-pass").Add($"pass:{keystorePassword}")
                    .Add("-ks-key-alias").Add(aliasName)
                    .Add("-key-pass").Add($"pass:{aliasPassword}")
                    .Add(apkPath);

            var cmd = Cli.Wrap(apkSignerPath)
                        .WithArguments(args.Build())
                        .WithEnvironmentVariables(environmentVariables)
                        .WithValidation(CommandResultValidation.None) // Handle process failures manually
                        | (Utils.Log.Message, Utils.Log.Message);

            var op = cmd.ExecuteAsync();
            await op;

            var exitCode = op.Task.Result.ExitCode;
            if (exitCode == 0)
            {
                Utils.Log.PublishArtifacts(apkPath, "Build");
            }
            else
            {
                Utils.Log.Error($"Failed to sign APK. Exit code ({op.Task.Result.ExitCode})");
            }

            Utils.Log.PopBlock();
            return exitCode;
        }

        static string GetAndroidToolsFolder(string editorFolder)
        {
            var androidToolsFolder = Path.Combine(editorFolder, "Data", "PlaybackEngines", "AndroidPlayer", "SDK", "build-tools");
            if (!Directory.Exists(androidToolsFolder))
            {
                Utils.Log.Message($"Could not find Android tools directory {androidToolsFolder}. Make sure Android support is installed.");
                return null;
            }

            var subdirs = Directory.GetDirectories(androidToolsFolder);
            foreach(var dir in subdirs)
            {
                var files = Directory.GetFiles(dir);
                foreach(var file in files)
                {
                    if (Path.GetFileName(file) == "aapt.exe")
                    {
                        return dir;
                    }
                }
            }

            Utils.Log.Message($"Could not find aapt.exe inside tools directories {androidToolsFolder}. Make sure Android support is installed.");
            return null;
        }

        static string GetApkSignerPath(string editorFolder)
        {
            var toolsPath = GetAndroidToolsFolder(editorFolder);
            if (toolsPath != null)
            {
                return Path.Combine(toolsPath, "apksigner.bat");
            }
            return null;
        }

        static string FindExecutable(string root)
        {
            if (!Directory.Exists(root))
                throw new System.Exception($"{root} directory does not exist!");
            var execs = Directory.GetFiles(root, "*.apk");

            var count = execs.Count();
            if (count == 0)
                throw new System.Exception("Failed to find an android apk.");
            else if (count > 1)
                throw new System.Exception($"Found {count} apks.");

            return execs.First();
        }

        static string GetKeystorePath(AutoEnv env)
        {
            var androidPublishingPath = Path.Combine(env.CheckoutDir, "Publishing", "Android");
            if (!Directory.Exists(androidPublishingPath))
            {
                Utils.Log.Message($"Could not find android publishing directory {androidPublishingPath}.");
                return null;
            }

            var keystores = Directory.GetFiles(androidPublishingPath, "*.keystore");
            if (keystores.Length > 0)
            {
                return keystores.First();
            }

            return null;
        }

        static string GetJavaHomePath(string editorFolder)
        {
            return Path.Combine(editorFolder, "Data", "PlaybackEngines", "AndroidPlayer", "OpenJDK");
        }
    }
}
