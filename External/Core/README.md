# Core

No Brakes Games engine components

[Release Notes](RELEASENOTES.md)

# Versioning

Master branch contains global _engine version tags_.

Packages are versioned individually: when package changes are introduced in a new engine release, package version is bumped up.

# Packages

* [Core](Packages/Core/Docs~/README.md) - collection of common utilities
	* [Game Systems](Packages/Core/Docs~/GameSystems.md) 
	* [Script templates](Packages/Core/Docs~/ScriptTemplates.md) - guide to making templates that are used to create custom scripts in the same way that we have Create/C# script for default MonoBehaviour.
	* [EventBus](Packages/Core/Docs~/EventBus.md) - Event Bus.
* Core.DataMinerStandardSources - standard data sources for the Data Miner
* Core.ObjectIdDatabase - persistent scene-unique id database
* [Actor](Packages/Actor/Docs~/Actor.md) - Actor system. An utility system that ties despawning, spawning, instantiation and pools of groups of objects and handles those functionalities through simplified api calls.
* Audio - audio system based on Unity audio
* Automation.RuntimeTests - runtime test framework
* DebugUI.View.uGUI - debug ui renderer based on Unity uGUI
* DebugUI.View.UIToolkit - debug ui renderer based on Unity UI Toolkit
* [Entities](Packages/Entities/Docs~/Entities.md) - lightweight implementation of Entity Component System.
* [Joints](Packages/Joints/Docs~/ReleaseNotes.md) - custom joints.
* [Logic Graph](Packages/LogicGraph/Docs~/LogicGraph.md) - visual scripting toolkit.
* [Logic Graph Editor UI](Packages/LogicGraph.EditorUI/Docs~/LogicGraphEditorUI.md) - editor UI for the Logic Graph.
* [ElementsSystem](Packages/MaterialSystem/Docs~/ElementsSystem.md) - Elements system.
* Net
	* [NetEventBus](Packages/Net/Docs~/NetEventBus.md) - Networked Event Bus.
	* [Net.Foundation](Packages/Net.Foundation/Docs~/Net.Foundation.md)- INetworkedBehaviour, INetStreamer
* Net.Transport
	* [NetTransportSockets](Packages/Net.Transport.Sockets/Docs~/README.md) - Socket Transport Layer.
	* [NetTransportSteamSockets](Packages/Net.Transport.SteamSockets/Docs~/README.md) - Socket Transport Layer for Steam.
* [Net.ReliableNetcode](Packages/Net.ReliableNetcode/Docs~/README.md) - adaptation of the reliable.io socket reliability layer.
* [Noodle](Packages/Noodle/Docs~/Noodle.md) - character dynamics.
* [Noodles.Animation.Editor.UI](Packages/Noodle.Animator.Editor.UI/README.md) - Noodle animator editor UI
* [Recoil](Packages/Recoil/Docs~/Recoil.md) - dynamics.
* [Recoil gravity](Packages/Recoil.Gravity/Docs~/Gravity.md) - Recoil extension that allows for custom region and/or object specific gravites.
* SuperCombiner - Mesh Batching Optimizer.
* [VehicleSystem](Packages/VehicleSystem/Docs~/VehicleSystem.md) - Vehicle system.
* VHACD.MeshColliderOptimizer - Unity editor tool for mesh collider optimization.
* [XPBDRope](Packages/XPBDRope/Docs~/XPBDRope.md) - extended position-based dynamics for ropes.
* [UndoSystem](Packages/UndoSystem/Readme.md) - Generic undo system.
* [Plugs](Packages/Plugs/Readme.md) - Plugs and Sockets system.
* [Pressure](Packages/Pressure/Docs~/PressureSystem.md) - Pressure system.
* [Impale](Packages/Impale/Docs~/Impale.md) - Impaling system.

# Scripting defines

* NBG_STEAM - enables Steamworks integration code
* NBG_DEVELOPMENT_MENU_COMMANDS - enables extra development menu items

# Automation

* [Build machine setup instructions](Docs/BuildMachines.md)
    * [Nintendo Switch setup instructions](Docs/NintendoSwitch.md)
* For CLI instructions, run the following command in Tools/Automation:

```
dotnet run
```

## Dependencies

* .NET 5.0 - https://dotnet.microsoft.com/download/dotnet/5.0
* 7-Zip - https://www.7-zip.org/

# Documentation

## Authoring
* Package documentation is expected to be in one or more Docs~ folders.
* Extra resources such as images are expected to be in Docs~/resources subfolders.

## Generating
* Visual Studio installation with `.NET SDK` module is required.
* Make sure that Unity project is closed
* Run Documentation/clean.bat, Documentation/generateProjects.bat and Documentation/build.bat

# Tests

Local packages are not testable by default, and must be registered in manifest.json testables key. See https://docs.unity3d.com/Manual/upm-manifestPrj.html for more info.

## Playtesting (Network)

Press F11 for debug menu and with E/Q navigate to <<Network>> screen. There are multiple choices:

* Start server (Sockets) - Sockets solution which will create a server on the local host. It will not work yet for testing across different devices.
* Start server (Sockets and Remote steam) - Needs steam connection and user with milkshake in the library. Will create two servers. The remote steam server can be tested with any steam user with milkshake in the library.
* Start server (Sockets and Local steam) - Needs steam connection and user with milkshake in the library. Will create two servers. Local steam server will be created on localhost so it can be connected just locally.
* Connect server (Sockets) - Can connect to a NON-steam network solution
* Refresh Steam Lobby List - All steam hosts will create lobbies (both remote steam and local steam). All lobbies will be listed and can be selected to connect to server. In most cases there will be just one lobby, but in some cases lobbies are not removed properly so it might be more than one. Newest lobbies are at list top. You can't connect to a different type of lobby (connecting locally to remote lobby or remotely to local lobby will yield errors for now).
* Clear Lobby List - Clears Steam Lobbies

Hosting can be done from any scene which supports networking (e.g. Bootloader scene will tell that it can't be a server). Connecting can be attempted from any scene. If the connection will be successful, then the correct scene will be loaded automatically. After connection is made, you can request player in the same <<Network>> screen.

There are still a lot of issues while testing multiplayer:

* Connecting to a server which is already has spawned player will break the game.
* Changing level while in networked mode sometimes yields errors (with spawned players, with objects implementing some networking API, etc.)
* Disconnecting is not handled in a proper way