using NBG.Core.Streams;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Net.Transport.Sockets
{
    public class BroadcastToPeers
    {
        #region Broadcast
        private ChannelType _currentBroadcastChannelType;
        private IStream _currentSendStream;

        public IStreamWriter BeginBroadcast(ChannelType channel)
        {
            _currentBroadcastChannelType = channel;
            _currentSendStream = TransportProvider.sendStream.AcquireShare();
            _currentSendStream.Reset();
            return _currentSendStream;
        }

        public void AbortBroadcast()
        {
            TransportProvider.sendStream.ReleaseShare();
            _currentSendStream = null;
        }

        public void EndBroadcast(IEnumerable<INetTransportPeer> transportPeers)
        {
            foreach (var peer in transportPeers)
            {
                peer.Send(_currentBroadcastChannelType, _currentSendStream);
            }
            TransportProvider.sendStream.ReleaseShare();
            _currentSendStream = null;
        }
        #endregion
    }
}
