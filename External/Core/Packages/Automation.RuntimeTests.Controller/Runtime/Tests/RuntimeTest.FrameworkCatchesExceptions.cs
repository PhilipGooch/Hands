using System;
using System.Collections;
using UnityEngine;

namespace NBG.Automation.RuntimeTests.Controller
{
    public class FrameworkCatchesExceptionsRuntimeTest : TestBase, IAutomationExceptionReceiver
    {
        public override string Name => "Framework Catches Exceptions";

        public override string Category => "Test Framework Tests";

        public override bool IsPerLevel => false;
        public override bool IsResultDeterminedBasedOnIssuesLogged => false;
        public override TestStatus Result => _result;
        TestStatus _result;

        public override IEnumerator RunTest(TestClient testClient, object levelData = null)
        {
            _result = TestStatus.Failed;

            var go = new GameObject(nameof(FrameworkCatchesExceptionsHelper));
            var helper = go.AddComponent<FrameworkCatchesExceptionsHelper>();
            while (!helper.Done)
                yield return null;
            GameObject.Destroy(go);
        }

        bool IAutomationExceptionReceiver.OnException(Exception exception, UnityEngine.Object context)
        {
            if (exception.Message == FrameworkCatchesExceptionsHelper.ExceptionMessage)
            {
                Utils.Log("[Automation] FrameworkCatchesExceptionsRuntimeTest detected the expected exception.");
                _result = TestStatus.Succeded;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
