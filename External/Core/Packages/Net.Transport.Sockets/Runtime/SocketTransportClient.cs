using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NBG.Core.GameSystems;
using Debug = UnityEngine.Debug;

namespace NBG.Net.Transport.Sockets
{
    public class SocketTransportClient : ITransportLayerUpdatable, INetTransportClient
    {
        public static bool enableLogEmptyPackages = false;
        private readonly byte[] readBufferUDP = new byte[2048];
        private Socket udp;
        private Socket tcp;
        private IAsyncResult taskReceiveUDP;
        //private EndPoint udpListenEndPoint;
        private EndPoint udpRemoteEndpoint;
        private SocketTransportPeer peer;

        public INetTransportPeer Peer => peer;
        public INetTransportPeerCallbacks Callbacks { get; set; }


        // Game calls this to create a client connection to a server
        public async Task<INetTransportPeer> Connect(IPEndPoint remoteEndPoint)
        {
            bool useIPv6 = remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6;

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Register(this);

            //Global timeout for the whole process
            var timeoutTask = Task.Delay(SocketTransportProvider.TcpConnectTimeoutMillis);
            // Establish a TCP connection
            tcp = SocketTransportProvider.CreateTcpSocket(useIPv6);
            {
                var connectTask = tcp.ConnectAsync(remoteEndPoint);

                await await Task.WhenAny(connectTask, timeoutTask);
                if (timeoutTask.IsCompleted)
                {
                    throw new TimeoutException($"TCP connect (remoteEndPoint = {remoteEndPoint}) timed out.");
                }

                if (connectTask.Status != TaskStatus.RanToCompletion)
                {
                    throw new Exception($"TCP connect (remoteEndPoint = {remoteEndPoint}) failed.");
                }
                Debug.Log($"TCP connected (remoteEndPoint = {remoteEndPoint}).");
            }
            var receiveBuffer = new byte[SocketTransportProvider.BufferSize];

            // (TCP) Receive the welcome message
            Guid guid = Guid.Empty;
            {
                var recieveTask = ReceiveTCPBytes(tcp, receiveBuffer, SocketTransportProvider.AuthMagic.Length + SocketTransportProvider.GuidLength);

                await await Task.WhenAny(recieveTask, timeoutTask);
                if (timeoutTask.IsCompleted)
                {
                    throw new TimeoutException($"TCP auth (remoteEndPoint = {remoteEndPoint}) timed out.");
                }

                var magic = new ArraySegment<byte>(receiveBuffer, 0, SocketTransportProvider.AuthMagic.Length);
                if (!ValidateMagic(magic, SocketTransportProvider.AuthMagic))
                {
                    throw new Exception($"TCP receive (remoteEndPoint = {remoteEndPoint}) failed: invalid auth magic.");
                }

                var guidBytes = new byte[SocketTransportProvider.GuidLength];
                Buffer.BlockCopy(receiveBuffer, SocketTransportProvider.AuthMagic.Length, guidBytes, 0, SocketTransportProvider.GuidLength);
                guid = new Guid(guidBytes);
                Debug.Log($"TCP auth received: {guid}");
            }

            udp = SocketTransportProvider.CreateUdpSocket(useIPv6, 0);
            //TODO: We cannot guarantee that the server is receiving on the same UDP port as tcp. This needs to be told by the server
            udpRemoteEndpoint = tcp.RemoteEndPoint;

            // (UDP) Authenticate by replying the welcome message
            // (TCP) Receive authentication acknowledgment
            {
                var guidBytes = guid.ToByteArray();

                var receiveTcpAck = ReceiveTCPBytes(tcp, receiveBuffer, SocketTransportProvider.AckMagic.Length);
                while (!receiveTcpAck.IsCompleted)
                {
                    udp.SendTo(guidBytes, udpRemoteEndpoint); // Keep replying until ack
                    await Task.Delay(SocketTransportProvider.UdpAuthResendInterval);
                }

                if (receiveTcpAck.Status != TaskStatus.RanToCompletion)
                {
                    throw new Exception($"TCP receive (remoteEndPoint = {remoteEndPoint}) failed @ ack.");
                }
                else
                {
                    var magic = new ArraySegment<byte>(receiveBuffer, 0, SocketTransportProvider.AckMagic.Length);
                    if (!ValidateMagic(magic, SocketTransportProvider.AckMagic))
                    {
                        throw new Exception($"TCP receive (remoteEndPoint = {remoteEndPoint}) failed: invalid ack magic.");
                    }
                    else
                    {
                        Debug.Log($"TCP ack received.");
                    }
                }
                taskReceiveUDP = udp.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpRemoteEndpoint, null, udp);
            }

            // Create a peer once authenticated
            peer = new SocketTransportPeer(null, this, tcp, udp, tcp.RemoteEndPoint);
            peer.Callbacks = Callbacks;
            return peer;
        }

        internal void Disconnect(SocketTransportPeer context, ConnectionClosedReason reason)
        {
            Peer.Disconnect(reason);
            peer.Callbacks.OnDisconnected(peer, ConnectionClosedReason.Connection);
            peer = null;
            //TODO make server or single player?? destroy client
        }

        void ITransportLayerUpdatable.OnUpdate()
        {
            //Recieve UDP Data
            if (taskReceiveUDP != null)
            {
                ReadUDP();
            }

            //Tick peer
            if (peer != null)
            {
                peer.Update();
            }
        }

        public void Shutdown(ConnectionClosedReason reason)
        {
            Debug.Log("[SocketTransportClient] Shutdown");

            //Disconnect
            if (peer != null)
            {
                Peer.Disconnect(reason);
                peer = null;
            }
            Callbacks.OnDisconnected(peer, reason);


            if (udp != null)
            {
                udp.Close();
                udp = null;
            }

            GameSystemWorldDefault.Instance.GetExistingSystem<TransportSystem>().Unregister(this);
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
                    int bytes = udpSocket.EndReceiveFrom(taskReceiveUDP, ref udpRemoteEndpoint);
                    peer.OnUnreliablePacket(readBufferUDP, bytes);
                    while (udpSocket.Available > 0)
                    {
                        bytes = udpSocket.ReceiveFrom(readBufferUDP, SocketFlags.None, ref udpRemoteEndpoint);
                        if (bytes > 0)
                        {
                            peer.OnUnreliablePacket(readBufferUDP, bytes);
                        }
                        else if (enableLogEmptyPackages)
                        {
                            Debug.Log("Read zero bytes from " + udpRemoteEndpoint);
                        }
                    }
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpRemoteEndpoint, null, udpSocket);
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
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpRemoteEndpoint, null, udpSocket);
                    Debug.Log("UDP listener restarted.");
                }
                catch (Exception ex)
                {
                    Debug.Log("UDP listener error. Restarting. " + ex.Message + " st: " + ex.StackTrace);
                    taskReceiveUDP = udpSocket.BeginReceiveFrom(readBufferUDP, 0, readBufferUDP.Length, SocketFlags.None, ref udpRemoteEndpoint, null, udpSocket);
                    Debug.Log("UDP listener restarted after generic exception.");
                }
            }
        }

        // Waits until a certain amount of data is received.
        private static async Task ReceiveTCPBytes(Socket socket, byte[] buffer, int bytesToReceive)
        {
            Debug.Assert(bytesToReceive <= buffer.Length);
            var offset = 0;
            while (offset < bytesToReceive)
            {
                var remaining = bytesToReceive - offset;
                var segment = new ArraySegment<byte>(buffer, offset, remaining);
                var bytesReceived = await socket.ReceiveAsync(segment, SocketFlags.None);
                offset += bytesReceived;
            }
        }
        private static bool ValidateMagic(ArraySegment<byte> buffer, byte[] magic)
        {
            Debug.Assert(buffer.Count == magic.Length);
            Debug.Assert(buffer.Array != null);
            for (int i = 0; i < buffer.Count; ++i)
            {
                if (buffer.Array[i] != magic[i])
                    return false;
            }
            return true;
        }
    }
}