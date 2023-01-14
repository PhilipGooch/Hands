using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NBG.Net.PlayerManagement
{
    public interface IPlayerManager
    {
        public IEnumerable<T> AllPlayers<T>(bool inChildren = false, bool includeInactive = true) where T : MonoBehaviour;
        public IEnumerable<GameObject> AllPlayers();
        public IEnumerable<T> LocalPlayers<T>() where T : MonoBehaviour;
        public IEnumerable<GameObject> LocalPlayers();
        public int NumPlayers { get; }
        public int NumLocalPlayers { get; }
        public int NumFreeLocalPlayers { get; }
        public T GetGlobalPlayer<T>(int globalID) where T : MonoBehaviour;
        public GameObject GetGlobalPlayer(int globalID);
        public T GetLocalPlayer<T>(int localPlayer) where T : MonoBehaviour;
        public GameObject GetLocalPlayer(int localID);
        public bool TryGetGlobalID(GameObject player, out int globalID);
        public bool TryGetLocalID(GameObject player, out int localID);
        public void RemoveLocalPlayer(int localID);
        public Task<PlayerAddedResultData> TryAddLocalPlayer();
    }
}