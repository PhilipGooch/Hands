using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NBG.Net.Transport.SteamSockets
{
    public static class LobbyExtension
    {
        public static string Info(this Lobby lobby)
        {
            var amMember = lobby.Members.Any(x => x.Id == SteamClient.SteamId);
            if (amMember)
            {
                return $"* {lobby.Owner.Name} ({lobby.Id}) {lobby.MemberCount}/{lobby.MaxMembers} [{string.Join(",", lobby.Members.ToArray())}]";
            }
            return $"- {lobby.Owner.Name} ({lobby.Id}) {lobby.MemberCount}/{lobby.MaxMembers} [{string.Join(",", lobby.Members.ToArray())}]"; ;
        }
    }

    public static class SteamLobbyManager
    {
        //private const string category = "Steam Lobby";
        private const string gameFilter = "Game";
        private const string gameFilterVal = "CoreSample";

        public static Lobby? currentLobby { get; set; }

        public static async Task<bool> HostLobby(int maximumPlayerNumber)
        {
            if (currentLobby.HasValue)
            {
                Debug.Log("You area already a member of a lobby: " + currentLobby.Value);
                return false;
            }
            currentLobby = await SteamMatchmaking.CreateLobbyAsync(maximumPlayerNumber);
            if (currentLobby == null)
            {
                throw new Exception("Failed to create lobby.");
            }
            currentLobby.Value.SetData(gameFilter, gameFilterVal);
            currentLobby.Value.SetPublic();
            currentLobby.Value.SetJoinable(true);

            return true;
        }

        public static bool SetLobbyInfoForJoin(SteamId steamId)
        {
            if (currentLobby.HasValue)
            {
                currentLobby.Value.SetGameServer(steamId);
                return true;
            }
            return false;
        }

        public static bool SetLobbyInfoForJoin(string ip, ushort port)
        {
            if (currentLobby.HasValue)
            {
                currentLobby.Value.SetGameServer(ip, port);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Currently just joins first room and that's it.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> Join(Lobby lobby)
        {
            var roomEnter = await lobby.Join();
            if (roomEnter == RoomEnter.Success)
            {
                currentLobby = lobby;
                return true;
            }
            Debug.LogWarning($"Failed to join Lobby due to {roomEnter}");
            return false;
        }

        public static bool GetLobbyGameServerInfo(out uint ip, out ushort port, out SteamId serverId)
        {
            ip = 0;
            port = 0;
            serverId = new SteamId();
            if (currentLobby.HasValue)
            {
                return currentLobby.Value.GetGameServer(ref ip, ref port, ref serverId);
            }
            return false;
        }

        public static void LeaveCurrentLobby()
        {
            if (!currentLobby.HasValue)
            {
                return;
            }
            currentLobby.Value.Leave();
            currentLobby = null;
        }

        public static async Task<List<Lobby>> RefreshList()
        {
            //In current use case, we might want to get the list before steamworks even is initialized (in reality it shouldn't happen)
            SteamSocketTransportProvider.SteamInit(1388550);//Milkshake ID

            var list = await SteamMatchmaking.LobbyList
                    .WithKeyValue(gameFilter, gameFilterVal)
                    .FilterDistanceWorldwide()
                    .WithMaxResults(25)
                    .RequestAsync();

            if (list == null)
            {
                return new List<Lobby>();
            }

            return list.ToList();
        }
    }
}
