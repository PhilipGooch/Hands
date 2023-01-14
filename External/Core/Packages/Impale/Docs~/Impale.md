# Impale system

A system which handles various situations which require penetrating objects.
Use cases include nailing multiple objects together, arrows sticking into walls, axe sticking into log, etc.

## Editor Usage

### Impaler

A component which handles impaling. Can be overridden via script for custom behaviour.
<B>Basic Setup</B>
* Impaler must have an impaling collider assigned.
* Impale Start Local - a point until which the object can impale another object.
* Impale Direction - object must move in this direction in order to impale another object.
* Impaler Length - what distance from the start the tip of the impaler should be.

![GizmoSetup](resources/GizmoSetup.png)

* Red sphere - Imapaler start.
* Green sphere - Impaler end.
* Blue sphere at the arrow start - Raycast start.
* Blue sphere at the arrow tip - Raycast end.

<B>Logic Graph</B>
* onJointCreated - fired for each object which gets impaled after a joint is created.
* onJointLocked - fired the first time the joint gets locked.
* onJointDestroyed - fired after each joint gets destroyed.
* onUnimpaled - fired when object is no longer impaling anything.
