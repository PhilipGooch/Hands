# filename: Configure.BuildMachine.ps1 (can be run via powershell)

# requirements
scoop install aria2
scoop install dark
scoop install git
scoop bucket add extras
scoop update

# command line tools
scoop install sudo
scoop install which
scoop install touch
scoop install wget
scoop install 7zip

# enable long paths
sudo Set-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1 -Verbose
# enable synchronous logon scripts
sudo Remove-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'RunLogonScriptSync' -Force -Verbose
sudo New-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System' -Name 'RunLogonScriptSync' -Value 1 -PropertyType 'DWORD' -Verbose
# disable hybernation
sudo powercfg -h off
# disable sleep
sudo powercfg /change standby-timeout-ac 0
sudo powercfg /change standby-timeout-dc 0
# add common Windows Defender exceptions
sudo powershell -Command Add-MpPreference -ExclusionPath 'C:\BuildAgent'
sudo powershell -Command Add-MpPreference -ExclusionPath 'C:\Program` Files\Unity'
sudo powershell -Command Add-MpPreference -ExclusionPath 'C:\Program` Files\Unity` Hub'
sudo powershell -Command Add-MpPreference -ExclusionProcess 'unity.exe'
sudo powershell -Command Add-MpPreference -ExclusionProcess 'UnityShaderCompiler.exe'
sudo powershell -Command Add-MpPreference -ExclusionProcess 'unity-accelerator.exe'

# update scoop
scoop update
scoop checkup

# GUI tools
scoop install notepadplusplus
scoop install filezilla
scoop install lockhunter
scoop install caffeine
