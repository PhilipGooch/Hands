using CoreSample.Network;
using NBG.Actor;
using NBG.Core;
using NBG.Core.GameSystems;
using NBG.Core.ObjectIdDatabase;
using NBG.Entities;
using NBG.Net;
using NBG.Recoil.Net;
using Recoil;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Base
{
    [ScriptExecutionOrder(-100)]
    public class WorldBootstrapper : MonoBehaviour
    {
        private enum State
        {
            Created,
            Initialized,
            Destroyed
        }
        private State state { get; set; } = State.Created;

        //TODO:cant we simplify it? without states and with simple callback/method for - isReady or something?
        private bool IsReadyToBootstrap() { return true; }

        private const int kObjectIdDbSceneStartingId = 1;

        
        public PlayerManager basicPlayerManager;//TODO: single player should be same as networked player, not different classes

        public event Action OnBeforeManagedBehavioursCreated;
        public event Action OnAfterManagedBehavioursCreated;
        public event Action OnAfterManagedBehavioursDestroyed;

        private void Awake()
        {
            Debug.Assert(state == State.Created);

            if (Bootloader.Instance == null)
                Bootloader.Create(nameof(WorldBootstrapper));

            if (IsReadyToBootstrap())
                Awake_Internal();
        }

        private void Awake_Internal()
        {
            Debug.Assert(state == State.Created);
            Debug.Log($"BOOTSTRAPPING RECOIL in {this.gameObject.scene.name}");

            Bootloader.Instance.OnBeforeDestroy += OnBeforeDestroy;
            Bootloader.Instance.OnBeforeSceneLoad += OnBeforeSceneLoad;

            Recoil.RigidbodyRegistration.RegisterAll();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                int sceneId = kObjectIdDbSceneStartingId + i;
                var scene = SceneManager.GetSceneAt(i);
                ObjectIdDatabaseResolver.instance.Register(sceneId, scene);
            }

            NetGame.RegisterWorldBootstrapper(this);

            // Boot actors after recoil. (And recommended before ManagedBehaviours, since activity in those is in user land) 
            BootActors.ClearBooterState();
            BootActors.RunInits(SceneManager.GetActiveScene());

            basicPlayerManager = FindObjectOfType<PlayerManager>();
            if (basicPlayerManager != null)
                basicPlayerManager.OnCreate();
            OnBeforeManagedBehavioursCreated?.Invoke();

            BootManagedBehaviours.RunOnLevelLoaded(SceneManager.GetActiveScene());
            BootManagedBehaviours.NotifyOnLevelLoadedDone();
            BootManagedBehaviours.RunOnAfterLevelLoaded(SceneManager.GetActiveScene());
            BootManagedBehaviours.NotifyOnAfterLevelLoadedDone();

            GameSystemWorldDefault.DebugPrint(this.gameObject.scene.name);

            OnAfterManagedBehavioursCreated?.Invoke();

            state = State.Initialized;
        }

        private void FixedUpdate()
        {
            if (state == State.Created && IsReadyToBootstrap())
            {
                Awake_Internal();
            }
        }

        private void OnDestroy()
        {
            if (state != State.Destroyed)
                Debug.LogError("WorldBootstrapper should be destroyed manually!");

            if (Bootloader.Instance != null)
            {
                Bootloader.Instance.OnBeforeDestroy -= OnBeforeDestroy;
                Bootloader.Instance.OnBeforeSceneLoad -= OnBeforeSceneLoad;
            }
        }

        private void OnBeforeSceneLoad()
        {
            OnDestroyIfNeeded();
        }

        private void OnBeforeDestroy()
        {
            OnDestroyIfNeeded();
        }

        private void OnDestroyIfNeeded()
        {
            if (state == State.Destroyed)
                return;
            state = State.Destroyed;
            OnDestroy_Internal();
        }

        private void OnDestroy_Internal()
        {
            Debug.Log($"CLEANING UP RECOIL in {this.gameObject.scene.name}");

            BootManagedBehaviours.RunUnloadAll();

            if (basicPlayerManager != null)
                basicPlayerManager.Dispose();
            OnAfterManagedBehavioursDestroyed?.Invoke();

            NetGame.UnregisterWorldBootstrapper(this);

            ObjectIdDatabaseResolver.instance.UnregisterAll();

            Recoil.RigidbodyRegistration.UnregisterAll();
        }
    }

}
