using System;
using System.Collections.Generic;

namespace NBG.Core.Events
{
    class LocalEventBus : IEventBus
    {
        const int kDefaultCapacity = 16;

        abstract class Event
        {
            public List<EventListener> listeners = new List<EventListener>(kDefaultCapacity);
        }

        class Event<T> : Event where T : struct
        {
            List<EventListener> listenersToTarget = new List<EventListener>(kDefaultCapacity);

            public void CallAll(T eventData)
            {
                listenersToTarget.AddRange(listeners);

                for (int j = 0; j < listenersToTarget.Count; ++j)
                {
                    var listener = listenersToTarget[j];

                    try
                    {
                        var call = (Action<T>)listener.callback;
                        call(eventData);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                    }
                }

                listenersToTarget.Clear();
                //logger.LogTrace($"Executed {?} event bus handles in frame {UnityEngine.Time.frameCount}");
            }
        }

        struct EventListener
        {
            public object callback; // Action<T>
        }

        private static readonly Logger logger = new Logger("Event Bus");
        private readonly Dictionary<Type, Event> events = new Dictionary<Type, Event>();

        Event<T> GetEntry<T>() where T : struct
        {
            var key = typeof(T);
            if (events.TryGetValue(key, out Event evt))
                return (Event<T>)evt;

            var newEvent = new Event<T>();
            events.Add(key, newEvent);
            return newEvent;
        }

        /// <summary>
        /// Registers an event handler for local.
        /// </summary>
        /// <param name="callback">The delegate or method that should be called when this event is received.</param>
        /// <typeparam name="T">Event data type.</typeparam>
        public void Register<T>(Action<T> callback) where T : struct
        {
            var entry = GetEntry<T>();

            var listener = new EventListener();
            listener.callback = callback;
            entry.listeners.Add(listener);
        }

        /// <summary>
        /// Unregisters an event handler.
        /// Does nothing, if the event was not registered.
        /// </summary>
        /// <param name="callback">The callback you want to unregister.</param>
        /// <typeparam name="T">Event data type.</typeparam>
        public void Unregister<T>(Action<T> callback) where T : struct
        {
            var entry = GetEntry<T>();

            entry.listeners.RemoveAll(x => (Action<T>)x.callback == callback);
        }

        /// <summary>
        /// Calls all event handlers.
        /// </summary>
        /// <param name="eventParameter">Event data that all events will recieve.</param>
        /// <typeparam name="T">Event data type.</typeparam>
        public void Send<T>(T eventData) where T : struct
        {
            var entry = GetEntry<T>();

            entry.CallAll(eventData);
        }
    }
}
