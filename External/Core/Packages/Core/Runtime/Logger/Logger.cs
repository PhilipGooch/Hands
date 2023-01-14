#if !NBG_LOGGER_LEVEL_TRACE && !NBG_LOGGER_LEVEL_LOG && !NBG_LOGGER_LEVEL_WARNING && !NBG_LOGGER_LEVEL_ERROR
#warning No NBG_LOGGER_LEVEL is defined, defaulting to NBG_LOGGER_LEVEL_LOG
#define NBG_LOGGER_LEVEL_LOG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
#endif

namespace NBG.Core
{
    public sealed class Logger
    {
        private string _scope;
        private Lazy<ILoggerBackend> _backend;

        public Logger(string scope = null)
        {
            this._scope = string.IsNullOrWhiteSpace(scope) ? Log.GlobalScopeName : scope;
            this._backend = new Lazy<ILoggerBackend>(() => Log.GetOrCreateBackend(_scope), LazyThreadSafetyMode.ExecutionAndPublication);

#if UNITY_EDITOR
            RegisterToBeCleanedWhenDomainReloadIsDisabled(this);
#endif
        }

#if UNITY_EDITOR
        // When running in Unity Editor with domain reload disabled, clear backend references explicitly.
        // This allows user-land code to not worry about domain reloads and keep static Loggers around, while backends will always be recreated.
        static readonly List<WeakReference> s_Loggers = new List<WeakReference>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void CleanBackendsWhenDomainReloadIsDisabledToAvoidHavingUserCodeDealWithIt()
        {
            lock (s_Loggers)
            {
                foreach (var weak in s_Loggers)
                {
                    if (!weak.IsAlive)
                        continue;
                    var logger = (Logger)weak.Target;
                    logger._backend = new Lazy<ILoggerBackend>(() => Log.GetOrCreateBackend(logger._scope), LazyThreadSafetyMode.ExecutionAndPublication);
                }
                s_Loggers.RemoveAll(x => !x.IsAlive);
            }
        }

        private static void RegisterToBeCleanedWhenDomainReloadIsDisabled(Logger logger)
        {
            lock (s_Loggers)
            {
                s_Loggers.Add(new WeakReference(logger));
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ILoggerBackend AcquireBackend()
        {
            return _backend.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE")]
        public void LogTrace(object message)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Trace)
                b.Log(LogLevel.Trace, null, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE")]
        public void LogTraceFormat(string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Trace)
                b.Log(LogLevel.Trace, null, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG")]
        public void LogInfo(object message)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Info)
                b.Log(LogLevel.Info, null, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG")]
        public void LogInfoFormat(string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Info)
                b.Log(LogLevel.Info, null, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG")]
        public void LogInfo(object message, UnityEngine.Object context)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Info)
                b.Log(LogLevel.Info, context, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG")]
        public void LogInfoFormat(UnityEngine.Object context, string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Info)
                b.Log(LogLevel.Info, context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public void LogWarning(object message)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Warning)
                b.Log(LogLevel.Warning, null, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public void LogWarningFormat(string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Warning)
                b.Log(LogLevel.Warning, null, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public void LogWarning(object message, UnityEngine.Object context)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Warning)
                b.Log(LogLevel.Warning, context, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING")]
        public void LogWarningFormat(UnityEngine.Object context, string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Warning)
                b.Log(LogLevel.Warning, context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING"), Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public void LogError(object message)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Error)
                b.Log(LogLevel.Error, null, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING"), Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public void LogErrorFormat(string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Error)
                b.Log(LogLevel.Error, null, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING"), Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public void LogError(object message, UnityEngine.Object context)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Error)
                b.Log(LogLevel.Error, context, (string)message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NBG_LOGGER_LEVEL_TRACE"), Conditional("NBG_LOGGER_LEVEL_LOG"), Conditional("NBG_LOGGER_LEVEL_WARNING"), Conditional("NBG_LOGGER_LEVEL_ERROR")]
        public void LogErrorFormat(UnityEngine.Object context, string format, params object[] args)
        {
            var b = AcquireBackend();
            if (b.Level <= LogLevel.Error)
                b.Log(LogLevel.Error, context, format, args);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(System.Exception exception)
        {
            var b = AcquireBackend();
            b.LogException(exception, null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogException(System.Exception exception, UnityEngine.Object context)
        {
            var b = AcquireBackend();
            b.LogException(exception, context);
        }
    }
}
