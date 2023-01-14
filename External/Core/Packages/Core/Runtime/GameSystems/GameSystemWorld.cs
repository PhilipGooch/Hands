using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.GameSystems
{
    public class GameSystemWorld : IDisposable
    {
        Dictionary<Type, GameSystemBase> m_SystemLookup = new Dictionary<Type, GameSystemBase>();

        List<GameSystemBase> m_Systems = new List<GameSystemBase>();
        public NoAllocReadOnlyCollection<GameSystemBase> Systems { get; }

        GameSystemDependencyManager m_DependencyManager = new GameSystemDependencyManager();
        internal GameSystemDependencyManager DependencyManager => m_DependencyManager;

        public GameSystemWorld()
        {
            Systems = new NoAllocReadOnlyCollection<GameSystemBase>(m_Systems);
        }

        public void Dispose()
        {
            DestroyAllSystemsAndLogException();

            m_SystemLookup.Clear();
            m_SystemLookup = null;
        }

        // Public system management

        public T GetOrCreateSystem<T>() where T : GameSystemBase
        {
            var system = GetExistingSystemInternal(typeof(T));
            return (T)(system ?? CreateSystemInternal(typeof(T)));
        }

        public GameSystemBase GetOrCreateSystem(Type type)
        {
            var system = GetExistingSystemInternal(type);
            return system ?? CreateSystemInternal(type);
        }

        public T CreateSystem<T>() where T : GameSystemBase, new()
        {
            return (T)CreateSystemInternal(typeof(T));
        }

        public GameSystemBase CreateSystem(Type type)
        {
            return CreateSystemInternal(type);
        }

        public T AddSystem<T>(T system) where T : GameSystemBase
        {
            if (GetExistingSystemInternal(system.GetType()) != null)
                throw new Exception($"Attempting to add system '{system.GetType().Name}' which has already been added to this world.");

            AddSystem_Add_Internal(system);
            AddSystem_OnCreate_Internal(system);
            return system;
        }

        public T GetExistingSystem<T>() where T : GameSystemBase
        {
            return (T)GetExistingSystemInternal(typeof(T));
        }

        public GameSystemBase GetExistingSystem(Type type)
        {
            return GetExistingSystemInternal(type);
        }

        public void DestroySystem(GameSystemBase system)
        {
            RemoveSystemInternal(system);
            system.DestroyInstance();
        }

        // Internal system management

        GameSystemBase CreateSystemInternal(Type type)
        {
            var system = ConstructSystem(type);
            AddSystem_Add_Internal(system);
            AddSystem_OnCreate_Internal(system);
            return system;
        }

        public static GameSystemBase ConstructSystem(Type systemType)
        {
            if (!typeof(GameSystemBase).IsAssignableFrom(systemType))
                throw new ArgumentException($"'{systemType.FullName}' cannot be constructed as it does not inherit from {nameof(GameSystemBase)}");
            return (GameSystemBase)Activator.CreateInstance(systemType);
        }

        GameSystemBase GetExistingSystemInternal(Type type)
        {
            GameSystemBase system;
            if (m_SystemLookup.TryGetValue(type, out system))
                return system;

            return null;
        }



        void AddTypeLookupInternal(Type type, GameSystemBase system)
        {
            while (type != typeof(GameSystemBase))
            {
                if (!m_SystemLookup.ContainsKey(type))
                    m_SystemLookup.Add(type, system);

                type = type.BaseType;
            }
        }

        void AddSystem_Add_Internal(GameSystemBase system)
        {
            m_Systems.Add(system);
            AddTypeLookupInternal(system.GetType(), system);
        }

        void AddSystem_OnCreate_Internal(GameSystemBase system)
        {
            try
            {
                var newState = SystemState.CreateDefault(this);
                system.CreateInstance(this, newState);
            }
            catch
            {
                RemoveSystemInternal(system);
                throw;
            }
        }

        void RemoveSystemInternal(GameSystemBase system)
        {
            if (!m_Systems.Remove(system))
                throw new ArgumentException($"System does not exist in the world");

            var type = system.GetType();
            while (type != typeof(GameSystemBase))
            {
                if (m_SystemLookup[type] == system)
                {
                    m_SystemLookup.Remove(type);

                    foreach (var otherSystem in m_Systems)
                    {
                        if (type != otherSystem.GetType() && type.IsAssignableFrom(otherSystem.GetType()))
                            AddTypeLookupInternal(otherSystem.GetType(), otherSystem);
                    }
                }

                type = type.BaseType;
            }
        }

        internal GameSystemBase[] GetOrCreateSystemsAndLogException(IEnumerable<Type> types, int typesCount)
        {
            var toInitSystems = new GameSystemBase[typesCount];
            // start before 0 as we increment at the top of the loop to avoid
            // special cases for the various early outs in the loop below
            var i = -1;
            foreach (var type in types)
            {
                i++;
                try
                {
                    if (GetExistingSystemInternal(type) != null)
                        continue;

                    var system = AllocateSystemInternal(type);
                    if (system == null)
                        continue;

                    toInitSystems[i] = system;
                    AddSystem_Add_Internal(system);
                }
                catch (Exception exc)
                {
                    Debug.LogException(exc);
                }
            }

            for (i = 0; i != typesCount; i++)
            {
                if (toInitSystems[i] != null)
                {
                    try
                    {
                        AddSystem_OnCreate_Internal(toInitSystems[i]);
                    }
                    catch (Exception exc)
                    {
                        Debug.LogException(exc);
                    }
                }
            }

            i = 0;
            foreach (var type in types)
            {
                toInitSystems[i] = GetExistingSystemInternal(type);
                i++;
            }

            return toInitSystems;
        }

        public void DestroyAllSystemsAndLogException()
        {
            if (m_Systems == null)
                return;

            // Systems are destroyed in reverse order from construction, in three phases:
            // 1. Stop all systems from running (if they weren't already stopped), to ensure OnStopRunning() is called.
            // 2. Call each system's OnDestroy() method
            // 3. Actually destroy each system
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    m_Systems[i].OnBeforeDestroyInternal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    m_Systems[i].OnDestroy_Internal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            for (int i = m_Systems.Count - 1; i >= 0; --i)
            {
                try
                {
                    m_Systems[i].OnAfterDestroyInternal();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            m_Systems.Clear();
            m_Systems = null;
        }

        GameSystemBase AllocateSystemInternal(Type systemType)
        {
            if (!typeof(GameSystemBase).IsAssignableFrom(systemType))
                throw new ArgumentException($"'{systemType.FullName}' cannot be constructed as it does not inherit from {nameof(GameSystemBase)}");
            return (GameSystemBase)Activator.CreateInstance(systemType);
        }
    }
}
