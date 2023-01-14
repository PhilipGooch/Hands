using System;
using UnityEngine;

namespace NBG.Automation.RuntimeTests.Controller
{
    /// <summary>
    /// Proxies errors and exceptions for additional handling by the standard automation controller.
    /// </summary>
    public class LogProxy : ILogHandler
    {
        public ILogHandler Original { get; private set; }
        public ILogHandler Forward { get; private set; }
        public IAutomationExceptionReceiver ExceptionReceiver { get; set; }
        public int ExceptionCount { get; private set; }
        public int ErrorCount { get; private set; }

        public LogProxy(ILogHandler original, ILogHandler forward)
        {
            Original = original;
            Forward = forward;
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            bool ignore = false;
            if (ExceptionReceiver != null)
                ignore = ExceptionReceiver.OnException(exception, context);

            ++ExceptionCount;

            if (!ignore)
                Forward?.LogException(exception, context);
            
            Original?.LogException(exception, context);
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            if (logType == LogType.Exception)
            {
                ++ExceptionCount;
                Forward?.LogFormat(logType, context, format, args);
            }
            else if (logType == LogType.Error)
            {
                ++ErrorCount;
                Forward?.LogFormat(logType, context, format, args);
            }

            Original?.LogFormat(logType, context, format, args);
        }



        bool _scopeActive;
        int _scopeExceptionCount;
        int _scopeErrorCount;

        public void BeginScope()
        {
            Debug.Assert(!_scopeActive);
            _scopeActive = true;
            _scopeExceptionCount = ExceptionCount;
            _scopeErrorCount = ErrorCount;
        }

        public void EndScope(out int deltaExceptionCount, out int deltaErrorCount)
        {
            Debug.Assert(_scopeActive);
            _scopeActive = false;
            deltaExceptionCount = ExceptionCount - _scopeExceptionCount;
            deltaErrorCount = ErrorCount - _scopeErrorCount;
            ExceptionCount = _scopeExceptionCount;
            ErrorCount = _scopeErrorCount;
        }
    }
}
