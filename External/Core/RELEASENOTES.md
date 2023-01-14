# Release Notes

## 0.8.4
- FEATURE: EditorTitleBar moved from Milkshake. Enable using "No Brakes Games/Utilities/Advanced Editor Title Bar".
- IMPROVEMENT: Impale - better stationary impaler detection
- IMPROVEMENT: Automation - RuntimeTests now detect HTTP errors.
- CHANGE: Automation - Switch build artifacts are not compressed anymore, as NSP is compressed itself.
- FIX: Automation - RuntimeTests now correctly detect process completion on Switch.

## 0.8.3
- FEATURE: Added a tool to force all materials to reserialize: No Brakes Games / Utilities / Touch All Materials.
- FEATURE: Enabled the use of C# 9 init-only properties to all code that references NBG.Core.
- IMPROVEMENT: CoreSample - [CTT-321] Net Sample can be tested in builds
- IMPROVEMENT: Impale - more cofiguration options for aligning with a hit normal, better actor integration.
- IMPROVEMENT: NoodleAnimator - Can toggle debug gizmos on human.
- IMPROVEMENT: NoodleAnimator - [CTT-421] Mirror (flip) selection horizontaly
- IMPROVEMENT: NoodleAnimator - [CTT-422] Set transitions to all selected tracks
- IMPROVEMENT: NoodleAnimator - "Select all keys" hotkey.
- FIX: NoodleAnimator - [CTT-418] Ctrl c + v no longer cuts keyframes instead of just copying.
- FIX: NoodleAnimator - [CTT-419] Now possible copy paste multiple tracks on diffrent tracks.
- FIX: Impale - Multihit impaling works as intended.

## 0.8.2
- CHANGE: Unity 2021.3.15f1.
- FIX: Automation - Write screenshots to disk using platform specific APIs. Fixes a race condition on Switch.
- FIX: Core - OnFixedUpdateSystem will always run before PhysicsSystemGroup.

## 0.8.1
- FEATURE: Electricity system.
- IMPROVEMENT: Validation tests now have importance. Automation can choose importance per level and per project.
- FIX: Steam - [CTT-384] No longer creating garbage on each message received.
- FIX: Net/CoreSample - [CTT-325] Connecting to a server with already spawned players no longer causes errors.
- FIX: Net/CoreSample - [CTT-387] Client camera can now follow player.
- FIX: Net - [CTT-324] Scene switching while in networked mode in CoreSample

## 0.8
- FEATURE: Destruction system.
- IMPROVEMENT: Impale - Various bug fixes.
- IMPROVEMENT: Ropes - constraint generation refactored & is much faster.
- IMPROVEMENT: Ropes - Add error when rope joints are destroyed to prevent incorrect behaviors.
- CHANGE: Help URLs now use HTTPS.
- CHANGE: Steam networking upgraded to the latest Steam Sockets.

## 0.7.26
- FEATURE: Locomotion - Allow agents to (optionally) jump into bottomless pits after some time.
- IMPROVEMENT: Locomotion - Better edge avoidance. Check close around the agent and also check in front of the current heading.
- IMPROVEMENT: Impale - Hit dot accuracy is now configurable. Removed unused method. Raycasts now ignore triggers.
- IMPROVEMENT: Plugs - Plug/Hole has a plugged-in state getter.
- IMPROVEMENT: Pressure - Ports can now be attached or detached on runtime.
- CHANGE: Material system renamed to Elements system.
- CHANGE: Wind - Removed particle system integration from the Wind package.
- FIX: Wind - Fixed gizmos.
- FIX: Wind - Fixed furthest blocker object distance check.
- FIX: Impale - Projecting impale position to prevent it from no clipping.
- FIX: Networking - Restored quick connect feature in CoreSample.

## 0.7.25
- FEATURE: Infrastructure setup to have Component help icons link to docs.hq.nobrakesgames.com.
- IMPROVEMENT: Locomotion - Expose currentFacingBias inside ObstacleAvoidance to allow better configuration of agents and their turning behaviors.
- IMPROVEMENT: Locomotion - Add the ability to disable locomotion handlers.
- IMPROVEMENT: LogicGraph - Scope icons can be enabled even if scopes are disabled, using #NBG_LOGIC_GRAPH_ENABLE_SCOPE_ICONS.
- IMPROVEMENT: LogicGraph - Settings menu allows to disable nodes from showing in the hierarchy searcher.
- IMPROVEMENT: LogicGraph - Support for opening graphs on disabled GameObjects.
- IMPROVEMENT: Pressure - "Connect to nearby port" button on port.
- IMPROVEMENT: Impale - Aligning with normal now happens over time.
- CHANGE: Locomotion - Edge avoidance only runs if the agent is on the ground.
- CHANGE: Locomotion - Adjusted edge avoidance input velocity reduction algorithm.
- FIX: LogicGraph - "Open Node Source" now works with [NodeAPI] nodes.
- FIX: LogicGraph - Updating Logic Graph hierarchy no longer brakes drag and drop node creation.
- FIX: LogicGraph - Updating Logic Graph hierarchy no longer brakes keyboard navigation.
- FIX: Pressure - Preventing open ports with connections from leaking pressure.

## 0.7.24
- IMPROVEMENT: Water - Added API to the FloatingMeshInstance to determine the final Mesh. 
- IMPROVEMENT: Water - Added a validation test to check if Meshes used for FloatingMesh are readable. 
- CHANGE: CoreSample project networking implementation upgraded.
- CHANGE: LogicGraph - Broadcast node does not use EventBus anymore. View scope events are handled in OnUpdate. Network functionality disabled for now.
- FIX: LogicGraph - More robust node deserialization. ErrorNode now shown in case of internal serialization errors too.
- FIX: LogicGraph - Built-in node names are now user friendly.
- FIX: Vehicles work over network correctly again.
- FIX: Network server broadcast no longer throws exceptions.
- FIX: Network build IL2CPP stipping problems fixed.

## 0.7.23
- IMPROVEMENT: BoxBounds now has API to encapsulate other bounds.
- FIX: LogicGraph - Runtime scope checks can now be correctly disabled.

## 0.7.22
- IMPROVEMENT: Wind - Box cast shape now can have any kind of dimensions.
- CHANGE: Expose extra rope radius for renderers in the rope profile.
- CHANGE: Added gizmos to Plugs and Holes for easier setup.
- CHANGE: Material system no longer checks if anything is between two intersecting objects. Big Colliders will be more responsive.

## 0.7.21
- IMPROVEMENT: ObjectId database now has compacted ids for use in networking.
- CHANGE: EditorProjectSettings is now deprecated.
- CHANGE: NBG.Joints settings moved to Assets\Settings\Editor\NBG.Joints.Settings.asset.
- CHANGE: OnFixedUpdateSystem and OnUpdateSystem are now in NBG.Core 
- FIX: Recoil - RigidbodyRegistration will now destroy internal components.
- FIX: Locomotion - Counteract target velocity when trying to avoid edges.
- FIX: Automation - Don't fail automation on Switch during file flushing.

## 0.7.20
- IMPROVEMENT: Logic Graph - moved some toggles to a new dropdown settings menu.
- FIX: Logic Graph - LogicGraphPlayer now does not allocate GC memory in Update and FixedUpdate.
- FIX: Logic Graph - [CTT-196] NodeAPI attribute now does not work on non-public members.
- FIX: Logic Graph - [CTT-252] Better light theme support.
- FIX: Logic Graph - [CTT-311] ErrorNode will not break prefabs anymore.
- FIX: Material system will not register objects if they are disabled on start.
- FIX: Recoil - Hacky workaround "Rotation quaternions must be unit length" error on Nintendo Switch. 

## 0.7.19
- IMPROVEMENT: Impale - Now uses volume casts instead of a single raycast.
- IMPROVEMENT: LogicGraph - [CTT-301] Nodes can now be Generic, Simulation or View. Linking Simulation and View nodes together is not allowed.
- IMPROVEMENT: LogicGraph - [CTT-163] Show toast notifications for user feedback.

## 0.7.18
- IMPROVEMENT: LogicGraph - [CTT-275] Color type is now supported.
- CHANGE: LogicGraph - Broadcast node split into Broadcast and OnBroadcast nodes.
- FIX: Water - Bring back FloatingMesh.BuoyancyMultiplier, which was removed in Core 0.7.16.

## 0.7.17
- FIX: Material system now works in builds [CTT-293].
- FIX: Wind - [MKSBUGS-1784] Wind now affects objects with multiple rigidbodies the same as objects with one rigidbody.

## 0.7.14 (hotfix 1)
- FIX: Automation - always restart the new Steam client if it is on the login window.

## 0.7.16
- IMPROVEMENT: Wind - Custom editor is now extendable. 
- IMPROVEMENT: Wind - Moved non game-specific code from specific implementations to the package.
- IMPROVEMENT: Water - Better split of simulation settings and per-instance settings.
- IMPROVEMENT: LogicGraph - [CTT-288] Hierarchy and right click searchers now start with root elements expanded
- FIX: LogicGraph - [CTT-286] Changing connected ports breaks Logic Graph editor
- FIX: LogicGraph - [CTT-287] Searcher now supports filtering by component name. Smarter and more consistent search overall.
- FIX: Impale - Impaling multiple colliders on the same rigidbody no longer creates a joint for each collider.
- FIX: Impale - Only one joint is created for all impaled non rigidbody objects.

## 0.7.15
- FEATURE: Actor has been extracted to Core.
- FFEATURE: LogicGraph - [CTT-278] Added a custom getter node.
- FIX: Water - WaterBox flow direction can now be tweaked in play-mode.

## 0.7.14
- FFEATURE: New generic Impaling (Nailing/Penetration) system.
- FFEATURE: Added MeshGeneration package with various utilities for polygons, meshes and voxels.
- FFEATURE: Added DotsPlus (Asset Store) package.
- FIX: Logic Graph - [CTT-244] Ensure assemblies with node code have AlwaysLinkAssembly attribute.
- FIX: Logic Graph - [CTT-282] Optimize initialization of node bindings.
- FIX: Workaround for the issue of Unity Editor not reloading scripts after code changes.

## 0.7.13
- FFEATURE: Water system v2.
- IMPROVEMENT: Material system performance improved.
- IMPROVEMENT: Material system tests.
- IMPROVEMENT: Vehicle system some editor changes for ease of use and engine parameters as a ScriptableObject.
- IMPROVEMENT: ValidateInvalidDisallowMultipleComponents test now detects corrupted GameObjects with double Transforms.
- FIX: Vehicle system - [CTT-269] Vehicles in flat hierarchy.

## 0.7.12
- FEATURE: Heat system initial release
- IMPROVEMENT: Pressure - added documentation and improved logic graph API
- FIX: Logic Graph - [CTT-251] [CTT-267] Node Searcher is no longer rebuilt after every hierarchy change.
- FIX: Logic Graph - [CTT-199] searcher reselects correct element after node creation

## 0.7.11
- IMPROVEMENT: Joints - Exposed prismatic joint read only range of motion to public API.

## 0.7.10
- IMPROVEMENT: Wind - improved performance, removed garbage allocations.
- IMPROVEMENT: Plugs - support instantiation and added Logic Graph hooks to events.

## 0.7.9
- FIX: Noodle - NoodleSkinBinder functionality restored.

## 0.7.8
- FEATURE: Pressure system (version 1).
- CHANGE: ALINE updated to 1.6.2.

## 0.7.7
- FEATURE: Added BoxBounds utility.
- FIX: Wind - Almost zero GC and added missing particle null checks.
- FIX: Logic Graph - Resolved serialization data inconsistency which happens after deleting a node on a prefab, if a modified instance of it exists in a scene.

## 0.7.6
- FEATURE: Water system and floating meshes (based on Sheep).
- IMPROVEMENT: Refactor rope building code to enable runtime rope building.
- FIX: Fix ropes causing connected bodies to explode if they are disabled.
- FIX: Logic Graph - Exception caused by disabling an Event node referencing a private event fixed.

## 0.7.5
- FIX: Vehicles - Physics iteration count reduced for performance.
- FIX: Logic Graph - Private event bindings now work.
- FIX: Logic Graph - Event re-entrancy issue resolved.

## 0.7.4
- FEATURE: Wind Zones.
- IMPROVEMENT: Vehicles - Added TargetSpeed to IChassis.
- FIX: Logic Graph - Fix exception which happens if a LogicGraph with an Event node is enabled during a callback from another event.
- FIX: Logic Graph - Fix error in case multiple events are defined in derived types.

## 0.7.3
- FEATURE: Logic Graph - Added a networked Broadcast node.
- IMPROVEMENT: Ropes - Rope property RopeLengthMultiplier is now accessible in Logic Graph.
- IMPROVEMENT: Networking - Apply [NetEventBusSerializer] to event serializers to automatically register them with NetEventBus with a dynamic network id.
- IMPROVEMENT: Logic Graph - Node archetype serialization is now defined via [NodeSerialization].
- IMPROVEMENT: CoreSample - Added a test for Unity event execution order.
- CHANGE: Networking - INetEventBusIDs is now used to override dynamic NetEventBus network ids.
- FIX: Logic Graph - CTT-234, Destroying the LogicGraphPlayer component now clears the Logic Graph window.
- FIX: Networking - NetBehaviourList now correctly registers scene root GameObjects.
- FIX: CoreSample - Networking example now registers INetBehaviours.

## 0.7.2
- FEATURE: Added ValidateThereAreNoErrorsInPrefabs validation test - detects prefab deserialization and OnValidate issues.
- CHANGE: ALINE upgraded to version 1.6.

## 0.7.1
- FIX: Validation Tests - Project-wide tests now correctly use the success value, even when unrelated asserts fire.
- FIX: Vehicles - Allow 1-wheeled axles to be at an offset.

## 0.7.0
- FEATURE: Vehicle System BETA.

## 0.6.27
- IMPROVEMENT: Logic Graph - Ctrl + A now selects all nodes.
- FIX: Logic Graph - Show user friendly description in the UI.
- FIX: Logic Graph - LocalRotation node description fixed.
- FIX: Logic Graph - Only allow one LogicGraphPlayer per GameObject.
- FIX: Logic Graph - Optimized DelayNode memory allocations.
- FIX: Logic Graph - Reverted Graph View layout changes which made minimap hidden (and caused other issues).
- FIX: Logic Graph - Fixed a lot functionality and layout bugs in Unity 2021.1+.
- FIX: Joints - Changing PrismaticJoint position in edit mode will now work.
- FIX: Noodle - NoodleHand grabbing logic will only consider the hierarchy down from the grabbed Rigidbody or Collider until a descendant (nested) Rigidbody.

## 0.6.26
- FEATURE: Added EarlyUpdateSystemGroup.
- FIX: Networking - Fixed CT-160, Networking error after long update delays (frame hiccups).
- FIX: Fixed Steam client connection creating an extra peer instance.
- FIX: Fixed incremental error caused by floating point inaccuracies when applying deltas for a long time.
- FIX: Networking interpolation will not reset forces anymore when lagging behind.
- FIX: Logic Graph - Fixed CT-197, Selecting multiple nodes no longer breaks the node inspector.
- FIX: Logic Graph - Fixed CT-198, Fixed null reference after clearing scene changes and having graph open.
- FIX: Logic Graph - Fixed CT-217, Large Component inspectors no longer break the node inspector.
- FIX: Logic Graph - Fixed CT-204, Node inspector now scrolls.
- FIX: Logic Graph - Added a Delay node.

## 0.6.25
- FEATURE: LowLevelPlayerLoopEventEmitter emits events from Unity's LowLevelPlayerLoop.
- CHANGE: ObjectIdDatabaseResolver replaces NetObjectIdDatabase.
- CHANGE: EventBus moved into NBG.Core assembly.
- CHANGE: TriggerProximityList received minor API changes.
- CHANGE: GameSystemWorldDefault now uses the LowLevelPlayerLoop approach by default, via the EventBus.
- FIX: Logic Graph - Fixed CT-184, Right click searcher not focusing filter field on open.
- FIX: Logic Graph - Fixed CT-200, Selecting node should not focus searcher filter field.
- FIX: Logic Graph - Fixed CT-207, Typing in node input field should not focus searcher filter field.

## 0.6.24
- FEATURE: [ReadOnlyField] and [ReadOnlyInPlayModeField] attributes added to Core.
- FEATURE: Automation - Command to sign Android App Bundle (APK).

## 0.6.23
- FEATURE: Recoil - Added ReBody, a helper-wrapper for performing common Rigidbody functions via Recoil. 
- CHANGE: Easing helpers moved to Core package NBG.Core.Easing namespace.

## 0.6.22
- IMPROVEMENT: Automation - Take a system screenshot when runtime tests timeout.
- IMPROVEMENT: Logic Graph - Searcher tabs are now toggleable.
- IMPROVEMENT: Logic Graph - Node links now indicate when they are executed.
- IMPROVEMENT: Logic Graph - Node flavor text is now multiline (makes nodes more compact).
- IMPROVEMENT: Logic Graph - Node inspector can now show the source code for the bindings.
- IMPROVEMENT: Logic Graph - Node bindings with outputs can be made into flow nodes via an opt-in NodeAPIFlags.ForceFlowNode flag.
- IMPROVEMENT: Logic Graph - Added standard math nodes for int/float types: Less, LessOrEqual, Greater, GreaterOrEqual, and also Approximately for float.
- CHANGE: Logic Graph - Moved blackboard and inspector toggles to toolbar.
- FIX: Logic Graph - UI will not be constantly repainted in edit mode.
- FIX: Logic Graph - Fixes for light Unity editor skin.
- FIX: Logic Graph - Changing the binding definition for a flow node will now correctly display ErrorNode in its stead.

## 0.6.21
- IMPROVEMENT: Logic Graph - C# properties can now be bound.
- IMPROVEMENT: Recoil - Added World.SleepWakeUp method for waking up rigidbodies.
- CHANGE: CoreSample now requires Unity 2020.3.37f1.
- Fix: Logger now correctly uses NBG_LOGGER_LEVEL define levels.

## 0.6.20
- FEATURE: Automation - Added `--requireInteractiveShell` global parameter to verify that automation is running in an interactive desktop shell.
- IMPROVEMENT: Networking - MasterStreamList can now return the last full frame.
- IMPROVEMENT: Logic Graph - Better script inspector view.
- FIX: Networking - Fixed SteamTansportPeer callbacks not being called when closing a connection.
- FIX: VHACD updated from upstream.
- Fix: Logger now correctly inherit NBG_LOGGER_LEVEL defines.

## 0.6.19
- CHANGE: Recoil - Angular velocity limit is now per Rigidbody and synched from PhysX.
- IMPROVEMENT: Plugs&Holes - Draw arrow gizmos and improve their tooltips.
- FIX: Logic Graph - Fix issue with multi-parameter events, manifesting as a "001a: callvirt" exception.
- FIX: Logic Graph - In case a code binding is missing, show a non-serializable error node. Data is preserved.
- FIX: Automation - Avoid crash in Steam active process detection.
- FIX: Recoil - Allow inertia tensor calculation for smaller masses.

## 0.6.18
- FIX: Logic Graph - (Bug CTT-149) Unexpected events will not trigger anymore on types which have multiple events defined.
- FIX: Ropes - Don't recalculate rope length in play mode when moving editor handles.

## 0.6.17
- CHANGE: Remove NBG.Core.Streams assembly. IStream code is now in NBG.Core.
- IMPROVEMENT: Logic Graph - Node creation menu foldout state is now saved for the duration of the session.
- IMPROVEMENT: Logic Graph - Node source script (MonoBehaviour) can be opened from node inspector.
- IMPROVEMENT: GameSystemDefaultWorld now supports the standard MonoBehaviour callback approach as well as the low-level PlayerLoop.
- FIX: Logic Graph - Graph name changes when target graph changes.
- FIX: Logic Graph - Undo not working with input fields fixed.
- FIX: Logic Graph - Fixed graph clearing its ui content on entering playmode.
- FIX: Logic Graph - Bindings now work in safe assemblies.

## 0.6.16
- FIX: GameSystemWorldDefault destruction does not break tests anymore.
- FIX: ObjectIdDatabase network functionality restored.
- FIX: NetEventBus works again (regression in 0.6.13).

## 0.6.15
- FIX: Fix ropes throwing errors if shrunk to a single segment with no attached objects.
- FIX: Fix ropes throwing memory leak errors after leaving play mode in the editor.
- FIX: Fix LogicGraph.EditorUI test setup.

## 0.6.14
- IMPROVEMENT: Logic Graph - Improved Vector and Quaternion field UX.
- IMPROVEMENT: Logic Graph - Added UI tests.
- IMPROVEMENT: Logic Graph - Added more standard nodes
- IMPROVEMENT: Spring & damp overrides for joints.
- FIX: Logic Graph - Better support for Light Editor theme.
- FIX: Logic Graph - Node creation menu supports multiple GameObjects with the same name.
- FIX: Fix ropes throwing errors/crashing when a rope that changes length is grabbed.
- FIX: Fix ropes yUV scaling incorrectly.
- FIX: Fix ropes creating leftover gameobjects when doing undo/redo.
- FIX: INetTransportClient Interface missing members added.

## 0.6.13
- IMPROVEMENT: INetBehaviour now supports delta compression.
- FIX : Conveyor concave radial rotations that cause human fly and attachments move wrong.

## 0.6.12
- CHANGE: Retired GameSystemLurker.
- CHANGE: EventBus is now created manually (so that it preceeds game systems).
- IMPROVEMENT: Recoil - Added Summary to SetBodyPlacementImmediate, so that it's obvious what coordinate system is used.
- IMPROVEMENT: Logic Graph - Added standard BoolToFloat and BoolToInt nodes.
- FIX: Recoil - RecoilBody component now properly syncs PhysX cache and Recoil body position cache when Rigidbody is enabled.
- FIX: Recoil - Fixed method name typo.
- FIX: Logic Graph - Calling convention fixed. Node inputs are now always pushed onto stack in reverse order.
- FIX: Logic Graph - GroupNode serialization fixed.
- FIX: Logic Graph - GroupNode node removal fixed.
- FIX: CoreSample play mode tests now correctly destroy the bootloader.

## 0.6.11
- IMPROVEMENT: Logic Graph - Added support for extension methods as NodeAPI.
- IMPROVEMENT: Validation Tests user preferences added: can now optionally clear the console window before running manual commands.
- FIX: Logic Graph - Blackboard UnityEngine.Object field works again.
- FIX: Logic Graph - Blackboard Quaternion field now works.

## 0.6.10
- FEATURE: Logic Graph - Added NodeParamVariants attribute. It allows users to specify a fixed list of allowed parameter values.
- FEATURE: Logic Graph - Added global configuration to toggle execution of FixedUpdate and Update event nodes.
- FIX: Logic Graph - Function node will now show a type-safe object selector for UnityEngine.Object sub-types based on the binding.
- FIX: Logic Graph - Bindings will be generated for all assembles that reference NBG.LogicGraph.Foundation instead of NBG.LogicGraph.
- FIX: Logic Graph - Bindings declared for parent types can now be used in child types.

## 0.6.9
- CHANGE: Ropes no longer sleep via Recoil to avoid small judders on hanging ropes.
- CHANGE: Removed INetStartable.
- CHANGE: Recoil BodyList replaced with an INetBehaviour based RigidbodyList.
- IMPROVEMENT: Null rope segments no longer prevent the clearing and the rebuilding of a rope.
- FIX: Rope build profiles are now correctly serialized.
- FIX: Fix multiple ropes not applying their forces when connected to the same object.
- FIX: Fix ropes juddering when connected to the same object.

## 0.6.8
- FEATURE: Added [IncludeInGameSystemWorldDefault] which will allow non-public GameSystem to be registered automatically with GameSystemWorldDefault.
- FEATURE: Added GameSystemLurker which acts like a World-wide singleton and is not registered for update calls.
- CHANGE: INetStream renamed to IStream and moved to NBG.Core.Streams.
- CHANGE: EventBus moved to NBG.Core.Events.
- CHANGE: EventBus event serialization is now based on IStream and is not network specific.
- CHANGE: Player manager moved to Net Player Management package.
- CHANGE: Added Net.Foundation package which contains the required APIs to build networked gameplay systems, without including the actual backend.
- CHANGE: NewPeerIsReadyEvent is now in Net.Foundation.
- CHANGE: Added LogicGraph.Foundation package which contains the required APIs to expose gameplay systems to the Logic Graph, without including the actual backend.
- CHANGE: Quantization utilities relocated to NBG.Core.
- IMPROVEMENT: NBG.Core.AssemblyUtilities now can optionally include non-exported types.

## 0.6.7
- CHANGE: NetReadState readers and NetCollectFramesSystem writers now have to be registered manually.
- CHANGE: Recoil specific network systems moved to Recoil.Net package.
- IMPROVEMENT: A standard AfterPhysicsSystemGroup GamesystemGroup added to NBG.Core to provide unified hierarchy for non-physics systems. Recoil PhysicsSystemGroup now schedules before AfterPhysicsSystemGroup.
- FIX: Some conveyors configurations couldn't be generated because the system wasn't ready to manage certain small angles.

## 0.6.6
- CHANGE: EventBus and NetEventBus split up.
- IMPROVEMENT: NewPeerIsReadyEvent added.
- IMPROVEMENT: INetTransportClient added.
- FIX: Fix rope handle calculation not handling null handles.

## 0.6.5
- FEATURE: New Rope tech with standard rope profiles.

## 0.6.4
- IMPROVEMENT: Noodle now allows grabbing grip target when slightly touching the collider.
- CHANGE: Made noodle player character mesh renderers belong to Player Layer.
- FIX: Logic Graph - Unbinding (removing) an Event Node will now not wipe the event handler for that binding, unless it is the last Event Node using that binding.

## 0.6.3
- CHANGE: Removed NetRecoilBody.
- CHANGE: INetStartable does not use LEGACYNetScope anymore.
- IMPROVEMENT: Logic Graph - UI tweaks.
- FIX: Automation tests included in IL2CPP builds.
- FIX: Automation will report metadata for succeeded/ignored tests too.
- FIX: Handle prefab load exceptions in validation tests.

## 0.6.2
- CHANGE: Logic Graph - CallCustomEventNode and HandleCustomOutputNode backing serialization has changed.
- IMPROVEMENT: Logic Graph - Node searcher window now has keyboard controls.
- FIX: Logic Graph - EventNodes using the same binding function will not trigger multiple times.

## 0.6.1
- FIX: Fix for empty LogicGraphs sometimes throwing errors.

## 0.6.0
- FEATURE: LogicGraph BETA.
- FEATURE: Steam transport library.

## 0.5.8
 - IMPROVEMENT: More UI Toolkit utilities.
 - FIX: IManagedBehaviour booter won't call LevelLoaded twice for some objects when used with timeouts.
 - FIX: Conveyor invalid rotation fix (part 2).

## 0.5.7
- CHANGE: Plugs - changed naming of a few plug/hole types, fixed setting snap joint spring.
- IMPROVEMENT: More warnings for NetStreams when going over limits.
- FIX: Conveyor invalid rotation fix.
- FIX: Fixed issues around the Event Bus.

## 0.5.6
- IMPROVEMENT: Expanded BodyProximityList public API with Count and Clear.
- IMPROVEMENT: Added extensions for VisualElement.

## 0.5.5
- IMPROVEMENT: Automation framework error and exception proxy now has scoping functionality.
- FIX: EventBus system registration fixed.
- FIX: CoreSample build system error handling fixed.
- FIX: Plugs demo scene fixed.

## 0.5.4
- FEATURE: Event Bus with network functionality.
- CHANGE: Added enabled field to IOnFixedUpdate interface. Monobehaviour derivatives return activeAndEnabled value, others just return true.

## 0.5.3
- FEATURE: Recoil sleep assistance can now be disabled per body.
- IMPROVEMENT: Recoil global sleep settings can now be modified.
- FIX: Gravity system only allows Recoil sleep assistance to bodies that use the default PhysX gravity.

## 0.5.2
- IMPROVEMENT: Noodle animations.
- FIX: UndoSystem not recording dummy object to Unity undo stack fixed.

## 0.5.1
- FIX: Recoil will keep applying small velocities below kinetic energy threshold to objects for 50 updates. Fixes custom gravity.

## 0.5.0
- FEATURE: Noodle animations.

## 0.4.17
- FIX: NBG.Audio components will not throw errors when Unity built-in audio is disabled.

## 0.4.16
- CHANGE: Remove rope overlap parameter. All ropes now have an enforced overlap of 2 * radius. Ropes need to be rebuilt in order to benefit from this. Unrebuilt ropes will still function normally.
- IMPROVEMENT: Better stability for extending/retracting ropes.

## 0.4.15
- FEATURE: Noodle now let's you modify the grabbing behaviour for special cases by implementing ICustomGrabHandler on Monobehaviours.
- IMPROVEMENT: Recoil will now use the same equation as PhysX to determine sleeping body energy, with an extra 10% grace.
- IMPROVEMENT: RecoilBody component inspector now shows body energy from Recoil and from PhysX.
- FIX: Don't reading empty streams (i.e. when no Rigidbodies or INetBehaviours are present).

## 0.4.13 hotfix 1
- CHANGE: Split rope compliance into elastic and bend. Bring back the default bend compliance to 0.01.

## 0.4.14
- CHANGE: Rope length system reworked. Easier use and configuration. Ropes now shrink and extend from the rope start instead of the rope end. AttachToRopeEnd component is deprecated and no longer needed. This upgrade requires all ropes to be rebuilt. There is a validator test to automate this.
- CHANGE: Rope segments no longer use inv mass of attached objects in simulation. This helps the ropes maintain their shape better.
- CHANGE: Slightly rework how rope renderer UVs are calculated, this will allow rope UVs to stretch a little bit if the rope is streched.
- IMPROVEMENT: Various rope building robustness improvements - more meaningful messages when rope building fails, added an inspector warning when a rope is out of date.
- IMPROVEMENT: Ignore collision between an attached object and the 2nd segment of a rope, since that segment can also overlap with the attached object, if the first segment is small enough.
- FIX: Final rope segment shrinks as the rope length multiplier approaches 0 (previously it would not shrink).

## 0.4.13
- FEATURE: Added support for INetBehaviour.
- CHANGE: Call ValidationTests.SaveChangesInContext() to save changes to GameObjects in scenes or prefabs when using ValidationTests API such as GetAllPrefabsOrAllRootsFromAllScenes.

## 0.4.12
- FEATURE: Added NetEventsSystem. This provides a one-off way of syncing data over network.
- IMPROVEMENT: Validation tests now open prefabs for editing in an isolated scene.
- CHANGE: IManagedBehaviour.OnAwake is now OnLevelLoaded. OnStart is now OnAfterLevelLoaded.

## 0.4.11
- FEATURE: NetApply and NetCollect System created. These sync recoil bodies over network.
- IMPROVEMENT: Client and Server Sample updated as basic test-bed for networking
- IMPROVEMENT: Net.Core package stripped of code not required for Systems
- IMPROVEMENT: Net.PlayerManagement use NetApply and NetCollect systems
- IMPROVEMENT: Recoil support for querying kinematic state (edited)
- CHANGE: BodyList Singleton added. Indexes RigidBodies for network systems
- CHANGE: Unity Hub version 3.1.1 or later is now required for automation.

## 0.4.10
* CHANGE: CoreSample now on Unity 2020.3.30f1.
* CHANGE: CoreSample now on Switch SDK 13.3.5.
* FIX: Fixed Recoil not applying terminal velocity limits when the object did not have angular velocity.
* FIX: Fixed SurfaceTypes not beeing initialized in Editor.

## 0.4.9
* FIX: Restore the execution order of FixedUpdateSystemGroup to be after ScriptRunBehaviourFixedUpdate.
* FIX: Noodle camera smoothing fixed.

## 0.4.8
* IMPROVEMENT: Added AlwaysCompleteWorldAfterUpdate attribute for game systems.
* IMPROVEMENT: Physics systems are now grouped better via PhysicsBeforeSolve, PhysicsSolve and PhysicsAfterSolve groups.
* CHANGE: Noodle skin interpolation is now a GameSystem.
* CHANGE: CoreSample is now based on Unity 2020.3.30f1.
* FIX: AlwaysSynchronizeWorld game systems attribute now works correctly.

## 0.4.7
* FEATURE: Added SceneField for convenient Unity Scene references. 
* IMPROVEMENT: Expose rope segment collision detection mode.
* IMPROVEMENT: Recoil World now exposes ResyncPhysXBody() for when Rigidbody changes need to be resynched.
* CHANGE: Noodle now exposes GetImpulseAtPos().
* FIX: Bring back missing BeforeRopeSolve event for rope segments.

## 0.4.6
* IMPROVEMENT: Exposed Plugs and Sockets AlignTransform which (if needed) can be used to override attachment anchor position and rotation.
* IMPROVEMENT: Automation will now delete save games on the Switch kit before running any tests.
* FIX: Fixed the execution order of Gravity system.
* OTHER: Burst updated to 1.6.4.

## 0.4.5
* IMPROVEMENT: New GameSystem framework, compatible with and based on Unity.Entities package.
* IMPROVEMENT: Bootloader for the sample project. Has play mode editor hooks.

## 0.4.4
* FEATURE: Moved Plugs and Sockets out of Noodle into a separate package.
* IMPROVEMENT: Plugs system documentation added.
* IMPROVEMENT: Validation tests, when ran via the Unity Test Runner, will now spawn an extra "Load and OnValidate" test in order to collect OnValidate issues.
* CHANGE: ConnectToRopeEnd component only moves the whole rope on the first frame instead of moving it every frame when toggled on.
* CHANGE: Use the rope compliance value for bend constraints instead of a hardcoded value of 0.01
* FIX: Rope length updated on first frame instead of waiting for a fixed update.
* FIX: Fix degree/rad error when solving rope bend constraint - should lead to more stable ropes.
* FIX: Fix rope juddering when increasing the length of a rope.
* FIX: Avoid a crash by preventing Recoil from using the unfinished emote system.

## 0.4.3
* FEATURE: Runtime tests can now record Unity profiler captures.
* IMPROVEMENT: Switch automation is now capable of writing screenshots and profiler captures to an SD card.

## 0.4.2
* FEATURE: EditorProjectSettings API for storing per-project editor-only settings.
* FEATURE: Revolute joints that encapsulate Unity hinge joints.
* IMPROVEMENT: Extended Noodle Plugs&Sockets system to make it more usable and customizable.
* IMPROVEMENT: Nodegraph Window live update is enabled by default.
* CHANGE: Make XPBD rope twist constraint opt-in instead of enabled by default.
* CHANGE: XPBD rope twist constraint now uses angular velocity instead of setting the rotation directly.
* CHANGE: Standard runtime test framework migrated from Milkshake.
* FIX (CT-11): Fix an incorrect exception in ValidationTests window which happens when not all scenes for a level are open.
* FIX (CT-16): Debug UI implementation for TextMeshPro surrounds \<\<VALUE\>\> with noparse tags.
* FIX: PlayerManager mixup between local and global id resolved.
* FIX: Noodle Animator Editor UI toolbar now updates when interacting with timeline.
* FIX: Noodle Animator Editor UI left/right arrow keys now do not scroll the timeline when not needed.

## 0.4.1
* FEATURE: Documentation generation.
* FIX: Audio system now prevents sounds from fading in and fading out at the same time.
* FIX: Fix Noodle character animation references.
* FIX: CameraManager will not call OnLateUpdate on disabled targets anymore.

## 0.4.0
* FEATURE: Undo system.
* FEATURE: Noodle animation system.
* FEATURE: Prismatic joint.
* FEATURE: Defines Manager editor utility.
* IMPROVEMENT: Rope system jobs no longer execute immediatelly.
* IMPROVEMENT: Added UnityLogInspection to automation.json for enabling code inspections in builds.
* IMPROVEMENT: Code inspection default rules will always be used. UnityLogInspectionRulesPath in automation.json is now for providing additional rules.
* IMPROVEMENT: Build system now supports verifying Unity license via --requireProLicense=true.
* FIX: Rope system Recoil bodies update positions on enable.

## 0.3.9
* FEATURE: Added script template importer to Core and script templates to Core and Recoil
* IMPROVEMENT: XPBD solver now also solves rope twist constraints. Mesh UVs utilize this system to lock on the rope segment orientation.
* CHANGE: XPBD Rope twist constraint enabled by default ant set to 15 degrees.
* FIX: Noodle rig now uses teleport immediate
* FIX: Prevent IL2CPP stripping Gravity system code.
* FIX: Delay registeration of loggers with DebugUI until first use to avoid registering from serialization threads.

## 0.3.8
* FEATURE: Add rope twist limits that can be used to prevent infinite rope twisting as well as stabilize UV coordinates.
* IMPROVEMENT: Improve rope renderer performance when regenerating rope mesh.
* CHANGE: DomainReloadHelper now requires NBG_NO_DOMAIN_RELOAD define to be set.
* CHANGE: Add better initial configuration for rope segments connected to other objects.
* FIX: Fix data not saving when building a rope on a prefab instance on scene.
* FIX: Fixed several rope UV wrapping/stretching bugs.

## 0.3.7
* FEATURE: [Logging system](Packages/Core/Docs~/README.md#logger).
* IMPROVEMENT: Can test GravityOverrideArea for gravity status in a specific location.
* IMPROVEMENT: Build automation will now collect Burst debug information.
* FIX: ObjectIdDatabase will now be created in the correct scene.

## 0.3.6
* CHANGE: Removed NetIdentity component. Automatic migration added to validation tests.
* CHANGE: Removed NetBody component. Automatic migration added to validation tests.
* CHANGE: NetBody configuration can be specified using NetRigidbodySettingsOverride when defaults are unsuitable.
* IMPROVEMENT: Recoil will now assert (in editor and development builds) when Rigidbody.isKinematic is changed on registered rigidbodies.
* FIX: Recoil will now not jitter rigidbodies that are meant to be sleeping.
* FIX: Audio system is now able to register game-specific surface types early.

## 0.3.5
* FEATURE: Added Recoil.Gravity system meant to allow custom gravity for specific objects and/or in specific areas/volumes.
* CHANGE: TriggerProximityList itme moved from Noodle package to NBG.Core package
* CHANGE: CollisionAudioEngine now exposes minAudioSourceDistance.
* CHANGE: LevelValidationTest system became a generic ValidationTest system capable of running project-wide tests as well.

## 0.3.4
* FEATURE: ObjectIdDatabase added to store persistent scene-unique GameObject ids.
* FEATURE: XPBD RopeRenderer supports UVs.
* FEATURE: Add XPBD Rope interfaces for reacting to rope creation and solving events.
* IMPROVEMENT: Rigidbodies in scene will be automatically registered with Recoil via transient RecoilBody components.
* IMPROVEMENT: XPBD RopeRenderer performance improved, no longer recalculates normals every frame.
* IMPROVEMENT: XPBD Rope no longer does spherecasts for collisions if no static collision layers are set.
* CHANGE: Removed PhysicsBehaviour component. Automatic migration added to validation tests.
* CHANGE: CenterOfMassOverride became a generic RigidbodySettingsOverride. Automatic migration added to validation tests.
* CHANGE: XPBD RopeSegment is now sealed and can not be subclassed.

## 0.3.3
* IMPROVEMENT: Various rope functionality improvements.
* FIX: Fixed grab getting stuck in concave collisions.
* OTHER: prepare.bat script added to repository.

## 0.3.2
* IMPROVEMENT: Major Noodle carryables upgrade.
* IMPROVEMENT: Various rope functionality improvements.
* IMPROVEMENT: Recoil now can set body placement immediately.
* FIX: Noodle IK pull damper calculation fixed.
* FIX: Noodle ReleaseGrab aim parameter removed.

## 0.3.1
Invalid release

## 0.3.0
* FEATURE: Network stack added (work in progress).
* FEATURE: Core sample project now behaves like a standalone application with runtime scene selection.
* IMPROVEMENT: Managed coroutines can now be spawned globally.
* IMPROVEMENT: Recoil IOnPhysicsAfterSolve added.
* FIX: DataMiner data sources are not missing on IL2CPP builds anymore.
* FIX: Audio transitions now correctly handle lowpass.
 
