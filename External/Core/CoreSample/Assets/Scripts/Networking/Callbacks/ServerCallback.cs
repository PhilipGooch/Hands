using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoreSample.Network
{
    /// <summary>
    /// Example Callback implementation.
    /// Registers MsgIDs and other callbacks.
    /// </summary>
    public class ServerCallback : INetTransportServerCallbacks
    {
        private readonly ServerPlayerManager serverPlayerManager;

        public ServerCallback(ServerPlayerManager serverPlayerManager)
        {
            this.serverPlayerManager = serverPlayerManager;
        }

        public void OnConnected(INetTransportServer self, INetTransportPeer peer)
        {
            var callbacks = new PeerCallback();


            Debug.Log($"Context {peer} just connected to server {self}");

            var collectSystem = GameSystemWorldDefault.Instance.GetExistingSystem<NetWriteAndSendFrame>();


            callbacks.RegisterOnDisconnect((INetTransportPeer context, ConnectionClosedReason reason) =>
            {
                Debug.Log("[ServerCallback] Disconnect");
                //Notify ServerPlayerManager that a Peer has disconnected
                serverPlayerManager.PeerDisconnected(context);
                GameSystemWorldDefault.Instance.GetExistingSystem<NetWriteAndSendFrame>().PeerDisconnected(context);//TODO: why doesn't this handle peer disconnections internally in networking lib?
                // Broadcast NetEvents that a Peer has disconnected
                GameSystemWorldDefault.Instance.GetExistingSystem<NetEventBus>().PeerDisconnected(context); //TODO: why doesn't this handle peer disconnections internally in networking lib?
            }); //When it disconnects again, we want to clean up and inform




            callbacks.Register(NetBehaviourListProtocol.FrameAck, (context, msgID, data, channel) =>
            {
                //Debug.Log($"[ServerReceived] FrameAck[{NetBehaviourListProtocol.FrameAck}]");
                collectSystem.DeltaAck(context, data);
            });

            //callbacks.Register(ObjectSyncProtocol.EventAck, (context, msgID,data, channel)  => Debug.LogWarning("DeltaACK NYI!"));
            callbacks.Register(PlayerManagementProtocol.RequestPlayer, (context, msgID, data, channel) =>
            {
                Debug.Log($"[ServerReceived: RequestPlayer[{PlayerManagementProtocol.RequestPlayer}]:unknown");
                serverPlayerManager.ReceiveRequestPlayer(context, data); 
            });

            callbacks.Register(PlayerManagementProtocol.RemovePlayer, (context, msgID, data, channel) =>
            {
                Debug.Log($"[ServerReceived: RemovePlayer[{PlayerManagementProtocol.RemovePlayer}]:unknown");
                serverPlayerManager.ReceiveRemovePlayer(context, data);
            });

            callbacks.Register(PlayerManagementProtocol.PlayerInput, (context, msgID, data, channel) =>
            {
                serverPlayerManager.ReceivePlayerInput(context, data);
            });

            callbacks.Register(ProjectSpecificProtocol.LevelLoadAck, (context, id, data, channel) =>
            {
                var levelIndex = data.ReadByte();
                Debug.Log($"[ServerReceived: LevelLoadAck[{ProjectSpecificProtocol.LevelLoadAck}]:{levelIndex}");

                //serverPlayerManager.NotifyAboutPlayers(context);

                var busEvent = new NewPeerIsReadyEvent(GameSystemWorldDefault.Instance.GetExistingSystem<NetEventBus>(), peer);
                EventBus.Get().Send(busEvent);
            });

            peer.Callbacks = callbacks;

            //Notify ServerPlayerManager that a peer has connected
            serverPlayerManager.PeerConnected(peer);

            //You can send additional Welcome Information (Like level, etc) here

            byte levelIndex = (byte)SceneManager.GetActiveScene().buildIndex;
            Debug.Log($"[Server] Send level index {levelIndex}");
            var stream = peer.BeginSend(ChannelType.Reliable);
            stream.WriteMsgId(ProjectSpecificProtocol.LevelLoad);
            stream.Write(levelIndex);
            peer.EndSend();
        }
    }
}
