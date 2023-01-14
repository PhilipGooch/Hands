using NBG.Automation.RuntimeTests;
using NBG.Automation.RuntimeTests.Controller;
using NBG.Core;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Base.Tests
{
    public class RuntimeTestSample : Controller
    {
        [System.Serializable]
        public struct SceneToTest
        {
            public SceneField scene;
        }
        [SerializeField] SceneToTest[] _scenesToTest;

        public RuntimeTestSample()
        {
#if UNITY_SWITCH && !UNITY_EDITOR
        Utils.File = new SwitchFileUtils();
#endif
        }

        protected override IEnumerator RunAutomationProcedure()
        {
            Debug.Log("[Test Automation] Waiting...");
            yield return new WaitForSecondsRealtime(1.0f);

            _client.LogMessage("Hello");
            while (_client.IsTransmitting)
                yield return null;

            _client.ReportTest("Samples", "Test 1", TestStatus.Succeded);
            _client.ReportTest("Samples", "Test 2", TestStatus.Ignored);

            yield return Utils.CaptureScreenshot("Sample");
            yield return Utils.Profile("Sample");

            // Process global tests
            foreach (var test in Tests)
            {
                if (!test.IsPerLevel)
                {
                    BeginTest(test, test.Name);

                    yield return test.RunTest(_client);

                    EndTest(test, null);
                }
            }

            // Process level tests
            foreach (var test in Tests)
            {
                if (test.IsPerLevel)
                {
                    for (int i = 0; i < _scenesToTest.Length; ++i)
                    {
                        var path = _scenesToTest[i].scene.SceneName;
                        var index = SceneUtility.GetBuildIndexByScenePath(path);
                        if (index == -1)
                        {
                            _client.ReportTest(test.Category, test.Name, TestStatus.Failed, $"Scene '{path}' not in build.");
                            continue;
                        }

                        BeginTest(test, path);

                        Bootloader.Instance.LoadScene(index);
                        var scene = SceneManager.GetSceneByBuildIndex(index);
                        yield return test.RunTest(_client, scene);

                        EndTest(test, scene);
                    }
                }
            }
        }

        string _activeTestScopeName = null;
        TestStatus _activeTestIssueStatus = TestStatus.Failed;

        private void BeginTest(TestBase test, string testScopeName)
        {
            _activeTestScopeName = testScopeName;
            _activeTestIssueStatus = test.IsResultDeterminedBasedOnIssuesLogged ? TestStatus.Failed : TestStatus.Ignored;

            _logProxy.ExceptionReceiver = (test as IAutomationExceptionReceiver);
            _logProxy.BeginScope();
        }

        private void EndTest(TestBase test, object levelData)
        {
            _logProxy.EndScope(out int deltaExceptionCount, out int deltaErrorCount);
            _logProxy.ExceptionReceiver = null;

            TestStatus result;
            if (test.IsResultDeterminedBasedOnIssuesLogged)
            {
                var failures = (deltaExceptionCount > 0 || deltaErrorCount > 0);
                if (!failures)
                    result = TestStatus.Succeded;
                else
                    result = TestStatus.Failed;
            }
            else
            {
                result = test.Result;
            }

            _client.ReportTest(test.Category, test.GetName(levelData), result, $"Detected {deltaErrorCount} errors and {deltaExceptionCount} exceptions");

            _activeTestScopeName = null;
            _activeTestIssueStatus = TestStatus.Failed;
        }

        protected override void OnLogException(System.Exception exception, UnityEngine.Object context)
        {
            var name = string.IsNullOrEmpty(_activeTestScopeName) ? "global" : _activeTestScopeName;

            _client.ReportTest("Unhandled Exceptions", name, _activeTestIssueStatus, exception.Message, exception.StackTrace);
        }

        protected override void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (logType == LogType.Error)
            {
                var name = string.IsNullOrEmpty(_activeTestScopeName) ? "global" : _activeTestScopeName;
                _client.ReportTest("Logged Error", name, _activeTestIssueStatus, string.Format(format, args), ParseObjectContext(context));
            }
            else if (logType == LogType.Exception)
            {
                var name = string.IsNullOrEmpty(_activeTestScopeName) ? "global" : _activeTestScopeName;
                _client.ReportTest("Logged Exception", name, _activeTestIssueStatus, string.Format(format, args), ParseObjectContext(context));
            }
        }

        static string ParseObjectContext(UnityEngine.Object context)
        {
            var go = context as GameObject;
            if (go != null)
            {
                return go.GetFullPath();
            }
            else if (context != null)
            {
                return context.name;
            }
            else
            {
                return null;
            }
        }
    }
}
