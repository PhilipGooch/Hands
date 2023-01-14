# Gravity

This package allows user to have custom gravities for specific objects, in specific regions or any combination of those two. It uses regular Unity gravity as fallback wherever possible to minimize awake objects.

## Key terms

* **Default gravity** - Unity default gravity value that is applied to all the relevant objects through regular Unity Physx
* **Custom gravity** - a gravity value that is not the unity default.
* **No gravity** - No gravity is applied to the object. This is achieved through unity defaults as well.
---
* **Main gravity** - the main gravity for an object. It can be of any type: **Default gravity**, **Custom gravity** or **No gravity**.
* **Override gravity** - a temporary gravity that is applied to an object that meets certain conditions (within a trigger volume, etc.). This gravity can also be of any type: **Default gravity**, **Custom gravity** or **No gravity**

## Utility components

### GravityOverrideRegion

This component allows you to set the areas within which rigidbodies receive an **Override gravity** value. Parameters:

* **OverrideGravityId** - Id of this gravity override. This is referenced when we want to modify incoming overrides for specific objects
* **GravityOverride** - A **Custom gravity** value that will be applied as **Override gravity** within the region.
* **DefaultGravitySubAreas** - A collection of smaller Areas within the Override region that will deactivate the **Override gravity** and fallback to **Main gravity** for objects within. Uses Rigidbody detector components as values.
* **OnOverrideGravitySet** - A custom unity event when that triggers **Override gravity** is started to be applied to an object. It passes Rigidbody and bodyId values to the methods attached.
* **OnOverrideGravityClear** - A custom unity event that triggers when **Override gravity** stops being applied to an object. It passes Rigidbody and bodyId values to the methods attached.

### GravityMainCustom

This component allows you to set the **Main gravity** values for a specific Rigidbody. Parameters:

* **AllowGravityOverride** - This checkbox tells whether the object reacts to **Override gravity** caused by external factors
* **UseCustomGravity** - If checked then the **Main gravity** for the object will be a custom value
* **CustomGravity** - If the checkbox above is checked, this will be the **Main gravity** of the object.


### GravityModifyOverrides

This component allows you to set the **Override gravity** values for a specific Rigidbody. They are not applied until the specific override starts taking effect. This is meant to be used for extremely unique objects. Please try using this one as a last resort only. Parameters:

* **OverrideGravityModification** - This is a collection where you can add specific override values to replace the defaults when that override is happening. Each element contains:
	* **GravityId** - The id of the override we want to replace see **OverrideGravityId** of GravityOverrideRegion.
	* **GravityType** - What gravity will be applied once an override with the same ID takes effect (**None**, **Global default**, **Custom**)
	* **CustomGravity** - if custom is chosen above, enter custom value here.