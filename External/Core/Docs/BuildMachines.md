# Build Machines

Build and test automation is expected to be very robust. This is why dedicated machines are used.

## Setting up a Windows build machine

* Install Scoop via Tools\BuildMachine\Windows\Install.Scoop.ps1 (admin mode powershell)
* Install various utilities and configure Windows settings via Tools\BuildMachine\Windows\Configure.BuildMachine.ps1 (admin mode powershell)

* Enable auto logon (netplwiz.exe).
    * Check Windows Sign-in Options and disable "For improved security..." option if needed.
	* Enable auto desktop lock after logon via Local Group Policy Editor (gpedit.msc):
		* Administrative Templates / System / Login / Run these programs at user logon
		* Run Tools\BuildMachine\Windows\LockDesktop.bat
* Install Unity Hub into the default path (https://unity3d.com/get-unity/download)
    * Install all required Unity versions and addons
    * Install a license or setup Unity licensing server
        * Unity licensing server `services-config.json` goes into `%PROGRAMDATA%\Unity\config`
* (Optional) Install Unity accelerator locally, unless the entire build farm shares one (https://docs.unity3d.com/Manual/UnityAccelerator.html)
    * Allow insecure access
    * Enable system-wide via Unity Editor preferences
        * Verify that `HKEY_CURRENT_USER\SOFTWARE\Unity Technologies\Unity Editor 5.x` property `CacheServer2` address is valid, and `CacheServer2` is `0`.
* Install Visual Studio C++ Workflow for IL2CPP support.
* Install dotnet 5 SDK (https://dotnet.microsoft.com/download/dotnet/5.0)
* Install Steam into the default path (https://store.steampowered.com/)
    * Login once with every Steam user used in automation and confirm 2FA
    * Move 2FA ssfn files to C:\SteamSentryFiles\STEAMUSERNAME from C:\Program Files (x86)\Steam for every user
        * ssfn files are hidden
* Install platform SDKs manually if required
* Install TeamCity build agent (via TeamCity UI Agents section)
    * Ensure agent auto starts in an interactive environment (via Task Scheduler)
    * Update C:\buildAgent\conf\buildAgent.properties
        * Set serverUrl
        * Set name
        * Set configuration parameters

## TeamCity build agent work flow

The build agent will handle everything else, including version control. No extra setup or software is required.

## Disk space considerations

* Limit TeamCity local artifact cache (build agent config):
  * teamcity.agent.filecache.size.limit.bytes=64000000000
* Ask TeamCity to ensure a certain amount of space is available prior to building (build agent config):
  * teamcity.agent.ensure.free.space = 64gb
* Limit Unity Accelerator cache size (config in %LOCALAPPDATA%\UnityAccelerator)
  * CacheMaxUsedBytes=64000000000
* Limit Unity GI cache (via UI or registry entry GICacheMaximumSizeGB_h1121868111)

## References

* https://superuser.com/questions/352616/automatically-login-and-lock/899927#899927
