# Actor

The main goal of actor and actor system is to provide a simple API for spawning and respawning related activities and to reduce the unnecessary boilerplate code. Because of that it directly affects native Unity collider, trigger and rigidbody related interactions and recoil related interactions. It is composed of the Main module and other optional modules that help take care of managing different elements.

One of the key components is accurately respawning an actor with multiple bodies connected with joints and retaining accurate constraints of those joints. To that end actor initial positions of the body and relative bodies are saved and every time actor is respawned, regardless of location of respawning, it's spawned in the same initial 'stance'.

## IActor

This is the main entity of the actor system. Actor is anything that implements **ActorSystem.IActor** interface. Currently actor system expects that interface to be implemented on Unity Components. **ActorSystem.IActor** elements:

* **ActorGameObject** - Main game object of the actor. Should be the GameObject on which the **ActorSystem.IActor** implementing component sits.
* **PivotBodyID** - Rigidbody that is considered` _main_ body of the actor. This body will be placed at the designated respawn positions and all other member bodies of the actor will be respawned/placed relative to this body.
* **DefaultSpawnRelativeToBodyID** - For actors that respawn not in absolute position, but relative to another rigidbody in the game, this is the recoil id of that body.

* **DefaultSpawnPoint** - Used to get default spawn position. Called automatically on actor initialization.

### IActorCallbacks (implemented by IActor)

* **OnAfterSpawn** - event that is called after the actor is spawned.
* **OnAfterDespawn** - event that is called after actor is despawned.


# Actor System (Main module)

This is the main module for actor. Access it through global **ActorSystem.Main**. Main API:

* **OnActorRegister** - event callback for when a new actor is registered (as in initiated/added to the system).
* **OnActorUnregister** - event callback for when an actor is unregistered (as in removed from the system and/or destroyed).
* **Despawn** - A method to despawn an existing actor.
* **RequestSpawn** - A method to spawn an existing actor. It spawns in the saved spawn location. Because of unity physics joint related cleanups, this is not resolved instantly. Please use `IActorCallbacks.OnAfterSpawn()` to do any special actions after spawning. Despawn calls are an exception, if you call actor despawn while spawning hasn't fully resolved, this will cancel the spawn.
* **RequestRespawn** - A method to despawn and then spawn an existing actor. It spawns in a saved spawn location. Because of unity physics joint related cleanups, this is not resolved instantly. Please use `IActorCallbacks.OnAfterSpawn()` to do any special actions after spawning. Despawn calls are an exception, if you call actor despawn while spawning hasn't fully resolved, this will cancel the spawn.
* **SavePivotBodySpawnLocation** - A method to save placement (location and rotation) as an actor spawn point

# Joint Module

Accessed through **ActorSystem.JointModule** This module handles the tracking of dynamic joints. Main API:

* **RegisterDynamicJoint** - register a joint that should be destroyed when either this actor or the actor it's connected to get despawned/respawned.

Note: There is no need to unregister joints, even if you destroy them by any other means.

# Instantiation Module

Accessed through **ActorSystem.InstantiationModule** This module is responsible for Instantiating new objects and initializing them so they become fully functional with other systems. Main API:

* **InitializeActorSet** - This initializes a collection of actors. Use it when you manually create new objects and you want them to be properly initialized: Actors of the new objects will be properly registered, recoil bodies of the new object will be properly registered and `IManagedBehaviour` `OnLevelLoaded` and `OnAfterLevelLoaded` will be called, simulating the level start for the entire set (first all set items will receive `OnLevelLoaded`, then all set items will receive `OnAfterLevelLoaded`)

* **CreateActorSetFromPrototype** - Creates an actor set from a prototype, instantiates and initializes it (see **InitializeActorSet**).

* **CreatePool** - Used to create a pool from a set of existing actors (for creation of actors please use other methods).

* **DisposePool** - Used to dispose of an existing pool. Note that same way pool creation does not actually handle actor item creation, the same way pool destruction does not destroy members of a pool. Former members are returned in an out parameter of this method, to be dealt with manually.

# Net Module

This is the module, that doesn't have a direct access and use from outside. It's used behind the scenes to automatically support net sync of other modules and their behaviour. Currently outline for support is written only for the Main Module and intends to synchronize Spawn/Despawn over the network. GameObject id resolver issues stop it from working when it instantiates objects. Because calling the initial spawn state of those tries to make use of ObjectIDDatabase and new game objects aren't registered with that. TODO: Should also synchronize new object instantiation.

# Integration tips

* Simplest way of defining actor instances is tying IActor interface with a component from your project. ActorSystem interacts through IActor interface, but properties of IActor interafce imply a quite heavy coupling with Rigidbody and GameObject

* In a similar vein to Recoil bodies and IManagedBehaviour. Actor also has BootActor api. It's supposed to be run on a new level/scene entry (to gather pre-existing actors that were constructed during editing of the scene and not dynamically. It depends on Recoil bodies being already present, so it should be run after recoil bodies. It's also recommended to run it before IManagedBehaviour boot, to make actors already referencable in IManagedBehaviour calls. This is the point in time where the 'stance' of the actor is saved for respawning purposes.

* Actor provides tools for respawning. It does not handle meeting the respawn conditions and calling actual respawns. These should be implemented per-project basis in whatever the requirements for the project are.

* See example scene for a very basic respawn condition setup and a very basic actor implementation.