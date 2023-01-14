using NBG.Core.GameSystems;
using NBG.Core.Streams;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace NBG.Net
{
    public enum ChannelType
    {
        Reliable,
        Unreliable
    }

    public interface INetTransportPeerCallbacks
    {
        public void OnData(INetTransportPeer self, IStreamReader data, ChannelType channel);
        public void OnDisconnected(INetTransportPeer self, ConnectionClosedReason reason);
    }

    public interface INetTransportPeer : IPeer
    {
        INetTransportPeerCallbacks Callbacks { get; set; }

        // Server this peer belongs to. Null if it is a client peer.
        //INetTransportServer Server { get; }
        void Disconnect(ConnectionClosedReason reason);

        IStreamWriter BeginSend(ChannelType channel);
        void AbortSend();
        void EndSend();
        void Send(ChannelType channel, IStream stream);
    }

    public interface INetTransportServerCallbacks
    {
        public void OnConnected(INetTransportServer self, INetTransportPeer peer);
    }

    public interface INetTransportServer
    {
        INetTransportServerCallbacks Callbacks { get; set; }

        // List of peers connected to this server.
        IEnumerable<INetTransportPeer> Peers { get; }

        IStreamWriter BeginBroadcast(ChannelType channel);
        void AbortBroadcast();
        void EndBroadcast();
        void Shutdown(ConnectionClosedReason reason);
    }

    public interface INetTransportClient
    {
        INetTransportPeerCallbacks Callbacks { get; set; }
        INetTransportPeer Peer { get; }
    }


    [UpdateInGroup(typeof(EarlyUpdateSystemGroup))]
    public class NetTransportsSystemGroup : GameSystemGroup // Helper group
    {
        [Preserve]
        public NetTransportsSystemGroup()
        {
        }
    }
}
