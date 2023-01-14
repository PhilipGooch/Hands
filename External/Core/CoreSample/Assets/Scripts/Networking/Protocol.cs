using System.Collections.Generic;
using System.Reflection;
using NBG.Core;
using NBG.Net;
using NBG.Net.PlayerManagement;
using NBG.Net.Systems;
using UnityEngine;

namespace CoreSample.Network
{
    public class Protocol
    {
        private const ushort reserved = 2;
        [ClearOnReload(value: reserved)]
        private static ushort currentID = reserved;

        /// <summary>
        /// ⚡⚡⚡⚡⚡ READ ME ⚡⚡⚡⚡⚡
        /// Game is in charge of assigning protocol ID's
        /// When making a new game, copy this file and strip what you don't need
        ///
        /// Rules to maintain backwards compatibility:
        /// - IDs stay assigned forever. If they get removed, keep the "currentID++"
        /// - New IDs go at the end (when making game messages or adding a new library)
        /// - Do not re-order or sort these, unless you want to break protocol
        /// - Message 1 and 2 are reserved, for really drastic protocol changes
        /// </summary>
        public static void Init()
        {
            //If you get this assert, check bugs around domain reloading. Init() need to be called only once
            Debug.Assert(currentID == reserved, "Protocol was already initialized");

            NetBehaviourListProtocol.MasterFrame = currentID++;
            NetBehaviourListProtocol.DeltaFrame = currentID++;
            NetBehaviourListProtocol.FrameAck = currentID++;
            //Events
            NetEventBusProtocol.Events = currentID++;
            //Player Input
            PlayerManagementProtocol.PlayerInput = currentID++;
            PlayerManagementProtocol.PlayerInputAck = currentID++;
            //Player Management
            PlayerManagementProtocol.PlayerAdded = currentID++;
            PlayerManagementProtocol.PlayerRemoved = currentID++;
            PlayerManagementProtocol.RequestPlayer = currentID++;
            PlayerManagementProtocol.RemovePlayer = currentID++;
            PlayerManagementProtocol.RequestPlayerSuccess = currentID++;
            PlayerManagementProtocol.RequestPlayerFailed = currentID++;
            //Project specific
            ProjectSpecificProtocol.LevelLoad = currentID++;
            ProjectSpecificProtocol.LevelLoadAck = currentID++;

            ValidateAll();
        }

        private static void ValidateAll()
        {
            var assigned = new HashSet<int>();
            var types = NBG.Core.AssemblyUtilities.GetAllTypesWithAttribute(typeof(ProtocolAttribute));
            foreach (var protocol in types)
            {
                var allField = protocol.GetFields(BindingFlags.Static | BindingFlags.Static);
                foreach (var fieldInfo in allField)
                {
                    var id = (int)fieldInfo.GetValue(null);
                    if (id < reserved)
                    {
                        Debug.LogError($"Reserved or unassigned message id {id} for protocol {protocol.FullName}");
                    }
                    if (assigned.Contains(id))
                    {
                        Debug.LogError($"Conflicting message id {id} for protocol {protocol.FullName}");
                    }
                    assigned.Add(id);
                }
            }
        }
    }
}
