using System;
using UnityEngine;
using ReliableNetcode;
using NBG.Core.Streams;

namespace NBG.Net.Transport
{
    public class TransportPeer
    {
        public INetTransportPeerCallbacks Callbacks { get; set; }

        private INetTransportPeer transportPeer;

        protected IStreamShareable reliabilityStream = BasicStream.Allocate(ReliableDefines.MaxPacketSize).MakeShareable();
        protected readonly ReliableEndpoint _reliableEndpoint;

        public TransportPeer(INetTransportPeer transportPeer, Action<byte[], int> transmitCallback)
        {
            this.transportPeer = transportPeer;

            _reliableEndpoint = new ReliableEndpoint();
            _reliableEndpoint.ReceiveCallback = OnReliableEndpointReceive;
            _reliableEndpoint.TransmitCallback = transmitCallback;
        }

        public void Disconnect(ConnectionClosedReason reason)
        {

        }

        public void Update()
        {
            _reliableEndpoint.Update();
        }

        #region Send
        private ChannelType _currentSendChannelType;
        private IStream _currentSendStream;

        public IStreamWriter BeginSend(ChannelType channel)
        {
            _currentSendChannelType = channel;
            _currentSendStream = TransportProvider.sendStream.AcquireShare();
            _currentSendStream.Reset();
            return _currentSendStream;
        }

        public void AbortSend()
        {
            TransportProvider.sendStream.ReleaseShare();
            _currentSendStream = null;
        }

        public void EndSend()
        {
            Send(_currentSendChannelType, _currentSendStream);

            TransportProvider.sendStream.ReleaseShare();
            _currentSendStream = null;
        }

        public void Send(ChannelType channel, IStream stream)
        {
            ushort length = (ushort)stream.PositionBytes;
            if (channel == ChannelType.Reliable)
            {
                _reliableEndpoint.SendMessage(stream.Buffer, length, QosType.Reliable);
            }
            else if (channel == ChannelType.Unreliable)
            {
                _reliableEndpoint.SendMessage(stream.Buffer, length, QosType.Unreliable);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        #endregion


        #region Reliability layer
        public void OnUnreliablePacket(byte[] data, int length)
        {
            _reliableEndpoint.ReceivePacket(data, length);
        }

        private void OnReliableEndpointReceive(byte[] buffer, int size) 
        {
            // this will be called when the endpoint extracts messages from received packets
            // buffer is byte[] and size is number of bytes in the buffer.
            // do not keep a reference to buffer as it will be pooled after this function returns

            // Reliable message is length-framed (message size does not include header).
            var stream = reliabilityStream.AcquireShare();
            stream.Seek(0);
            stream.WriteArray(buffer, (ushort)size);
            stream.Flip();

            try
            {
                Callbacks.OnData(transportPeer, stream, ChannelType.Unreliable);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            reliabilityStream.ReleaseShare();
        }
        #endregion
    }
}
