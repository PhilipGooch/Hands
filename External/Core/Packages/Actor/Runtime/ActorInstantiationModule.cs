using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Logger = NBG.Core.Logger;

namespace NBG.Actor
{
    /// <summary>
    /// A module responsible for handling new object instantiation and existing object pooling.
    /// </summary>
    public class ActorInstantiationModule
    {
        /// <summary>
        /// Data class. Used only in instantiation module and we need nullability.
        /// So we can just make this a class instad of using a boxed struct everywhere.
        /// </summary>
        private class ActorPool
        {
            public readonly int minItemsPerFrame;
            public readonly List<int> available;
            public readonly List<int> alive;

            public ActorPool (int minItemsPerFrame, List<int> available, List<int> alive)
            {
                this.minItemsPerFrame = minItemsPerFrame;
                this.available = available;
                this.alive = alive;
            }
        }

        private readonly List<ActorPool> actorPools = new List<ActorPool>();
        private readonly Dictionary<int, int> actorToPoolMap = new Dictionary<int, int>();

        private readonly List<ActorSystem.IActor> actorCache = new List<ActorSystem.IActor>();

        private readonly ActorSystem actorSystem;

        public delegate void OnInstantiateDelegate(ICollection<ActorSystem.IActor> instantiatedActors);
        public event OnInstantiateDelegate OnBeforeInstanceInitialization;
        public event OnInstantiateDelegate OnAfterInstanceInitialization;

        private static List<Core.IManagedBehaviour> managedBehaviourCache = new List<Core.IManagedBehaviour>();
        private static List<Core.IManagedBehaviour> managedBehaviourCompoundCache = new List<Core.IManagedBehaviour>();

        private readonly Logger logger = new Logger(ActorSystem.kActorLoggerScope);

        internal ActorInstantiationModule(ActorSystem actorSystem)
        {
            this.actorSystem = actorSystem;
            actorSystem.OnReadyForSpawn += OnReadyForSpawn;
            actorSystem.OnAfterActorUnregister += OnActorUnregister;
        }

        private void OnReadyForSpawn(int actorID, ActorSystem.IActor actor)
        {
            if (!actorToPoolMap.ContainsKey(actorID))
                return;

            int poolID = actorToPoolMap[actorID];
            actorPools[poolID].alive.Remove(actorID);
            actorPools[poolID].available.Add(actorID);
        }

        private void OnActorUnregister(int actorID)
        {
            if (actorToPoolMap.ContainsKey(actorID))
            {
                ActorPool pool = actorPools[actorToPoolMap[actorID]];
                pool.alive.Remove(actorID);
                pool.available.Remove(actorID);
                actorToPoolMap.Remove(actorID);
            }
            
        }

        /// <summary>
        /// Initializes already existing actor items.
        /// </summary>
        public void InitializeActorSet(ICollection<ActorSystem.IActor> actors)
        {
            foreach (ActorSystem.IActor actor in actors)
            {

                // NOTE: At first glance it would make sense to invert this dependency, but that would make Recoil dependant on actor.
                global::Recoil.RigidbodyRegistration.RegisterHierarchy(actor.ActorGameObject);
            }

            OnBeforeInstanceInitialization?.Invoke(actors);

            Utility.ClearTempCollectionsForActorMapping();

            foreach (ActorSystem.IActor actor in actors)
                Utility.CreateActorEdges(actor, actors);

            while (Utility.UnprocessedLeafActorsCount > 0)
                Utility.InitializeActors_LeafToRoot(actorSystem);

            Utility.ClearTempCollectionsForActorMapping();

            // Actor depends on core, so we have this instead of ManagedBehaviour registering to OnAfterInstanceInitialization event.
            managedBehaviourCompoundCache.Clear();
            managedBehaviourCache.Clear();
            foreach (ActorSystem.IActor actor in actors)
            {
                actorSystem.GetActorComponents(actor, managedBehaviourCache);
                managedBehaviourCompoundCache.AddRange(managedBehaviourCache);
            }
                
            for (int i = 0; i < managedBehaviourCompoundCache.Count; i++)
                managedBehaviourCompoundCache[i].OnLevelLoaded();

            for (int i = 0; i < managedBehaviourCompoundCache.Count; i++)
                managedBehaviourCompoundCache[i].OnAfterLevelLoaded();

            OnAfterInstanceInitialization?.Invoke(actors);
        }

        /// <summary>
        /// Instantiates and initializes items based on parent.
        /// </summary>
        public void CreateActorSetFromPrototype<T>(Transform collectionRoot, ActorSystem.IActor prototype, int newItemCount, T result) where T : ICollection<ActorSystem.IActor>
        {
            result.Clear();
            actorCache.Clear();

            for (int i = 0; i < newItemCount; i++)
            {
                GameObject newCreationRoot = Object.Instantiate(prototype.ActorGameObject, collectionRoot);
                newCreationRoot.SetActive(false);
                newCreationRoot.GetComponentsInChildren<ActorSystem.IActor>(true, actorCache);
                for (int j = 0; j < actorCache.Count; j++)
                    result.Add(actorCache[j]);
                actorCache.Clear();
            }

            InitializeActorSet(result);
        }

        /// <summary>
        /// Creates a pool from an already existing set of actors.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="members">Members to include in the pool</param>
        /// <param name="memberSpawnPlacement">Where members are supposed to spawn</param>
        /// <param name="relativeRespawnBodyID">A body in relation to which member spawn point moves</param>
        /// <param name="minItemsAvailablePerFrame">Because of despawning and spawning delay, this is the amount
        /// of members the pool will always keep ready (that means alive member cap is members.Count - minItemsAvailablePerFrame</param>
        /// <returns>Unique pool id</returns>
        public int CreatePool<T>(ICollection<T> members, RigidTransform memberSpawnPlacement,
            int relativeRespawnBodyID = global::Recoil.World.environmentId, int minItemsAvailablePerFrame=0) where T : ActorSystem.IActor
        {
            foreach (ActorSystem.IActor member in members)
                ActorSystem.Main.SetActorSpawnPlacement(member, relativeRespawnBodyID, memberSpawnPlacement);

            return CreatePool(members, minItemsAvailablePerFrame);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="members">Members to include in the pool</param>
        /// <param name="minItemsAvailablePerFrame">Because of despawning and spawning delay, this is the amount
        /// of members the pool will always keep ready (that means alive member cap is members.Count - minItemsAvailablePerFrame
        /// <returns>Unique pool id</returns>
        public int CreatePool<T>(ICollection<T> members, int minItemsAvailablePerFrame = 0) where T : ActorSystem.IActor
        {
            Debug.Assert(members.Count > minItemsAvailablePerFrame, $"Max alive members count for pool: {members.Count - minItemsAvailablePerFrame}");
            int poolID = actorPools.IndexOf(null);

            ActorPool newPool = new ActorPool(minItemsAvailablePerFrame, new List<int>(), new List<int>());
            if (poolID == -1)
            {
                actorPools.Add(newPool);
                poolID = actorPools.Count - 1;
            }
            else
            {
                actorPools[poolID] = newPool;
            }

            foreach (ActorSystem.IActor member in members)
            {
                int actorID = actorSystem.actorMap[member];
                if (actorToPoolMap.ContainsKey(actorID))
                {
                    logger.LogError($"Actor {member.ActorGameObject.name} already belongs to {actorToPoolMap[actorID]} pool, but you're trying to add it to pool {poolID}." +
                        $" Adding the same actor to multiple pools is not allowed.");
                    continue;
                }
                actorToPoolMap[actorID] = poolID;
                if (actorSystem.IsActorActive(member))
                    actorPools[poolID].alive.Add(actorID);
                else
                    actorPools[poolID].available.Add(actorID);
            }

            return poolID;
        }

        /// <summary>
        /// Used to dispose of a pool. Disposing of a pool does not destoy the pool members.
        /// those are returned and are to be dealt with manually.
        /// </summary>
        /// <param name="poolID">Unique pool id. Received on pool creation</param>
        /// <param name="outAllPoolMembers">All former members of said pool as actorID</param>
        public void DisposePool(int poolID, List<int> outAllPoolMembers=null)
        {
            outAllPoolMembers?.Clear();

            for (int i = 0; i < actorPools[poolID].alive.Count; i++)
            {
                actorToPoolMap.Remove(actorPools[poolID].alive[i]);
                outAllPoolMembers?.Add(actorPools[poolID].alive[i]);
            }
                
            for (int i = 0; i < actorPools[poolID].available.Count; i++)
            {
                actorToPoolMap.Remove(actorPools[poolID].available[i]);
                outAllPoolMembers?.Add(actorPools[poolID].available[i]);
            }

            actorPools[poolID] = null;
        }

        private ActorSystem.IActor GetNextItem(ActorPool pool)
        {
            Debug.Assert((pool.available.Count + pool.alive.Count) > pool.minItemsPerFrame, $"Less items total than expected. Pool members got disposed and something is still trying to use the pool?");

            if (pool.available.Count == 0)
                return null;

            int targetActorID = pool.available[0];
            pool.available.RemoveAt(0);
            pool.alive.Add(targetActorID);

            if (pool.available.Count < pool.minItemsPerFrame)
                actorSystem.Despawn(pool.alive[0]);

            return actorSystem.Actors[targetActorID];
        }

        public bool TrySpawnFromPool(int poolID, out ActorSystem.IActor nextItem)
        {
            nextItem = GetNextItem(actorPools[poolID]);
            if (nextItem == null)
                return false;

            actorSystem.RequestSpawn(nextItem);
            return true;
        }
    }
}
