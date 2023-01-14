REM Safely terminates a remote desktop connection
REM Must have elevated privileges
REM @ECHO OFF
set MY_SESSION_ID=unknown
for /f "tokens=3-4" %%a in ('query session %username%') do @if "%%b"=="Active" set MY_SESSION_ID=%%a
tscon %MY_SESSION_ID% /dest:console
REM Locks the desktop
timeout 5
rundll32.exe user32.dll,LockWorkStation
