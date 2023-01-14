using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using NBG.Core.GameSystems;
using UnityEngine.TestTools;
using System.Collections;
using NBG.Core.Events;

namespace NBG.Actor.Tests
{
    /// <summary>
    /// Unit tests that try various permutations of calling spawning and despawning
    /// and check whether the state of these is accurate.
    /// </summary>
    public static class ActorSpawnTest
    {
        private const float kWaitForSecondsInRespawnSequence = 0.5f;

        private static List<Scene> loadedScenes = new List<Scene>();
        private static List<GameObject> rootActorHolders = new List<GameObject>();

        private static List<ActorSystem.IActor> spawnEventExecutions = new List<ActorSystem.IActor>();
        private static void AddSpawnEntry(ActorSystem.IActor actor) { spawnEventExecutions.Add(actor); }
        private static List<ActorSystem.IActor> despawnEventExecutons = new List<ActorSystem.IActor>();
        private static void AddDespawnEntry(ActorSystem.IActor actor) { despawnEventExecutons.Add(actor); }

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
            Debug.Log($"[Setup] Before. Actors collection length: {actorSystem.Actors.Count}, Live actors in collection: {actorCount}\nActor System id: {actorSystem.GetHashCode()}");

            const int totalFirstLevelActors = 1;
            const int networkDepth = 1;
            const int childWidth = 0;
            const float actorChanceOnitem = 0f;

            ActorTestUtils.CreateDummyActorNetwork(rootActorHolders, totalFirstLevelActors / loadedScenes.Count, networkDepth, childWidth, actorChanceOnitem,
                out _, out _, null, AddSpawnEntry, AddDespawnEntry);
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
            loadedScenes.Clear();
            spawnEventExecutions.Clear();
            despawnEventExecutons.Clear();

            GameSystemWorldDefault.Destroy();
            EventBus.Destroy();

            Entities.EntityStore.Destroy();
            Recoil.ManagedWorld.Destroy();
        }

        [UnityTest, Order(1)]
        public static IEnumerator RespawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.RequestRespawn(targetActor);
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));
            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            Assert.IsTrue(ActorSystem.Main.IsActorActive(targetActor));
            Assert.IsTrue(spawnEventExecutions.Contains(targetActor));
        }

        [Test, Order(2)]
        public static void DespawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));
            Assert.IsFalse(spawnEventExecutions.Contains(targetActor));
        }

        [UnityTest, Order(3)]
        public static IEnumerator DespawnImmediatelySpawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));
            Assert.IsFalse(spawnEventExecutions.Contains(targetActor));

            ActorSystem.Main.RequestSpawn(targetActor);
            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            Assert.IsTrue(ActorSystem.Main.IsActorActive(targetActor));
            Assert.IsTrue(spawnEventExecutions.Contains(targetActor));
        }

        [UnityTest, Order(4)]
        public static IEnumerator DespawnWaitSpawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));
            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            ActorSystem.Main.RequestSpawn(targetActor);
            Assert.IsTrue(ActorSystem.Main.IsActorActive(targetActor));
        }

        [UnityTest, Order(5)]
        public static IEnumerator DespawnImmediatelyRespawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));

            ActorSystem.Main.RequestRespawn(targetActor);

            Assert.IsTrue(despawnEventExecutons.Contains(targetActor) && despawnEventExecutons.Count == 1); // Respawn with a despawned item should not trigger another despawn.

            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            Assert.IsTrue(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(spawnEventExecutions.Contains(targetActor));
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor) && despawnEventExecutons.Count == 1); // Respawn with a despawned item should not trigger another despawn.
        }

        [UnityTest, Order(6)]
        public static IEnumerator DespawnWaitRespawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));

            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            ActorSystem.Main.RequestRespawn(targetActor);
            Assert.IsTrue(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(spawnEventExecutions.Contains(targetActor));
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor) && despawnEventExecutons.Count == 1); // Respawn with a despawned item should not trigger another despawn.
        }

        [UnityTest, Order(7)]
        public static IEnumerator RespawnImmediatelyDespawnTest()
        {
            for (int i = 0; i < loadedScenes.Count; i++)
                BootActors.RunInits(loadedScenes[i]);

            ActorSystem.IActor targetActor = ActorTestUtils.GetAnyActor(rootActorHolders);
            Debug.Log($"[Test] Target actor: {targetActor.ActorGameObject.name}");

            ActorSystem.Main.RequestRespawn(targetActor);
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));

            ActorSystem.Main.Despawn(targetActor);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));

            yield return new WaitForSeconds(kWaitForSecondsInRespawnSequence);
            Assert.IsFalse(ActorSystem.Main.IsActorActive(targetActor));

            Assert.IsTrue(spawnEventExecutions.Count == 0);
            Assert.IsTrue(despawnEventExecutons.Contains(targetActor));
        }
    }
}
