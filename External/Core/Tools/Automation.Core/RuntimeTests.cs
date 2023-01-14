using NBG.Core;
using NBG.Automation.RuntimeTests;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using System.Threading;
using System.Diagnostics;
using System;
using System.IO;
using NiceIO;

namespace Automation.Core
{
    public static class RuntimeTests
    {
        class TestReport
        {
            public string testName;
            public TestStatus status;
            public string message;
            public string details;
        }

        public static async Task<int> RunSteam(AutoEnv env, BuildPlatform platform, string username, string password, string steamSentryFiles)
        {
            switch (platform)
            {
                case BuildPlatform.Windows:
                    return await RunDesktopSteam(env, platform, username, password, steamSentryFiles);
                case BuildPlatform.MacOS:
                    return await RunDesktopSteam(env, platform, username, password, steamSentryFiles);
                case BuildPlatform.Linux:
                    return await RunDesktopSteam(env, platform, username, password, steamSentryFiles);
                default:
                    throw new System.NotSupportedException();
            }
        }

        public static async Task<int> RunTest(AutoEnv env, int durationSeconds)
        {
            var rtp = new RuntimeTestPlatformTest(env, BuildPlatform.Windows);
            rtp.RunTimeout = durationSeconds;
            return await RunInternal(env, rtp);
        }

        static string GetLocalIP()
        {
            // Get the public-facing local ip by creating an UDP socket
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("1.1.1.1", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

        static async Task<int> RunDesktopSteam(AutoEnv env, BuildPlatform platform, string username, string password, string steamSentryFiles)
        {
            // Deploy Steam sentry files to bypass Steam Guard
            if (string.IsNullOrWhiteSpace(steamSentryFiles))
            {
                Utils.Log.Message($"Assuming Steam Guard has been activated.");
            }
            else
            {
                Steam.DeploySteamSentryFiles(env, Steam.Target.Steam, steamSentryFiles, username);
            }

            // Login to Steam, collect updates
            await Steam.LoginSteam(env, Steam.Target.Steam, username, password);

            return await Run(env, platform, null);
        }

        public static async Task<int> Run(AutoEnv env, BuildPlatform platform, string target)
        {
            var rtp = RuntimeTestPlatform.Create(env, platform, target);
            return await RunInternal(env, rtp);
        }

        static async Task<int> RunInternal(AutoEnv env, IRuntimeTestPlatform rtp)
        {
            int retCode = 404;
            bool retCodeReceived = false;

            // Prepare artifact paths
            var artifactsDir = new NPath(env.ArtifactsDir);
            artifactsDir.DeleteIfExists();
            artifactsDir.EnsureDirectoryExists();

            var artifactPaths = new HashSet<string>();
            artifactPaths.Add(env.ArtifactsDir);

            var systemScreenshotPath = new NPath(env.ArtifactsDir).Combine("SystemScreenshots");
            systemScreenshotPath.EnsureDirectoryExists();

            // Prepare the platform
            await rtp.Prepare();

            // Prepare the test server
            var server = new RuntimeTestServer(env);
            server.OnSetTimeout += (timeout) =>
            {
                Utils.Log.Message($"Changing timeout from {rtp.RunTimeout} to {timeout} seconds.");
                rtp.RunTimeout = timeout;
            };
            server.OnLogMessage += (message, buildStatus) =>
            {
                if (buildStatus)
                    Utils.Log.Status(message + " {build.status.text}");
                else
                    Utils.Log.Message(message);
            };
            server.OnHello += (companyName, productName) =>
            {
                var hello = rtp as IRuntimeTestPlatformUsesCompanyAndProductNames;
                if (hello != null)
                {
                    hello.CompanyName = companyName;
                    hello.ProductName = productName;
                }
            };
            server.OnArtifactsPath += (path) =>
            {
                lock (artifactPaths)
                {
                    artifactPaths.Add(path);
                }
            };
            var testSuites = new Dictionary<string, List<TestReport>>();
            server.OnReportTest += (suiteName, testName, status, message, details) =>
            {
                lock (testSuites)
                {
                    List<TestReport> testReports;
                    if (!testSuites.TryGetValue(suiteName, out testReports))
                    {
                        testReports = new List<TestReport>();
                        testSuites.Add(suiteName, testReports);
                    }

                    var report = new TestReport();
                    report.testName = testName;
                    report.status = status;
                    report.message = message;
                    report.details = details;
                    testReports.Add(report);
                }
            };
            server.OnTakeSystemScreenshot += (name) =>
            {
                var path = Path.Combine(systemScreenshotPath, $"{name}.png");
                rtp.TakeSystemScreenshot(path);
            };
            server.OnReportTestRunResult += (returnCode) =>
            {
                retCode = returnCode;
                retCodeReceived = true;
            };
            server.Run();

            // Run the game
            var localIP = GetLocalIP();
            Utils.Log.Message($"Local ip detected as: {localIP}");
            await rtp.Deploy();
            await rtp.Run(localIP, () => {
                rtp.TakeSystemScreenshot(Path.Combine(systemScreenshotPath, $"Automation - OnTimeout.png"));
            });
            await rtp.Cleanup();

            // Cleanup
            await server.Stop();
            ProcessArtifactPaths(artifactPaths);
            ProcessTestReports(testSuites);

            if (rtp.RunReturnCode != null)
            {
                var dword = string.Format("0x{0:X8}", rtp.RunReturnCode);
                Utils.Log.Message($"Process returned {rtp.RunReturnCode} ({dword}).");
                return (int)rtp.RunReturnCode;
            }
            else if (retCodeReceived)
            {
                Utils.Log.Message($"Automation returned {retCode}.");
                return retCode;
            }
            else
            {
                Utils.Log.Error($"Can't determine process exit code.");
                return -1;
            }
        }

        public static async Task<int> Deploy(AutoEnv env, BuildPlatform platform, string target)
        {
            var rtp = RuntimeTestPlatform.Create(env, platform, target);
            await rtp.Prepare();
            await rtp.Deploy();
            await rtp.Cleanup();
            return 0;
        }

        static void ProcessArtifactPaths(IEnumerable<string> artifactsPaths)
        {
            Utils.Log.Message($"Processing artifact paths...");

            foreach (var artifactsPath in artifactsPaths)
            {
                Utils.Log.PushBlock($"Artifacts at {artifactsPath}");
                try
                {
                    var path = new NiceIO.NPath(artifactsPath);
                    if (path.FileExists())
                    {
                        Utils.Log.Message($"Found file {path}");
                    }
                    else if (path.DirectoryExists())
                    {
                        foreach (var filePath in path.Files(true))
                            Utils.Log.Message($"Found file {filePath}");
                    }
                    else
                    {
                        Utils.Log.Message($"Nothing found at {path}");
                    }
                }
                catch
                {
                }
                Utils.Log.PublishArtifacts(artifactsPath);
                Utils.Log.PopBlock();
            }
        }

        static void ProcessTestReports(Dictionary<string, List<TestReport>> testSuites)
        {
            Utils.Log.Message($"Processing test reports...");
            using (var writer = new TeamCityServiceMessages().CreateWriter()) //MAYBETODO: Currently this is TeamCity specific output.
            {
                foreach (var pair in testSuites)
                {
                    using (var testSuite = writer.OpenTestSuite(pair.Key))
                    {
                        var testReports = pair.Value;
                        foreach (var testReport in testReports)
                        {
                            using (var test = testSuite.OpenTest(testReport.testName))
                            {
                                switch (testReport.status)
                                {
                                    case TestStatus.Succeded:
                                        test.WriteStdOutput(testReport.message + "\n\n" + testReport.details);
                                        break;
                                    case TestStatus.Failed:
                                        test.WriteFailed(testReport.message, testReport.details);
                                        break;
                                    case TestStatus.Ignored:
                                        test.WriteStdOutput(testReport.message + "\n\n" + testReport.details);
                                        test.WriteIgnored();
                                        break;
                                    default:
                                        throw new System.NotImplementedException();
                                }
                            }
                        }
                    }
                }
            }
        } // ProcessTestReports
    }
}
