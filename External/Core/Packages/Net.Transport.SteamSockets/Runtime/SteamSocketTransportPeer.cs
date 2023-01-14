using NBG.Core.Streams;
using ReliableNetcode;
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;

namespace NBG.Net.Transport.SteamSockets
{
    internal class SteamSocketTransportPeer : INetTransportPeer
    {
        public INetTransportPeerCallbacks Callbacks
        {
            get => transportPeer.Callbacks;
            set { transportPeer.Callbacks = value; }
        }

        private Connection connection;

        private TransportPeer transportPeer;

        internal SteamSocketTransportPeer(Connection connection) : base()
        {
            Debug.Log($"[SteamSocketTransportPeer] created with id: {connection.Id}");

            this.connection = connection;

            transportPeer = new TransportPeer(this, OnReliableEndpointTransmit);
        }

        void INetTransportPeer.Disconnect(ConnectionClosedReason reason)
        {

        }

        private void OnReliableEndpointTransmit(byte[] buffer, int size)
        {
            // this will be called when a datagram is ready to be sent across the network.
            // buffer is byte[] and size is number of bytes in the buffer
            // do not keep a reference to the buffer as it will be pooled after this function returns

            try
            {
                var sent = connection.SendMessage(buffer, 0, size, SendType.Unreliable);
                //If no connection is present, then disconnect event is already pending, but is not received yet
                if (!(sent == Result.OK || sent == Result.NoConnection))
                {
                    Debug.Log(sent.ToString());
                    throw new System.Exception($"[SteamSocketTransportPeer] UDP Send(endPoint = {connection.ConnectionName}:{connection.Id}) failed.");
                }

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
            transportPeer.Update();
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
