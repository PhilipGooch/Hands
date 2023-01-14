using System;
using UnityEngine.LowLevel;

namespace NBG.Core
{
    internal static class LowLevelPlayerLoopUtilities
    {
        static bool AppendToPlayerLoopListImpl(Type systemType, PlayerLoopSystem.UpdateFunction systemDelegate, ref PlayerLoopSystem playerLoop, Type targetPlayerLoopSystemType, bool front)
        {
            if (playerLoop.type == targetPlayerLoopSystemType)
            {
                int oldListLength = (playerLoop.subSystemList != null) ? playerLoop.subSystemList.Length : 0;
                var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                var entryLoc = front ? 0 : oldListLength;
                var copyOffset = front ? 1 : 0;
                for (var i = 0; i < oldListLength; ++i)
                    newSubsystemList[i + copyOffset] = playerLoop.subSystemList[i];
                newSubsystemList[entryLoc].type = systemType;
                newSubsystemList[entryLoc].updateDelegate = systemDelegate;
                playerLoop.subSystemList = newSubsystemList;
                return true;
            }
            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AppendToPlayerLoopListImpl(systemType, systemDelegate, ref playerLoop.subSystemList[i], targetPlayerLoopSystemType, front))
                        return true;
                }
            }
            return false;
        }

        static bool RemoveFromPlayerLoopListImpl(Type systemType, ref PlayerLoopSystem playerLoop, Type targetPlayerLoopSystemType)
        {
            if (playerLoop.type == targetPlayerLoopSystemType)
            {
                int count = (playerLoop.subSystemList != null) ? playerLoop.subSystemList.Length : 0;
                for (var i = 0; i < count; ++i)
                {
                    var system = playerLoop.subSystemList[i];
                    if (system.type == systemType)
                    {
                        var newSubsystemList = new PlayerLoopSystem[count - 1];
                        var srcOffset = 0;
                        for (var j = 0; j < count - 1; ++j)
                        {
                            if (j == i)
                                srcOffset = 1;
                            newSubsystemList[j] = playerLoop.subSystemList[j + srcOffset];
                        }
                        playerLoop.subSystemList = newSubsystemList;
                        return true;
                    }
                }
            }

            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (RemoveFromPlayerLoopListImpl(systemType, ref playerLoop.subSystemList[i], targetPlayerLoopSystemType))
                        return true;
                }
            }

            return false;
        }

        static bool Contains(Type systemType, in PlayerLoopSystem playerLoop, Type targetPlayerLoopSystemType)
        {
            if (playerLoop.type == targetPlayerLoopSystemType)
            {
                int count = (playerLoop.subSystemList != null) ? playerLoop.subSystemList.Length : 0;
                for (var i = 0; i < count; ++i)
                {
                    var system = playerLoop.subSystemList[i];
                    if (system.type == systemType)
                        return true;
                }
            }
            
            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (Contains(systemType, playerLoop.subSystemList[i], targetPlayerLoopSystemType))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add a callback to a specific point in the Unity player loop, so that it is updated every frame.
        /// </summary>
        /// <remarks>
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="systemType">System type being added to the player loop.</param>
        /// <param name="systemDelegate">System callback delegate.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <param name="targetPlayerLoopSystemType">The Type of the PlayerLoopSystem subsystem to which the system should be appended.
        /// <param name="front">Add to front of list when true.</param>
        /// See the UnityEngine.PlayerLoop namespace for valid values.</param>
        public static void AppendToPlayerLoopList(Type systemType, PlayerLoopSystem.UpdateFunction systemDelegate, ref PlayerLoopSystem playerLoop, Type targetPlayerLoopSystemType, bool front = false)
        {
            if (Contains(systemType, playerLoop, targetPlayerLoopSystemType))
            {
                throw new ArgumentException($"PlayerLoopSystem {systemType.Name} is already registered.");
            }

            if (!AppendToPlayerLoopListImpl(systemType, systemDelegate, ref playerLoop, targetPlayerLoopSystemType, front))
            {
                throw new ArgumentException($"Could not Append PlayerLoopSystem {systemType.Name} to target={targetPlayerLoopSystemType}");
            }
        }

        /// <summary>
        /// Remove a callback from Unity player loop.
        /// </summary>
        /// <param name="systemType">System type being removed from the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <param name="targetPlayerLoopSystemType">The Type of the PlayerLoopSystem subsystem from which the system should be removed.
        public static void RemoveFromPlayerLoopList(Type systemType, ref PlayerLoopSystem playerLoop, Type targetPlayerLoopSystemType)
        {
            if (!Contains(systemType, playerLoop, targetPlayerLoopSystemType))
            {
                throw new ArgumentException($"PlayerLoopSystem {systemType.Name} is not registered.");
            }

            if (!RemoveFromPlayerLoopListImpl(systemType, ref playerLoop, targetPlayerLoopSystemType))
            {
                throw new ArgumentException($"Could not remove PlayerLoopSystem {systemType.Name} from target={targetPlayerLoopSystemType}");
            }
        }
    }
}
