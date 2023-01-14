using System;

namespace NBG.Core
{
    public sealed class LoggerBackendNull : ILoggerBackend
    {
        public string Name { get; }
        public LogLevel StackTraceLevel { get; set; } = Core.Log.DefaultGlobalStackTraceLevel;
        public LogLevel Level { get; set; } = Core.Log.DefaultGlobalLogLevel;

        public LoggerBackendNull(string name)
        {
            Name = name;
        }

        public void Log(LogLevel level, UnityEngine.Object context, string format, params object[] args)
        {
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
        }
    }
}
