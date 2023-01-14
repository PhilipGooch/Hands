using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NBG.Core.GameSystems;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Transport.Sockets
{
    public class SocketTransportServer : ITransportLayerUpdatable, INetTransportServer
    {
        private class ConnectingPeer
        {
            public float ConnectionStartTime; // Time.realtimeSinceStartup
            public Socket tcp;
        }

        readonly byte[] guidBuffer = new byte[16];

        public INetTransportServerCallbacks Callbacks { get; set; }
        public IEnumerable<INetTransportPeer> Peers => _livePeers.Values;

        private readonly bool useIPv6;
        private Socket listenTCP;
        private Task<Socket> taskAccept;


        private Dictionary<Guid, ConnectingPeer> _connectingPeers = new Dictionary<Guid, ConnectingPeer>();
        private Dictionary<EndPoint, SocketTransportPeer> _livePeers = new Dictionary<EndPoint, SocketTransportPeer>();
        private List<SocketTransportPeer> disconnectingPeers = new List<SocketTransportPeer>();
        private Socket udp;
        readonly byte[] readBufferUDP = new byte[2048];
        private IAsyncResult taskReceiveUDP;
        private EndPoint udpListenEndPoint;

        private BroadcastToPeers broadcastToPeers;

        internal SocketTransportServer(bool allowIPv6)
        {
            this.useIPv6 = allowIPv6 && Socket.OSSupportsIPv6;
            udpListenEndPoint = new IPEndPoint((useIPv6 ? IPAddress.IPv6Any : IPAddress.Any), 0);

            broadcastToPeers = new BroadcastToPeers();
        }

        internal void PeerDisconnected(SocketTransportPeer context, ConnectionClosedReason reason)
        {
            Debug.Log("[SocketTransportServer] Peer disconnected");
            disconnectingPeers.Add(context);
        }

        /// <summary>
        /// Update loop.
        /// Accepts new connections and reads new data from network.
        /// Peers update method will be called from here too.
        /// </summary>
        void ITransportLayerUpdatable.OnUpdate()
        {
            //Accept new Connections
            if (taskAccept != null)
            {
                ListenForConnections();
            }

            //Recieve UDP Data
            if (taskReceiveUDP != null)
            {
                ReadUDP();
            }

            //Tick peers
            foreach (var pair in _livePeers)
            {
                pair.Value.Update();
            }

            //Disconnect
            foreach (var disconnectingPeer in disconnectingPeers)
            {
                ((INetTransportPeer)disconnectingPeer).Disconnect(ConnectionClosedReason.Connection);
                _livePeers.Remove(disconnectingPeer.UdpRemoteEndPoint);
                disconnectingPeer.Callbacks.OnDisconnected(disconnectingPeer, ConnectionClosedReason.Connection);
            }
            disconnectingPeers.Clear();
        }

        /// <summary>
        /// Stops accepting new players and will disconnect existing players.
        /// This also disposes all sockets
        /// </summary>
        /// <param name="reason">Reason for Shutdown will be send to connecting players before disconnecting them</param>
        public void Shutdown(ConnectionClosedReason reason)
        {
            Debug.Log("[SocketTransportServer] Shutdown");
            StopListen();

            //disconnect
            foreach (var peer in Peers)
            {
                peer.Disconnect(reason);
            }

            foreach (var pair in _livePeers)
            {
                pair.Value.Callbacks.OnDisconnected(pair.Value, reason);
            }
            _livePeers.Clear();


            if (udp != null)
            {
                udp.Close();
                udp = null;
            }
        }

        /// <summary>
        /// Call this to start accepting connections
        /// </summary>
        /// <param name="port">The port you want to listen to. Defaults to SocketTransportServer.DefaultPort</param>
        /// <param name="listenBacklog">Size of the accept queue. Makes Syn-Flood attacks harder, for the cost of memory
        /// Console and Firewalled: 10, Public facing dedicated servers: 1k+</param>
        public void Listen(int port = SocketTransportProvider.DefaultPort, int listenBacklog = 10)
        {
            udp = SocketTransportProvider.CreateUdpSocket(useIPv6, SocketTransportProvider.DefaultPort);
            taskReceiveUDP = udp.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpListenEndPoint, null, udp);

            listenTCP = SocketTransportProvider.CreateTcpSocket(useIPv6);
            SocketTransportProvider.SetSocketOptionSafe(listenTCP, SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            SocketTransportProvider.SetSocketOptionSafe(listenTCP, SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            listenTCP.LingerState.Enabled = true;
            listenTCP.LingerState.LingerTime = 10;

            listenTCP.Bind(new IPEndPoint(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port));
            listenTCP.Listen(listenBacklog);
            taskAccept = listenTCP.AcceptAsync();

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Register(this);
        }

        /// <summary>
        /// Call this to stop accepting connections.
        /// Server continues to function but will not accept new players
        /// Use <see cref="Shutdown"/> to also disconnect players
        /// </summary>
        public void StopListen()
        {
            if (listenTCP != null)
            {
                listenTCP.Dispose();
                listenTCP = null;
            }

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Unregister(this);
        }

        private void ListenForConnections()
        {
            if (!taskAccept.IsCompleted)
                return;

            Socket socket = null;

            switch (taskAccept.Status)
            {
                case TaskStatus.RanToCompletion:
                    socket = taskAccept.Result;
                    break;
                case TaskStatus.Faulted:
                    //Check for exceptions
                    if (taskAccept.Exception != null && taskAccept.Exception.InnerException != null)
                    {
                        //If the socket was disposed (by stopListen) we cancel and don't restart accepts;
                        if (taskAccept.Exception.InnerException.GetType() == typeof(ObjectDisposedException))
                        {
                            taskAccept = null;
                            return;
                        }
                        Debug.LogException(taskAccept.Exception.InnerException);
                    }
                    break;
                case TaskStatus.Canceled:
                    //Task cannot be cancelled. But lets not restart here
                    taskAccept = null;
                    return;
                default:
                    throw new InvalidOperationException();
            }

            if (socket != null)
                AcceptConnection(socket);

            taskAccept = listenTCP.AcceptAsync();
        }

        private void AcceptConnection(Socket socket)
        {
            var connectingPeer = new ConnectingPeer();
            connectingPeer.ConnectionStartTime = Time.realtimeSinceStartup;
            connectingPeer.tcp = socket;

            // (TCP) Send welcome message (AuthMagic + GUID)
            var guid = Guid.NewGuid();
            _connectingPeers.Add(guid, connectingPeer);

            socket.Send(SocketTransportProvider.AuthMagic);
            socket.Send(guid.ToByteArray());
        }

        private void ReadUDP()
        {
            while (taskReceiveUDP != null && taskReceiveUDP.IsCompleted)
            {
                var udpSocket = taskReceiveUDP.AsyncState as Socket;
                if (udpSocket == null)
                {
                    throw new InvalidOperationException($"udp recieve task is missing socket as AsyncState object. AsyncState was {taskReceiveUDP.AsyncState}");
                }
                try
                {
                    EndPoint remoteUDPEndpoint = new IPEndPoint((useIPv6 ? IPAddress.IPv6Any : IPAddress.Any), 0);
                    int bytes = udpSocket.EndReceiveFrom(taskReceiveUDP, ref remoteUDPEndpoint);
                    if (_livePeers.TryGetValue(remoteUDPEndpoint, out SocketTransportPeer peer))
                    {
                        peer.OnUnreliablePacket(readBufferUDP, bytes);
                        //Deplete buffer fully, but only for authed peers
                        while (udpSocket.Available > 0)
                        {
                            bytes = udpSocket.ReceiveFrom(readBufferUDP, SocketFlags.None, ref remoteUDPEndpoint);
                            if (bytes > 0)
                            {
                                peer.OnUnreliablePacket(readBufferUDP, bytes);
                            }
                        }
                    }
                    else if (bytes == SocketTransportProvider.GuidLength)
                    {
                        Array.Copy(readBufferUDP, guidBuffer, guidBuffer.Length);
                        // Try to authenticate this connection
                        var guid = new Guid(guidBuffer);
                        if (_connectingPeers.TryGetValue(guid, out var cp))
                        {
                            _connectingPeers.Remove(guid);
                            // (TCP) Send ACK message
                            cp.tcp.Send(SocketTransportProvider.AckMagic);
                            // Create peer
                            var newPeer = new SocketTransportPeer(this, null, cp.tcp, udpSocket, remoteUDPEndpoint);
                            _livePeers.Add(remoteUDPEndpoint, newPeer);
                            Callbacks?.OnConnected(this, newPeer);
                        }
                        else
                        {
                            Debug.LogError($"Received {bytes} bytes from an unknown endpoint ({remoteUDPEndpoint}) over UDP.");
                        }
                    }
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpListenEndPoint, null, udpSocket);
                }
                catch (ObjectDisposedException)
                {
                    //Normal Behavior, listen socket was shutdown while in queue
                    //Debug.Log("UDP listener got disposed");
                    taskReceiveUDP = null;
                }
                catch (SocketException ex)
                {
                    //This Frequently happens when on Windows, when sending UDP that is refused by the other side
                    Debug.Log("UDP listener error. Restarting. " + ex.Message + " st: " + ex.StackTrace);
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpListenEndPoint, null, udpSocket);
                    Debug.Log("UDP listener restarted.");
                }
                catch (Exception ex)
                {
                    Debug.Log("UDP listener error. Restarting. " + ex.Message + " st: " + ex.StackTrace);
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpListenEndPoint, null, udpSocket);
                    Debug.Log("UDP listener restarted after generic exception.");
                }
            }
        }

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