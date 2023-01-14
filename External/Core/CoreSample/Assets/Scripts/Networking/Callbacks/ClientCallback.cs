using CoreSample.Base;
using NBG.Core.GameSystems;
using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Systems;
using UnityEngine;

namespace CoreSample.Network
{
    internal class ClientCallback
    {
        public static void RegisterCallbacks(ClientPlayerManager playerManager, INetTransportClient transportClient)
        {
            var callbacks = new PeerCallback();


            var clientSystem = GameSystemWorldDefault.Instance.GetExistingSystem<NetReadAndApplyFrame>();
            //var netEventBus = GameSystemWorldDefault.Instance.GetExistingSystem<NetEventBus>();


            callbacks.RegisterOnDisconnect((context, reason) =>
            {
                playerManager.RemoveRemotePlayers();
                Debug.Log("[ClientCallback] Disconnect");
                Debug.LogWarning("Disconnect. Cleanup NYI!");
            });


            
            callbacks.Register(NetBehaviourListProtocol.MasterFrame, (peer, msgID, data, channel) =>
            {
                //Debug.Log("[Client] Received NetBehaviourListProtocol.MasterFrame");
                Debug.Assert(msgID == NetBehaviourListProtocol.MasterFrame);
                int ackID = clientSystem.InsertStream(data);
                var ackStream = peer.BeginSend(ChannelType.Unreliable);
                ackStream.WriteMsgId(NetBehaviourListProtocol.FrameAck);
                ackStream.WriteFrameId(ackID);
                peer.EndSend();
            });

            callbacks.Register(NetBehaviourListProtocol.DeltaFrame, (peer, msgID, data, channel) =>
            {
                //Debug.Log("[Client] Received NetBehaviourListProtocol.DeltaFrame");
                Debug.Assert(msgID == NetBehaviourListProtocol.DeltaFrame);
                int ackID = clientSystem.InsertStream(data);
                var ackStream = peer.BeginSend(ChannelType.Unreliable);
                ackStream.WriteMsgId(NetBehaviourListProtocol.FrameAck);
                ackStream.WriteFrameId(ackID);
                peer.EndSend();
            });

            
            callbacks.Register(NetEventBusProtocol.Events, (peer, msgID, data, channel) =>
            {
                //Debug.Log($"[ClientReceived: Events[{NetEventBusProtocol.Events}]:unknown");
                Debug.Assert(msgID == NetEventBusProtocol.Events);
                clientSystem.TempProcessNetEventBus(data);
            });

            callbacks.Register(PlayerManagementProtocol.PlayerAdded, (peer, msgID, data, channel) =>
            {
                Debug.Log($"[ClientReceived: PlayerAdded[{PlayerManagementProtocol.PlayerAdded}]:unknown");
                playerManager.ReceivePlayerAdded(peer, data);
            });

            callbacks.Register(PlayerManagementProtocol.PlayerRemoved, (peer, msgID, data, channel) =>
            {
                Debug.Log($"[ClientReceived: PlayerRemoved[{PlayerManagementProtocol.PlayerRemoved}]:unknown");
                playerManager.ReceivePlayerRemoved(peer, data);
            });

            callbacks.Register(PlayerManagementProtocol.RequestPlayerFailed, (peer, msgID, data, channel) =>
            {
                Debug.Log($"[ClientReceived: RequestPlayerFailed[{PlayerManagementProtocol.RequestPlayerFailed}]:unknown");
                playerManager.RecieveRequestPlayerFailed(peer, data);
            });

            callbacks.Register(PlayerManagementProtocol.RequestPlayerSuccess, (peer, msgID, data, channel) =>
            {
                Debug.Log($"[ClientReceived: RequestPlayerSuccess[{PlayerManagementProtocol.RequestPlayerSuccess}]:unknown");
                playerManager.ReceiveRequestPlayerSucess(peer, data);
            });

            callbacks.Register(ProjectSpecificProtocol.LevelLoad, (peer, msgID, data, channel) =>
            {
                int levelIndex = data.ReadByte();
                Debug.Log($"[ClientReceived: LevelLoad[{ProjectSpecificProtocol.LevelLoad}]:{levelIndex}");

                Bootloader.Instance.LoadScene(levelIndex);

                // Ack
                var stream = peer.BeginSend(ChannelType.Reliable);
                stream.WriteMsgId(ProjectSpecificProtocol.LevelLoadAck);
                stream.Write((byte)levelIndex);
                peer.EndSend();
            });

            transportClient.Callbacks = callbacks;
        }
    }
}
