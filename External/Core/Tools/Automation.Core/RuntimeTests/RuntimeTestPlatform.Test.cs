using NBG.Core;
using NiceIO;
using System;
using System.Threading.Tasks;

namespace Automation.Core
{
    internal class RuntimeTestPlatformTest : IRuntimeTestPlatform
    {
        const int kDefaultTimeoutSeconds = 120;
        public int? RunReturnCode => null; // Test platform expects OnReportTestRunResult.

        public int RunTimeout { get; set; } = kDefaultTimeoutSeconds;

        AutoEnv _env;
        BuildPlatform _platform;

        internal RuntimeTestPlatformTest(AutoEnv env, BuildPlatform platform)
        {
            _env = env;
            _platform = platform;
        }

        public async Task Prepare()
        {
            await Task.Yield();
        }

        public async Task Deploy()
        {
            await Task.Yield();
        }

        public async Task Run(string testServerAddress, Action onTimeout)
        {
            const int kStepSeconds = 5;
            int seconds = 0;
            while (seconds < RunTimeout)
            {
                seconds += kStepSeconds;
                Utils.Log.Message($"Waiting for timeout: {seconds}/{RunTimeout}...");
                await Task.Delay(kStepSeconds * 1000);
            }
            onTimeout?.Invoke();
        }

        public async Task TakeSystemScreenshot(string path)
        {
            await RuntimeTestPlatformDesktop.GrabSystemScreenshot(path);
        }

        public async Task Cleanup()
        {
            await Task.Yield();
        }
    }
}
