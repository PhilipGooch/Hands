using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBG.Core;
using NBG.Core.GameSystems;
using NBG.Core.Streams;
using NBG.Net.Systems;
using NBG.Recoil.Net;
using Unity.Collections;
using UnityEngine;

namespace NBG.Net.PlayerManagement
{
    public class ServerPlayerManager : IPlayerManager
    {
        private struct PlayerEntry
        {
            public int globalID;
            public int localID;
            public GameObject go;
            public INetTransportPeer peer;
        }

        public const int ID_UNASSIGNED = -1;


        private int maxLocalPlayers;
        public int MaxLocalPlayers => maxLocalPlayers;

        private int maxGlobalPlayers;
        public int MaxGlobalPlayers => maxGlobalPlayers;
        
        
        private readonly Queue<int> localPlayerIDs = new Queue<int>();
        private readonly Queue<int> globalPlayerIDs = new Queue<int>();
        private readonly List<PlayerEntry> playerEntries = new List<PlayerEntry>();

        private List<INetTransportServer> allServers;
        private IPlayerEvents playerEvents;

        public void Init(int maxLocalPlayers, int maxGlobalPlayers, IPlayerEvents playerEvents)
        {
            //TODO: remove all gameobjects of old one?

            this.maxLocalPlayers = maxLocalPlayers;
            this.maxGlobalPlayers = maxGlobalPlayers;
            this.playerEvents = playerEvents;
            this.allServers = new List<INetTransportServer>();
            for (int i = 0; i < maxLocalPlayers; i++)
            {
                localPlayerIDs.Enqueue(i);
            }
            for (int i = 0; i < maxGlobalPlayers; i++)
            {
                globalPlayerIDs.Enqueue(i);
            }
        }
        
        //TODO: Make this work with IPeerCollection
        public void AddServer(INetTransportServer server)
        {
            allServers.Add(server);
        }

        public void RemoveServer(INetTransportServer server)
        {
            allServers.Remove(server);
        }

        public Task<PlayerAddedResultData> TryAddLocalPlayer()
        {
            var taskCompletionSource = new TaskCompletionSource<PlayerAddedResultData>();
            var result = new PlayerAddedResultData()
            {
                globalID = ID_UNASSIGNED,
                localID = ID_UNASSIGNED,
                result = PlayerAddResult.InternalError,
            };

            if (localPlayerIDs.Count <= 0)
            {
                result.result = PlayerAddResult.FailedLocalLimit;
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }

            if (globalPlayerIDs.Count <= 0)
            {
                result.result = PlayerAddResult.FailedServerLimit;
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }

            result.localID = localPlayerIDs.Dequeue();
            result.globalID = globalPlayerIDs.Dequeue();

            var newPlayer = SpawnPlayerPrefab(result.globalID);
            if (newPlayer == null)
            {
                localPlayerIDs.Enqueue(result.localID);
                globalPlayerIDs.Enqueue(result.globalID);
                result.localID = ID_UNASSIGNED;
                result.globalID = ID_UNASSIGNED;
                result.result = PlayerAddResult.InternalError;
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }

            try
            {
                playerEvents.OnLocalPlayerAdded(newPlayer, result.globalID, result.localID);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                playerEvents.DestroyInstance(newPlayer.gameObject, result.globalID);
                localPlayerIDs.Enqueue(result.localID);
                globalPlayerIDs.Enqueue(result.globalID);
                result.localID = ID_UNASSIGNED;
                result.globalID = ID_UNASSIGNED;
                result.result = PlayerAddResult.InternalError;
                taskCompletionSource.SetResult(result);
                return taskCompletionSource.Task;
            }

            playerEntries.Add(new PlayerEntry()
            {
                go = newPlayer,
                globalID = result.globalID,
                localID = result.localID,
                peer = null
            });
            result.result = PlayerAddResult.Success;
            taskCompletionSource.SetResult(result);
            return taskCompletionSource.Task;
        }

        public void RemoveLocalPlayer(int localID)
        {
            PlayerEntry entry = default;
            bool wasRemoved = false;
            int globalID = ID_UNASSIGNED;

            for (var index = 0; index < playerEntries.Count; index++)
            {
                entry = playerEntries[index];
                if (entry.localID == localID && entry.peer == null)
                {
                    playerEntries.RemoveAtSwapBack(index);
                    globalID = entry.globalID;
                    wasRemoved = true;
                    break;
                }
            }

            if (!wasRemoved)
            {
                Debug.LogError($"tried to remove local player with localID {localID} but was not found");
                return;
            }

            localPlayerIDs.Enqueue(localID);
            globalPlayerIDs.Enqueue(globalID);

            try
            {
                playerEvents.OnLocalPlayerRemoved(entry.go, entry.globalID, entry.localID);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            NetBehaviourList.instance.Unregister((int)globalID * 2 + 1); // Network Rigidbody list
            NetBehaviourList.instance.Unregister((int)globalID * 2); // All INetBehaviours on the target
            BroadcastPlayerRemoved(entry.go, (uint)globalID);
            playerEvents.DestroyInstance(entry.go, entry.globalID);
        }

        public void ReceiveRequestPlayer(INetTransportPeer context, IStreamReader data)
        {
            byte localID = data.ReadByte();
            if (globalPlayerIDs.Count <= 0)
            {
                Debug.Log($"{context} requested to add a player, but server is full");
                var msg = context.BeginSend(ChannelType.Reliable);
                msg.WriteMsgId(PlayerManagementProtocol.RequestPlayerFailed);
                msg.Write(localID);
                msg.Write((byte)PlayerAddResult.FailedServerLimit);
                context.EndSend();
                return;
            }

            byte newGlobalID = (byte)globalPlayerIDs.Dequeue();
            var player = SpawnPlayerPrefab(newGlobalID);
            if (player != null)
            {
                playerEntries.Add(new PlayerEntry()
                {
                    go = player,
                    globalID = newGlobalID,
                    localID = localID,
                    peer = context
                });
                var msg = context.BeginSend(ChannelType.Reliable);
                msg.WriteMsgId(PlayerManagementProtocol.RequestPlayerSuccess);
                msg.Write(localID);
                msg.Write(newGlobalID);
                context.EndSend();
                playerEvents.OnServerRemotePlayerAdded(player, newGlobalID);
            }
            else
            {
                globalPlayerIDs.Enqueue(newGlobalID);
                var msg = context.BeginSend(ChannelType.Reliable);
                msg.WriteMsgId(PlayerManagementProtocol.RequestPlayerFailed);
                msg.Write(localID);
                msg.Write((byte)PlayerAddResult.InternalError);
                context.EndSend();
            }
        }

        public void ReceiveRemovePlayer(INetTransportPeer context, IStreamReader data)
        {
            byte globalID = data.ReadByte();
            PlayerEntry entry = default;
            bool wasRemoved = false;
            for (var index = 0; index < playerEntries.Count; index++)
            {
                entry = playerEntries[index];
                if (entry.globalID == globalID && entry.peer == context)
                {
                    playerEntries.RemoveAtSwapBack(index);
                    wasRemoved = true;
                    break;
                }
            }

            if (!wasRemoved)
            {
                Debug.LogError($"peer {context} tried to remove globalID {globalID} but was not found");
                return;
            }

            NetBehaviourList.instance.Unregister(entry.globalID * 2); // Network Rigidbody list
            NetBehaviourList.instance.Unregister(entry.globalID * 2 + 1); // All INetBehaviours on the target

            BroadcastPlayerRemoved(entry.go, globalID);
            globalPlayerIDs.Enqueue(globalID);
            playerEvents.DestroyInstance(entry.go, entry.globalID);
        }

        public void ReceivePlayerInput(INetTransportPeer context, IStreamReader data)
        {
            var numPlayers = data.ReadInt32(4);
            for (int i = 0; i < numPlayers; i++)
            {
                var globalID = data.ReadID();
                bool wasFound = false;
                for (var index = 0; index < playerEntries.Count; index++)
                {
                    var entry = playerEntries[index];
                    if (entry.peer == context && entry.globalID == globalID)
                    {
                        wasFound = true;
                        playerEvents.OnReceivePlayerInput(entry.go, entry.globalID, data);
                    }
                }
                if (!wasFound)
                {
                    //Reducing it to warning for now as client is sending data until it knows that player is removed (with delay). TODO: Need to disable data sent with request for removal
                    Debug.LogWarning($"{context} send input for globalID {globalID} but no player was found");
                    //TODO: remove these then it will be fixed
                    _ = data.ReadBool();
                    _ = data.ReadBool();
                    _ = data.ReadBool();
                    _ = data.ReadBool();
                    _ = data.ReadInt32(6, 12);
                    _ = data.ReadInt32(6, 12);
                    _ = data.ReadInt32(6, 12);
                    _ = data.ReadInt32(6, 12);
                    return;
                }
            }
        }

        public void PeerDisconnected(INetTransportPeer context)
        {
            //remove all players owned by peer and notify other existing peers of that
            for (var index = playerEntries.Count - 1; index >= 0; index--)
            {
                var entry = playerEntries[index];
                if (entry.peer == context)
                {
                    playerEntries.RemoveAtSwapBack(index);
                    
                    NetBehaviourList.instance.Unregister(entry.globalID * 2); // Network Rigidbody list
                    NetBehaviourList.instance.Unregister(entry.globalID * 2 + 1); // All INetBehaviours on the target

                    BroadcastPlayerRemoved(entry.go, (uint)entry.globalID);
                    globalPlayerIDs.Enqueue(entry.globalID);
                    playerEvents.DestroyInstance(entry.go, entry.globalID);
                }
            }
        }

        public void PeerConnected(INetTransportPeer context)
        {
            //Send all created player to this peer so it would create them too
            for (var index = playerEntries.Count - 1; index >= 0; index--)
            {
                var entry = playerEntries[index];
                var streamWriter = context.BeginSend(ChannelType.Reliable);
                streamWriter.WriteMsgId(PlayerManagementProtocol.PlayerAdded);
                streamWriter.WriteID((uint)entry.globalID);
                //Payload would go here.
                context.EndSend();
            }
        }

        /// <summary>
        /// Will send broadcast about removal
        /// </summary>
        public void RemovePlayers()
        {
            PlayerEntry entry = default;
            for (var index = playerEntries.Count - 1; index >= 0; index--)
            {
                entry = playerEntries[index];
                playerEntries.RemoveAt(index);

                if (entry.localID >= 0)
                {
                    try
                    {
                        playerEvents.OnLocalPlayerRemoved(entry.go, entry.globalID, entry.localID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                NetBehaviourList.instance.Unregister(entry.globalID * 2); // Network Rigidbody list
                NetBehaviourList.instance.Unregister(entry.globalID * 2 + 1); // All INetBehaviours on the target

                BroadcastPlayerRemoved(entry.go, (uint)entry.globalID);
                globalPlayerIDs.Enqueue(entry.globalID);
                playerEvents.DestroyInstance(entry.go, entry.globalID);
            }
            playerEntries.Clear();
        }

        #region PlayerAPI
        public IEnumerable<T> AllPlayers<T>(bool inChildren = false, bool includeInactive = true) where T : MonoBehaviour
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (inChildren)
                {
                    var ret = entry.go.GetComponentInChildren<T>(includeInactive);
                    if (ret != null)
                        yield return ret;
                }
                else
                {
                    if (entry.go.TryGetComponent(out T component))
                    {
                        yield return component;
                    }
                }
            }
        }
        
        public IEnumerable<GameObject> AllPlayers()
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.go != null)
                {
                    yield return entry.go;
                }
            }
        }

        public IEnumerable<T> LocalPlayers<T>() where T : MonoBehaviour
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.peer == null && entry.go.TryGetComponent(out T component))
                {
                    yield return component;
                }
            }
        }

        public IEnumerable<GameObject> LocalPlayers()
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.peer == null && entry.go != null)
                {
                    yield return entry.go;
                }
            }
        }

        public IEnumerable<T> RemotePlayers<T>() where T: MonoBehaviour
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.peer != null && entry.go.TryGetComponent(out T component))
                {
                    yield return component;
                }
            }
        }

        public IEnumerable<GameObject> RemotePlayers()
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.peer != null && entry.go != null)
                {
                    yield return entry.go;
                }
            }
        }

        public int NumPlayers => playerEntries.Count;
        public int NumLocalPlayers => MaxLocalPlayers - localPlayerIDs.Count; 
        public int NumFreeLocalPlayers => localPlayerIDs.Count;

        public T GetGlobalPlayer<T>(int globalID) where T : MonoBehaviour
        {
            for (int i = 0; i < playerEntries.Count; i++)
            {
                var current = playerEntries[i];
                if (current.globalID == globalID)
                {
                    return current.go.GetComponent<T>();
                }
            }
            return null;
        }

        public GameObject GetGlobalPlayer(int globalID)
        {
            for (int i = 0; i < playerEntries.Count; i++)
            {
                var current = playerEntries[i];
                if (current.globalID == globalID)
                {
                    return current.go;
                }
            }
            return null;
        }

        public GameObject GetLocalPlayer(int localPlayer)
        {
            Debug.Assert(localPlayer >= 0, "Local player ID must be positive");
            Debug.Assert(localPlayer < MaxLocalPlayers, "Local player ID must be smaller then MaxLocalPlayers");

            for (int i = 0; i < playerEntries.Count; i++)
            {
                var current = playerEntries[i];
                if (current.localID == localPlayer)
                {
                    return current.go;
                }
            }
            return null;
        }

        public T GetLocalPlayer<T>(int localPlayer) where T : MonoBehaviour
        {
            Debug.Assert(localPlayer >= 0, "Local player ID must be positive");
            Debug.Assert(localPlayer < MaxLocalPlayers, "Local player ID must be smaller then MaxLocalPlayers");

            for (int i = 0; i < playerEntries.Count; i++)
            {
                var current = playerEntries[i];
                if (current.localID == localPlayer)
                {
                    return current.go.GetComponent<T>();
                }
            }
            return null;
        }

        public bool TryGetGlobalID(GameObject player, out int globalID)
        {
            for (var index = 0; index < playerEntries.Count; index++)
            {
                var playerEntry = playerEntries[index];
                if (playerEntry.go == player && playerEntry.globalID >= 0)
                {
                    globalID = playerEntry.globalID;
                    return true;
                }
            }
            globalID = ID_UNASSIGNED;
            return false;
        }

        public bool TryGetLocalID(GameObject player, out int localID)
        {
            for (var index = 0; index < playerEntries.Count; index++)
            {
                var playerEntry = playerEntries[index];
                if (playerEntry.go == player && playerEntry.localID >= 0)
                {
                    localID = playerEntry.localID;
                    return true;
                }
            }
            localID = ID_UNASSIGNED;
            return false;
        }
        #endregion

        private GameObject SpawnPlayerPrefab(int globalID)
        {
            try
            {
                var instance = playerEvents.CreateInstance(globalID);
                UnityEngine.Object.DontDestroyOnLoad(instance);

                // Register all INetBehaviours on the target
                Debug.Assert(globalID < 8);
                NetBehaviourList.instance.Register((int)globalID * 2, instance.transform);

                // Register all rigidbodies in a rigidbody list for networking
                var rbList = RigidbodyList.BuildFrom(instance.transform); //TODO@! maybe just add as a MonoBehaviour?
                NetBehaviourList.instance.Register((int)globalID * 2 + 1, rbList);
                
                BroadcastPlayerAdded(instance, globalID);

                return instance;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        private void BroadcastPlayerAdded(GameObject instance, int globalID)
        {
            foreach (var transport in allServers)
            {
                var streamWriter = transport.BeginBroadcast(ChannelType.Reliable);
                streamWriter.WriteMsgId(PlayerManagementProtocol.PlayerAdded);
                streamWriter.WriteID((uint)globalID);
                playerEvents.OnPlayerCreatedBroadcast(instance, globalID, streamWriter);
                transport.EndBroadcast();
            }
        }

        private void BroadcastPlayerRemoved(GameObject go, uint globalID)
        {
            foreach (var transport in allServers)
            {
                var streamWriter = transport.BeginBroadcast(ChannelType.Reliable);
                streamWriter.WriteMsgId(PlayerManagementProtocol.PlayerRemoved);
                streamWriter.WriteID(globalID);
                //playerEvents.OnPlayerCreatedBroadcast(go, (int)globalID, streamWriter);//TODO: why was it here?
                transport.EndBroadcast();
            }
        }
    }
}
