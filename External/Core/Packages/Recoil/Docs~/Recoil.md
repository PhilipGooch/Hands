# Recoil Concepts

Recoil library has three primary design goals:

* provide high performance access to Unity Rigidbodies,
* implement stable articulations on top of Unity Rigidbodies
* provide abstraction layer over PhysX, enabling switch to alternative physics engine if needed (Bullet, HAVOK, DOTS physics)

## Recoil.World

Performance is achieved by reading all the Rigidbody state ONCE per game step and storing it BURST compatible Recoil.Body structure. All operations like moving position or applying forces are done directly on cached state, finally it is written to PhysX scene exactly ONCE - at the end of game step.

### Frame processing order:

* Unity standard FixedUpdate
* FixedUpdateSystemGroup (IOnFixedUpdate, Recoil integration, Recoil WriteState)
* PhysX integration
* Recoil ReadState (in first trigger)
* PhysX trigger handling

## Recoil.ManagedWorld 

ManagedWorld is a gateway between PhysX bodies and Recoil World. Primary methods to be used are:

* RegisterBody - add Rigidbody to Recoil World and returns id to be used with all recoil methods, called by object that controls rigidbody lifecycle on initialization,
* UnregisterBody - untracks Rigidbody, usually called right before destroying body.
* FindBody - returns body id for already registered rigidbody, used by other scripts that need access to body,
* GetRigidbody - gets PhysX rigidbody, used to call PhysX API like add joint, or make kinematic.  

## IGetBodyExtensions

Most operations on bodies are done using IGetBodyExtensions of recoil World.

CG space to world conversions:

* TransformPoint - transforms point specified relative to CG to world space,
* TransformDirection - transforms direction in local coordinates to world space,
* InverseTransformPoint - transforms world point to CG space,
* InverseTransformDirection - transforms world direction to local space,

Velocity manipulation:

* GetVelocity - world velocity of center of mass
* GetRelativePointVelocity - world velocity of point specified as world offset from center of mass,
* GetLocalPointVelocity - world velocity of point specified in local coordinates,

* SetVelocity - write world velocity of center of mass
* AddVelocity, AddLinearVelocity, AddAngularVelocity - add deltaV 

Impulses:

* ApplyImpulse(ForceVector) - add angular and linear impulse
* ApplyImpulse(float3) - add linear impulse
* ApplyImpulseAtWorldPos - add linear impulse at world position
* ApplyImpulseAtRelativePoint - add linear impulse at world offset from center of mass
* ApplyImpulseAtLocalPoint - add linear world impulse at local point

Forces, for every ApplyImpulse, there's ApplyForce method that converts provided force to impulse by multiplying it by World.main.dt

