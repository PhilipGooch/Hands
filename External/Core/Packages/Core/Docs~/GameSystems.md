# Game Systems

Mostly a clone of systems from Unity.Entities

## World

A World owns a set of GameSystems. You can create as many World objects as you like.

GameSystemWorldDefault is populated with all available GameSystem objects in the project.

## Systems

A System provides logic that happens in a predefined order.

Systems provide event-style callback functions, such as OnCreate() and OnUpdate() that you can implement to run code at the correct time in a system's life cycle. These functions are invoked on the main thread. In a GameSystemWithJobs, you typically schedule Jobs in the OnUpdate() function. The Jobs themselves run on worker threads. In general, GameSystemWithJobs provide the best performance since they take advantage of multiple CPU cores. Performance can be improved even more when your Jobs are compiled by the Burst compiler.

System classes are automatically discovered in your project and instantiated at runtime. Systems are organized within a World by group. You can control which group a system is added to and the order of that system within the group using system attributes. You can disable automatic creation using a system attribute.

A system's update loop is driven by its parent System Group. A  System Group is, itself, a specialized kind of system that is responsible for updating its child systems.

### System event functions

You can implement a set of system lifecycle event functions when you implement a system. Invoke order:

    OnCreate() -- called when the system is created.
    OnStartRunning() -- before the first OnUpdate and whenever the system resumes running.
    OnUpdate() -- every frame as long as the system has work to do (see ShouldRunSystem()) and the system is Enabled. Note that the OnUpdate function is defined in the subclasses of GameSystemBase; each type of system class can define its own update behavior.
    OnStopRunning() -- whenever the system stops updating because it finds no entities matching its queries. Also called before OnDestroy.
    OnDestroy() -- when the system is destroyed.

All of these functions are executed on the main thread. Note that you can schedule Jobs from the OnUpdate(JobHandle) function of a GameSystemWithJobs to perform work on background threads.

### System types

In general, the systems you write to implement your game behaviour and data transformation will extend either GameSystem or GameSystemWithJobs. The other system classes have specialized purposes.

    GameSystem -- Implement a subclass for systems that perform their work on the main thread or that use custom threading.
    GameSystemWithJobs -- Implement a subclass for systems that perform their work using jobs.
    GameSystemGroup -- provides nested organization and update order for other systems.

## Job dependency management

All jobs and thus systems declare what data types they read or write to. As a result when a GameSystemWithJobs returns a JobHandle it is automatically combined with the necessary dependencies.

Thus if a system writes to data type A, and another system later on reads from data type A, then the GameSystemWithJobs looks through the list of types it is reading from and thus passes you a dependency against the job from the first system.

GameSystemWithJobs simply chains jobs as dependencies where needed and thus causes no stalls on the main thread. But what happens if a non-job GameSystem accesses the same data? Because all access is declared, the GameSystem automatically completes all jobs running against data types that the system uses before invoking OnUpdate.

## Dependency management is conservative & deterministic

Dependency management is conservative. GameSystem simply tracks all data types ever used and stores which types are being written or read based on that.

Also when scheduling multiple jobs in a single system, dependencies must be passed to all jobs even though different jobs may need less dependencies. If that proves to be a performance issue the best solution is to split a system into two.

The dependency management approach is conservative. It allows for deterministic and correct behaviour while providing a very simple API.

## Sync points

Every World has its own DependencyManager and thus a separate set of JobHandle dependency management. A sync point in one world will not affect the other World.

Additional synchronization can be requested using synchronization attributes.

    [AlwaysSynchronizeSystem] — completes all jobs based on the declared dependencies.
    [AlwaysSynchronizeWorld] — completes all jobs in the current world.

## System Update Order

Use Game System Groups to specify the update order of your systems. You can place a systems in a group using the [UpdateInGroup] attribute on the system’s class declaration. You can then use [UpdateBefore] and [UpdateAfter] attributes to specify the update order within the group.

The GameSystemGroup class represents a list of related component systems that should be updated together in a specific order. GameSystemGroup is derived from GameSystemBase, so it acts like a component system in all the important ways -- it can be ordered relative to other systems, has an OnUpdate() method, etc. Most relevantly, this means component system groups can be nested in other component system groups, forming a hierarchy.

By default, when a GameSystemGroup's Update() method is called, it calls Update() on each system in its sorted list of member systems. If any member systems are themselves system groups, they will recursively update their own members. The resulting system ordering follows a depth-first traversal of a tree.

## System Ordering Attributes

The existing system ordering attributes are maintained, with slightly different semantics and restrictions.

    [UpdateInGroup] — specifies a GameSystemGroup that this system should be a member of. If this attribute is omitted, the system is automatically added to the default World’s UpdateSystemGroup (see below).
    [UpdateBefore] and [UpdateAfter] — order systems relative to other systems. The system type specified for these attributes must be a member of the same group. Ordering across group boundaries is handled at the appropriate deepest group containing both systems:
        Example: if SystemA is in GroupA and SystemB is in GroupB, and GroupA and GroupB are both members of GroupC, then the ordering of GroupA and GroupB implicitly determines the relative ordering of SystemA and SystemB; no explicit ordering of the systems is necessary.
    [DisableAutoCreation] — prevents the system from being created during default world initialization. You must explicitly create and update the system. However, you can add a system with this tag to a GameSystemGroup’s update list, and it will then be automatically updated just like the other systems in that list.
    [DisableAutoRegistration] - prevents the system from being automatically added to the default group. Can't be used with [UpdateInGroup].

## Default System Groups

The default World contains a hierarchy of GameSystemGroup instances. Only three root-level system groups are added to the Unity player loop:

    FixedUpdateSystemGroup (updated at the start of FixedUpdate phase of the player loop)
    UpdateSystemGroup (updated at the end of the Update phase of the player loop)
    LateUpdateSystemGroup (updated at the end of the PreLateUpdate phase of the player loop)

## Tips and Best Practices

Use [UpdateInGroup] to specify the system group for each system you write. If not specified, the implicit default group is UpdateSystemGroup.

Use manually-ticked GameSystemGroups to update systems elsewhere in the Unity player loop. Adding the [DisableAutoCreation] attribute to a component system (or system group) prevents it from being created or added to the default system groups. You can still manually create the system with World.GetOrCreateSystem() and update it by calling manually calling MySystem.Update() from the main thread. This is an easy way to insert systems elsewhere in the Unity player loop (for example, if you have a system that should run later or earlier in the frame).

Avoid putting custom logic in GameSystemGroup.OnUpdate(). Since GameSystemGroup is functionally a component system itself, it may be tempting to add custom processing to its OnUpdate() method, to perform some work, spawn some jobs, etc. We advise against this in general, as it’s not immediately clear from the outside whether the custom logic is executed before or after the group’s members are updated. It’s preferable to keep system groups limited to a grouping mechanism, and to implement the desired logic in a separate gme system, explicitly ordered relative to the group.
