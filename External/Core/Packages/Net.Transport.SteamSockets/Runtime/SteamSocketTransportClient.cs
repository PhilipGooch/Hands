using NBG.Core.GameSystems;
using Steamworks;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace NBG.Net.Transport.SteamSockets
{
    public class SteamSocketClientConnectionManager : ConnectionManager
    {
        public SteamSocketTransportClient steamSocketTransportClient;

        public override void OnConnecting(ConnectionInfo data)
        {
            Debug.Log("OnConnecting");
            base.OnConnecting(data);

            steamSocketTransportClient.OnConnecting(data);
        }

        public override void OnConnected(ConnectionInfo data)
        {
            Debug.Log("OnConnected");
            base.OnConnected(data);

            steamSocketTransportClient.OnConnected(data);
        }

        public override void OnDisconnected(ConnectionInfo data)
        {
            Debug.Log("OnDisconnected");
            base.OnDisconnected(data);

            steamSocketTransportClient.OnDisconnected(data);
        }

        public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            //Debug.Log("OnMessage");
            base.OnMessage(data, size, messageNum, recvTime, channel);

            steamSocketTransportClient.OnMessage(data, size, messageNum, recvTime, channel);
        }
    }

    public class SteamSocketTransportClient : ITransportLayerUpdatable, INetTransportClient
    {
        private SteamSocketTransportPeer peer;
        public INetTransportPeer Peer => peer;
        public INetTransportPeerCallbacks Callbacks { get; set; }

        private SteamSocketClientConnectionManager connectionManager;
        private bool connected;
        private byte[] managedArray = new byte[SteamSocketTransportProvider.BufferSize];

        public async Task<INetTransportPeer> Connect(Lobby lobby)
        {
            bool success = await SteamLobbyManager.Join(lobby);
            if (success)
            {
                success = SteamLobbyManager.GetLobbyGameServerInfo(out var ip, out var port, out var serverId);
                Debug.Assert(success, $"Lobby is not a valid entity any longer");
                Debug.Log($"GET INFO: {ip} {port} {serverId}");

                if (serverId != 0)
                {
                    Debug.Log($"Trying to connect to steam relay server: {serverId}");
                    connectionManager = SteamNetworkingSockets.ConnectRelay<SteamSocketClientConnectionManager>(serverId);
                    connectionManager.steamSocketTransportClient = this;
                    peer = new SteamSocketTransportPeer(connectionManager.Connection);
                    peer.Callbacks = Callbacks;

                    return peer;
                }
                else if (ip != 0 && port != 0)
                {
                    var ipaddress = Utility.Int32ToIp(ip);
                    var netAddress = NetAddress.From(ipaddress, port);
                    Debug.Log($"Trying to connect to steam socket server: {netAddress.Address.ToString()}:{netAddress.Port}");
                    connectionManager = SteamNetworkingSockets.ConnectNormal<SteamSocketClientConnectionManager>(netAddress);
                    connectionManager.steamSocketTransportClient = this;
                    peer = new SteamSocketTransportPeer(connectionManager.Connection);
                    peer.Callbacks = Callbacks;

                    return peer;
                }
                else
                {
                    Debug.LogError($"Couldn't find ip or relay address.");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"Couldn't connect to steam lobby");
                return null;
            }
        }
        
        public void Shutdown(ConnectionClosedReason reason)
        {
            OnDisconnected(connectionManager.ConnectionInfo);
           
            SteamSocketTransportProvider.SteamShutdown();
        }

        void ITransportLayerUpdatable.OnUpdate()
        {
            if (connectionManager != null)
            {
                //without it, messages will not be received
                connectionManager.Receive();
            }

            if (peer != null)
            {
                peer.Update();
            }
        }

        #region ConnectionManager
        public void OnConnecting(ConnectionInfo data)
        {
            Debug.Log($"[SteamSocketsClient] {data.Identity} IConnectionManager OnConnecting");
        }

        public void OnConnected(ConnectionInfo data)
        {
            Debug.Log($"[SteamSocketsClient] {data.Identity} IConnectionManager OnConnected");

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Register(this);

            connected = true;
        }

        public void OnDisconnected(ConnectionInfo data)
        {
            if (!connected)
            {
                return;
            }

            Debug.Log($"[SteamSocketsClient] {data.Identity} IConnectionManager OnDisconnected");
            if (peer != null)
            {
                Peer.Disconnect(ConnectionClosedReason.Connection);//TODO: data.EndReason
                Callbacks.OnDisconnected(peer, ConnectionClosedReason.Connection);//TODO: data.EndReason
                peer = null;
            }

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Unregister(this);

            if (connectionManager != null)
            {
                connectionManager.Close();
            }

            connected = false;

            //TODO make server or single player?? destroy client
        }

        public void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            Marshal.Copy(data, managedArray, 0, size);
            peer.OnUnreliablePacket(managedArray, size);
        }
        #endregion
    }
}
