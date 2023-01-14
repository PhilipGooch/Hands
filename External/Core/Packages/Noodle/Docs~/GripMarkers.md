# Grip Markers

Grip markers are helpers that define how and object will get grabbed by the character. Most of the markers can be used for both carryables and non-carryable objects (such as static geometry or anchored objects e.g. button). 

The way characted grabs an object DIFFERS between carryables  and not. Carryables being considerably lower mass than human are treated as being attached to the character. And non-carryables "anchor the human to the environment" so joints are aligned to lower arm axis to minimize torque on human character. 

![GripMarkers](resources/GripMarkers.png)

There are two ways to place grip markers - marking a collider GrabTarget **OR** placing child object with IGripMarker component. Try not to use both as this confuses targeting system.

#### Grab Target Colliders

While human can grab all colliders belonging to one of **Grabbable** layers, only objects in **GrabTarget** layer will be actively reached. **GrabTarget** colliders MUST be either primitive or convex. Grab target colliders should only be used for small objects or targets - buttons, small carryables that don't rely on other grip markers like stone.

**NOTE.** Don't mark big objects like crates or planks as **GrabTarget**. Human is agile enough to point in general direction of an object, Grab targets are designed for reaching small items.

#### GripPoint

GripPoint is the main grip marker to be used. If defines:

* where players palm should grab a carryable,
* where players fist should be anchored to non-carryable.

#### GripAxis

Grip axis can be seen as cylinder to which palm or fist will snap when grabbing an object. It runs along local Z axis, had limitFrom, limitTo to define cylinder length, and radius. Grip axis serves two purposes:

* Offseting anchor point from **GripPoint**  in carryables.  For example a handle of a stick has certain thickness - zero length **GripAxis** may be used if **GripPoint** places hand inside the handle - hand will be pushed away from core by radius.
* Carryables that do not depend on specific point to be grabbed, like pole, or thin non carryables that need snapping hand to a line.

**NOTE.** Non-carryables are only allowed 0 radius **GripAxis** - as the player will be able to rotate around it freely and this does not work with thicker objects.

#### GripCircle

GripCircle is used to snap grab anchor to a circle - for example a coin, steering wheel or some form of a valve.

#### GripDisc (experimental)

GripDisc is experimental grip marker, only to be used when **GripPoint** does not work.

It only supports for non-carryables and is designed to mark circular patches on objects that should be touched when grabbing, for example a large button. Small buttons should use **GripPoint**. 

**NOTE.** GripDisc may be cut from functionality, it is suggested not to use it.
