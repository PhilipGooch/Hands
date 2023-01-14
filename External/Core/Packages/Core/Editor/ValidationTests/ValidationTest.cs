using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace NBG.Core.Editor
{
    /// <summary>
    /// Capabilities of a test
    /// </summary>
    [System.Flags]
    public enum ValidationTestCaps
    {
        None = 0,
        /// <summary>
        /// Can run project wide (i.e. to fix prefabs)
        /// </summary>
        ChecksProject = (1 << 0),
        /// <summary>
        /// Can run on scenes
        /// </summary>
        ChecksScenes = (1 << 1),
        /// <summary>
        /// Can run only on a completely loaded level (i.e. to do cross-scene checks)
        /// </summary>
        RequiresCompleteLevels = (1 << 2),
        /// <summary>
        /// Is this test strict (mandatory to pass tests) in project scope.
        /// Strict tests are 100% accurate and unquestionable.
        /// </summary>
        StrictProjectScope = (1 << 3),
        /// <summary>
        /// Is this test strict (mandatory to pass tests) in scene scope.
        /// Strict tests are 100% accurate and unquestionable.
        /// </summary>
        StrictScenesScope = (1 << 4),

        Strict = StrictProjectScope | StrictScenesScope,
    }

    public enum ValidationTestStatus
    {
        Unknown,
        OK,
        Failure,
    }

    public abstract class ValidationTest
    {
        public const int ImportanceMissionCritical = 0;
        public const int ImportanceUrgent = 1;

        // Name of the test
        public abstract string Name { get; }

        // Category of the test
        public abstract string Category { get; }

        // Importance of the test (DEFCON, where 0 is top priority)
        public virtual int Importance { get; } = ImportanceMissionCritical;

        // Capabilities
        public abstract ValidationTestCaps Caps { get; set; }
        public bool HasCaps(ValidationTestCaps requiredCaps) { return (Caps & requiredCaps) == requiredCaps; }
        public bool DoesntHaveCaps(ValidationTestCaps excludedCaps) { return (Caps & excludedCaps) == 0; }

        // Run before a certain test (optional)
        public virtual System.Type RunBefore { get; } = null;
        // Run after a certain test (optional)
        public virtual System.Type RunAfter { get; } = null;

        // Does it support assistance via <Assist()>
        public virtual bool CanAssist { get; } = false;
        public virtual string AssistTooltip { get; } = string.Empty;
        public virtual bool AutoRerunAfterAssist { get; } = false;

        // Does it support automatic fixing via <Fix()>
        public virtual bool CanFix { get; } = false;
        public virtual string FixTooltip { get; } = string.Empty;
        public virtual bool AutoRerunAfterFix { get; } = true;

        // Status of the test
        public ValidationTestStatus Status { get; protected set; } = ValidationTestStatus.Unknown;
        public int StatusCount { get; protected set; } = 0;

        protected string _currentTestScopeName = string.Empty;

        protected struct Result
        {
            public ValidationTestStatus Status;
            public int Count;

            public static Result FromCount(int errors, int total = 0)
            {
                return new Result
                {
                    Status = (errors == 0) ? ValidationTestStatus.OK : ValidationTestStatus.Failure,
                    Count = (errors == 0) ? total : errors,
                };
            }
        }

        // Reset the test status (and whatever internal state)
        public void Reset()
        {
            Status = ValidationTestStatus.Unknown;
            StatusCount = 0;

            OnReset();
        }

        protected virtual void OnReset()
        {
        }

        // Run the test
        // Test will be reset before OnRun()
        public void Run(ILevel context)
        {
            if (Status != ValidationTestStatus.Unknown)
                Reset();

            _currentTestScopeName = this.GetType().Name;
            var result = OnRun(context);
            _currentTestScopeName = string.Empty;

            Assert.IsTrue(result.Status != ValidationTestStatus.Unknown, "OnRun should not return Unknown status");
            Status = result.Status;
            StatusCount = result.Count;
        }

        // Return true on success
        protected abstract Result OnRun(ILevel context);

        // Run an extra function (when <CanAssist> is true)
        public void Assist(ILevel context)
        {
            if (!CanAssist)
                throw new System.InvalidOperationException();

            _currentTestScopeName = this.GetType().Name;
            OnAssist(context);
            _currentTestScopeName = string.Empty;

            if (AutoRerunAfterAssist)
                Run(context);
        }

        protected virtual void OnAssist(ILevel context)
        {
            throw new System.NotSupportedException();
        }

        // Run automatic fixes (when <CanFix> is true)
        public void Fix(ILevel context)
        {
            if (!CanFix)
                throw new System.InvalidOperationException();

            _currentTestScopeName = this.GetType().Name;
            OnFix(context);
            _currentTestScopeName = string.Empty;

            if (AutoRerunAfterFix)
                Run(context);
        }

        protected virtual void OnFix(ILevel context)
        {
            throw new System.NotSupportedException();
        }

        protected void PrintError(string error, Object context)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(_currentTestScopeName), "PrintError can only be used from within OnRun, OnAssist or OnFix");

            if (context is Component contextComp)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, contextComp, $"[<b>{_currentTestScopeName}</b>]: <color=red>{error}</color> @ {contextComp.gameObject.GetFullPath()}");
            }
            else if (context is GameObject contextGo)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, contextGo, $"[<b>{_currentTestScopeName}</b>]: <color=red>{error}</color> @ {contextGo.GetFullPath()}");
            }
            else
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, context, $"[<b>{_currentTestScopeName}</b>]: <color=red>{error}</color>");
            }
        }

        protected void PrintWarning(string warning, Object context)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(_currentTestScopeName), "PrintWarning can only be used from within OnRun, OnAssist or OnFix");

            if (context is Component contextComp)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, contextComp, $"[<b>{_currentTestScopeName}</b>]:  <color=yellow>{warning}</color> @ {contextComp.gameObject.GetFullPath()}");
            }
            else if (context is GameObject contextGo)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, contextGo, $"[<b>{_currentTestScopeName}</b>]:  <color=yellow>{warning}</color> @ {contextGo.GetFullPath()}");
            }
            else
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, context, $"[<b>{_currentTestScopeName}</b>]:  <color=yellow>{warning}</color>");
            }
        }

        protected void PrintLog(string text, Object context)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(_currentTestScopeName), "PrintLog can only be used from within OnRun, OnAssist or OnFix");

            if (context is Component contextComp)
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, contextComp, $"[<b>{_currentTestScopeName}</b>]: {text} @ {contextComp.gameObject.GetFullPath()}");
            }
            else if (context is GameObject contextGo)
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, contextGo, $"[<b>{_currentTestScopeName}</b>]: {text} @ {contextGo.GetFullPath()}");
            }
            else
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, context, $"[<b>{_currentTestScopeName}</b>]: {text} ");
            }
        }
    }
}
