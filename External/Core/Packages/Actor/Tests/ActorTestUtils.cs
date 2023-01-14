using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBG.Actor.Tests
{
    /// <summary>
    /// Test utilities for Actor Tests
    /// </summary>
    internal static class ActorTestUtils
    {
        internal static ActorSystem.IActor GetAnyActor(List<GameObject> rootActorHolders)
        {
            ActorSystem.IActor targetActor = null;
            for (int i = 0; i < rootActorHolders.Count; i++)
            {
                targetActor = rootActorHolders[i].GetComponentInChildren<ActorSystem.IActor>(true);
                if (targetActor != null)
                    break;
            }

            return targetActor;
        }

        private static void ConstructMinimumActorOnGO(GameObject targetGameObject, HashSet<ActorSystem.IActor> outNewActorDeposit,
            Action<ActorSystem.IActor> OnAfterSpawn, Action<ActorSystem.IActor> OnAfterDespawn)
        {
            Rigidbody rb = targetGameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            ActorTestComponent actorTestComponent = targetGameObject.AddComponent<ActorTestComponent>();
            if (OnAfterSpawn != null) actorTestComponent.OnAfterSpawn += OnAfterSpawn;
            if (OnAfterDespawn != null) actorTestComponent.OnAfterDespawn += OnAfterDespawn;

            outNewActorDeposit?.Add(actorTestComponent);
        }

        private static void CreateChildActor(GameObject parent, int depthRemaining, int width, float actorChanceOnItem, ref int totalObjectsCreated, ref int totalActorsCreated,
            HashSet<ActorSystem.IActor> outNewActorDeposit, Action<ActorSystem.IActor> OnAfterSpawn, Action<ActorSystem.IActor> OnAfterDespawn)
        {
            for (int i = 0; i < width; i++)
            {
                GameObject newGo = new GameObject($"D: {depthRemaining} W: {i}");
                newGo.transform.parent = parent.transform;
                totalObjectsCreated++;
                if (UnityEngine.Random.Range(0f, 1f) <= actorChanceOnItem)
                {
                    ConstructMinimumActorOnGO(newGo, outNewActorDeposit, OnAfterSpawn, OnAfterDespawn);
                    totalActorsCreated++;
                }
                if (depthRemaining > 0)
                    CreateChildActor(newGo, depthRemaining - 1, width, actorChanceOnItem, ref totalObjectsCreated, ref totalActorsCreated, outNewActorDeposit,
                        OnAfterSpawn, OnAfterDespawn);
            }
        }

        internal static void CreateDummyActorNetwork(List<GameObject> rootGameObjects, int firstLevelWidth, int networkDepth, int childWidth, float actorChanceOnItem,
            out int totalObjectsCreated, out int totalActorsCreated, HashSet<ActorSystem.IActor> outNewActorDeposit = null,
            Action<ActorSystem.IActor> OnAfterSpawn = null, Action<ActorSystem.IActor> OnAfterDespawn = null)
        {
            totalObjectsCreated = 0;
            totalActorsCreated = 0;

            foreach (GameObject rootGameObject in rootGameObjects)
            {
                for (int i = 0; i < firstLevelWidth; i++)
                {
                    GameObject firstLevel = new GameObject($"D: {networkDepth} W: {i}");
                    totalObjectsCreated++;
                    firstLevel.transform.parent = rootGameObject.transform;
                    ConstructMinimumActorOnGO(firstLevel, outNewActorDeposit, OnAfterSpawn, OnAfterDespawn);
                    totalActorsCreated++;

                    if (networkDepth > 0)
                        CreateChildActor(firstLevel, networkDepth - 1, childWidth, actorChanceOnItem, ref totalObjectsCreated, ref totalActorsCreated, outNewActorDeposit,
                            OnAfterSpawn, OnAfterDespawn);
                }
            }

            for (int i = 0; i < rootGameObjects.Count; i++)
            {
                Recoil.RigidbodyRegistration.RegisterHierarchy(rootGameObjects[i]);
            }
        }
    }
}
