using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NBG.Core.Streams;
using NBG.Recoil.Net;
using Unity.Collections;
using UnityEngine;

namespace NBG.Net.PlayerManagement
{
    public class ClientPlayerManager : IPlayerManager
    {
        private struct PlayerEntry
        {
            public int globalID;
            public int localID;
            public GameObject go;
        }

        public const int ID_UNASSIGNED = -1;


        private int maxLocalPlayers;
        public int MaxLocalPlayers => maxLocalPlayers;


        private Queue<int> localPlayerIDs = new Queue<int>();
        private Dictionary<int, TaskCompletionSource<PlayerAddedResultData>> playersRequested = new Dictionary<int, TaskCompletionSource<PlayerAddedResultData>>();
        private List<PlayerEntry> playerEntries = new List<PlayerEntry>();
        

        protected INetTransportPeer clientPeer;
        protected IPlayerEvents PlayerEvents;

        public void Init(INetTransportPeer peer, int maxLocalPlayers, IPlayerEvents playerEvents)
        {
            this.clientPeer = peer;
            this.maxLocalPlayers = maxLocalPlayers;
            this.PlayerEvents = playerEvents;
            localPlayerIDs.Clear();
            playerEntries.Clear();
            for (int i = 0; i < maxLocalPlayers; i++)
            {
                localPlayerIDs.Enqueue(i);
            }

            playersRequested.Clear();
        }

        public async Task<PlayerAddedResultData> TryAddLocalPlayer()
        {
            if (localPlayerIDs.Count <= 0)
            {
                return new PlayerAddedResultData
                {
                    result = PlayerAddResult.FailedLocalLimit,
                };
            }

            var requestedID = localPlayerIDs.Dequeue();
            var stream = clientPeer.BeginSend(ChannelType.Reliable);
            stream.WriteMsgId(PlayerManagementProtocol.RequestPlayer);
            stream.Write((byte) requestedID);
            clientPeer.EndSend();
            var source = new TaskCompletionSource<PlayerAddedResultData>();
            playersRequested.Add(requestedID, source);
            return await source.Task;
        }

        public void ReceivePlayerAdded(INetTransportPeer context, IStreamReader data)
        {
            var globalID = data.ReadID();
            Debug.Log($"Server added player with scopeID {globalID}");

            //instantiate and parse payload
            var newPlayerInstance = PlayerEvents.CreateInstance((int)globalID);
            UnityEngine.Object.DontDestroyOnLoad(newPlayerInstance);
            PlayerEvents.OnClientRemotePlayerAdded(newPlayerInstance, (int)globalID, data);

            // Register all INetBehaviours on the target
            Debug.Assert(globalID < 8);
            NetBehaviourList.instance.Register((int)globalID * 2, newPlayerInstance.transform);
            // Register all rigidbodies in a rigidbody list for networking
            var rbList = RigidbodyList.BuildFrom(newPlayerInstance.transform); //TODO@! maybe just add as a MonoBehaviour?
            NetBehaviourList.instance.Register((int)globalID * 2 + 1, rbList);
            rbList.SetKinematic();

            //Creating entry to make player official
            playerEntries.Add(new PlayerEntry()
            {
                go = newPlayerInstance,
                localID = ID_UNASSIGNED, //Will be assigned later, if this is a local player
                globalID = (int)globalID,
            });
        }

        public void RecieveRequestPlayerFailed(INetTransportPeer context, IStreamReader data)
        {
            Debug.Log("Failed to add player");
            int localPlayerID = data.ReadByte();
            PlayerAddResult result = (PlayerAddResult)data.ReadByte();
            if (!playersRequested.TryGetValue(localPlayerID, out var taskSource))
            {
                Debug.LogError($"Server send unknown local player ID {localPlayerID}");
                return;
            }

            playersRequested.Remove(localPlayerID);
            taskSource.SetResult(new PlayerAddedResultData()
            {
                result = result,
                localID = localPlayerID,
                globalID = ID_UNASSIGNED, //Error case has no global ID assigned
            });
        }

        public void ReceiveRequestPlayerSucess(INetTransportPeer context, IStreamReader data)
        {
            Debug.Log($"[{nameof(ClientPlayerManager)}] Successfully added player");
            int localPlayerID = data.ReadByte();
            int globalPlayerID = data.ReadByte();
            if (!playersRequested.TryGetValue(localPlayerID, out var taskSource))
            {
                Debug.LogError($"[{nameof(ClientPlayerManager)}] Server send unknown local player ID {localPlayerID}");
                return;
            }

            playersRequested.Remove(localPlayerID);
            PlayerEntry entry;
            bool globalIdPlayerFound = false;
            for (var index = 0; index < playerEntries.Count; index++)
            {
                entry = playerEntries[index];
                if (entry.globalID == globalPlayerID)
                {
                    globalIdPlayerFound = true;
                    entry.localID = localPlayerID;
                    playerEntries[index] = entry;
                    try
                    {
                        PlayerEvents.OnLocalPlayerAdded(entry.go, globalPlayerID, localPlayerID);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
            }

            Debug.Assert(globalIdPlayerFound, $"[{nameof(ClientPlayerManager)}] There was no player with globalId {globalPlayerID} found");

            taskSource.SetResult(new PlayerAddedResultData()
            {
                result = PlayerAddResult.Success,
                localID = localPlayerID,
                globalID = globalPlayerID,
            });
        }


        public void RemoveLocalPlayer(int localID)
        {
            //Check if exists first
            foreach (var playerEntry in playerEntries)
            {
                if (playerEntry.localID == localID)
                {
                    RequestPlayerRemoval(playerEntry.globalID);
                    return;
                }
            }

            throw new ArgumentException($"Player with localID {localID} was not found");
        }

        //For now then we disconnect from server, remove all non local players as we can't control them and host migration isn't implemented either
        public void RemoveRemotePlayers()
        {
            PlayerEntry entry;
            for (var index = playerEntries.Count - 1; index >= 0; index--)
            {
                entry = playerEntries[index];
                if (entry.localID < 0)
                {
                    playerEntries.RemoveAt(index);

                    NetBehaviourList.instance.Unregister(entry.globalID * 2); // Network Rigidbody list
                    NetBehaviourList.instance.Unregister(entry.globalID * 2 + 1); // All INetBehaviours on the target

                    PlayerEvents.DestroyInstance(entry.go, entry.globalID);
                }
            }
        }

        private void RequestPlayerRemoval(int globalPlayerID)
        {
            var reliable = clientPeer.BeginSend(ChannelType.Reliable);
            reliable.WriteMsgId(PlayerManagementProtocol.RemovePlayer);
            reliable.Write((byte)globalPlayerID);
            clientPeer.EndSend();
        }

        public void ReceivePlayerRemoved(INetTransportPeer context, IStreamReader data)
        {
            var globalID = data.ReadID();
            Debug.Log($"Server removed player with scopeID {globalID}");

            PlayerEntry entry = default;
            bool wasRemoved = false;
            for (var index = 0; index < playerEntries.Count; index++)
            {
                entry = playerEntries[index];
                if (entry.globalID == globalID)
                {
                    playerEntries.RemoveAtSwapBack(index);
                    wasRemoved = true;
                    break;
                }
            }

            if (wasRemoved)
            {
                if (entry.localID >= 0)
                {
                    localPlayerIDs.Enqueue(entry.localID);
                    try
                    {
                        PlayerEvents.OnLocalPlayerRemoved(entry.go, entry.globalID, entry.localID);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }

                NetBehaviourList.instance.Unregister((int)globalID * 2); // Network Rigidbody list
                NetBehaviourList.instance.Unregister((int)globalID * 2 + 1); // All INetBehaviours on the target
                PlayerEvents.DestroyInstance(entry.go, entry.globalID);
            }
            else
            {
                Debug.LogWarning($"Server tried to remove ragdoll with globalID {globalID} but was not found");
            }
        }


        public void Destroy()
        {
            foreach (var entry in playerEntries)
            {
                try
                {
                    if (entry.localID >= 0)
                        PlayerEvents.OnLocalPlayerRemoved(entry.go, entry.globalID, entry.localID);

                    NetBehaviourList.instance.Unregister(entry.globalID * 2); // Network Rigidbody list
                    NetBehaviourList.instance.Unregister(entry.globalID * 2 + 1); // All INetBehaviours on the target

                    PlayerEvents.DestroyInstance(entry.go, entry.globalID);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            playerEntries.Clear();
        }



        #region PlayerAPI

        public IEnumerable<GameObject> AllPlayers()
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                yield return entry.go;
            }
        }

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

        public IEnumerable<GameObject> LocalPlayers()
        {
            for (var i = 0; i < playerEntries.Count; i++)
            {
                var entry = playerEntries[i];
                if (entry.localID > ID_UNASSIGNED)
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
                if (entry.localID > ID_UNASSIGNED && entry.go.TryGetComponent(out T component))
                {
                    yield return component;
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

        //TODO: Make System
        public void FixedUpdate()
        {
            if (!playerEntries.Any())
                return;

            var writer = clientPeer.BeginSend(ChannelType.Unreliable);
            writer.WriteMsgId(PlayerManagementProtocol.PlayerInput);
            try
            {
                var numPlayers = playerEntries.Count(x => x.localID >= 0);
                writer.Write(numPlayers, 4);
                for (var i = 0; i < playerEntries.Count; i++)
                {
                    var entry = playerEntries[i];
                    if (entry.localID < 0)
                        continue;
                    writer.WriteID((uint) entry.globalID);
                    PlayerEvents.OnGatherPlayerInput(entry.go, entry.globalID, entry.localID, writer);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                clientPeer.AbortSend();
            }
            clientPeer.EndSend();
        }
    }
}