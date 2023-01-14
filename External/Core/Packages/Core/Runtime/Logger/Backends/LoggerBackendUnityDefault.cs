using System;

namespace NBG.Core
{
    public sealed class LoggerBackendDefaultUnity : ILoggerBackend
    {
        public string Name { get; }
        public LogLevel StackTraceLevel { get; set; } = Core.Log.DefaultGlobalStackTraceLevel;
        public LogLevel Level { get; set; } = Core.Log.DefaultGlobalLogLevel;

        static UnityEngine.LogType FromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return UnityEngine.LogType.Log;
                case LogLevel.Info:
                    return UnityEngine.LogType.Log;
                case LogLevel.Warning:
                    return UnityEngine.LogType.Warning;
                case LogLevel.Error:
                    return UnityEngine.LogType.Error;
                default:
                    throw new System.NotImplementedException();
            }
        }

        public LoggerBackendDefaultUnity(string name)
        {
            Name = name;
        }

        //TODO: support string builders in thread local storage
        public void Log(LogLevel level, UnityEngine.Object context, string format, params object[] args)
        {
            var options = UnityEngine.LogOption.None;
            if (StackTraceLevel > level)
                options |= UnityEngine.LogOption.NoStacktrace;

            var time = TimeSpan.FromSeconds(UnityEngine.Time.realtimeSinceStartup);
            var timeText = time.ToString(@"hh\:mm\:ss\.ffffff");
            var frameCount = UnityEngine.Time.frameCount;

            if (Name == Core.Log.GlobalScopeName)
                format = $"[{timeText} ({frameCount})] {format}";
            else
                format = $"[{Name}][{timeText} ({frameCount})] {format}";

            UnityEngine.Debug.LogFormat(FromLogLevel(level), options, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogException(exception, context);
        }   
    }
}
