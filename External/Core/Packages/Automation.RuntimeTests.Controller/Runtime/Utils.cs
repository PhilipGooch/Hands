using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

namespace NBG.Automation.RuntimeTests.Controller
{
    public interface IFileUtils
    {
        string MountName { get; }
        string OutputPath { get; }
        string PlayerLogPath { get; }
        void Mount(string mountName);
        void Unmount(string mountName);
        void WriteFile(string path, byte[] data);
        void FlushFile(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path);
    }

    /// <summary>
    /// Utilities for the standard automation controller.
    /// </summary>
    public static class Utils
    {
        public static IFileUtils File { get; set; } = new DefaultFileUtils();

        public static void Log(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, message);
        }

        private static string _screenshotDir;
        public static void SetScreenshotDirectory(string dir)
        {
            _screenshotDir = dir;
        }

        public static IEnumerator CaptureScreenshot(string name)
        {
            if (_screenshotDir == null)
            {
                Log($"[Automation] Will not capture screenshot '{name}' - screenshot directory is not set.");
                yield break;
            }

            Log($"[Automation] Start capture screenshot '{name}'...");
            var filePath = Path.Combine(_screenshotDir, $"{name}.png");

            yield return new WaitForEndOfFrame();
            var ssTex = ScreenCapture.CaptureScreenshotAsTexture();
            var ssBytes = ssTex.EncodeToPNG();
            File.WriteFile(filePath, ssBytes);

            Log($"[Automation] Finish capture screenshot '{name}' ({filePath}).");
        }


        private static string _profilingDir;
        public static void SetProfilingDirectory(string dir)
        {
            _profilingDir = dir;
        }

        public static IEnumerator Profile(string name)
        {
            if (_profilingDir == null)
            {
                Log($"[Automation] Will not record using profiler as '{name}' - profiling directory is not set.");
                yield break;
            }

            Log($"[Automation] Start recording performance '{name}'...");
            var filePath = Path.Combine(_profilingDir, $"{name}.raw");
            
            Profiler.logFile = filePath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            yield return new WaitForSeconds(0.5f);
            Profiler.enabled = false;
            Profiler.logFile = "";

            Log($"[Automation] Finish recording performance '{name}' ({filePath}).");

            File.FlushFile(filePath);
        }
    }

    class DefaultFileUtils : IFileUtils
    {
        public string MountName { get { return null; } }
        public string OutputPath { get { return Application.persistentDataPath; } }
        public string PlayerLogPath { get { return Path.Combine(Application.persistentDataPath, "Player.log"); } }

        public void Mount(string _)
        {
            // Nothing to do
        }

        public void Unmount(string _)
        {
            // Nothing to do
        }

        public void WriteFile(string path, byte[] data)
        {
            File.WriteAllBytes(path, data);
        }

        public void FlushFile(string _)
        {
            // Nothing to do
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
