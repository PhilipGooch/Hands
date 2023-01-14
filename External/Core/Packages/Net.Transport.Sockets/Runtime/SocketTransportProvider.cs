using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NBG.Core;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Transport.Sockets
{
    public static class SocketTransportProvider
    {
        public const int DefaultPort = 45221;
        internal const int BufferSize = 1024 * 1024; // Limit for reliable messages
        internal const int GuidLength = 16;

        /// <summary>
        /// Client Timeout for establishing a TCP connection and getting welcome message
        /// </summary>
        [ClearOnReload(value: 10000)]
        public static int TcpConnectTimeoutMillis { get; set; } = 10000;
        /// <summary>
        /// Intervall to resend UDP auth message, in case it was dropped
        /// </summary>
        [ClearOnReload(value: 300)]
        public static int UdpAuthResendInterval { get; set; } = 300;


        internal static readonly byte[] AuthMagic = Encoding.ASCII.GetBytes("NBG!");
        internal static readonly byte[] AckMagic = Encoding.ASCII.GetBytes("ACK!");

        public static SocketTransportServer CreateServer(bool allowIPv6)
        {
            TransportProvider.CreateStream();

            var ret = new SocketTransportServer(allowIPv6);
            return ret;
        }

        public static SocketTransportClient CreateClient()
        {
            TransportProvider.CreateStream();

            var ret = new SocketTransportClient();
            return ret;
        }

        internal static Socket CreateTcpSocket(bool ipv6)
        {
            Socket tcp;

            if (ipv6)
            {
                tcp = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                tcp.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            else
            {
                tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            //switching off Nagle -> Watch your write sizes.
            SetSocketOptionSafe(tcp, SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            return tcp;
        }

        internal static Socket CreateUdpSocket(bool ipv6, int port)
        {
            Socket udp;
            IPEndPoint endPoint;
            if (ipv6)
            {
                endPoint = new IPEndPoint(IPAddress.IPv6Any, port);
                udp = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                SetSocketOptionSafe(udp, SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            else
            {
                endPoint = new IPEndPoint(IPAddress.Any, port);
                udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            }

            SetSocketOptionSafe(udp, SocketOptionLevel.IP, SocketOptionName.DontFragment, true);
            SetSocketOptionSafe(udp, SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                udp.IOControl((IOControlCode)(-1744830452), new byte[] { 0, 0, 0, 0 }, null);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
#endif
            udp.Bind(endPoint);
            return udp;
        }

        internal static void SetSocketOptionSafe(Socket socket, SocketOptionLevel level, SocketOptionName name, bool value)
        {
            try
            {
                socket.SetSocketOption(level, name, value);
            }
            catch (Exception ex)
            {
                Debug.Log($"Failed to set socket option {level} {name} to {value}. Reason: {ex.Message}");
            }
        }
    }
}