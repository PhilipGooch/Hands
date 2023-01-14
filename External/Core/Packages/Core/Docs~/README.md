# NBG.Core

### Validation Tests

System for running custom tests on the entire project, loose scenes or a collection of scenes within a game specific definiton of a level.

* Implement _ILevelIndexer_ to provide level information to the test framework.
* Implement _ValidationTest_ to provide validation behaviour.
* Implementations will be registered via reflection.
* Strict tests are executed as NUnit tests.

```No Brakes Games -> Validation Tests...``` menu item opens the validation test overview window.

### Game Systems

* [Read more here](GameSystems.md)

### Logger

Wrapper for logging.

```
static Logger log = new Logger("Test Scope");
log.LogTrace("Test Trace");
```
Output:
```
[Test Scope][00:00:02.408000 (868)] Test Trace
```

How to override global defaults:
```
Log.DefaultLogLevel = LogLevel.Error;
```
How to override specific scope settings:
```
var backend = Log.GetOrCreateBackend("Test Scope");
backend.Level = LogLevel.Trace;
```

* NBG.Core.Log provides a static logging API and outputs to the global scope.
* NBG.Core.Logger is instantiatable and provides API which outputs to the user selected scope.
  * Logger support Domain Reload being disabled and can be statically allocated.
* Prefixes log scope.
* Prefixes frame number.
* Prefixes time stamp.
* Calls to logging API can be removed at compile time removal by defining an appropriate minimum verbosity:
  * NBG_LOGGER_LEVEL_TRACE,
  * NBG_LOGGER_LEVEL_LOG,
  * NBG_LOGGER_LEVEL_WARNING,
  * NBG_LOGGER_LEVEL_ERROR.
* Log scopes are automatically registered in the Debug UI after first use.
* Log scopes can be pre-warmed and different default settings applied using NBG.Core.GetOrCreateBackend().

Roadmap (TODO):

* Implement callbacks to not have a tight dependency on DebugUI.
* Support replacing default global Unity logger via Application.logMessageReceivedThreaded. For this a backend needs to handle file writing (or other output).

### Script templates

* [Script templates](ScriptTemplates.md) - guide to making templates that are used to create custom scripts in the same way that we have Create/C# script for default MonoBehaviour.