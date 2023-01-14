using System.Collections.Generic;
using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Systems;
using NBG.Net.Transport.Sockets;
#if NBG_STEAM
using NBG.Net.Transport.SteamSockets;
#endif

namespace CoreSample.Network
{
    public class SamplePeers : IPeerCollection
    {
        public SocketTransportServer socketServer;
#if NBG_STEAM
        public SteamSocketTransportServer steamSocketsRelay;
        //public SteamSocketTransportServer steamSocketServerWithIP;
#endif

        public SamplePeers(DebugConnectionMode connectionMode, ServerCallback serverCallback, ServerPlayerManager serverPlayerManager)
        {
            socketServer = SocketTransportProvider.CreateServer(true);
            socketServer.Callbacks = serverCallback;
            serverPlayerManager.AddServer(socketServer);

#if NBG_STEAM
            if (connectionMode == DebugConnectionMode.Steam)
            {
                SteamSocketTransportProvider.SteamInit(1388550);//Milkshake ID

                steamSocketsRelay = SteamSocketTransportProvider.CreateServer();
                steamSocketsRelay.Callbacks = serverCallback;
                serverPlayerManager.AddServer(steamSocketsRelay);
            }
            else if (connectionMode == DebugConnectionMode.SteamSockets)
            {
                SteamSocketTransportProvider.SteamInit(1388550);//Milkshake ID

                steamSocketsRelay = SteamSocketTransportProvider.CreateServer(SteamSocketTransportProvider.DefaultPort);
                steamSocketsRelay.Callbacks = serverCallback;
                serverPlayerManager.AddServer(steamSocketsRelay);
            }
            //steamSocketServerWithIP = SteamSocketTransportProvider.CreateServer(SteamSocketTransportProvider.DefaultPort);
            //steamSocketServerWithIP.Callbacks = serverCallback;
            //serverPlayerManager.AddServer(steamSocketServerWithIP);  
#endif
        }

        public void Listen(int maximumPlayers, bool relayServer)
        {
            socketServer.Listen();

#if NBG_STEAM
            //if (steamSocketServerWithIP != null)
            //{
            //    steamSocketServerWithIP.Listen(maximumPlayers);
            //}
            if (steamSocketsRelay != null)
            {
                steamSocketsRelay.Listen(maximumPlayers, relayServer);
            }
#endif
        }

        public IEnumerable<INetTransportPeer> GetReadyPeers()
        {
            if (socketServer != null)
            {
                foreach (var socketServerPeer in socketServer.Peers)
                {
                    yield return socketServerPeer;
                }
            }
#if NBG_STEAM
            //if (steamSocketServerWithIP != null)
            //{
            //    foreach (var steamSocketServerPeer in steamSocketServerWithIP.Peers)
            //    {
            //        yield return steamSocketServerPeer;
            //    }
            //}
            if (steamSocketsRelay != null)
            {
                foreach (var steamSocketServerPeer in steamSocketsRelay.Peers)
                {
                    yield return steamSocketServerPeer;
                }
            }
#endif
        }

        public void ShutdownAll(ConnectionClosedReason reason)
        {
            if (socketServer != null)
            {
                socketServer.Shutdown(reason);
            }

#if NBG_STEAM
            //if (steamSocketServerWithIP != null)
            //{
            //    steamSocketServerWithIP.Shutdown(reason);
            //}
            if (steamSocketsRelay != null)
            {
                steamSocketsRelay.Shutdown(reason);
            }
#endif
        }

        public bool WantsBodyList(INetTransportPeer peer, int bodyListID)
        {
            return true;
        }

        public bool WantsBehaviourList(INetTransportPeer peer, int behaviourListID)
        {
            return true;
        }
    }
}
