@ECHO OFF
PUSHD "%~dp0"

ECHO Building documentation...
CALL "../Tools/docfx/docfx.exe" metadata %*
CALL "../Tools/docfx/docfx.exe" build %*

POPD
ECHO Done
