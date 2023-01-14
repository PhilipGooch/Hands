using CoreSample.Network;
using NBG.Core;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Entities;
using Recoil;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CoreSample.Base
{

    [ScriptExecutionOrder(-32500)]
    public class Bootloader : MonoBehaviour
    {
        public const string BootloaderScene = "bootloader";

        [ClearOnReload]
        public static GameObject _autoCreated;
        [ClearOnReload]
        public static Bootloader Instance { get; private set; }

        [SerializeField] public GameObject netPlayerPrefab; //TODO: should this be scene-specific?
        private GameObject cameraManagerGO;

        public event Action OnBeforeDestroy;
        public event Action OnBeforeSceneLoad;
        public event Action OnAfterSceneLoad;

        private void Awake()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags |= HideFlags.NotEditable;

            CoreSample.Network.Protocol.Init();//Is it used outside of multiplayer? If not, then we should move it to world bootstrapper or netsample isntead

            Debug.Log("INITIALIZING WORLD");

            // Initialize entity world
            ManagedWorld.Create(16);
            EntityStore.Create(10, 500);

            // Initialize game system world
            EventBus.Create();
            GameSystemWorldDefault.Create();
            Recoil.RecoilSystems.Initialize(GameSystemWorldDefault.Instance);
            GameSystemWorldDefault.DebugPrint("boot");

            var cameraManagerPrefab = Resources.Load<GameObject>("CameraManager");
            cameraManagerGO = Instantiate(cameraManagerPrefab);
            cameraManagerGO.name = "CameraManager";
            UnityEngine.Object.DontDestroyOnLoad(cameraManagerGO);

            NetGame.Initialization(netPlayerPrefab);

            DebugSystemToggle.Register();
        }

        private void Start()
        {
            // Show debug ui by default
            if (SceneManager.GetActiveScene().path.Contains(BootloaderScene + ".unity"))
            {
                var debugUI = NBG.DebugUI.DebugUI.Get();
                debugUI.Show();
                debugUI.Print("Select a scene to open...");
            }
        }

        private void Update()
        {
            InputSystem.Update(); // Input system is set to "Process Events Manually" in this sample project
        }

        private void FixedUpdate()
        {
            NetGame.FixedUpdate();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (Instance == null)
                return;

            Debug.Log("DESTROYING WORLD");

            NetGame.StopNetworking();
            Destroy(cameraManagerGO);

            try
            {
                OnBeforeDestroy?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            // Shutdown game system world
            Recoil.RecoilSystems.Shutdown();
            GameSystemWorldDefault.Destroy();
            EventBus.Destroy();

            // Shutdown entity world
            EntityStore.Destroy();
            ManagedWorld.Destroy();

            Instance = null;
        }

        public void LoadScene(int index)
        {
            OnBeforeSceneLoad?.Invoke();
            SceneManager.LoadScene(index, LoadSceneMode.Single);
            OnAfterSceneLoad?.Invoke();
        }


        public static void Create(string createdByName)
        {
            Debug.Assert(_autoCreated == null && Instance == null);

            var prefab = Resources.Load("Bootloader", typeof(GameObject)) as GameObject;
            var go = UnityEngine.Object.Instantiate(prefab);
            go.name = $"BOOTLOADER (by {createdByName})";

            _autoCreated = go;
        }

        //TODO: do we really need this? Why cant we relly on simple destoy as it is always in runtime, and on destroy is always called?
        public static void DestroyIfAutoCreated()
        {
            if (Instance != null)
            {
                Instance.Cleanup();
            }

            if (_autoCreated != null)
            {
                GameObject.DestroyImmediate(_autoCreated);
                _autoCreated = null;
            }
        }
    }
}
