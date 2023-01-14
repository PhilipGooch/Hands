# Vehicle System

## Concepts

* IEngine - abstraction of an engine.
  * Outputs rotational power into IEngineAttachment.
* IChassis - abstraction of an unpowered chassis with one or more axles.
  * Inputs rotational power.
* IAxle - abstraction of an axle with one or more wheel hub assemblies.
* IWheelHubAssembly.
* IWheelHubAttachment - abstraction of something attachable to a wheel hub assembly.

![chassis](resources/chassis.png)

## Physical Chassis

Implementation of a physically based chassis using Unity's Rigidbodies.

* PhysicalChassis component - complete chassis configuration.
  * Applies steering to all steerable axles equally.
  * Distributes drive-shaft power to all powered axles based on differential settings.
* PhysicalWheelHubAttachment component - implementation of an attachable object (e.g., wheel).
* PhysicalWheelCollider mesh - recommended collider for all vehicles. Makes tweaking easier, as collider impacts a lot of game-feel, like bumpiness, speed and turning.
  * You can use custom colliders.

![chassis](resources/geometry.png)

Implementation details:

* Turning circle defines the smallest circle possible with no under-steering or over-steering.
* Utility Rigidbody will be added for every wheel with suspension.

## Helper Components

* ReferenceVehicleInputHandler
  * WSAD and gamepad input handler for debugging.

* VehicleDownForce
  * Applies constant force down.
  * Simulates larger tire friction.
  * Affects the suspension system as the entire car is pushed down.

## Known Issues & Limitations

* Better reverse handling, e.g.:
  * direction is changed while vehicle is not fully stationary.
* In case of large Rigidbody mass differences, flipped car wheels will go under the ground.
