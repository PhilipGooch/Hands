using System;
using System.Collections.Generic;
using NBG.Core.Streams;
using NBG.Net;
using UnityEngine;

namespace CoreSample.Network
{
    internal class PeerCallback : INetTransportPeerCallbacks
    {
        public delegate void MsgHandler(INetTransportPeer context, ushort msgID, IStreamReader data, ChannelType channel);
        public delegate void DisconnectHandler(INetTransportPeer context, ConnectionClosedReason reason);
        private readonly Dictionary<ushort, MsgHandler> msgHandlers = new Dictionary<ushort, MsgHandler>();
        private DisconnectHandler disconnectHandler;

        public PeerCallback()
        {
        }

        void INetTransportPeerCallbacks.OnDisconnected(INetTransportPeer context, ConnectionClosedReason reason)
        {
            Debug.Log($"Context {context} just dis-connected with reason {reason} ");
            if (disconnectHandler != null)
            {
                try
                {
                    Debug.Log("[PeerCallback] Disconnected");
                    disconnectHandler.Invoke(context, reason);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error while handling Disconnect of {context} because {reason}");
                    Debug.LogException(ex);
                }
            }
            else
            {
                //This should always be registered, but if not, we print a message to help!
                Debug.Log($"Disconnect: {context} because {reason}");
            }
        }

        void INetTransportPeerCallbacks.OnData(INetTransportPeer context, IStreamReader data, ChannelType channel)
        {
            Debug.Assert(data.LimitBits > 0, $"OnData called with an empty {nameof(IStreamReader)}");

            while (data.BitsAvailable(16)) // need 16 bts for msg id
            {
                ushort msgID = data.ReadMsgId();
                //Debug.Log($"OnData({msgID})");

                if (!msgHandlers.TryGetValue(msgID, out MsgHandler handler))
                {
                    Debug.LogError($"Unhandled msgID {msgID} on channel {channel}");
                    return;
                }

                try
                {
                    if (handler == null)
                    {
                        Debug.LogWarning($"Unhandled msg {msgID} from {context}");
                    }
                    else
                    {
                        handler.Invoke(context, msgID, data, channel);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error while handling {msgID} on channel {channel}");
                    Debug.LogException(ex);
                    return;
                }
            }
        }

        public void Register(ushort msgID, MsgHandler handler)
        {
            if (!msgHandlers.TryGetValue((ushort)msgID, out MsgHandler existing))
            {
                msgHandlers.Add((ushort)msgID, handler);
            }
            else
            {
                msgHandlers[(ushort)msgID] = existing + handler;
            }
        }

        public void RegisterOnDisconnect(DisconnectHandler handler)
        {
            if (disconnectHandler == null)
            {
                disconnectHandler = handler;
            }
            else
            {
                disconnectHandler += handler;
            }
        }
    }
}
