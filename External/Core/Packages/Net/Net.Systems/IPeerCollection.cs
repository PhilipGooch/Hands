using System.Collections.Generic;
using NBG.Net;

namespace NBG.Net.Systems
{
    /// <summary>
    /// Provides a List of Peers and send filters for BodyLists and BehaviourLists
    /// </summary>
    public interface IPeerCollection
    {
        /// <summary>
        /// List of ready peers that can receive frames and deltas
        /// </summary>
        /// <returns>Enumerator for all ready peers</returns>
        IEnumerable<INetTransportPeer> GetReadyPeers();

        /// <summary>
        /// Called for all body lists if they should be included in the current message 
        /// </summary>
        /// <param name="peer">The peer that will get those bodies</param>
        /// <param name="bodyListID">the bodies the pier will receive</param>
        /// <returns>true, if it should be included, else false</returns>
        bool WantsBodyList(INetTransportPeer peer, int bodyListID);

        /// <summary>
        /// Called for all behaviour lists if they should be included in the current message
        /// </summary>
        /// <param name="peer">The peer that will get those Behaviours</param>
        /// <param name="behaviourListID">The behaviour list ID that will be send to this peer</param>
        /// <returns>true if it should be included, else false</returns>
        bool WantsBehaviourList(INetTransportPeer peer, int behaviourListID);
    }
}