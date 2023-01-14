@ECHO OFF
CD /D "%~dp0"
ECHO Preparing checkout (%CD%)

ECHO [git] Configuring git lfs (large file storage)...
CALL git lfs install

ECHO Done
