using System;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Automation.RuntimeTests
{
    public class TestClient
    {
        public const string EnableToggleEditorMenuItemName = "No Brakes Games/Automation/Runtime Tests/Enable in Play Mode";
        public const string OfflineToggleEditorMenuItemName = "No Brakes Games/Automation/Runtime Tests/Enable Offline Mode";

        public const string AutomationArg = "--automation=";
        public const string OfflineAddress = "offline";
        public const string LocalAddress = "127.0.0.1";

        internal TestClient(string serverIP)
        {
        }

        public virtual bool IsTransmitting { get; } = false;

        public virtual void Hello(string companyName, string productName)
        {
            Debug.Log($"[Automation][Offline] Hello: {companyName}, {productName}");
        }

        // Notify where to collect artifacts from
        public virtual void CollectArtifactsFrom(string path)
        {
            Debug.Log($"[Automation][Offline] CollectArtifactsFrom: {path}");
        }

        // Send a status message
        public virtual void LogMessage(string message, bool buildStatus = false)
        {
            Debug.Log($"[Automation][Offline] LogMessage (status={buildStatus}): {message}");
        }

        // Send a test report
        public virtual void ReportTest(string suiteName, string testName, TestStatus status, string message = null, string details = null)
        {
            Debug.Log($"[Automation][Offline] ReportTest: {suiteName}, {testName}, {status}, {message}, {details}");
        }

        // Send the overall test run return code
        public virtual void ReportTestRunResult(int returnCode)
        {
            Debug.Log($"[Automation][Offline] ReportTestRunResult: {returnCode}");
        }

        // Override the default timeout
        public virtual void SetTimeout(int seconds)
        {
            Debug.Log($"[Automation][Offline] SetTimeout: {seconds}");
        }

        // Requests a system screenshot
        public virtual void TakeSystemScreenshot(string name)
        {
            Debug.Log($"[Automation][Offline] TakeSystemScreenshot: {name}");
        }



        #region static
        public static TestClient Create(string address)
        {
            if (address == OfflineAddress)
                return new TestClient(address);
            else
                return new TestClientHTTP(address);
        }

        // Returns a connection address
        // Uses --automation= cli argument
        // Uses editor override when available
        // Returns null if automation is disabled
        public static string GetServerAddressAuto()
        {
#if UNITY_EDITOR
            if (Menu.GetChecked(EnableToggleEditorMenuItemName))
            {
                if (Menu.GetChecked(OfflineToggleEditorMenuItemName))
                    return OfflineAddress;
                else
                    return LocalAddress;
            }
#endif

            var args = Environment.GetCommandLineArgs();
            var set = args.Any(a => a.StartsWith(AutomationArg));
            if (!set)
                return null;

            try
            {
                var address = args
                    .First(a => a.StartsWith(AutomationArg))
                    .Substring(AutomationArg.Length)
                    .Trim();
                return address;
            }
            catch (Exception ex)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Failed to parse {AutomationArg}");
                Debug.LogException(ex);
                return null;
            }
        }
        #endregion
    }
}
