using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.PlayerManagement
{
    public interface IPlayerEvents
    {
        /// <summary>
        /// Player managers will call this to create a new player instance.
        /// You can return any gameObject you wish. The PlayerManager will use it
        /// together with globalID as a reference object for all callbacks.
        /// </summary>
        /// <param name="globalID"></param>
        /// <returns></returns>
        public GameObject CreateInstance(int globalID);
        /// <summary>
        /// Called before the player prefab is Destroyed.
        /// Do any unregistering, freeing of resources, etc here.
        /// </summary>
        /// <param name="playerObject">The gameObject that is about to be destroyed</param>
        public void DestroyInstance(GameObject playerObject, int globalID);
        /// <summary>
        /// Called after OnCreate if this player is a local player.
        /// This will be called on Server and on Client.
        /// Use this callback to assign any input or connect your player to any local user data
        /// </summary>
        /// <param name="playerObject">The gameObject for this player</param>
        /// <param name="globalID">The assigned globalID for this player</param>
        /// <param name="localID">The localID of this player</param>
        public void OnLocalPlayerAdded(GameObject playerObject, int globalID, int localID);
        /// <summary>
        /// Called before OnBeforeDestroy and only for local players
        /// use this to disconnect input or save any local player data.
        /// </summary>
        /// <param name="playerObject">The gameobject that is about to be destroyed</param>
        /// <param name="globalID">The assigned globalID for this player</param>
        /// <param name="localID">The localID of this player</param>
        public void OnLocalPlayerRemoved(GameObject playerObject, int globalID, int localID);
        /// <summary>
        /// Called only on Clients when the server spawns a player.
        /// Use this to parse any payload for created players. <see cref="OnPlayerCreatedBroadcast"/>
        /// </summary>
        /// <param name="playerObject">The gameObject for this player</param>
        /// <param name="globalID">The assigned globalID for this player</param>
        /// <param name="payload">payload stream. This will be what the server send in <see cref="OnPlayerCreatedBroadcast"/></param>
        public void OnClientRemotePlayerAdded(GameObject playerObject, int globalID, IStreamReader payload);
        /// <summary>
        /// Called only on Servers when the server spawns a player that was requested by a client
        /// 
        /// </summary>
        /// <param name="playerObject">The gameObject for this player</param>
        /// <param name="globalID">The assigned globalID</param>
        public void OnServerRemotePlayerAdded(GameObject playerObject, int globalID);
        /// <summary>
        /// Called whenever player input is received.
        /// NOTE: Input is send via UDP, might not be for all players, might arrive in bursts or out of order.
        /// </summary>
        /// <param name="playerObject">The gameObject this input is for</param>
        /// <param name="globalID">The assigned globalID</param>
        /// <param name="reader">payload stream</param>
        public void OnReceivePlayerInput(GameObject playerObject, int globalID, IStreamReader reader);
        /// <summary>
        /// Called every fixed update, if there are local players present.
        /// </summary>
        /// <param name="playerObject">the player for whom input is gathered</param>
        /// <param name="globalID">the assigned globalID</param>
        /// <param name="localID">the assigned localID</param>
        /// <param name="writer">payload stream</param>
        public void OnGatherPlayerInput(GameObject playerObject, int globalID, int localID, IStreamWriter writer);
        /// <summary>
        /// Called only on the Server
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="scopeID"></param>
        /// <param name="streamWriter"></param>
        public void OnPlayerCreatedBroadcast(GameObject instance, int scopeID, IStreamWriter streamWriter);
    }
}