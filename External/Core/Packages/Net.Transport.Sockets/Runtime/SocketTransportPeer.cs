using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using NBG.Core.Streams;

namespace NBG.Net.Transport.Sockets
{
    //TODO: after removing tcp code check if all callbacks are comming thought. Especially on data and ondisconnect
    internal class SocketTransportPeer : INetTransportPeer
    {
        public IPEndPoint UdpRemoteEndPoint => udpEndPoint as IPEndPoint;

        public INetTransportPeerCallbacks Callbacks
        {
            get => transportPeer.Callbacks;
            set { transportPeer.Callbacks = value; }
        }

        private readonly Socket udp;
        private readonly EndPoint udpEndPoint;

        //all these tcp things is just for listening for disconnect, no acctual sending is done
        SocketTransportServer server;
        SocketTransportClient client;
        private readonly Socket tcp;
        private IStream tcpStream;
        private IAsyncResult taskRecvTcp;

        private TransportPeer transportPeer;

        internal SocketTransportPeer(SocketTransportServer server, SocketTransportClient client, Socket tcp, Socket udp, EndPoint udpEndPoint, byte[] tcpBuffer = null) : base()
        {
            this.udp = udp;
            this.udpEndPoint = udpEndPoint;

            this.server = server;
            this.client = client;
            this.tcp = tcp;
            tcpStream = tcpBuffer != null ? BasicStream.AllocateFromBuffer(tcpBuffer) : BasicStream.Allocate(SocketTransportProvider.BufferSize);
            taskRecvTcp = tcp.BeginReceive(tcpStream.Buffer, 0, 2, SocketFlags.None, out SocketError errorCode, null, tcp);

            transportPeer = new TransportPeer(this, OnReliableEndpointTransmit);
        }

        void INetTransportPeer.Disconnect(ConnectionClosedReason reason)
        {
            tcp.Close();
            tcpStream = null;
            taskRecvTcp = null;
        }

        private void OnReliableEndpointTransmit(byte[] buffer, int size)
        {
            // this will be called when a datagram is ready to be sent across the network.
            // buffer is byte[] and size is number of bytes in the buffer
            // do not keep a reference to the buffer as it will be pooled after this function returns

            try
            {
                var sent = udp.SendTo(buffer, 0, size, SocketFlags.None, udpEndPoint);
                if (sent != size)
                    throw new System.Exception($"UDP Send(endPoint = {udpEndPoint}) failed.");

                // Statistics //TODO@NET: Transport lib should have API to return stats
                //var netGameInst = NBG.Net.NetGame.instance;
                //netGameInst?.sendBps.ReportBits(size * 8);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        internal void Update()
        {
            if (taskRecvTcp != null && taskRecvTcp.IsCompleted)
            {
                ReadTcp();
            }
            transportPeer.Update();
        }

        private void ReadTcp()
        {
            //all these tcp things is just for listening for disconnect, no acctual sending is done
            while (taskRecvTcp != null && taskRecvTcp.IsCompleted)
            {
                var tcpSocket = taskRecvTcp.AsyncState as Socket;
                if (tcpSocket == null)
                {
                    throw new InvalidOperationException($"tcp recieve task is missing socket as AsyncState object. AsyncState was {taskRecvTcp.AsyncState}");
                }

                try
                {
                    int bytes = tcpSocket.EndReceive(taskRecvTcp);
                    tcpStream.Seek(tcpStream.PositionBits + bytes * 8);
                    //Handle disconnect case: 
                    if (bytes == 0)
                    {
                        Debug.Log($"Recieved {bytes} bytes for disconnect");
                        if (server != null)
                        {
                            server.PeerDisconnected(this, ConnectionClosedReason.Connection);
                        }
                        else
                        {
                            client.Disconnect(this, ConnectionClosedReason.Connection);
                        }
                        taskRecvTcp = null;
                    }
                }
                catch (ObjectDisposedException)
                {
                    //Normal Behavior, listen socket was shutdown while in queue
                    //Debug.Log("UDP listener got disposed");
                    taskRecvTcp = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    //Socket errors and other Exceptions are treated as 
                    if (server != null)
                    {
                        server.PeerDisconnected(this, ConnectionClosedReason.Connection);
                    }
                    else
                    {
                        client.Disconnect(this, ConnectionClosedReason.Connection);
                    }
                    taskRecvTcp = null;
                }
            }
        }

        internal void OnUnreliablePacket(byte[] data, int length)
        {
            transportPeer.OnUnreliablePacket(data, length);
        }

        IStreamWriter INetTransportPeer.BeginSend(ChannelType channel)
        {
            return transportPeer.BeginSend(channel);
        }

        void INetTransportPeer.AbortSend()
        {
            transportPeer.AbortSend();
        }

        void INetTransportPeer.EndSend()
        {
            transportPeer.EndSend();
        }

        void INetTransportPeer.Send(ChannelType channel, IStream stream)
        {
            transportPeer.Send(channel, stream);
        }
    }
}
