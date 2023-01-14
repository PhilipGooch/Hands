using CoreSample.Base;
using CoreSample.Network.Noodle;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Systems;
using NBG.Net.Transport.Sockets;
using NBG.Recoil.Net;
using Recoil;
using System;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Network
{
    public enum DebugConnectionMode
    {
        Sockets = 0,
        Steam = 1,
        SteamSockets = 2,
    }

    public static class NetGame
    {
        private const int kNetIdStart = 16;//reserved for players?
        private const string ServerToConnectTo = "127.0.0.1"; // <-- Replace with the IP Address you want to connect to//move to socket solution
        private const int MAX_LOCAL_PLAYERS = 4;
        private const int MAX_GLOBAL_PLAYERS = 8;

        private static WorldBootstrapper worldBootstrapper;
        private static GameObject netPlayerPrefab;

        private static IPlayerManager playerManager;
        private static NoodlePlayerEvents playerEvents;

        private static SampleClients sampleClients;
        private static SamplePeers samplePeers;
        public static IPeerCollection Peers => samplePeers;

        public static bool IsNetGameEnabled { get; set; } = false;

        public static event Action<bool> OnNetworkingStarted;//isServer
        public static event Action OnNetworkingStopped;
        public static event Action<int, int> OnLocalMachinePlayerCountChanged;//currentCount, maxCount

        
        public static void Initialization(GameObject playerPrefab)
        {
            netPlayerPrefab = playerPrefab;

            playerEvents = new NoodlePlayerEvents(netPlayerPrefab);//some magic happening here. not sure how everything works inside
            playerEvents.Init(MAX_GLOBAL_PLAYERS);
            playerEvents.OnLocalPlayerCountChanged += () =>
            {
                Debug.Assert(playerManager != null, "Player manager should not be null");
                OnLocalMachinePlayerCountChanged?.Invoke(playerManager.NumLocalPlayers, MAX_LOCAL_PLAYERS);
            };
        }

        public static void RegisterWorldBootstrapper(WorldBootstrapper bootstrapper)
        {
            Debug.Assert(worldBootstrapper == null);
            worldBootstrapper = bootstrapper;
            bootstrapper.OnBeforeManagedBehavioursCreated += OnBeforeManagedBehavioursCreated;
            bootstrapper.OnAfterManagedBehavioursCreated += OnAfterManagedBehavioursCreated;
            bootstrapper.OnAfterManagedBehavioursDestroyed += OnAfterManagedBehavioursDestroyed;
        }

        public static void UnregisterWorldBootstrapper(WorldBootstrapper bootstrapper)
        {
            bootstrapper.OnBeforeManagedBehavioursCreated -= OnBeforeManagedBehavioursCreated;
            bootstrapper.OnAfterManagedBehavioursCreated -= OnAfterManagedBehavioursCreated;
            bootstrapper.OnAfterManagedBehavioursDestroyed -= OnAfterManagedBehavioursDestroyed;
        }

        public static void FixedUpdate()
        {
            if (!IsNetGameEnabled)
                return;

            //Client Player Manager needs fixed Update tick to send out Input
            if (playerManager is ClientPlayerManager clientPlayerManager)
            {
                clientPlayerManager.FixedUpdate();
            }

            //Server Player Manager needs us to copy input from Network into Recoil
            if (playerManager is ServerPlayerManager serverPlayerManager)
            {
                foreach (var player in serverPlayerManager.RemotePlayers<NoodlePlayerController>())
                {
                    if (serverPlayerManager.TryGetGlobalID(player.gameObject, out var globalID))
                    {
                        playerEvents.ApplyNetworkInputFrame(player.gameObject, globalID);
                    }
                }
            }

            //Tick players
            foreach (var player in playerManager.AllPlayers<NoodlePlayerController>())
            {
                player.OnFixedUpdate();
            }

            //Tick players post fixed Update.
            //NOTE: This is a work-Around bespoke to netSample to deal with Input being sticky
            foreach (var player in playerManager.AllPlayers<NoodlePlayerController>())
            {
                player.OnPostFixedUpdate();
            }
        }

        #region Server
        public static void HostServer(DebugConnectionMode connectionMode)
        {
            if (IsNetGameEnabled)
            {
                Debug.LogWarning("Already a networked instance");
                return;
            }

            if (worldBootstrapper == null)
            {
                Debug.LogWarning("Cant host on scenes without worldBootstrapper");
                return;
            }

            //in theory we could be server and client at the same time, but probably it is not supprted by the game logic?
            if (sampleClients != null)
            {
                Debug.LogError("Cannot host, a Client is running");
                return;
            }

            if (samplePeers != null)
            {
                Debug.LogError("Cannot create server, Server is already running");
                return;
            }

            var serverPlayerManager = new ServerPlayerManager();
            serverPlayerManager.Init(MAX_LOCAL_PLAYERS, MAX_GLOBAL_PLAYERS, playerEvents);
            playerManager = serverPlayerManager;
            OnLocalMachinePlayerCountChanged?.Invoke(0, MAX_LOCAL_PLAYERS);

            //Socket Server
            samplePeers = new SamplePeers(connectionMode, new ServerCallback(serverPlayerManager), serverPlayerManager);

            var serverSystem = GameSystemWorldDefault.Instance.GetExistingSystem<NetWriteAndSendFrame>();
            serverSystem.allPeers = samplePeers;
            serverSystem.Enabled = true;
            Bootloader.Instance.OnBeforeSceneLoad += serverSystem.LimitHistoryToCurrentFrame;

            samplePeers.Listen(MAX_GLOBAL_PLAYERS, connectionMode == DebugConnectionMode.Steam);

            IsNetGameEnabled = true;

            OnBeforeManagedBehavioursCreated();
            OnAfterManagedBehavioursCreated();

            OnNetworkingStarted?.Invoke(true);

            GameSystemWorldDefault.DebugPrint("Post Server Start");
        }

        private static void StopServer()
        {
            var serverSystem = GameSystemWorldDefault.Instance.GetExistingSystem<NetWriteAndSendFrame>();
            serverSystem.allPeers = null;
            serverSystem.Enabled = false;
            Bootloader.Instance.OnBeforeSceneLoad -= serverSystem.LimitHistoryToCurrentFrame;
        }
        #endregion

        #region Client
        //TODO: sometimes something isn't destroyed and connecting for the second time yields errors
        public static async void JoinServer(DebugConnectionMode connectionMode)
        {
            if (IsNetGameEnabled)
            {
                Debug.LogWarning("Already a networked instance");
                return;
            }

            if (samplePeers != null)
            {
                Debug.LogError("Cannot connect, a Server is running");
                return;
            }

            if (sampleClients != null)
            {
                Debug.LogError("Cannot connect, a Client is running");
                return;
            }

            if (!IPAddress.TryParse(ServerToConnectTo, out IPAddress addr))
            {
                Debug.LogError("Failed to parse ip address to connect to");
                return;
            }

            var clientPlayerManager = new ClientPlayerManager();
            playerManager = clientPlayerManager;

            //Socket Client
            sampleClients = new SampleClients(connectionMode, clientPlayerManager);

            try
            {
                StartClient();

                var localhost = new IPEndPoint(addr, SocketTransportProvider.DefaultPort);
                
                var connectedPeer = await sampleClients.Connect(localhost);

                if (connectedPeer == null)
                {
                    Debug.LogWarning("Couldn't connect");
                    return;
                }

                clientPlayerManager.Init(connectedPeer, MAX_LOCAL_PLAYERS, playerEvents);

                OnNetworkingStarted?.Invoke(false);
                IsNetGameEnabled = true;
                OnLocalMachinePlayerCountChanged?.Invoke(0, MAX_LOCAL_PLAYERS);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                sampleClients = null;
            }
        }

        private static void StartClient()
        {
            //Enable parsing and applying of netstreams
            GameSystemWorldDefault.Instance.GetExistingSystem<NetReadAndApplyFrame>().Enabled = true;
            //Disable ReadState, we will be reading from NetStreams instead
            GameSystemWorldDefault.Instance.GetExistingSystem<ReadState>().Enabled = false;
            //Disable large parse of Recoil physics solver because we will be using data from the server
            GameSystemWorldDefault.Instance.GetExistingSystem<PhysicSolve>().Enabled = false;

            NetBehaviourList.instance.OnNetworkAuthorityChanged(NetworkAuthority.Client);
            EventBus.Get().Send(new NetworkAuthorityChangedEvent(NetworkAuthority.Client));

            GameSystemWorldDefault.DebugPrint("Post Client Start");
        }

        private static void StopClient()
        {
            GameSystemWorldDefault.Instance.GetExistingSystem<PhysicSolve>().Enabled = true;
            GameSystemWorldDefault.Instance.GetExistingSystem<ReadState>().Enabled = true;
            GameSystemWorldDefault.Instance.GetExistingSystem<NetReadAndApplyFrame>().Enabled = false;

            EventBus.Get().Send(new NetworkAuthorityChangedEvent(NetworkAuthority.Server));
            NetBehaviourList.instance.OnNetworkAuthorityChanged(NetworkAuthority.Server);

            GameSystemWorldDefault.DebugPrint("Post Client Stop");
        }
        #endregion


        public static void StopNetworking()
        {
            if (!IsNetGameEnabled)
            {
                return;
            }

            OnAfterManagedBehavioursDestroyed();

            //Because Core uses Domain Reload, we need to make sure to clean up. Sockets are not automatically freed, when exiting play mode
            if (samplePeers != null)
            {
                samplePeers.ShutdownAll(ConnectionClosedReason.ServerShutdown);
                StopServer();
                samplePeers = null;
            }

            if (sampleClients != null)
            {
                sampleClients.ShutdownAll(ConnectionClosedReason.ClientShutdown);
                StopClient();
                sampleClients = null;
            }

            OnLocalMachinePlayerCountChanged?.Invoke(0, 0);
            OnNetworkingStopped?.Invoke();

            IsNetGameEnabled = false;
        }



        private static void OnBeforeManagedBehavioursCreated()
        {
            if (!IsNetGameEnabled)
                return;

            if (worldBootstrapper.basicPlayerManager != null)
                worldBootstrapper.basicPlayerManager.Dispose();

            RegisterActiveScenesForNetworking();
        }

        /// <summary>
        /// Register all Rigidbodies as NetBehaviourList and everything what implements net interfaces
        /// Move it to netsample maybe?
        /// </summary>
        private static void RegisterActiveScenesForNetworking()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                int netId = kNetIdStart + (i * 2);
                NetBehaviourList.instance.Register(netId, scene);

                var rbList = RigidbodyList.BuildFrom(scene);
                if (rbList.Count > 0)
                {
                    NetBehaviourList.instance.Register(netId + 1, rbList);
                }
                else
                {
                    Debug.LogWarning($"Scene {SceneManager.GetSceneAt(i).name} has no RigidBodies and was not registered");
                }
            }
            Debug.Log($"Registered {SceneManager.sceneCount} scenes for networking.");
        }

        private static void OnAfterManagedBehavioursCreated()
        {
            if (!IsNetGameEnabled)
                return;

            // This is needed as all scenes are by default treated as server. If we are client and new scene is loaded, then we need to tell that it is client specificaly
            // TODO: should we move it before managed behaviours maybe (Bootstraper.Instance.OnAfterSceneLoaded)? But then some things might be not initialized and will be inconvenient to code
            if (sampleClients != null)
            {
                NetBehaviourList.instance.OnNetworkAuthorityChanged(NetworkAuthority.Client);
                EventBus.Get().Send(new NetworkAuthorityChangedEvent(NetworkAuthority.Client));
            }
            //If this is server, then new scene is loaded, tell everyone who is already connected that they need to load new scene
            else if (samplePeers != null)
            {
                byte levelIndex = (byte)SceneManager.GetActiveScene().buildIndex;
                //TODO:better use broadcast here, but cant access right now
                foreach (var transport in samplePeers.GetReadyPeers())
                {
                    var streamWriter = transport.BeginSend(ChannelType.Reliable);
                    streamWriter.WriteMsgId(ProjectSpecificProtocol.LevelLoad);
                    streamWriter.Write(levelIndex);
                    transport.EndSend();
                }
            }
            
        }

        private static void OnAfterManagedBehavioursDestroyed()
        {
            if (!IsNetGameEnabled)
                return;

            UnregisterScenesFromNetworking();
        }

        private static void UnregisterScenesFromNetworking()
        {
            NetBehaviourList.instance.UnregisterAll(kNetIdStart);
        }






        public async static void RequestPlayer()
        {
            if (!IsNetGameEnabled)
            {
                Debug.LogWarning("Not a networked instance");
                return;
            }

            //On Clients requesting a ragdoll is Async and might fail, due to race-conditions between clients.
            //Make sure to process the return value. Result tells you why it failed and the ID's of the new player
            var result = await playerManager.TryAddLocalPlayer();
            Debug.Log($"Player add result {result.result}");
        }


        public static void RequestReleasePlayer()
        {
            if (!IsNetGameEnabled)
            {
                Debug.LogWarning("Not a networked instance");
                return;
            }

            //This request requests just first local owned player remval
            foreach (var playerGO in playerManager.LocalPlayers().ToArray())
            {
                if (playerManager.TryGetLocalID(playerGO, out var localID))
                {
                    playerManager.RemoveLocalPlayer(localID);
                    return;
                }
            }
        }
    }
}
