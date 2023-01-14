using NBG.Automation.RuntimeTests;
using NBG.Automation.RuntimeTests.Controller;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Base.Tests
{
    public class CheckLevelRuntimeTest : TestBase
    {
        public override string Name => "Enter and Exit Level";
        public override string Category => "Level Tests";
        public override bool IsPerLevel => true;
        public override bool IsResultDeterminedBasedOnIssuesLogged => true;
        public override TestStatus Result => throw new System.NotSupportedException();

        public override string GetName(object levelData)
        {
            var level = (Scene)levelData;
            var testName = (level == null) ? Name : $"{Name} [{level.name}]";
            return testName;
        }

        public override IEnumerator RunTest(TestClient testClient, object levelData)
        {
            var level = (Scene)levelData;
            Debug.Assert(level != null);
            Utils.Log($"[Automation] Performing {nameof(CheckLevelRuntimeTest)}");

            yield return new WaitForSeconds(3.0f); // Settle

            var screenshotName = $"{Name} - {level.name}";
            yield return Utils.CaptureScreenshot(screenshotName);

            while (testClient.IsTransmitting)
                yield return null;
        }
    }
}
