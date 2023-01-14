using NBG.Automation.RuntimeTests;
using NBG.Automation.RuntimeTests.Controller;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Base.Tests
{
    public class RePhysXPluginRuntimeTest : TestBase
    {
        public override string Name => "RePhysX custom test";
        public override string Category => "RePhysX";
        public override bool IsPerLevel => false;
        public override bool IsResultDeterminedBasedOnIssuesLogged => true;
        public override TestStatus Result => throw new System.NotSupportedException();

        public override string GetName(object levelData)
        {
            Debug.Assert(levelData == null);
            return Name;
        }

        public override IEnumerator RunTest(TestClient testClient, object levelData)
        {
            Debug.Assert(levelData == null);
            Utils.Log($"[Automation] Performing {nameof(RePhysXPluginRuntimeTest)}");

            SceneManager.LoadScene("RePhysXPluginSample");
            yield return new WaitForSeconds(3.0f); // Settle

            var screenshotName = Name;
            yield return Utils.CaptureScreenshot(screenshotName);

            while (testClient.IsTransmitting)
                yield return null;
        }
    }
}
