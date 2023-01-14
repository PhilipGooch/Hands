using NBG.Core;
using NBG.Core.Streams;
using Steamworks;
using UnityEngine;

namespace NBG.Net.Transport.SteamSockets
{
    public class SteamSocketTransportProvider
    {
        public const ushort DefaultPort = 45222;
        internal const int BufferSize = 1024 * 1024; // Limit for reliable messages

        public static void SteamInit(uint appID = 480, bool asyncCallbacks = true)
        {
            if (SteamClient.IsValid)
            {
                return;
            }

            try
            {
                SteamClient.Init(appID, asyncCallbacks);
                SteamNetworkingUtils.InitRelayNetworkAccess();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }

            Dispatch.OnDebugCallback = (type, str, server) =>
            {
                //Debug.Log($"[Callback {type} {(server ? "server" : "client")} {str}]");
            };

            Dispatch.OnException = (e) =>
            {
                Debug.LogError(e.Message + " " + e.StackTrace);
            };
        }

        public static void SteamShutdown()
        {
            if (SteamClient.IsValid)
            {
                SteamClient.Shutdown();
            }
        }

        public static SteamSocketTransportServer CreateServer(ushort port)
        {
            TransportProvider.CreateStream();

            SteamSocketTransportServer steamServer = SteamNetworkingSockets.CreateNormalSocket<SteamSocketTransportServer>(Steamworks.Data.NetAddress.AnyIp(port));
            return steamServer;
        }

        public static SteamSocketTransportServer CreateServer()
        {
            TransportProvider.CreateStream();

            SteamSocketTransportServer steamServer = SteamNetworkingSockets.CreateRelaySocket<SteamSocketTransportServer>(0);
            return steamServer;
        }

        public static SteamSocketTransportClient CreateClient()
        {
            TransportProvider.CreateStream();

            SteamSocketTransportClient steamClient = new SteamSocketTransportClient();
            return steamClient;
        }
    }
}
