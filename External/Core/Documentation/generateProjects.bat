@ECHO OFF
PUSHD "%~dp0"/../Tools/Automation

ECHO Generating projects...
CALL dotnet run --config "core.automation.json" runUnityEditor "-executeMethod DocsUtils.Prepare"
SET ERRORCODE=%ERRORLEVEL%

POPD
ECHO Done
EXIT /B %ERRORCODE%
