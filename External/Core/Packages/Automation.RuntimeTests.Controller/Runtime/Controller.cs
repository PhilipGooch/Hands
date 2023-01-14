using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NBG.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Automation.RuntimeTests.Controller
{
    /// <summary>
    /// Standard automation controller implementation.
    /// </summary>
    public abstract class Controller : MonoBehaviour, ILogHandler
    {
        protected TestClient _client;

        protected const bool kCanWriteToTargetDisk = true;
        protected const bool kCanWriteToHostDisk =
#if UNITY_SWITCH
            false;
#else
            true;
#endif

        protected string _artifactDir;
        protected string _screenshotDir;
        protected string _profilerDir;

        protected LogProxy _logProxy;

        static List<TestBase> _tests;
        protected static IEnumerable<TestBase> Tests
        {
            get
            {
                if (_tests == null)
                {
                    _tests = new List<TestBase>();
                    var types = AssemblyUtilities.GetAllDerivedClasses(typeof(TestBase));
                    foreach (var type in types)
                    {
                        if (!type.IsAbstract)
                            _tests.Add((TestBase)Activator.CreateInstance(type));
                    }
                }
                return _tests;
            }
        }

        private void Start()
        {
            var address = TestClient.GetServerAddressAuto();
            if (address == null)
            {
                enabled = false;
                return;
            }

            _client = TestClient.Create(address);
            StartCoroutine(RunAutomationProcedureWrapper());
        }

        private void OnDestroy()
        {
            if (_logProxy != null)
                Debug.unityLogger.logHandler = _logProxy.Original;
        }

        private bool Initialize()
        {
            _client.Hello(Application.companyName, Application.productName);

            if (kCanWriteToTargetDisk)
            {
                try
                {
                    var root = Utils.File.OutputPath;
                    
                    // Initialize base output directory
                    _artifactDir = Path.Combine(root, Settings.AutomationDirName);
                    Utils.Log($"[Automation] Output to {_artifactDir}");
                    if (Utils.File.MountName != null)
                        Utils.File.Mount(Utils.File.MountName);
                    Utils.File.DeleteDirectory(_artifactDir);
                    Utils.File.CreateDirectory(_artifactDir);

                    // Setup for screenshots
                    _screenshotDir = Path.Combine(_artifactDir, "Screenshots");
                    Utils.File.CreateDirectory(_screenshotDir);
                    Utils.SetScreenshotDirectory(_screenshotDir);

                    // Setup for profiler
                    _profilerDir = Path.Combine(_artifactDir, "Profiler");
                    Utils.File.CreateDirectory(_profilerDir);                   
                    Utils.SetProfilingDirectory(_profilerDir);
                }
                catch (Exception e)
                {
                    Utils.Log($"[Automation] Error initializing artifact folder: {e.Message}");
                    //ExitAutomation(-2);
                    return false;
                }
            }

            _logProxy = new LogProxy(Debug.unityLogger.logHandler, this);
            Debug.unityLogger.logHandler = _logProxy;

            if (kCanWriteToHostDisk)
            {
                _client.CollectArtifactsFrom(_artifactDir);
                var playerLogPath = Utils.File.PlayerLogPath;
                if (playerLogPath != null)
                    _client.CollectArtifactsFrom(playerLogPath);
            }

            return true;
        }

        private IEnumerator ExitAutomation(int returnCode)
        {
            while (_client.IsTransmitting) // Wait to finish transmitting data
                yield return null;

            if (Utils.File.MountName != null)
                Utils.File.Unmount(Utils.File.MountName);

            if (_client != null)
            {
                _client.ReportTestRunResult(returnCode);
                while (_client.IsTransmitting)
                    yield return null;
                _client = null;
            }

            Utils.Log($"[Automation] Exiting with code: {returnCode}");
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit(returnCode);
#endif
        }

        abstract protected IEnumerator RunAutomationProcedure();

        private IEnumerator RunAutomationProcedureWrapper()
        {
            if (!Initialize())
                yield return ExitAutomation(-2);
            while (_client.IsTransmitting) // Wait to initialize
                yield return null;

            yield return RunAutomationProcedure();

            Utils.Log(Settings.AutomationCompleteLogMessage);
            yield return ExitAutomation(0);
        }

        void ILogHandler.LogException(Exception exception, UnityEngine.Object context)
        {
            OnLogException(exception, context);
        }

        void ILogHandler.LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            LogFormat(logType, context, format, args);
        }

        protected abstract void OnLogException(Exception exception, UnityEngine.Object context);
        protected abstract void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args);
    }
}
