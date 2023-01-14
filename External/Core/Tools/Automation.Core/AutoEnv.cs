using System;
using System.IO;

namespace Automation.Core
{
    // Automation environment
    // Helpers for paths in the checkout directory
    public class AutoEnv
    {
        // Repository checkout directory (root)
        public string CheckoutDir { get; private set; }

        // Repository configuration
        public RepoConfig Config { get; private set; }

        // Build system working directory (ignored)
        public string BuildSystemDir { get; private set; }
        // Output (artifacts) directory
        public string ArtifactsDir { get; private set; }
        // Input (dependencies) directory
        public string DependenciesDir { get; private set; }
        // Input (build dependency) directory
        public string BuildDependencyDir { get; private set; }

        // Unity project directory (root)
        public string UnityProjectDir { get; private set; }
        public string UnityEditorVersion { get; private set; }
        public string UnityEditorRevision { get; private set; }

        // Module directory
        public string CoreModuleDir { get; private set; }

        public AutoEnv(string checkoutDir, RepoConfig repoConfig)
        {
            CheckoutDir = checkoutDir;
            Config = repoConfig;

            BuildSystemDir = Path.Combine(CheckoutDir, "BuildSystem");
            ArtifactsDir = Path.Combine(BuildSystemDir, "Artifacts");
            DependenciesDir = Path.Combine(BuildSystemDir, "Dependencies");
            BuildDependencyDir = Path.Combine(DependenciesDir, "Build");

            UnityProjectDir = Path.Combine(CheckoutDir, Config.UnityProjectDir);
            {
                var projectVersionPath = Path.Combine(UnityProjectDir, "ProjectSettings", "ProjectVersion.txt");
                var lines = File.ReadAllLines(projectVersionPath);
                UnityEditorVersion = Unity.GetUnityEditorVersion(lines);
                UnityEditorRevision = Unity.GetUnityEditorRevision(lines);
            }

            if (Config.CoreModulePath == null)
                CoreModuleDir = Path.Combine(CheckoutDir, "External", "Core");
            else
                CoreModuleDir = Path.Combine(CheckoutDir, Config.CoreModulePath);
        }

        public void Dump()
        {
            Utils.Log.Message($"--- Environment ---");
            Utils.Log.Message($"\tInteractive shell     : {Utils.IsInteractiveShell()}");
            Utils.Log.Message($"\tBuild system          : {BuildSystemDir}");
            Utils.Log.Message($"\tArtifacts             : {ArtifactsDir}");
            Utils.Log.Message($"\tDependencies          : {DependenciesDir}");
            Utils.Log.Message($"\tDependency (build)    : {BuildDependencyDir}");
            Utils.Log.Message($"\tUnity project         : {UnityProjectDir}");
            Utils.Log.Message($"\tUnity version         : {UnityEditorVersion}");
            Utils.Log.Message($"\tUnity revision        : {UnityEditorRevision}");
            Utils.Log.Message($"\tCore module           : {CoreModuleDir}");

            if (Config.EnvironmentVariables.Count > 0)
            {
                Utils.Log.Message($"\tEnvironment variable overrides:");
                foreach (var pair in Config.EnvironmentVariables)
                {
                    Utils.Log.Message($"\t\t{pair.Key} = {pair.Value}");
                }
            }
        }

        public string GetEnvironmentVariable(string variable)
        {
            if (Config.EnvironmentVariables.ContainsKey(variable))
                return Config.EnvironmentVariables[variable];
            else
                return Environment.GetEnvironmentVariable(variable);
        }
    }
}
