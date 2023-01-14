using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Transport.Sockets;
using System;
#if NBG_STEAM
using NBG.Net.Transport.SteamSockets;
#endif
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace CoreSample.Network
{
    public class SampleClients
    {
        public SocketTransportClient socketClient;
#if NBG_STEAM
        private SteamSocketTransportClient steamClient;
#endif

        public SampleClients(DebugConnectionMode connectionMode, ClientPlayerManager clientPlayerManager)
        {
            if (connectionMode == DebugConnectionMode.Sockets)
            {
                socketClient = SocketTransportProvider.CreateClient();
                ClientCallback.RegisterCallbacks(clientPlayerManager, socketClient);
            }
            else if (connectionMode == DebugConnectionMode.Steam || connectionMode == DebugConnectionMode.SteamSockets)
            {
#if NBG_STEAM
                SteamSocketTransportProvider.SteamInit(1388550);//Milkshake ID


                steamClient = SteamSocketTransportProvider.CreateClient();
                ClientCallback.RegisterCallbacks(clientPlayerManager, steamClient);
#else
                Debug.LogError("Not possible to create steam client");
#endif
            }
        }

        public async Task<INetTransportPeer> Connect(IPEndPoint connection)
        {
            if (socketClient != null)
            {
                var connectedPeer = await socketClient.Connect(connection);
                return connectedPeer;
            }
#if NBG_STEAM
            else if (steamClient != null)
            {
                //Steam connection is using matchmaking so pass just lobby.
                if (SteamLobbyManager.currentLobby.HasValue)
                {
                    var connectedPeer = await steamClient.Connect(SteamLobbyManager.currentLobby.Value);
                    return connectedPeer;
                }
                else
                {
                    Debug.LogError("No lobby selected");
                    return null;
                }
            }
#endif

            new NotImplementedException("Not implemented connection mode");
            return null;
        }

        public void ShutdownAll(ConnectionClosedReason reason)
        {
            if (socketClient != null)
            {
                socketClient.Shutdown(reason);
            }

#if NBG_STEAM
            if (steamClient != null)
            {
                steamClient.Shutdown(reason);
            }
#endif
        }
    }
}
