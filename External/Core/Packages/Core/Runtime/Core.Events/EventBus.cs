using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.Events
{
    public static class EventBus
    {
        [ClearOnReload]
        private static EventBuses eventBuses = null;

        public static void Create()
        {
            Debug.Assert(eventBuses == null, $"Initializing event bus even though it already exists");
            eventBuses = new EventBuses();
        }

        public static void Destroy()
        {
            Debug.Assert(eventBuses != null, $"Disposing event bus even though it doesn't exists");
            eventBuses = null;
        }

        public static IEventBus Get()
        {
            //Debug.Assert(eventBuses != null, $"Event Bus not initialized");
            return eventBuses;
        }

        public static void Register(IEventBus bus)
        {
            Debug.Assert(eventBuses != null, $"Event Bus not initialized");
            Debug.Assert(!eventBuses.buses.Contains(bus));
            eventBuses.buses.Add(bus);
        }

        public static void Unregister(IEventBus bus)
        {
            Debug.Assert(eventBuses != null, $"Event Bus not initialized");
            Debug.Assert(eventBuses.buses.Contains(bus));
            eventBuses.buses.Remove(bus);
        }
    }

    class EventBuses : IEventBus
    {
        LocalEventBus localBus = new LocalEventBus();
        internal List<IEventBus> buses = new List<IEventBus>();

        internal EventBuses()
        {
            buses.Add(localBus);
        }

        public void Register<T>(Action<T> callback) where T : struct
        {
            for (int i = 0; i < buses.Count; ++i)
            {
                var bus = buses[i];
                bus.Register<T>(callback);
            }
        }

        public void Send<T>(T eventData) where T : struct
        {
            for (int i = 0; i < buses.Count; ++i)
            {
                var bus = buses[i];
                bus.Send<T>(eventData);
            }
        }

        public void Unregister<T>(Action<T> callback) where T : struct
        {
            for (int i = 0; i < buses.Count; ++i)
            {
                var bus = buses[i];
                bus.Unregister<T>(callback);
            }
        }
    }
}
