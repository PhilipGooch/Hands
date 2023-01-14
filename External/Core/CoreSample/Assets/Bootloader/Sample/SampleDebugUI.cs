using CoreSample.Network;
using NBG.DebugUI;
using NBG.DebugUI.View.uGUI;
#if NBG_STEAM
using NBG.Net.Transport.SteamSockets;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CoreSample.Base
{
#if UNITY_EDITOR
    class NetworkMenuItems
    {
        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSockets, true)]
        static bool EnableToggleNetworkServerSocketsValidate() => (!UnityEditor.EditorApplication.isPlaying);
        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSockets)]
        static void EnableToggleNetworkServerSockets()
        {
            var on = UnityEditor.Menu.GetChecked(SampleDebugUI.EnableToggleNetworkServerSockets);
            DisableItems();
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSockets, !on);
        }

        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSocketsNSteam, true)]
        static bool EnableToggleNetworkServerSocketsNSteamValidate() => (!UnityEditor.EditorApplication.isPlaying);
        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSocketsNSteam)]
        static void EnableToggleNetworkServerSocketsNSteam()
        {
            var on = UnityEditor.Menu.GetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteam);
            DisableItems();
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteam, !on);
        }

        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSocketsNSteamSockets, true)]
        static bool EnableToggleNetworkServerSocketsNSteamSocketsValidate() => (!UnityEditor.EditorApplication.isPlaying);
        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkServerSocketsNSteamSockets)]
        static void EnableToggleNetworkServerSocketsNSteamSockets()
        {
            var on = UnityEditor.Menu.GetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteamSockets);
            DisableItems();
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteamSockets, !on);
        }

        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkClientSockets, true)]
        static bool EnableToggleNetworkClientSocketsValidate() => (!UnityEditor.EditorApplication.isPlaying);
        [UnityEditor.MenuItem(SampleDebugUI.EnableToggleNetworkClientSockets)]
        static void EnableToggleNetworkClientSockets()
        {
            var on = UnityEditor.Menu.GetChecked(SampleDebugUI.EnableToggleNetworkClientSockets);
            DisableItems();
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkClientSockets, !on);
        }


        static void DisableItems()
        {
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSockets, false);
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteam, false);
            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkServerSocketsNSteamSockets, false);

            UnityEditor.Menu.SetChecked(SampleDebugUI.EnableToggleNetworkClientSockets, false);
        }
    }
#endif

    public class SampleDebugUI : MonoBehaviour
    {
        IDebugUI _debugUI;
        IDebugItem serverSocketsItem;
        IDebugItem serverSteamItem;
        IDebugItem serverSteamSocketsItem;

        IDebugItem socketsClientItem;

        IDebugItem steamSearchLobbies;
        IDebugItem steamClearLobbies;

        List<IDebugItem> lobbies = new List<IDebugItem>();

        IDebugItem stopNetworkItem;
        IDebugItem requestPlayerItem;
        IDebugItem releasePlayerItem;

        const string kNetCategory = "Network";
#if UNITY_EDITOR
        public const string EnableToggleNetworkServerSockets = "No Brakes Games/Networking/Test/Host (Sockets)";
        public const string EnableToggleNetworkServerSocketsNSteam = "No Brakes Games/Networking/Test/Host (Sockets n Steam)";
        public const string EnableToggleNetworkServerSocketsNSteamSockets = "No Brakes Games/Networking/Test/Host (Sockets n Steam Sockets)";

        public const string EnableToggleNetworkClientSockets = "No Brakes Games/Networking/Test/Join (Sockets)";
#endif

        static LogType LogTypeFromVerbosity(Verbosity verbosity)
        {
            switch (verbosity)
            {
                case Verbosity.Info:
                    return LogType.Log;
                case Verbosity.Warning:
                    return LogType.Warning;
                case Verbosity.Error:
                    return LogType.Error;
                default:
                    throw new System.NotImplementedException();
            }
        }

        void Awake()
        {
            _debugUI = NBG.DebugUI.DebugUI.Get();
            _debugUI.SetView(UGUIView.GetScreenSpace());
            _debugUI.OnPrint += (message, verbosity) =>
            {
                Debug.LogFormat(LogTypeFromVerbosity(verbosity), LogOption.NoStacktrace, null, message);
            };

            _debugUI.SetExtraInfoText("CORE SAMPLE PROJECT");

            NetGame.OnNetworkingStarted += OnNetworkingStarted;
            NetGame.OnNetworkingStopped += OnNetworkingStopped;
            NetGame.OnLocalMachinePlayerCountChanged += OnLocalMachinePlayerCountChanged;

            RegisterStartNetworkDebugItems();
            RegisterSearchLobbiesDebugItems();

            // Register scenes for opening
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; ++i)
            {
                var index = i;
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                if (path.Contains(Bootloader.BootloaderScene + ".unity"))
                    continue;

                _debugUI.RegisterAction($"Open {path}", "SCENES", () =>
                {
                    Bootloader.Instance.LoadScene(index);
                    _debugUI.Hide();
                });
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (UnityEditor.Menu.GetChecked(EnableToggleNetworkServerSockets))
            {
                HostSocketsOnly();
            }
            else if (UnityEditor.Menu.GetChecked(EnableToggleNetworkServerSocketsNSteam))
            {
                HostSocketsAndSteam();
            }
            else if (UnityEditor.Menu.GetChecked(EnableToggleNetworkServerSocketsNSteamSockets))
            {
                HostSocketsAndSteamSockets();
            }

            else if (UnityEditor.Menu.GetChecked(EnableToggleNetworkClientSockets))
            {
                ConnectToServerSockets();
            }
#endif
        }

        private void OnDestroy()
        {
            NetGame.OnNetworkingStopped -= OnNetworkingStopped;
            NetGame.OnNetworkingStarted -= OnNetworkingStarted;
            NetGame.OnLocalMachinePlayerCountChanged -= OnLocalMachinePlayerCountChanged;

            UnregisterStartNetworkDebugItems();
            UnregisterSearchLobbiesDebugItems();
            ClearLobbyList();
        }

        private void RegisterStartNetworkDebugItems()
        {
            if (serverSocketsItem == null)
            {
                serverSocketsItem = _debugUI.RegisterAction($"Start server (Sockets)", kNetCategory, HostSocketsOnly);
            }

            if (serverSteamItem == null)
            {
                serverSteamItem = _debugUI.RegisterAction($"Start server (Sockets and Remote Steam)", kNetCategory, HostSocketsAndSteam);
            }

            if (serverSteamSocketsItem == null)
            {
                serverSteamSocketsItem = _debugUI.RegisterAction($"Start server (Sockets and Local Steam)", kNetCategory, HostSocketsAndSteamSockets);
            }

            if (socketsClientItem == null)
            {
                socketsClientItem = _debugUI.RegisterAction($"Connect to server (Sockets)", kNetCategory, ConnectToServerSockets);
            }
        }

        private void UnregisterStartNetworkDebugItems()
        {
            if (serverSocketsItem != null)
            {
                _debugUI.Unregister(serverSocketsItem);
                serverSocketsItem = null;
            }

            if (serverSteamItem != null)
            {
                _debugUI.Unregister(serverSteamItem);
                serverSteamItem = null;
            }

            if (serverSteamSocketsItem != null)
            {
                _debugUI.Unregister(serverSteamSocketsItem);
                serverSteamSocketsItem = null;
            }

            if (socketsClientItem != null)
            {
                _debugUI.Unregister(socketsClientItem);
                socketsClientItem = null;
            }
        }

        private void RegisterSearchLobbiesDebugItems()
        {

            if (steamSearchLobbies == null)
            {
                steamSearchLobbies = _debugUI.RegisterAction($"Refresh Steam Lobby List", kNetCategory, RefreshLobbyList);
            }

            if (steamClearLobbies == null)
            {
                steamClearLobbies = _debugUI.RegisterAction($"Clear Lobby List", kNetCategory, ClearLobbyList);
            }
        }

        private void UnregisterSearchLobbiesDebugItems()
        {
            if (steamSearchLobbies != null)
            {
                _debugUI.Unregister(steamSearchLobbies);
                steamSearchLobbies = null;
            }

            if (steamClearLobbies != null)
            {
                _debugUI.Unregister(steamClearLobbies);
                steamClearLobbies = null;
            }
        }

        private void RegisterStopNetworkDebugItems()
        {
            if (stopNetworkItem == null)
            {
                stopNetworkItem = _debugUI.RegisterAction($"Close Connection", kNetCategory, CloseConnection);
            }
        }

        private void UnregisterStopNetworkDebugItems()
        {
            if (stopNetworkItem != null)
            {
                _debugUI.Unregister(stopNetworkItem);
                stopNetworkItem = null;
            }
        }

        private void RegisterRequestPlayerDebugItem()
        {
            if (requestPlayerItem == null)
            {
                requestPlayerItem = _debugUI.RegisterAction($"Request Player", kNetCategory, RequestPlayer);
            }
        }

        private void UnregisterRequestPlayerDebugItem()
        {
            if (requestPlayerItem != null)
            {
                _debugUI.Unregister(requestPlayerItem);
                requestPlayerItem = null;
            }
        }

        private void RegisterReleasePlayerDebugItem()
        {
            if (releasePlayerItem == null)
            {
                releasePlayerItem = _debugUI.RegisterAction($"Release Player", kNetCategory, ReleasePlayer);
            }
        }

        private void UnregisterReleasePlayerDebugItem()
        {
            if (releasePlayerItem != null)
            {
                _debugUI.Unregister(releasePlayerItem);
                releasePlayerItem = null;
            }
        }

        private void HostSocketsOnly()
        {
            NetGame.HostServer(DebugConnectionMode.Sockets);
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void HostSocketsAndSteam()
        {
            NetGame.HostServer(DebugConnectionMode.Steam);
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void HostSocketsAndSteamSockets()
        {
            NetGame.HostServer(DebugConnectionMode.SteamSockets);
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void ConnectToServerSockets()
        {
            NetGame.JoinServer(DebugConnectionMode.Sockets);
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

#if NBG_STEAM
        private void ConnectToServerSteam(Steamworks.Data.Lobby lobby)
        {
            SteamLobbyManager.currentLobby = lobby;
            NetGame.JoinServer(DebugConnectionMode.Steam);
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }
#endif

        private async void RefreshLobbyList()
        {
            ClearLobbyList();
#if NBG_STEAM
            var resultLobbies = await SteamLobbyManager.RefreshList();
            foreach (var lobby in resultLobbies)
            {
                var tmpLobby = lobby;
                IDebugItem item = _debugUI.RegisterAction($"{lobby.Info()}", kNetCategory, () =>
                {
                    ConnectToServerSteam(tmpLobby);
                });

                lobbies.Add(item);
            }
#endif
        }

        private void ClearLobbyList()
        {
            foreach (var lobby in lobbies)
            {
                _debugUI.Unregister(lobby);
            }
            lobbies.Clear();
        }

        private void CloseConnection()
        {
            NetGame.StopNetworking();
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void RequestPlayer()
        {
            NetGame.RequestPlayer();
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void ReleasePlayer()
        {
            NetGame.RequestReleasePlayer();
            if (_debugUI.IsVisible)
                _debugUI.Hide();
        }

        private void OnNetworkingStarted(bool isServer)
        {
            UnregisterStartNetworkDebugItems();
            UnregisterSearchLobbiesDebugItems();
            ClearLobbyList();
            RegisterStopNetworkDebugItems();
        }

        private void OnNetworkingStopped()
        {
            UnregisterStopNetworkDebugItems();
            RegisterStartNetworkDebugItems();
            RegisterSearchLobbiesDebugItems();
        }

        private void OnLocalMachinePlayerCountChanged(int playerCount, int maxCount)
        {
            if (playerCount > 0)
            {
                if (releasePlayerItem == null)
                    RegisterReleasePlayerDebugItem();
            }
            else
            {
                if (releasePlayerItem != null)
                    UnregisterReleasePlayerDebugItem();
            }

            if (maxCount > 0 && playerCount < maxCount)
            {
                if (requestPlayerItem == null)
                    RegisterRequestPlayerDebugItem();
            }
            else
            {
                if (requestPlayerItem != null)
                    UnregisterRequestPlayerDebugItem();
            }
        }

        void Update()
        {
            var toggle = false;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                toggle = keyboard.f11Key.wasReleasedThisFrame;
            }

            var pad = Gamepad.current;
            if (pad != null) // Joystick combo: (Cross + Square) then press and release Triangle
            {
                var joystickToggle = pad.crossButton.isPressed && pad.squareButton.isPressed && pad.triangleButton.wasReleasedThisFrame;
                toggle |= joystickToggle; // Apply
            }

            if (toggle)
            {
                if (!_debugUI.IsVisible)
                    _debugUI.Show();
                else
                    _debugUI.Hide();
            }

            if (!_debugUI.IsVisible)
                return;

            var input = new NBG.DebugUI.Input();
            if (keyboard != null)
            {
                input.up = Keyboard.current.upArrowKey.isPressed;
                input.down = Keyboard.current.downArrowKey.isPressed;
                input.left = Keyboard.current.leftArrowKey.isPressed;
                input.right = Keyboard.current.rightArrowKey.isPressed;
                input.categoryLeft = Keyboard.current.qKey.isPressed;
                input.categoryRight = Keyboard.current.eKey.isPressed;
                input.ok = Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed;
            }
            else if (pad != null)
            {
                input.up = pad.leftStick.up.isPressed;
                input.down = pad.leftStick.down.isPressed;
                input.left = pad.leftStick.left.isPressed;
                input.right = pad.leftStick.right.isPressed;
                input.categoryLeft = pad.leftShoulder.isPressed;
                input.categoryRight = pad.rightShoulder.isPressed;
                input.ok = pad.buttonSouth.isPressed;
            }

            _debugUI.Update(input);
        }
    }
}
