using NBG.Core;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NBG.Automation.RuntimeTests
{
    internal class TestClientHTTP : TestClient
    {
        string _addressPrefix;
        int _transmitting;

        internal TestClientHTTP(string serverIP) : base(serverIP)
        {
            _addressPrefix = $"http://{serverIP}:{Settings.Port}";
        }

        public override bool IsTransmitting => (_transmitting > 0);

        void DoPost(string uri, WWWForm formData, string logPrefix)
        {
            _transmitting++;

            var request = UnityWebRequest.Post(uri, formData);
            request.SendWebRequest();

            Coroutines.StartManagedCoroutine(CoPost_Internal(request, logPrefix));
        }

        IEnumerator CoPost_Internal(UnityWebRequest request, string logPrefix)
        {
            while (request.result == UnityWebRequest.Result.InProgress)
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"{logPrefix} response: {request.responseCode}");
            }
            else
            {
                Debug.LogError($"{logPrefix} response: {request.responseCode}. Error: {request.error}");
            }

            Debug.Assert(_transmitting > 0);
            --_transmitting;
            request.Dispose();
        }

        public override void Hello(string companyName, string productName)
        {
            const string logPrefix = "[Automation][HTTP] Hello";
            Debug.Log($"{logPrefix}: {companyName}, {productName}");

            var form = new WWWForm();
            form.AddField("companyName", companyName);
            form.AddField("productName", productName);

            DoPost($"{_addressPrefix}/hello", form, logPrefix);
        }

        public override void CollectArtifactsFrom(string path)
        {
            const string logPrefix = "[Automation][HTTP] CollectArtifactsFrom";
            Debug.Log($"{logPrefix}: {path}");

            var form = new WWWForm();
            form.AddField("value", path);

            DoPost($"{_addressPrefix}/artifactsPath", form, logPrefix);
        }

        public override void LogMessage(string message, bool buildStatus = false)
        {
            const string logPrefix = "[Automation][HTTP] LogMessage";
            Debug.Log($"{logPrefix} (status={buildStatus}): {message}");

            var form = new WWWForm();
            form.AddField("message", message);
            form.AddField("status", buildStatus ? 1 : 0);
            
            DoPost($"{_addressPrefix}/log", form, logPrefix);
        }

        public override void ReportTest(string suiteName, string testName, TestStatus status, string message = null, string details = null)
        {
            const string logPrefix = "[Automation][HTTP] ReportTest";
            Debug.Log($"{logPrefix}: {suiteName}, {testName}, {status}, {message}, {details}");

            var form = new WWWForm();
            form.AddField("suiteName", suiteName);
            form.AddField("testName", testName);
            form.AddField("status", status.ToString());
            if (!string.IsNullOrWhiteSpace(message))
                form.AddField("message", message);
            if (!string.IsNullOrWhiteSpace(details))
                form.AddField("details", details);

            DoPost($"{_addressPrefix}/reportTest", form, logPrefix);
        }

        public override void ReportTestRunResult(int returnCode)
        {
            const string logPrefix = "[Automation][HTTP] ReportTestRunResult";
            Debug.Log($"{logPrefix}: {returnCode}");

            var form = new WWWForm();
            form.AddField("value", returnCode);
            
            DoPost($"{_addressPrefix}/reportTestRunResult", form, logPrefix);
        }

        public override void SetTimeout(int seconds)
        {
            const string logPrefix = "[Automation][HTTP] SetTimeout";
            Debug.Log($"{logPrefix}: {seconds}");

            var form = new WWWForm();
            form.AddField("value", seconds);
            
            DoPost($"{_addressPrefix}/timeout", form, logPrefix);
        }

        public override void TakeSystemScreenshot(string name)
        {
            const string logPrefix = "[Automation][HTTP] TakeSystemScreenshot";
            Debug.Log($"{logPrefix}: {name}");

            var form = new WWWForm();
            form.AddField("value", name);
            
            DoPost($"{_addressPrefix}/takeSystemScreenshot", form, logPrefix);
        }
    }
}
