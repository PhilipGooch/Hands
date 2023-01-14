using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine;
using System.Linq;
using NBG.Core.GameSystems;
using NBG.Core.Events;

namespace NBG.Actor.Tests
{
    /// <summary>
    /// Unit tests that create a large hierarchy of actors
    /// and then validate whether actors get their game object
    /// scopes assigned accurately.
    /// </summary>
    public static class ActorHierarchyTest
    {
        private static int setupObjectsCreated = 0;
        private static int setupActorsCreated = 0;
        private static List<Scene> loadedScenes = new List<Scene>();
        private static List<GameObject> rootActorHolders = new List<GameObject>();

        [SetUp]
        public static void Setup()
        {
            Recoil.ManagedWorld.Create(16);
            Entities.EntityStore.Create(10, 500);

            EventBus.Create();
            GameSystemWorldDefault.Create();

            List<GameObject> rootGameObjects = new List<GameObject>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                loadedScenes.Add(scene);

                GameObject rootGameObject = new GameObject($"{scene.name} root actor holder");

                SceneManager.MoveGameObjectToScene(rootGameObject, scene);
                rootActorHolders.Add(rootGameObject);
            }

            ActorSystem actorSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ActorSystem>();
            int actorCount = actorSystem.Actors.Where(actor => actor != null).Count();
            Debug.Log($"[Setup] Actors collection length: {actorSystem.Actors.Count}, Live actors in collection: {actorCount}\nActor System id: {actorSystem.GetHashCode()}");

            const int totalFirstLevelActors = 200;
            const int networkDepth = 5;
            const int childWidth = 3;
            const float actorChanceOnitem = 0.05f;

            ActorTestUtils.CreateDummyActorNetwork(rootActorHolders, totalFirstLevelActors / loadedScenes.Count, networkDepth, childWidth, actorChanceOnitem,
                out setupObjectsCreated, out setupActorsCreated);
        }

        [TearDown]
        public static void TearDown()
        {
            BootActors.ClearBooterState();
            for (int i = 0; i < rootActorHolders.Count; i++)
            {
                Object.DestroyImmediate(rootActorHolders[i]);
            }
            rootActorHolders.Clear();
            setupObjectsCreated = 0;
            setupActorsCreated = 0;
            loadedScenes.Clear();

            GameSystemWorldDefault.Destroy();
            EventBus.Destroy();

            Entities.EntityStore.Destroy();
            Recoil.ManagedWorld.Destroy();
        }

        [Test, Order(1)]
        public static void ActorGatheringOnInitInstant()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
            {
                BootActors.RunInits(loadedScenes[i]);
            }

            AssertActorHierarchyTestResults(ActorSystem.Main.Actors, setupActorsCreated, setupObjectsCreated, "Boot");
        }

        [UnityTest, Order(2)]
        public static IEnumerator ActorGatheringOnInitAsync()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
            {
                while (!BootActors.RunInits(loadedScenes[i], 5))
                    yield return null;
            }

            AssertActorHierarchyTestResults(ActorSystem.Main.Actors, setupActorsCreated, setupObjectsCreated, "Boot");
        }

        [UnityTest, Order(3)]
        public static IEnumerator ActorGatheringOnInitInstantAndAddRuntimeActors()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            yield return null; // Here to simulate gameplay happening between initial registration and appending new items.

            AssertActorHierarchyTestResults(ActorSystem.Main.Actors, setupActorsCreated, setupObjectsCreated, "Boot");

            // Randomize the locations where new actors will be instantiated.
            List<GameObject> randomizedActorDeposits = new List<GameObject>();
            List<Transform> transformCache = new List<Transform>();

            const float chanceToRuntimeSpawnAChildActor = 0.2f;

            for (int i = 0; i < rootActorHolders.Count; i++)
            {
                randomizedActorDeposits.Add(rootActorHolders[i]);
                for (int j = 0; j < rootActorHolders[i].transform.childCount; j++)
                {
                    if (Random.Range(0f, 1f) > chanceToRuntimeSpawnAChildActor)
                        continue;

                    rootActorHolders[i].transform.GetChild(j).gameObject.GetComponentsInChildren(true, transformCache);
                    int random = Random.Range(0, transformCache.Count);
                    randomizedActorDeposits.Add(transformCache[random].gameObject);
                    transformCache.Clear();
                }
            }

            HashSet<ActorSystem.IActor> newActorsAdded = new HashSet<ActorSystem.IActor>();

            ActorTestUtils.CreateDummyActorNetwork(randomizedActorDeposits, 1, 3, 7, 0.1f, out int runtimeObjectsCreated, out int runtimeActorsCreated, newActorsAdded);

            Utility.ClearTempCollectionsForActorMapping();

            foreach (ActorSystem.IActor actor in newActorsAdded)
                Utility.CreateActorEdges(actor, newActorsAdded);

            while (Utility.UnprocessedLeafActorsCount > 0)
                Utility.InitializeActors_LeafToRoot(ActorSystem.Main);

            Utility.ClearTempCollectionsForActorMapping();

            int expectedTotalActors = setupActorsCreated + runtimeActorsCreated;
            int expectedTotalObjects = setupObjectsCreated + runtimeObjectsCreated;

            AssertActorHierarchyTestResults(ActorSystem.Main.Actors, expectedTotalActors, expectedTotalObjects, "Runtime");
        }

        [UnityTest, Order(4)]
        public static IEnumerator ActorGatheringOnInitAsyncAndAddRuntimeActors()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
            {
                while (!BootActors.RunInits(loadedScenes[i], 5))
                    yield return null;
            }

            yield return null; // Here to simulate gameplay happening between initial registration and appending new items.

            ActorSystem actorSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ActorSystem>();
            AssertActorHierarchyTestResults(actorSystem.Actors, setupActorsCreated, setupObjectsCreated, "Boot");

            // Randomize the locations where new actors will be instantiated.
            List<GameObject> randomizedActorDeposits = new List<GameObject>();
            List<Transform> transformCache = new List<Transform>();

            for (int i = 0; i < rootActorHolders.Count; i++)
            {
                randomizedActorDeposits.Add(rootActorHolders[i]);
                for (int j = 0; j < rootActorHolders[i].transform.childCount; j++)
                {
                    if (Random.Range(0f, 1f) <= 0.2f)
                        continue;

                    rootActorHolders[i].transform.GetChild(j).gameObject.GetComponentsInChildren(true, transformCache);
                    int random = Random.Range(0, transformCache.Count);
                    randomizedActorDeposits.Add(transformCache[random].gameObject);
                    transformCache.Clear();
                }
            }

            HashSet<ActorSystem.IActor> newActorsAdded = new HashSet<ActorSystem.IActor>();

            ActorTestUtils.CreateDummyActorNetwork(randomizedActorDeposits, 1, 3, 7, 0.1f, out int runtimeObjectsCreated, out int runtimeActorsCreated, newActorsAdded);

            Utility.ClearTempCollectionsForActorMapping();

            foreach (ActorSystem.IActor actor in newActorsAdded)
                Utility.CreateActorEdges(actor, newActorsAdded);

            while (Utility.UnprocessedLeafActorsCount > 0)
                Utility.InitializeActors_LeafToRoot(actorSystem);

            Utility.ClearTempCollectionsForActorMapping();

            int expectedTotalActors = setupActorsCreated + runtimeActorsCreated;
            int expectedTotalObjects = setupObjectsCreated + runtimeObjectsCreated;

            AssertActorHierarchyTestResults(actorSystem.Actors, expectedTotalActors, expectedTotalObjects, "Runtime");
        }

        private static void AssertActorHierarchyTestResults(IReadOnlyList<ActorSystem.IActor> actors, int totalActorsCreated, int totalObjectsCreated, string stepInfo)
        {
            ActorSystem actorSystem = GameSystemWorldDefault.Instance.GetExistingSystem<ActorSystem>();

            int actorCount = actors.Where(actor => actor != null).Count();

            Debug.Log($"[{stepInfo}] Created actors: {totalActorsCreated}. Actors registered to ActorManager: {actorCount}");
            Assert.AreEqual(totalActorsCreated, actorCount, $"[{stepInfo}] Actor count");

            int totalObjectsBelongingToActors = 0;
            foreach (ActorSystem.IActor actor in actors)
            {
                if (actor == null)
                    continue;
                int actorID = actorSystem.actorMap[actor];
                totalObjectsBelongingToActors += actorSystem.actorIDToActorGameObjectsSorted[actorID].Count;
            }

            Debug.Log($"[{stepInfo}] Created objects in actor hierachy: {totalObjectsCreated}. Collected into actor hierachy: {totalObjectsBelongingToActors}");
            Assert.AreEqual(totalObjectsCreated, totalObjectsBelongingToActors, $"[{stepInfo}] Object count");
        }
    }
}
