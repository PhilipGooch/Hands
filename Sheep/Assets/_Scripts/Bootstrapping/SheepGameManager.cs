using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.SceneManagement;
using VR.System;

// AKA BOOTSTRAPPER
public class SheepGameManager : SingletonBehaviour<SheepGameManager>
{
    [SerializeField]
    Player player;
    [SerializeField]
    VRSystem vrSystem;
    //[SerializeField]
    //LevelManager levelManager;
    //[SerializeField]
    //PlayerUIManager playerUIManager;

    List<GameObject> tempObjectList = new List<GameObject>();

    DataManager dataManager;
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            vrSystem.Initialize();
            player.Initialize();
            //levelManager.Initialize();
            //playerUIManager.Initialize();

            //ManagedWorld.Create(16);
            //EntityStore.Create(10, 500);
            //
            //EventBus.Create();
            //GameSystemWorldDefault.Create();
            //RecoilSystems.Initialize(GameSystemWorldDefault.Instance);

            dataManager = DataManager.EnsureInitialized();

            //LevelManager.onSceneUnloaded += OnSceneUnload;
            //LevelManager.onSceneLoaded += OnSceneLoad;

            //Threat.Initialize();

            //GameSystemWorldDefault.DebugPrint("boot");

            transform.parent = null;
            DontDestroyOnLoad(this);
        }
    }

    void OnSceneLoad(Scene scene)
    {
        //RigidbodyRegistration.Register(scene);
        // Boot actors after recoil. (And recommended before ManagedBehaviours, since activity in those is in user land)
        //BootActors.ClearBooterState();
        //BootActors.RunInits(scene);
        //BootManagedBehaviours.RunOnLevelLoaded(scene);
        //BootManagedBehaviours.NotifyOnLevelLoadedDone();
        //BootManagedBehaviours.RunOnAfterLevelLoaded(scene);
        //BootManagedBehaviours.NotifyOnAfterLevelLoadedDone();
    }

    void OnSceneUnload(Scene scene)
    {
        //BootManagedBehaviours.RunUnloadAll();
        //RigidbodyRegistration.Unregister(scene);
        // Fixed update will run after this. Disable everything to prevent executing code for unregistered recoil bodies.
        tempObjectList.Clear();
        scene.GetRootGameObjects(tempObjectList);
        foreach(var obj in tempObjectList)
        {
            obj.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Must Unload all managed behaviours in order to correctly dispose of things like ropes.
            //BootManagedBehaviours.RunUnloadAll();
            //RigidbodyRegistration.UnregisterAll();
            ////Threat.Dispose();
            ////SheepScareIterative.Dispose();
            ////LevelManager.onSceneUnloaded -= OnSceneUnload;
            ////LevelManager.onSceneLoaded -= OnSceneLoad;
            //RigidbodyRegistration.UnregisterAll();
            //RecoilSystems.Shutdown();
            //GameSystemWorldDefault.Destroy();
            //EventBus.Destroy();
            //EntityStore.Destroy();
            //ManagedWorld.Destroy();
        }
    }
}
