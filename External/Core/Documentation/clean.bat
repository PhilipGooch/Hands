@ECHO OFF
PUSHD "%~dp0"

ECHO Cleaning...
DEL /F /Q "../CoreSample/*.csproj"
DEL /F /Q "../CoreSample/*.sln"
DEL /F /S /Q "../BuildSystem/Artifacts/Documentation"
RMDIR /S /Q "../BuildSystem/Artifacts/Documentation"

POPD
ECHO Done
