using NBG.Core.Events;
using NBG.Core.GameSystems;
using Recoil;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Actor
{
    /// <summary>
    /// Main actor system. Takes care of actor indexing, tracking and respawning
    /// Other actor functions (joints, network, pools, etc.) are split inside specific Modules.
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsBeforeSolve))]
    public class ActorSystem : GameSystem
    {
        public interface IActorCallbacks
        {
            void OnAfterSpawn();
            void OnAfterDespawn();
        }

        /// <summary>
        /// Currently only properly works if this interface is implemented by a component.
        /// </summary>
        public interface IActor : IActorCallbacks
        {
            /// <summary>
            /// Main game object of the actor. Should be the GO on which the actor component sits.
            /// </summary>
            GameObject ActorGameObject { get; }

            /// <summary>
            /// Rigidbody that is considered 'main' body of the actor. This body will be placed at the designated respawn positions
            /// and all other member bodies of the actor will be respawned/placed relative to this body.
            /// </summary>
            int PivotBodyID { get; }

            /// <summary>
            /// Used to set default body for relative respawn. Called automatically on actor initialization. 
            /// For actors that respawn not in absolute position, but relative to another rigidbody in the game, this is the recoil id of that body.
            /// </summary>
            int DefaultSpawnRelativeToBodyID { get; }

            /// <summary>
            /// Used to generate default spawn position. Called automatically on actor registration/initialization. It can be context sensitive
            /// thus calling it multiple times during play may yield different results depending on implementation.
            /// </summary>
            RigidTransform DefaultSpawnPoint { get; }
        }

        internal const string kActorLoggerScope = "Actor";

        public struct ChangeActiveStateEvent { public int actorID; public bool activeValue; }
        public struct RespawnEvent { public int actorID; public bool applyTeleport; }

        private static ActorSystem main = null;
        public static ActorSystem Main
        {
            get
            {
                if (main == null)
                {
                    main = GameSystemWorldDefault.Instance.GetExistingSystem<ActorSystem>();
                    Debug.Assert(main != null, $"Game system {nameof(ActorSystem)} not found.");
                }

                return main;
            }
        }
        public static ActorJointModule JointModule { get; private set; } = null;
        public static ActorInstantiationModule InstantiationModule { get; private set; } = null;
        internal static ActorNetModule NetModule { get; private set; } = null;

        /// <summary>
        /// Index in list is the actor ID. Since it doubles as ID, list can have null gaps for destroyed actors
        /// </summary>
        private readonly List<IActor> actors = new List<IActor>(); // NOTE: Native array when optimizing is viable.
        public IReadOnlyList<IActor> Actors => actors;

        internal readonly Dictionary<IActor, int> actorMap = new Dictionary<IActor, int>(); // TODO: Map keys can be null.
        public int GetActorID(IActor actor) => actorMap[actor];
        public bool ActorRegistered(IActor actor) => actorMap.ContainsKey(actor);

        internal readonly Dictionary<int, List<int>> actorIDToActorGameObjectsSorted = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> actorIDToBodiesID = new Dictionary<int, List<int>>();
        private Dictionary<int, List<IActorCallbacks>> actorIDToIActorCallbacks = new Dictionary<int, List<IActorCallbacks>>();

        /// <summary>
        /// This is a mixed list of absolute and relative positions. That is determined by context in IActor
        /// Cooridnate system is transform position/rotation and NOT Center of Mass
        /// </summary>
        private Dictionary<int, RigidTransform> bodyIDToTransformSpawnPosition = new Dictionary<int, RigidTransform>();

        /// <summary>
        /// Collection of pivot bodies and their relative respawn bodies.
        /// For the sake of simplicity we expect this to always have a value. If relative respawn is not used, the value should be World.environmentID.
        /// </summary>
        private Dictionary<int, int> pivotBodyIDToRelativeRespawnBodyID = new Dictionary<int, int>();

        /// <summary>
        /// int - actorID
        /// bool - do we teleport?
        /// </summary>
        private Dictionary<int, bool> delayedActivationsWithOptionalTeleport { get; set; } = new Dictionary<int, bool>();

        /// <summary>
        /// Key - actorID.
        /// Value - timeStamp in between two fixed frames after which, the next fixed frame will have despawn completed
        /// </summary>
        private Dictionary<int, float> despawnsInProgress { get; set; } = new Dictionary<int, float>();

        public event Action<int, IActor> OnActorRegister;
        public event Action<int> OnAfterActorUnregister;

        internal event Action<int, IActor> OnBeforeActorDespawn;

        /// <summary>
        /// After despawn delay expires.
        /// </summary>
        internal event Action<int, IActor> OnReadyForSpawn;

        // TODO: This one is silly, because ActorNetManager depends on main ActorManager, but the exception is lifecycle, where Main Actor Manager dictates the lifecycle of ActorNetManager.
        internal event Action OnDispose;


        private IEventBus eventBus;
        private List<Rigidbody> rigidbodyTempCache = new List<Rigidbody>();

        private readonly List<int> tempCache = new List<int>();

        protected override void OnCreate()
        {
            eventBus = EventBus.Get();

            eventBus.Register<ChangeActiveStateEvent>(HandleSetActorActive);
            eventBus.Register<RespawnEvent>(HandleRespawn);

            // This one is a full on dependency on actor. It's basically utilites for actor to invoke.
            JointModule = new ActorJointModule(this);
            InstantiationModule = new ActorInstantiationModule(this);

            // This one has one annoying dependency that is on net peer callback. 
            NetModule = new ActorNetModule(this); // TODO: The creation of this can maybe be moved out of main actor manager.
        }

        /// <summary>
        /// Send Deactivate event:
        ///     Disable game object, reserves some frames to fully resolve despawn (joint destruction, etc.), plus all associated things that come with disable.
        ///     Same event flow on client and on server.
        /// </summary>
        /// <param name="actorID"></param>
        public void Despawn(int actorID)
        {
            eventBus.Send(new ChangeActiveStateEvent { actorID = actorID, activeValue = false }); // Deactivate and add time buffer
        }
        public void Despawn(IActor actor)
        {
            Despawn(actorMap[actor]);
        }

        /// <summary>
        /// Checks if spawn for this actor is being blocked by ongoing despawn reservation.
        /// If yes schedules teleportation otherwise executes it.
        /// Sends activate event:
        ///     Event execution also checks for being blocked by despawn reservation and either executes immediately or schedules activation.
        ///     Same event flow on client and on server.
        /// </summary>
        /// <param name="actor"></param>
        public void RequestSpawn(IActor actor)
        {
            int actorID = actorMap[actor];

            if (SpawnAllowed(actorID))
                Teleport(actorID, bodyIDToTransformSpawnPosition[actor.PivotBodyID], pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID]);
            else
                delayedActivationsWithOptionalTeleport[actorID] = true;

            eventBus.Send(new ChangeActiveStateEvent { actorID = actorMap[actor], activeValue = true }); // Activate or schedule activation after time buffer
        }

        /// <summary>
        /// Sends Respawn Event:
        ///     Executes deactivation, then schedules teleportation and activation.
        ///     When sending event over network we force applyTeleport to false, because all positions are handled by a different system on client.
        /// </summary>
        /// <param name="actor"></param>
        public void RequestRespawn(IActor actor)
        {
            eventBus.Send(new RespawnEvent { actorID = actorMap[actor], applyTeleport = true });
        }

        public bool IsActorActive(IActor actor) { return actor.ActorGameObject.activeSelf; }

        private void HandleSetActorActive(ChangeActiveStateEvent desiredSpawnState)
        {
            int actorID = desiredSpawnState.actorID;
            IActor actor = actors[actorID];

            if (desiredSpawnState.activeValue)
            {
                if (SpawnAllowed(actorID))
                {
                    actor.ActorGameObject.SetActive(true);
                    for (int i = 0; i < actorIDToIActorCallbacks[actorID].Count; i++)
                        actorIDToIActorCallbacks[actorID][i].OnAfterSpawn();
                }
                else if (!delayedActivationsWithOptionalTeleport.ContainsKey(actorID))
                {
                    delayedActivationsWithOptionalTeleport[actorID] = false;
                }
            }
            else
            {
                OnBeforeActorDespawn?.Invoke(actorID, actor);

                delayedActivationsWithOptionalTeleport.Remove(actorID); // If there was a pending spawn, despawn call should wipe it.

                const int fixedFrameDelay = 1;
                // To avoid rounding errors when comparing we pass (X-0.5) frames instead of X frames. If we had Time.fixedFrameCount, we could rely on exact comparison.
                float fixedTimeDelay = Time.fixedDeltaTime * (fixedFrameDelay - 0.5f);
                despawnsInProgress[actorID] = Time.fixedTime + fixedTimeDelay;

                actor.ActorGameObject.SetActive(false); // Properly cache former value before
                for (int i = 0; i < actorIDToIActorCallbacks[actorID].Count; i++)
                    actorIDToIActorCallbacks[actorID][i].OnAfterDespawn();
            }
        }

        public void SetActorSpawnPlacement(IActor actor, int relativeToBodyID, RigidTransform pivotTransformPlacement)
        {
            pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID] = relativeToBodyID;

            if (relativeToBodyID == global::Recoil.World.environmentId)
            {
                bodyIDToTransformSpawnPosition[actor.PivotBodyID] = pivotTransformPlacement;
            }
            else
            {
                RigidTransform inverseReferencePosition = math.inverse(ManagedWorld.main.GetRigidbody(relativeToBodyID).transform.GetRigidWorldTransform());
                bodyIDToTransformSpawnPosition[actor.PivotBodyID] = math.mul(inverseReferencePosition, pivotTransformPlacement);
            }
        }

        public void ChangeActorSpawnPlacement(IActor actor, RigidTransform pivotTransformPlacement)
        {
            SetActorSpawnPlacement(actor, pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID], pivotTransformPlacement);
        }

        public void ChangeActorSpawnRotation(IActor actor, Quaternion rotation)
        {
            RigidTransform placement = new RigidTransform(rotation, bodyIDToTransformSpawnPosition[actor.PivotBodyID].pos);
            SetActorSpawnPlacement(actor, pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID], placement);
        }

        public void ChangeActorSpawnPosition(IActor actor, Vector3 position)
        {
            RigidTransform placement = new RigidTransform(bodyIDToTransformSpawnPosition[actor.PivotBodyID].rot, position);
            SetActorSpawnPlacement(actor, pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID], placement);
        }

        public void ChangeActorRelativeRespawn(IActor actor, int relativeToBodyID)
        {
            SetActorSpawnPlacement(actor, relativeToBodyID, bodyIDToTransformSpawnPosition[actor.PivotBodyID]);
        }



        /// <summary>
        /// Using transform for respawn caching is deliberate because of position and rotation value validity:
        /// If object is disabled, but was enabled before: transform - valid, rigidbody - invalid, recoilPosition - valid
        /// If object is disabled and was never enabled: transform - valid, rigidbody - invalid, recoilPosition - invalid
        /// If object is enabled: all three - valid
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="actorID"></param>
        private void SaveFullTransformSpawnLocations(IActor actor, int actorID)
        {
            GetActorComponents(actor, rigidbodyTempCache);

            actorIDToBodiesID[actorID] = new List<int>();

            RigidTransform pivotTransformInverse = math.inverse(ManagedWorld.main.GetRigidbody(actor.PivotBodyID).transform.GetRigidWorldTransform());

            for (int i = 0; i < rigidbodyTempCache.Count; i++)
            {
                int bodyID = ManagedWorld.main.FindBody(rigidbodyTempCache[i]);
                actorIDToBodiesID[actorID].Add(bodyID);

                if (bodyID == actor.PivotBodyID)
                {
                    SetActorSpawnPlacement(actor, actor.DefaultSpawnRelativeToBodyID, actor.DefaultSpawnPoint);
                    continue;
                }

                bodyIDToTransformSpawnPosition[bodyID] = math.mul(pivotTransformInverse, rigidbodyTempCache[i].transform.GetRigidWorldTransform());
            }

            rigidbodyTempCache.Clear();
        }

        internal void RegisterActor(IActor actor, HashSet<int> actorGameObjects)
        {
            Debug.Assert(!actorMap.ContainsKey(actor), $"Registering an actor that is already registered: {actor.ActorGameObject.name}");
            Debug.Assert(actor.ActorGameObject.GetComponent<IActor>() == actor, $"Can't find the correct Actor on ActorGameObject.");

            int actorID = actors.IndexOf(null);

            if (actorID == -1)
            {
                actorID = actors.Count;
                actors.Add(actor);
            }
            else
                actors[actorID] = actor;


            actorMap.Add(actor, actorID);

            // Actor initialization goes here.

            actorIDToActorGameObjectsSorted[actorID] = actorGameObjects.OrderBy(value => value).ToList();

            SaveFullTransformSpawnLocations(actor, actorID);

            actorIDToIActorCallbacks[actorID] = new List<IActorCallbacks>();
            GetActorComponents(actor, actorIDToIActorCallbacks[actorID]);

            OnActorRegister?.Invoke(actorID, actor);
        }

        public void UnregisterActor(IActor actor)
        {
            Debug.Assert(actorMap.ContainsKey(actor), $"Unregistering an actor that is not registered: {actor.ActorGameObject.name}");

            int actorID = actorMap[actor];

            actorIDToIActorCallbacks.Remove(actorID);

            // Spawn location data cleanup
            for (int i = 0; i < actorIDToBodiesID[actorID].Count; i++)
                bodyIDToTransformSpawnPosition.Remove(actorIDToBodiesID[actorID][i]);
            actorIDToBodiesID.Remove(actorID);
            pivotBodyIDToRelativeRespawnBodyID.Remove(actor.PivotBodyID);

            // Spawning process data cleanup
            delayedActivationsWithOptionalTeleport.Remove(actorID);
            despawnsInProgress.Remove(actorID);

            actorMap.Remove(actor);
            actors[actorID] = null;

            OnAfterActorUnregister?.Invoke(actorID);
        }

        public bool ComponentPartOfActor<T>(IActor actor, T component) where T : Component
        {
            return ComponentPartOfActor(actorMap[actor], component);
        }

        public bool ComponentPartOfActor<T>(int actorID, T component) where T : Component
        {
            return actorIDToActorGameObjectsSorted[actorID].BinarySearch(component.gameObject.GetInstanceID()) >= 0;
        }

        /// <summary>
        /// A general approach for getting actor components. Some of the default ones
        /// should probably be processed on intialization.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actor"></param>
        /// <param name="results"></param>
        public void GetActorComponents<T>(IActor actor, List<T> results)
        {
            int appendStart = results.Count;
            actor.ActorGameObject.GetComponentsInChildren(true, results);

            int actorID = actorMap[actor];

            for (int i = results.Count - 1; i >= appendStart; i--)
            {
                if (!ComponentPartOfActor(actorID, results[i] as Component))
                    results.RemoveAt(i);
            }
        }

        public IReadOnlyList<int> GetActorBodies(IActor actor)
        {
            int actorId = actorMap[actor];
            return actorIDToBodiesID[actorId].AsReadOnly();
        }

        private void HandleRespawn(RespawnEvent respawnEventData)
        {
            int actorID = respawnEventData.actorID;

            if (IsActorActive(actors[actorID]))
            {
                HandleSetActorActive(new ChangeActiveStateEvent { actorID = actorID, activeValue = false });

                // Guaranteed delay after despawn, no need to do extra checks on whether we can execute spawn immediately, only on whether it's already scheduled.
                delayedActivationsWithOptionalTeleport[actorID] = true;
            }
            else
            {
                IActor actor = actors[actorID];

                if (SpawnAllowed(actorID))
                {
                    Teleport(actorID, bodyIDToTransformSpawnPosition[actor.PivotBodyID], pivotBodyIDToRelativeRespawnBodyID[actor.PivotBodyID]);
                    HandleSetActorActive(new ChangeActiveStateEvent { actorID = actorMap[actor], activeValue = true });
                }
                else
                {
                    delayedActivationsWithOptionalTeleport[actorID] = true;
                }
            }
        }

        private bool SpawnAllowed(int actorID)
        {
            return !despawnsInProgress.ContainsKey(actorID);
        }

        private void Teleport(int actorID, RigidTransform spawnPosition, int relativeBody = global::Recoil.World.environmentId)
        {
            IActor actor = actors[actorID];

            RigidTransform pivotBodyPlacement = spawnPosition;
            if (relativeBody != global::Recoil.World.environmentId)
                pivotBodyPlacement = math.mul(ManagedWorld.main.GetRigidbody(relativeBody).GetRigidTransform(), spawnPosition);

            for (int i = 0; i < actorIDToBodiesID[actorID].Count; i++)
            {
                int bodyID = actorIDToBodiesID[actorID][i];

                RigidTransform rigidbodyPlacement = bodyID == actor.PivotBodyID ? pivotBodyPlacement : math.mul(pivotBodyPlacement, bodyIDToTransformSpawnPosition[bodyID]);
                // NOTE: We have rigidbody position saved, but we apply it to transform, because Unity applies transform to rigidbody upon setting GameObject active
                Rigidbody rigidbody = ManagedWorld.main.GetRigidbody(bodyID);

                rigidbody.transform.SetPositionAndRotation(rigidbodyPlacement.pos, rigidbodyPlacement.rot);

                ManagedWorld.main.SetVelocity(bodyID, MotionVector.zero);
            }
        }

        private static void RemoveItemsFromDictionary<T>(List<int> itemsToRemove, Dictionary<int, T> itemsToRemoveFrom)
        {
            if (itemsToRemove.Count == itemsToRemoveFrom.Count)
                itemsToRemoveFrom.Clear();
            else
                for (int i = 0; i < itemsToRemove.Count; i++)
                    itemsToRemoveFrom.Remove(itemsToRemove[i]);
        }

        protected override void OnUpdate()
        {
            float fixedTimeStamp = Time.fixedTime;
            foreach (KeyValuePair<int, float> runningDespawns in despawnsInProgress)
            {
                if (fixedTimeStamp > runningDespawns.Value)
                    tempCache.Add(runningDespawns.Key);
            }

            RemoveItemsFromDictionary(tempCache, despawnsInProgress);

            for (int i = 0; i < tempCache.Count; i++)
                OnReadyForSpawn?.Invoke(tempCache[i], actors[tempCache[i]]);

            tempCache.Clear();

            foreach (KeyValuePair<int, bool> delayedActivationWithOptionalTeleport in delayedActivationsWithOptionalTeleport)
            {
                int actorID = delayedActivationWithOptionalTeleport.Key;
                if (SpawnAllowed(actorID))
                {
                    if (delayedActivationWithOptionalTeleport.Value)
                    {
                        int pivotBodyID = actors[actorID].PivotBodyID;
                        Teleport(actorID, bodyIDToTransformSpawnPosition[pivotBodyID], pivotBodyIDToRelativeRespawnBodyID[pivotBodyID]);
                    }


                    HandleSetActorActive(new ChangeActiveStateEvent() { actorID = actorID, activeValue = true });

                    tempCache.Add(delayedActivationWithOptionalTeleport.Key);
                }
            }

            RemoveItemsFromDictionary(tempCache, delayedActivationsWithOptionalTeleport);
            tempCache.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            eventBus?.Unregister<ChangeActiveStateEvent>(HandleSetActorActive);
            eventBus?.Unregister<RespawnEvent>(HandleRespawn);

            main = null;

            JointModule = null;
            InstantiationModule = null;
            NetModule = null;

            OnDispose?.Invoke();
        }
    }
}
