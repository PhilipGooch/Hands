# Elements System

## Concepts

<b>`ElementsSystem` and `IElementsSystemObject`</b>

`ElementsSystem` can handle `IElementsSystemObject` which serves as base for all elementsSystem properties. GameSystem will handle all collisions with other `IElementsSystemObject` objects so you don't need to handle collisions.

One thing to keep in mind, that tracking finding collisions is performance heavy operation. Try reducing collider count on the objects or changing layers. You can change which collision layers will be checked inside ElementsSystem `layerMask`.
Heat system has two types of colliders, environmentalInteraction and object itself colliders. Environmental colliders are used for interacting with other objects (for example heat trigger is bigger than object itself). And object colliders is triggered by other object environmental colliders. So one type is receiver, another is sender.

<b>`ElementsSystemObject`</b>

All elementsSystem objects currently inherits this class. The purpose of this class is to hide methods so that only ElementsSystem would be able to call some methods.

<b>ElementsSystem currently can handle Heating, Burning and Melting</b>

Thus entire system consists `IHeatable`, `IFlammable`, `IMeltable`, `IExtinguisher` (for objects which will be acting as extinguishers for heatable and flammable). You can create more interfaces for extending ElementsSystem in specific way, for example IElecticConductor, etc.

Each part of ElementsSystem contains some generic parameters as ScriptableObjects, for example `IFlammable` contains `IFlammableSettings` and so on. Those settings is generic and it is expected that each implementation should at least implement them.

<b>Generic Implementations</b>

Each part of elementsSystem system contains one generic implementation:

* `ExtinguisherElement` implement `IExtinguisher`
* `FlammableElement` implement `IFlammable`
* `HeatableElement` implement `IHeatable`
* `MeltableElement` don't have generic implementation now

You can place one of these components on GameObject with collider/trigger and it will become part of ElementsSystem.

It is important that it would be one elementsSystem object per gameObject. It is possible to combine few elementsSystem objects but place them as siblings on children gameobjects with different colliders instead.

Each of those components can interact with predefined set of other elementsSystem parts. For Exmaple `HeatableElement` object will be triggered by `ExtinguisherElement` and will be extinguisher (or not) based on its settings, but Extinguisher will not be triggered by Heatable, and will not be heated.
If you want some specific different interaction between different parts of ElementsSystem, then you can extend, for example, `ExtinguisherElement` and override so it would interract with another specific part of system, for example, "IElectricConductor" or "HeatableObject". You can even combine multiple Interfaces into one implementation,
for example having `HeatableExtuinguishableElement` or so on.

Interaction Matrix:
![InteractionMatrix](resources/InteractionMatrix.png)

It is recommented to extend each of concrete implementations (for example inherit from `HeatableElement` and if needed inherit new settings for it from `HeatableElementSettings`).
If that is not enough, you can create your own implementation based on interfaces like (IHeatable, IElementsSystemObject, etc). Both ways shouldn't brake existing interactions.

<b>Colliders</b>

![ReceiversAndTransmitters](resources/DifferentHeatCollidersExplained.png)

By default all colliders on elementsSystem object is receivers. You can add transmitter colliders for heatable and flammable objects. In most cases those colliders should be marked as triggers. Also, add them to "IgnoreRaycast" layer for performance reasons. If you want to improve performance make sure that you dont have a lot of overlapping colliders, use as less colliders for elementsSystem objects as possible or place them in the layer that is not triggered by elementsSystem (configurable).

## Known Issues & Limitations

* EventSystem doesn't unregister events now
* Potentialy non deterministic id (in network). Currently we take them based on getComponents in scene order. There was a discussion that it isn't reliable and we need to take another approach
* Do we need actor system then it will be in core?