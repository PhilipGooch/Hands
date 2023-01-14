# Sheep

[![Build status](https://factory.nobrakesgames.com/app/rest/builds/buildType:id:Sheep_VerifyBuild/statusIcon.svg)](https://factory.nobrakesgames.com/viewType.html?buildTypeId=Sheep_VerifyBuild)

## Setup: LFS

This repository uses Git Large File Storage (LFS).

Execute 'git lfs install' after cloning.

## Components Documentation:

* Serialization and Global data:
	* ***Game Settings*** - store data in PlayerPrefs.
	* ***Game Parameters*** - instance of a ScriptableObject for storing global variables (particle prefabs, materials, etc)

* [Haptics](Documentation/Haptics.md) - Haptic feedback components.
* [Activators](Documentation/Activators.md) - Activator components.
* [Platforms](Documentation/Platforms.md) - Platform components.
* [Grab Interactions](Documentation/GrabInteractions.md) - Various grab components.
* [Utilities](Documentation/Utilities.md) - Various utilities.
* [Audio](Documentation/Audio.md) - Audio setup and utilities.
* [Plugs and Electricity](Documentation/Plugs.md) - Plugs and Sockets + Electricity sheep setup.
* [ReBody and Recoil](Documentation/ReBody.md) - ReBody guide.

## Systems Documentation:

* [Level Validation Tool](Documentation/LevelValidationTool.md) - Window used to validate scenes.

## Devices Documentation
* [Oculus Platform](Documentation/Oculus.md) - Oculus device setup.

## Publishing
* [Publishing](Documentation/Publishing.md) - General publishing info.
