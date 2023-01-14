#if !NBG_LOGGER_LEVEL_TRACE && !NBG_LOGGER_LEVEL_LOG && !NBG_LOGGER_LEVEL_WARNING && !NBG_LOGGER_LEVEL_ERROR
#warning No NBG_LOGGER_LEVEL is defined, defaulting to NBG_LOGGER_LEVEL_LOG
#define NBG_LOGGER_LEVEL_LOG
#endif

#if NBG_LOGGER_LEVEL_TRACE
#define NBG_LOGGER_LEVEL_LOG
#endif

#if NBG_LOGGER_LEVEL_LOG
#define NBG_LOGGER_LEVEL_WARNING
#endif

#if NBG_LOGGER_LEVEL_WARNING
#define NBG_LOGGER_LEVEL_ERROR
#endif

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NBG.Core
{
    public enum LogLevel : int
    {
        Trace = 0,
        Info,
        Warning,
        Error,
    }

    public static class Log
    {
        public const string GlobalScopeName = "Global";
        public const string DebugUICategoryName = "Loggers";

        internal const LogLevel DefaultGlobalLogLevel = LogLevel.Info;
        internal const LogLevel DefaultGlobalStackTraceLevel = LogLevel.Error;

        public static LogLevel DefaultLogLevel { get; } = DefaultGlobalLogLevel;
        public static LogLevel DefaultStackTraceLevel { get; } = DefaultGlobalStackTraceLevel;

        static readonly Logger s_GlobalLogger = new Logger();

        [ClearOnReload(newInstance: true)]
        static ConcurrentDictionary<string, ILoggerBackend> s_Backends = new ConcurrentDictionary<string, ILoggerBackend>();

        [ClearOnReload(newInstance: true)]
        static ConcurrentQueue<ILoggerBackend> s_BackendsToRegisterWithDebugUI = new ConcurrentQueue<ILoggerBackend>();

        static Log()
        {
            DebugUI.DebugUI.Get().OnShow += DebugUI_OnShow;

            UnityEngine.Debug.LogFormat("Initializing Core logging...\nStack tracing settings:\nLog: {0}\nWarning: {1}\nError: {2}\nException: {3}\nAssert: {4}",
                UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Log),
                UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Warning),
                UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Error),
                UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Exception),
                UnityEngine.Application.GetStackTraceLogType(UnityEngine.LogType.Assert)
                );
        }

        public static ILoggerBackend GetOrCreateBackend(string scope)
        {
            var backend = s_Backends.GetOrAdd(scope, (key) => CreateBackend(key));
            return backend;
        }

        static ILoggerBackend CreateBackend(string scope)
        {
            var backend = new LoggerBackendDefaultUnity(scope);
            backend.Level = Log.DefaultLogLevel;
            backend.StackTraceLevel = Log.DefaultStackTraceLevel;

            s_BackendsToRegisterWithDebugUI.Enqueue(backend);

            return backend;
        }

        static void DebugUI_OnShow()
        {
            while (s_BackendsToRegisterWithDebugUI.TryDequeue(out ILoggerBackend backend))
            {
                var debug = DebugUI.DebugUI.Get();
                debug.RegisterEnum($"{backend.Name} verbosity", DebugUICategoryName, typeof(LogLevel), () => { return backend.Level; }, (level) => { backend.Level = (LogLevel)level; });
                debug.RegisterEnum($"{backend.Name} stack traces", DebugUICategoryName, typeof(LogLevel), () => { return backend.StackTraceLevel; }, (level) => { backend.StackTraceLevel = (LogLevel)level; });
            }
        }

        #region API
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE")]
        public static void LogTrace(object message)
        {
            s_GlobalLogger.LogTrace(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE")]
        public static void LogTraceFormat(string format, params object[] args)
        {
            s_GlobalLogger.LogTraceFormat(format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_LOG")]
        public static void LogInfo(object message)
        {
            s_GlobalLogger.LogInfo(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_LOG")]
        public static void LogInfoFormat(string format, params object[] args)
        {
            s_GlobalLogger.LogInfoFormat(format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_LOG")]
        public static void LogInfo(object message, UnityEngine.Object context)
        {
            s_GlobalLogger.LogInfo(message, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_LOG")]
        public static void LogInfoFormat(UnityEngine.Object context, string format, params object[] args)
        {
            s_GlobalLogger.LogInfoFormat(context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public static void LogWarning(object message)
        {
            s_GlobalLogger.LogWarning(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            s_GlobalLogger.LogWarningFormat(format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public static void LogWarning(object message, UnityEngine.Object context)
        {
            s_GlobalLogger.LogWarning(message, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public static void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
        {
            s_GlobalLogger.LogWarningFormat(context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public static void LogError(object message)
        {
            s_GlobalLogger.LogError(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            s_GlobalLogger.LogErrorFormat(format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public static void LogError(object message, UnityEngine.Object context)
        {
            s_GlobalLogger.LogError(message, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public static void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
        {
            s_GlobalLogger.LogErrorFormat(context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(System.Exception exception)
        {
            s_GlobalLogger.LogException(exception);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(System.Exception exception, UnityEngine.Object context)
        {
            s_GlobalLogger.LogException(exception, context);
        }
        #endregion
    }
}
