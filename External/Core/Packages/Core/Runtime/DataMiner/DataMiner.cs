using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBG.Core.DataMining
{
    public static class DataMiner
    {
        public const byte FileVersion = 1;

        [ClearOnReload]
        static string kDefaultFolder;
        public static string DefaultFolder
        {
            get
            {
                if (string.IsNullOrWhiteSpace(kDefaultFolder))
                    kDefaultFolder = Path.Combine(Application.dataPath, "..", "DataMining");
                return kDefaultFolder;
            }
        }
        public const string FileExtension = "nbgrec";

        static DataMiner()
        {
            RegisterDataSources();
        }

        static List<IDataSource> _allSources;
        static List<IDataSource> _enabledSources;
        public static IEnumerable<IDataSource> Sources => _allSources;
        public static IEnumerable<IDataSource> EnabledSources => _enabledSources;

        static void RegisterDataSources()
        {
            Debug.Log("[DataMiner] Registering data sources...");
            var types = AssemblyUtilities.GetAllDerivedClasses(typeof(IDataSource));
            _allSources = new List<IDataSource>(types.Count);
            _enabledSources = new List<IDataSource>(types.Count);
            foreach (var type in types)
            {
                Debug.Log($"RegisterDataSource: {type}");
                var source = (IDataSource)Activator.CreateInstance(type);
                _allSources.Add(source);
                _enabledSources.Add(source);
            }
        }

        internal static void OnBeginRecording()
        {
            foreach (var source in _enabledSources)
                source.OnBeginRecording();
        }

        internal static void OnEndRecording()
        {
            foreach (var source in _enabledSources)
                source.OnEndRecording();
        }

        internal static void OnFixedUpdate()
        {
            foreach (var source in _enabledSources)
            {
                var uw = source as IDataSourceFixedUpdater;
                if (uw == null)
                    continue;
                uw.OnFixedUpdate();
            }
        }

        internal static void OnLateUpdate()
        {
            foreach (var source in _enabledSources)
            {
                var uw = source as IDataSourceLateUpdater;
                if (uw == null)
                    continue;
                uw.OnLateUpdate();
            }
        }



        [ClearOnReload]
        internal static DataMinerRecorder activeRecorder;

        public static bool IsRecording
        {
            get
            {
                return (activeRecorder == null) ? false : activeRecorder.Enabled;
            }
        }

        public static bool StartRecording()
        {
            if (activeRecorder.Enabled)
                throw new InvalidOperationException("DataMiner is already recording!");
            activeRecorder.Enabled = true;
            return true;
        }

        // Returns recording output path
        public static string StopRecording()
        {
            if (!activeRecorder.Enabled)
                throw new InvalidOperationException("DataMiner is not recording!");
            activeRecorder.Enabled = false;

            var outputPath = activeRecorder.OutputPath;
            Log($"[DataMiner] Recording finished: {outputPath}");
            return outputPath;
        }

        // Data source must ask to be written in the current frame
        public static void RequestWrite(IDataSource dataSource, uint lengthBytes)
        {
            var dataWriter = activeRecorder.Writer;
            var binaryWriter = dataWriter.BeginTag(dataSource.Id, lengthBytes);
            dataSource.Write(binaryWriter);
            dataWriter.EndTag();
        }

        private static void Log(string message, bool error = false) // TODO: refactor once we have the unified logging system
        {
            if (DebugUI.DebugUI.IsCreated)
                DebugUI.DebugUI.Get().Print(message, error ? DebugUI.Verbosity.Error : DebugUI.Verbosity.Info);
            else
                Debug.LogFormat(error ? LogType.Error : LogType.Log, LogOption.NoStacktrace, null, message);
        }

#if UNITY_EDITOR
        const string kStartItem = "No Brakes Games/Data Miner/Recording/Start";
        const string kStopItem = "No Brakes Games/Data Miner/Recording/Stop";

        [MenuItem(kStartItem, true)]
        static bool MenuStartRecordingValidate() => EditorApplication.isPlaying && !IsRecording;
        [MenuItem(kStartItem)]
        static void MenuStartRecording() => StartRecording();

        [MenuItem(kStopItem, true)]
        static bool MenuStopRecordingValidate() => EditorApplication.isPlaying && IsRecording;
        [MenuItem(kStopItem)]
        static void MenuStopRecording() => StopRecording();
#endif

        public static void ToggleRecording()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            else
            {
                StartRecording();
            }
        }

        public static bool IsSourceEnabled(IDataSource source)
        {
            return _enabledSources.Contains(source);
        }

        public static void EnableSource(IDataSource source, bool enable)
        {
            if (enable)
            {
                Debug.Assert(_allSources.Contains(source));
                if (!_enabledSources.Contains(source))
                    _enabledSources.Add(source);
            }
            else
            {
                _enabledSources.Remove(source);
            }
        }
    }
}
