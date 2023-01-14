@ECHO OFF
PUSHD "%~dp0"

ECHO Serving documentation...
CALL "../Tools/docfx/docfx.exe" docfx.json --serve

POPD
ECHO Done
