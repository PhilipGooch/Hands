using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NBG.Core;
using NBG.Core.Streams;
using UnityEngine;

namespace NBG.Net.Transport
{
    //todo: this class can be avoided probably with redesigning how sendstream is accessed. Remove it if possible
    public static class TransportProvider
    {
        internal const int BufferSize = 1024 * 1024; // Limit for reliable messages

        [ClearOnReload]
        public static IStreamShareable sendStream;

        public static void CreateStream()
        {
            if (sendStream == null)
            {
                sendStream = BasicStream.Allocate(BufferSize).MakeShareable();
            }
        }
    }
}