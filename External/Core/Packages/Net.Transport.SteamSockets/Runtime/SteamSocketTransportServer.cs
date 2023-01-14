using NBG.Core.GameSystems;
using NBG.Core.Streams;
using NBG.Net.Transport.Sockets;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NBG.Net.Transport.SteamSockets
{
    public class SteamSocketTransportServer : SocketManager, ITransportLayerUpdatable, INetTransportServer
    {
        public INetTransportServerCallbacks Callbacks { get; set; }

        public IEnumerable<INetTransportPeer> Peers => _livePeers.Values;
        private Dictionary<Connection, SteamSocketTransportPeer> _livePeers = new Dictionary<Connection, SteamSocketTransportPeer>();

        private bool isListening;

        private BroadcastToPeers broadcastToPeers;

        private byte[] managedArray = new byte[SteamSocketTransportProvider.BufferSize];

        public SteamSocketTransportServer()
        {
            broadcastToPeers = new BroadcastToPeers();
        }

        public async void Listen(int maxPlayerAmount, bool relayServer)
        {
            bool success = await SteamLobbyManager.HostLobby(maxPlayerAmount);
            if (success)
            {
                if (SteamLobbyManager.currentLobby.HasValue)
                {
                    if (relayServer)
                    {
                        success = SteamLobbyManager.SetLobbyInfoForJoin(SteamClient.SteamId);
                        Debug.Assert(success, $"Couldn't set room info");
                    }
                    else
                    {
                        //connection for local player as remote players will use ownerUserId
                        //TODO: is this type of connection needed for acctual game and not testing? Is it possible to use only this
                        //with remote ip and still have connection without port forwarding n stuff?
                        var netAddress = NetAddress.From("127.0.0.1", SteamSocketTransportProvider.DefaultPort);
                        var addressStr = netAddress.Address.ToString();
                        success = SteamLobbyManager.SetLobbyInfoForJoin(addressStr, SteamSocketTransportProvider.DefaultPort);
                        Debug.Assert(success, $"Couldn't set room info");
                    }

                    GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Register(this);
                    isListening = true;
                }
            }
        }

        public void StopListen()
        {
            if (isListening)
            {
                GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Unregister(this);

                isListening = false;
            }

            SteamLobbyManager.LeaveCurrentLobby();
        }

        void ITransportLayerUpdatable.OnUpdate()
        {
            //wont receive onMessage callbacks without it
            Receive();

            //Tick peers
            foreach (var pair in _livePeers)
            {
                pair.Value.Update();
            }
        }

        public void Shutdown(ConnectionClosedReason reason)
        {
            StopListen();

            foreach (var pair in _livePeers)
            {
                pair.Value.Callbacks.OnDisconnected(pair.Value, reason);
            }
            _livePeers.Clear();

            foreach (var connection in Connecting)
            {
                connection.Close();
            }
            foreach (var connection in Connected)
            {
                connection.Close();
            }
            Close();

            SteamSocketTransportProvider.SteamShutdown();
        }

        #region SocketManager
        public override void OnConnecting(Connection connection, ConnectionInfo info)
        {
            Debug.Log($"[SteamSocketTransportServer] {info.Identity.SteamId} is connecting");

            if (isListening)
            {
                connection.Accept();
            }
            else
            {
                connection.Close();
            }
        }

        public override void OnConnected(Connection connection, ConnectionInfo info)
        {
            Debug.Log($"[SteamSocketTransportServer] {info.Identity} has joined the game");

            base.OnConnected(connection, info);

            var peer = new SteamSocketTransportPeer(connection);
            _livePeers.Add(connection, peer);

            Callbacks?.OnConnected(this, peer); 
        }

        public override void OnDisconnected(Connection connection, ConnectionInfo info)
        {
            Debug.Log($"[SteamSocketTransportServer] {info.Identity} is out of here");

            base.OnDisconnected(connection, info);

            if (_livePeers.TryGetValue(connection, out var peer))
            {
                peer.Callbacks?.OnDisconnected(peer, ConnectionClosedReason.Connection);
                _livePeers.Remove(connection);
            }
            else
            {
                Debug.Assert(false, "OnDisconnected Client which was never connected");
            }
        }

        public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
        {
            base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);

            if (_livePeers.TryGetValue(connection, out SteamSocketTransportPeer peer))
            {
                Marshal.Copy(data, managedArray, 0, size);
                peer.OnUnreliablePacket(managedArray, size);
            }
        }
        #endregion

        #region Broadcast
        IStreamWriter INetTransportServer.BeginBroadcast(ChannelType channel)
        {
            return broadcastToPeers.BeginBroadcast(channel);
        }
        void INetTransportServer.AbortBroadcast()
        {
            broadcastToPeers.AbortBroadcast();
        }
        void INetTransportServer.EndBroadcast()
        {
            broadcastToPeers.EndBroadcast(_livePeers.Values);
        }
        #endregion
    }
}