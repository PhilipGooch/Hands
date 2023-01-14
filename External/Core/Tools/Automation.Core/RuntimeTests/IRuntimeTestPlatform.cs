using NBG.Core;
using System;
using System.Threading.Tasks;

namespace Automation.Core
{
    internal interface IRuntimeTestPlatform
    {
        int RunTimeout { get; set; }
        int? RunReturnCode { get; }

        // Prepare the device for running a build: remove old log/save/cache files, etc.
        Task Prepare();

        // The build is assumed to be uncompressed into the standard location (AutoEnv.BuildDependencyDir)
        // Upload it to a device here if necessary
        Task Deploy();

        // Run the build on device
        Task Run(string testServerAddress, Action onTimeout);

        // Cleanup after tests, collect artifacts such as crash dumps, etc.
        Task Cleanup();

        // Take a system level screenshot and save into <path> as PNG
        Task TakeSystemScreenshot(string path);
    }

    internal interface IRuntimeTestPlatformUsesCompanyAndProductNames
    {
        string CompanyName { get; set; }
        string ProductName { get; set; }
    }

    internal static class RuntimeTestPlatform
    {
        public static IRuntimeTestPlatform Create(AutoEnv env, BuildPlatform platform, string target)
        {
            switch (platform)
            {
                case BuildPlatform.Windows:
                    if (!string.IsNullOrWhiteSpace(target))
                        Utils.Log.Message($"Runtime tests on Windows do not expect <target> yet '{target}' was provided.");
                    return new RuntimeTestPlatformDesktop(env, platform);
                case BuildPlatform.Switch:
                    return new RuntimeTestPlatformSwitch(env, platform, target);
                default:
                    throw new System.NotImplementedException();
            }
        }
    }
}
