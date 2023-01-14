using System;
using System.Collections;

namespace NBG.Automation.RuntimeTests.Controller
{
    /// <summary>
    /// Test interface for the standard automation controller.
    /// </summary>
    public abstract class TestBase
    {
        public abstract string Name { get; }
        public abstract string Category { get; }

        public abstract bool IsPerLevel { get; }
        public abstract bool IsResultDeterminedBasedOnIssuesLogged { get; }

        public abstract TestStatus Result { get; }

        public abstract IEnumerator RunTest(TestClient testClient, object levelData = null);

        public virtual string GetName(object levelData)
        {
            if (levelData == null)
            {
                return Name;
            }
            else
            {
                throw new NotImplementedException($"GetName() not implemented for {this.GetType().Name} for non-null levelData cases.");
            }
        }
    }

    /// <summary>
    /// Allows tests to handle exceptions in case they expect them.
    /// </summary>
    public interface IAutomationExceptionReceiver
    {
        // Return <true> to ignore automatic exception handling
        public abstract bool OnException(Exception exception, UnityEngine.Object context);
    }
}
