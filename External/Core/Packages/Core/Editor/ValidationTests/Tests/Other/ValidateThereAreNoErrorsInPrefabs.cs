using System;
using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor.StandardLevelValidators
{
    public class ValidateThereAreNoErrorsInPrefabs : ValidationTest
    {
        public override string Name => "There are no errors in prefabs (deserialization and OnValidate).";
        public override string Category => "Prefab";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksProject;
        
        private int _errorCount;
        private int _totalCount;

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
        }

        protected override Result OnRun(ILevel level)
        {
            var originalLogHandler = Debug.unityLogger.logHandler;

            try
            {
                using (var enumerator = ValidationTests.GetAllPrefabs().GetEnumerator())
                {
                    bool available = true;
                    while (available)
                    {
                        var logProxy = new LogHandler(originalLogHandler);
                        Debug.unityLogger.logHandler = logProxy;

                        available = enumerator.MoveNext(); // Loads the prefab
                        
                        GameObject go = null;
                        if (available)
                        {
                            go = enumerator.Current;
                            _totalCount++;
                        }

                        if (logProxy.ErrorCount > 0 || logProxy.ExceptionCount > 0 || logProxy.AssertCount > 0)
                        {
                            var path = AssetDatabase.GetAssetPath(go);
                            PrintError($"Found a Prefab with errors at {path}", go);
                            _errorCount++;
                        }

                        Debug.unityLogger.logHandler = originalLogHandler;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintError($"Unhandled exception", null);
                _totalCount++;
                _errorCount++;
            }
            
            Debug.unityLogger.logHandler = originalLogHandler;

            return Result.FromCount(_errorCount, _totalCount);
        }

        class LogHandler : ILogHandler
        {
            public ILogHandler Original { get; private set; }
            public int ExceptionCount { get; private set; }
            public int ErrorCount { get; private set; }
            public int AssertCount { get; private set; }

            public LogHandler(ILogHandler original)
            {
                Original = original;
            }

            public void LogException(Exception exception, UnityEngine.Object context)
            {
                ++ExceptionCount;
                Original?.LogException(exception, context);
            }

            public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
            {
                if (logType == LogType.Exception)
                {
                    ++ExceptionCount;
                }
                else if (logType == LogType.Error)
                {
                    ++ErrorCount;
                }
                else if (logType == LogType.Assert)
                {
                    ++AssertCount;
                }

                Original?.LogFormat(logType, context, format, args);
            }
        }
    }
}
