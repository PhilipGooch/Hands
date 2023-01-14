namespace NBG.Core
{
    public interface ILoggerBackend
    {
        string Name { get; }
        LogLevel StackTraceLevel { get; set; }
        LogLevel Level { get; set; }

        void Log(LogLevel level, UnityEngine.Object context, string format, params object[] args);
        void LogException(System.Exception exception, UnityEngine.Object context);
    }
}
