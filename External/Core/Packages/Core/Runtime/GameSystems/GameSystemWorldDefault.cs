using NBG.Core.Events;
using NBG.Core.LowLevelPlayerLoop;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;
using UnityEngine.Scripting;

namespace NBG.Core.GameSystems
{
    public class FixedUpdateSystemGroup : GameSystemGroup
    {
        [Preserve]
        public FixedUpdateSystemGroup()
        {
        }

        protected override void OnCreate()
        {
            AlwaysCompleteWorldAfterUpdate = true;
        }
    }

    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class AfterPhysicsSystemGroup : GameSystemGroup // Helper separator group
    {
    }

    public class EarlyUpdateSystemGroup : GameSystemGroup
    {
        [Preserve]
        public EarlyUpdateSystemGroup()
        {
        }

        protected override void OnCreate()
        {
            AlwaysCompleteWorldAfterUpdate = true;
        }
    }

    public class UpdateSystemGroup : GameSystemGroup
    {
        [Preserve]
        public UpdateSystemGroup()
        {
        }

        protected override void OnCreate()
        {
            AlwaysCompleteWorldAfterUpdate = true;
        }
    }

    public class LateUpdateSystemGroup : GameSystemGroup
    {
        [Preserve]
        public LateUpdateSystemGroup()
        {
        }

        protected override void OnCreate()
        {
            AlwaysCompleteWorldAfterUpdate = true;
        }
    }

    /// <summary>
    /// Allows a non-exported type to be automatically included in the default GameSystemWorld.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IncludeInGameSystemWorldDefaultAttribute : Attribute
    {
    }

    public static class GameSystemWorldDefault
    {
        public enum Flags
        {
            Default = 0,
            UseMonoBehaviourApproach = (1 << 0),
        }

        [ClearOnReload]
        static Flags _flags;
        [ClearOnReload]
        static GameSystemWorld _instance;
        [ClearOnReload]
        static GameSystemWorldDefaultCallbacks _callbacks;

        public static GameSystemWorld Instance => _instance;

        public static void Create(Flags flags = Flags.Default)
        {
            Debug.Assert(_instance == null);
            _flags = flags;

            // Create world
            var world = new GameSystemWorld();
            _instance = world;

            // Add all systems
            var systemList = GetAllSystems();
            AddSystemToRootLevelSystemGroupsInternal(world, systemList, systemList.Count);

            if (_flags.HasFlag(Flags.UseMonoBehaviourApproach))
            {
                _callbacks = GameSystemWorldDefaultCallbacks.Create("default", world);
            }
            else
            {
                var eventBus = EventBus.Get();

                var fixedUpdate = world.GetExistingSystem<FixedUpdateSystemGroup>();
                eventBus.Register<LowLevelFixedUpdateEvent>(OnLowLevelFixedUpdateEvent);
                fixedUpdate.SortSystems();

                var earlyUpdate = world.GetExistingSystem<EarlyUpdateSystemGroup>();
                eventBus.Register<LowLevelEarlyUpdateEvent>(OnLowLevelEarlyUpdateEvent);
                earlyUpdate.SortSystems();

                var update = world.GetExistingSystem<UpdateSystemGroup>();
                eventBus.Register<LowLevelUpdateEvent>(OnLowLevelUpdateEvent);
                update.SortSystems();

                var lateUpdate = world.GetExistingSystem<LateUpdateSystemGroup>();
                eventBus.Register<LowLevelLateUpdateEvent>(OnLowLevelLateUpdateEvent);
                lateUpdate.SortSystems(); lateUpdate.SortSystems();
            }
        }

        public static void Destroy()
        {
            Debug.Assert(_instance != null);

            if (_flags.HasFlag(Flags.UseMonoBehaviourApproach))
            {
                GameSystemWorldDefaultCallbacks.Destroy(_callbacks);
                _callbacks = null;
            }
            else
            {
                var eventBus = EventBus.Get();

                eventBus.Unregister<LowLevelLateUpdateEvent>(OnLowLevelLateUpdateEvent);
                eventBus.Unregister<LowLevelUpdateEvent>(OnLowLevelUpdateEvent);
                eventBus.Unregister<LowLevelEarlyUpdateEvent>(OnLowLevelEarlyUpdateEvent);
                eventBus.Unregister<LowLevelFixedUpdateEvent>(OnLowLevelFixedUpdateEvent);
            }

            // Destroy world
            _instance.Dispose();
            _instance = null;
        }

        static void OnLowLevelFixedUpdateEvent(LowLevelFixedUpdateEvent evt)
        {
            var fixedUpdate = _instance.GetExistingSystem<FixedUpdateSystemGroup>();
            fixedUpdate.Update();
        }

        static void OnLowLevelEarlyUpdateEvent(LowLevelEarlyUpdateEvent evt)
        {
            var earlyUpdate = _instance.GetExistingSystem<EarlyUpdateSystemGroup>();
            earlyUpdate.Update();
        }

        static void OnLowLevelUpdateEvent(LowLevelUpdateEvent evt)
        {
            var update = _instance.GetExistingSystem<UpdateSystemGroup>();
            update.Update();
        }

        static void OnLowLevelLateUpdateEvent(LowLevelLateUpdateEvent evt)
        {
            var lateUpdate = _instance.GetExistingSystem<LateUpdateSystemGroup>();
            lateUpdate.Update();
        }

        public static IReadOnlyList<Type> GetAllSystems()
        {
            var filteredSystemTypes = new List<Type>();
            
            foreach (var systemType in AssemblyUtilities.GetAllDerivedClasses(typeof(GameSystemBase), includePrivateTypes: true))
            {
                if (!systemType.IsVisible)
                {
                    var attr = systemType.GetCustomAttribute<IncludeInGameSystemWorldDefaultAttribute>();
                    if (attr == null)
                        continue;
                }

                if (TypeManager.FilterSystemType(systemType))
                    filteredSystemTypes.Add(systemType);
            }

            return filteredSystemTypes;
        }

        private static void AddSystemToRootLevelSystemGroupsInternal(GameSystemWorld world, IEnumerable<Type> systemTypes, int systemTypesCount)
        {
            var fixedUpdateSystemGroup = world.GetOrCreateSystem<FixedUpdateSystemGroup>();
            var earlyUpdateSystemGroup = world.GetOrCreateSystem<EarlyUpdateSystemGroup>();
            var updateSystemGroup = world.GetOrCreateSystem<UpdateSystemGroup>();
            var lateUpdateSystemGroup = world.GetOrCreateSystem<LateUpdateSystemGroup>();

            foreach (var stype in systemTypes)
            {
                if (!typeof(GameSystemBase).IsAssignableFrom(stype))
                    throw new InvalidOperationException("Bad type");
            }

            var systems = world.GetOrCreateSystemsAndLogException(systemTypes, systemTypesCount);

            // Add systems to their groups, based on the [UpdateInGroup] attribute.
            foreach (var system in systems)
            {
                if (system == null)
                    continue;

                // Skip the built-in root-level system groups
                var type = system.GetType();
                if (type == typeof(FixedUpdateSystemGroup) ||
                    type == typeof(EarlyUpdateSystemGroup) ||
                    type == typeof(UpdateSystemGroup) ||
                    type == typeof(LateUpdateSystemGroup))
                {
                    continue;
                }

                var updateInGroupAttributes = TypeManager.GetSystemAttributes(system.GetType(), typeof(UpdateInGroupAttribute));
                var autoRegAttribute = system.GetType().GetCustomAttribute<DisableAutoRegistrationAttribute>(false); // Do not inherit
                Debug.Assert(updateInGroupAttributes.Length == 0 || autoRegAttribute == null, $"Should not use {nameof(DisableAutoRegistrationAttribute)} with {nameof(UpdateInGroupAttribute)}");

                if (autoRegAttribute != null)
                {
                    //Debug.Log($"System {system.GetType().Name} will not be registered");
                    continue;
                }

                if (updateInGroupAttributes.Length == 0)
                {
                    updateSystemGroup.AddSystemToUpdateList(system);
                }

                foreach (var attr in updateInGroupAttributes)
                {
                    var group = FindGroup(world, type, (UpdateInGroupAttribute)attr);
                    if (group != null)
                    {
                        group.AddSystemToUpdateList(system);
                    }
                }
            }

            // Update player loop
            fixedUpdateSystemGroup.SortSystems();
            earlyUpdateSystemGroup.SortSystems();
            updateSystemGroup.SortSystems();
            lateUpdateSystemGroup.SortSystems();
        }

        private static GameSystemGroup FindGroup(GameSystemWorld world, Type systemType, Attribute attr)
        {
            var uga = attr as UpdateInGroupAttribute;

            if (uga == null)
                return null;

            if (!TypeManager.IsSystemAGroup(uga.GroupType))
            {
                throw new InvalidOperationException($"Invalid [UpdateInGroup] attribute for {systemType}: {uga.GroupType} must be derived from {nameof(GameSystemGroup)}.");
            }
            if (uga.OrderFirst && uga.OrderLast)
            {
                throw new InvalidOperationException($"The system {systemType} can not specify both OrderFirst=true and OrderLast=true in its [UpdateInGroup] attribute.");
            }

            var groupSys = world.GetExistingSystem(uga.GroupType);
            if (groupSys == null)
            {
                // Warn against unexpected behaviour combining DisableAutoCreation and UpdateInGroup
                var parentDisableAutoCreation = TypeManager.GetSystemAttributes(uga.GroupType, typeof(DisableAutoCreationAttribute)).Length > 0;
                if (parentDisableAutoCreation) //TODO: what about the global assembly flag? Does it work?
                {
                    Debug.LogWarning($"A system {systemType} wants to execute in {uga.GroupType} but this group has [DisableAutoCreation] and {systemType} does not. The system will not be added to any group and thus not update.");
                }
                else
                {
                    Debug.LogWarning($"A system {systemType} could not be added to group {uga.GroupType}, because the group was not created. Fix these errors before continuing. The system will not be added to any group and thus not update.");
                }
            }

            return groupSys as GameSystemGroup;
        }

        private static bool IsSystemInAnyGroupOrRootLevel(GameSystemWorld world, GameSystemBase system)
        {
            if (system == world.GetExistingSystem<FixedUpdateSystemGroup>() ||
                system == world.GetExistingSystem<EarlyUpdateSystemGroup>() ||
                system == world.GetExistingSystem<UpdateSystemGroup>() ||
                system == world.GetExistingSystem<LateUpdateSystemGroup>())
                return true;

            foreach (var item in world.Systems)
            {
                if (item.DebugContainsRecursive(system))
                    return true;
            }

            return false;
        }

        public static void DebugPrint(string header)
        {
            var world = _instance;
            
            var sb = new StringBuilder(8 * 1024);
            sb.AppendLine($"=================[ Game System World: {header} ]=================");

            world.GetExistingSystem<FixedUpdateSystemGroup>().DebugPrint(sb, 0);
            world.GetExistingSystem<EarlyUpdateSystemGroup>().DebugPrint(sb, 0);
            world.GetExistingSystem<UpdateSystemGroup>().DebugPrint(sb, 0);
            world.GetExistingSystem<LateUpdateSystemGroup>().DebugPrint(sb, 0);

            sb.AppendLine("--- Manual systems ---");

            foreach (var item in world.Systems)
            {
                if (IsSystemInAnyGroupOrRootLevel(world, item))
                    continue;

                item.DebugPrint(sb, 0);
            }

            sb.AppendLine($"========================================================");

            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, sb.ToString());
        }
    }
}
