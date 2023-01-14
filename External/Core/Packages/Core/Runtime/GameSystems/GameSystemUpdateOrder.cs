using System;
using UnityEngine.LowLevel;

namespace NBG.Core.GameSystems
{
    // Updating before or after a system constrains the scheduler ordering of these systems within a GameSystemGroup.
    // Both the before & after system must be a members of the same GameSystemGroup.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public UpdateBeforeAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    // Updating before or after a system constrains the scheduler ordering of these systems within a GameSystemGroup.
    // Both the before & after system must be a members of the same GameSystemGroup.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public UpdateAfterAttribute(Type systemType)
        {
            if (systemType == null)
                throw new ArgumentNullException(nameof(systemType));

            SystemType = systemType;
        }

        public Type SystemType { get; }
    }

    /// <summary>
    /// The specified Type must be a GameSystemGroup.
    /// Updating in a group means this system will be automatically updated by the specified GameSystemGroup when the group is updated.
    /// The system may order itself relative to other systems in the group with UpdateBefore and UpdateAfter. This ordering takes
    /// effect when the system group is sorted.
    ///
    /// If the optional OrderFirst parameter is set to true, this system will act as if it has an implicit [UpdateBefore] targeting all other
    /// systems in the group that do *not* have OrderFirst=true, but it may still order itself relative to other systems with OrderFirst=true.
    ///
    /// If the optional OrderLast parameter is set to true, this system will act as if it has an implicit [UpdateAfter] targeting all other
    /// systems in the group that do *not* have OrderLast=true, but it may still order itself relative to other systems with OrderLast=true.
    ///
    /// An UpdateInGroup attribute with both OrderFirst=true and OrderLast=true is invalid, and will throw an exception.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UpdateInGroupAttribute : Attribute
    {
        public bool OrderFirst = false;
        public bool OrderLast = false;

        public UpdateInGroupAttribute(Type groupType, bool orderFirst = false, bool orderLast = false)
        {
            if (groupType == null)
                throw new ArgumentNullException(nameof(groupType));

            GroupType = groupType;
            OrderFirst = orderFirst;
            OrderLast = orderLast;
        }

        public Type GroupType { get; }
    }

    public static class GameSystemUpdateOrder
    {
        static bool AppendSystemToPlayerLoopListImpl(GameSystemBase system, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType, bool front)
        {
            if (playerLoop.type == playerLoopSystemType)
            {
                int oldListLength = (playerLoop.subSystemList != null) ? playerLoop.subSystemList.Length : 0;
                var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                var entryLoc = front ? 0 : oldListLength;
                var copyOffset = front ? 1 : 0;
                for (var i = 0; i < oldListLength; ++i)
                    newSubsystemList[i + copyOffset] = playerLoop.subSystemList[i];
                newSubsystemList[entryLoc].type = system.GetType();
                newSubsystemList[entryLoc].updateDelegate = system.Update;
                playerLoop.subSystemList = newSubsystemList;
                return true;
            }
            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AppendSystemToPlayerLoopListImpl(system, ref playerLoop.subSystemList[i], playerLoopSystemType, front))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Add a  system to a specific point in the Unity player loop, so that it is updated every frame.
        /// </summary>
        /// <remarks>
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="system">The system to add to the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <param name="playerLoopSystemType">The Type of the PlayerLoopSystem subsystem to which the system should be appended.
        /// <param name="front">Add to front of list when true.</param>
        /// See the UnityEngine.PlayerLoop namespace for valid values.</param>
        public static void AppendSystemToPlayerLoopList(GameSystemBase system, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType, bool front = false)
        {
            if (!AppendSystemToPlayerLoopListImpl(system, ref playerLoop, playerLoopSystemType, front))
            {
                throw new ArgumentException($"Could not find PlayerLoopSystem with type={playerLoopSystemType}");
            }
        }
    }
}
