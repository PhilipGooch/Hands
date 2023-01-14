# Nintendo Switch

## General
* _automation.json_ overrides the NINTENDO_SDK_ROOT environment variable for builds.
* Standard SDK path naming: C:\Nintendo\SDK#\_#\_# (e.g. C:\Nintendo\SDK12_3_1 for 12.3.1)

## Updating the Nintendo SDK
* Install the new SDK using _Nintendo Dev Interface_.
* Update NINTENDO_SDK_ROOT environment variable override in _automation.json_.

## Upgrading Unity version
* Update the Unity component of the Nintendo SDK using _Nintendo Dev Interface_.
* Install %NINTENDO_SDK_ROOT%\..\UnityForNintendoSwitch\UnitySetup-Nintendo-Switch-Support...

## Updating project after Unity upgrade
Deploy the new Unity plugin:
* Source: %NINTENDO_SDK_ROOT%\..\UnityForNintendoSwitch\Plugins\NintendoSDKPlugin\Libraries\NintendoSDKPlugin
* Destination: Packages/NintendoSDK.Switch/NintendoSDKPlugin

## Devkit settings
* htc-gen 2 (not 1)
* wifi disabled

## Connecting to a devkit with Target Manager 2
Our devkits are or should be set to use htc-gen 2 instead of 1, in order to do that both machine and devkit need some adjustments.
* From your pc open Nintendo Dev Interface and select "dev environments", select the newest version you have installed and look for "Launch Switching Tool".
* For the devkit hit the button on the bottom to switch it to gen2 if is not already done.
* For the machine select Enable and hit Apply. NOTE: This variable is USER BASED, so you need to do this for every user that is using the machine (special attention to this point for automation)
* documentation: https://developer.nintendo.com/html/online-docs/g1kr9vj6-en/document.html?doc=Packages/SDK/NintendoSDK/Documents/index.html?docname=NintendoSDK%20Documents

## Other
* WiFi should be setup if projects call into any networking API.
* User should be setup if projects call into any user API.
  * Enable "Skip the account selection if there is only one account" in devkit settings.
  * Multiple users on one system might prevent automation from working.
* SDEV usage manual: https://developer.nintendo.com/html/online-docs/g1kr9vj6-en/Packages/DevEnvironment/NX-SDEV_Usage_Manual/contents/title.html
* EDEV usage manual: https://developer.nintendo.com/html/online-docs/g1kr9vj6-en/Packages/DevEnvironment/NX-EDEV_Usage_Manual/contents/title.html
