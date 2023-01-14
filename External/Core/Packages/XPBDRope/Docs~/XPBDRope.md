# XPBDRope

The XPBDRope system is used to stabilize existing rope systems that use multiple joints to simulate a rope. Internally it uses the extended position based dynamics algorithms. It also features self collision, collision with specific layers, static&kinetic friction. The rope rigidbodies are updated via velocity.

Collisions are handled via continuous collision constraints that are generated internally based on predicted movement.

Continuous collision is based on "Real-time inextensible surgical thread simulation" https://link.springer.com/content/pdf/10.1007/s11548-018-1739-1.pdf
Friction is based on "Position-Based Simulation Methods in Computer Graphics": http://mmacklin.com/EG2015PBD.pdf

## Usage

There are three components that are required for the system to function.
1. **RopeSystem**. The RopeSystem is the main component that tracks all the ropes in the scene and executes their simulation. You must have a single RopeSystem in the scene in order to simulate ropes.

    The RopeSystem requires for the NBG physics World to be created and for the Dynamics SystemsManager to be bootstrapped. It also requires the BootManagedBehaviours to be called. **An external game manager needs to do this prior to using the ropes.**

    Parameters:
    - Iterations: the number of simulation iterations. The higher the number, the better the results, at the cost of performance.
    - Static friction: static friction coefficient.
    - Kinetic friction: kinetic friction coefficient.
    - Static Collision Layers: layers to use for collision detection. These layers will be checked for collisions inside the XPBD simulation. They will only affect the rope and will not receive any forces themselves, therefore it's best to use these layers for static colliders. These must not include the rope itself.


2. **Rope**. Each rope instance must have a Rope component in order to be simulated. The Rope component has a reference to each RopeSegment, reads and writes the data from the rope rigidbodies. The Rope component is also able to construct a rope based on handle positions and the rope parameters (Right click on the component and select Build Rope)

    Parameters:
    - Attach Start/End To: Objects to connect the start or end of the rope to when building the rope via context menu.
    - FixRopeStart/End : Prevent the rope start or end from moving. If the rope start/end is not attached to anything, it will create a fixed joint to the world position of the rope. If it's attached so something, it will tell the solver not to move the segment away from the object when simulating the rope (useful when attaching to kinematic rigidbodies or similar).
    - FixRopeStart/End Rotation : Prevent the rope start or end from rotation. If the rope start/end is attached to something (either another object or the world itself) it will no longer be able to rotate freely. This does nothing if the rope is not attached to anything.
    - Radius: determines how thick the rope is in the simulation, based on capsule radius.
    - SegmentLength: determines how long each rope segment is in the simulation, based on capsule length. Segments might be a bit longer to fully fill the whole rope.
    - SegmentOverlap: determine the overlap between two rope segments. The recommeneded value is twice the rope radius.
    - UseTwistLimits: should we use twist constraints for the rope segments. A twist constraint applies angular velocity if a segment twists too far apart from its neighboring segment.
    - TwistLimit: the maximum angle in degrees that two segments can twist from each other.
    - Compliance: XPBD compliance for rope bending and stretching. The lower the value, the more rigid the rope. Setting this to 0 makes the rope simulate as a PBD system. Too soft will not pull rope together, too stiff send too much waves.
        - 0.00000000004f;  0.04 x 10^(-9) (M^2/N) Concrete
        - 0.00000000016f;  0.16 x 10^(-9) (M^2/N) Wood - good for ropes
        - 0.000000001f;  1.0  x 10^(-8) (M^2/N) Leather
        - 0.000000002f;  0.2  x 10^(-7) (M^2/N) Tendon
        - 0.0000001f,  1.0  x 10^(-6) (M^2/N) Rubber
        - 0.00002f;  0.2  x 10^(-3) (M^2/N) Muscle (too soft)
        - 0.0001f;  1.0  x 10^(-3) (M^2/N) Fat

    - SegmentMass: the mass of the segments generated via context menu.
    - Drag: the drag value for the segment rigidbodies.
    - Angular Drag: the angular drag value for the segment rigidbodies.
    - Interpolation: the interpolation mode of the segment rigidbodies.
    - CollisionDetectionMode: the collision detection mode of the segment rigidbodies.
    - Physic Material: the physic material of the segment rigidbodies.
    - RopeLengthMultiplier: the relative length of the rope. By changing this value you can control how long the rope is, based on max rope length.
    - MaxRopeLength: the maximum length of the rope.
    - Handles: A list of transforms to use when constructing the rope via context menu. It will build the rope from one handle to the next. Can have multiple handles.
    - Bones: A list that contains each rope segment of this rope. Must contain valid references to the rope segments, the rope system uses these for reading/writing data to rigidbodies.


    Public properties:
    - RopeLengthMultiplier: values from 0 to 1. Set this to change the length of the rope.
    - MaxRopeLength: returns the maximum possible length of the rope.

3. **RopeSegment**. Each rope segment must contain a RopeSegment component. This is the component that is referenced by the rope when reading/writing rigidbody data.

    Parameters:
    - Fixed position: if enabled, the segment will be simulated as fixed in place. Use this for stabilizing fixed joints.
    - Inv Mass Override: if larger than zero it will override the inverse mass of the object. The smaller the number, the heavier the object is in the simulation.

There are some optional components that work with the existing system, but are not required.
1. **RopeRenderer**. Attach this component to a rope in order to render it. It generates a basic mesh based on the rope segment positions.

    Parameters:
    - Segments Around: how many subdivisions should form the rope cylinder. Thicker ropes might need more segments to not look blocky.
    - Radius: how thick is the rope mesh. Ideally this should match the rope radius.
    - MeshSegmentsForRopeSegment: how many mesh subdivisions should each rope segment get. The more subdivisions, the smoother the rope, but this comes at the cost of performance and more vertices.
    - yUVScale: how should the rope UVs scale along the y uv axis. UVs wrap along the y axis and can repeat indefinitely.
    - xUVLimits: what are the limits for the x uv axis. The x uv axis will go from the limit.x value to the limit.y value as it is drawn around the rope. It will not wrap or repeat. In most cases this should be from 0 to 1, but if you have other sprites inside the rope texture, it might be helpful to limit the rope to its own sprite instead of the whole texture.

## Interfaces

You can extend the rope behaviour by implementing one of these interfaces. The rope system automatically collects the interfaces attached to the rope and rope segment objects and calls them when needed. All you have to do is implement the interface and add it to the right gameobject. The idea is that you should be able to combine, mix&match any number of different interface implementations without bloating out the base rope & ropeSegment classes. Create a rope that floats or an electric fire rope that also sends packets through the world wide web for some reason. The sky it the limit!

1. **IRopeCreationListener**. Add this next to the rope component. Get callbacks before and after the rope is created. Useful for tweaking some rope values after creation or calculating some custom values in your own component.
2. **IRopeSegmentCreationListener** Add this next to the rope component. Get callbacks before and after a rope segment is created. Useful for attaching custom scripts or components next to a rope segment.
3. **IRopeSolveListener** Add this next to the rope component. Get callbacks before and after the rope simulation happens.
4. **ISegmentRigidbodyListener** Add this next to the rope segment component. Get callbacks before the rope system reads data from the recoil body and after the rope system writes data into the recoil body. Useful for adjusting the recoil body before the simulation happens.

## Extendable Ropes

Extendable ropes are now possible and can be achieved by following these steps:
1. Set the rope handles where you want the initial visible rope to be.
2. Set the max rope length to the desired length.
3. Rebuild the rope.
4. In runtime, adjust the rope length multiplier from 0 to 1 to control the rope length.

The maximum rope length can be any number you want, but it can't be smaller than the length of the rope that you get from the rope handles. 

If you increase the max length, the rope length multiplier will be automatically adjusted, so that only the rope segments set by the handles are active on start. For example, if you make a rope that is 5 units long through handles and then increase its max length to 10, the multiplier will automatically set to 0.5. In runtime you can change the multiplier to extend/retract the rope further.
