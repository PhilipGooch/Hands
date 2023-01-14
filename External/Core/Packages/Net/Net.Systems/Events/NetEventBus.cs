using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Core.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;
using Logger = NBG.Core.Logger;

namespace NBG.Net.Systems
{
    [Protocol]
    public static class NetEventBusProtocol
    {
        public static ushort Events; // Server->client
    }

    [UpdateInGroup(typeof(LateUpdateSystemGroup))]
    [UpdateAfter(typeof(NetReadAndApplyFrame))] //TODO: Verify that this runs in the correct group and order.
    public class NetEventBus : GameSystem, IEventBus, IEventCallOnPeer
    {
        abstract class Event
        {
            public uint netId;
            public List<EventListener> listeners = new List<EventListener>();

            public abstract void CallFromNetwork(IStreamReader reader);
        }

        class Event<T> : Event where T : struct
        {
            public IEventSerializer<T> serializer;

            List<EventListener> listenersToTarget = new List<EventListener>();

            public override void CallFromNetwork(IStreamReader reader)
            {
                listenersToTarget.Clear();
                listenersToTarget.AddRange(listeners);

                var eventData = serializer.Deserialize(reader);

                for (int i = 0; i < listenersToTarget.Count; ++i)
                {
                    var listener = listenersToTarget[i];

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
            }   
        }

        struct EventListener
        {
            public object callback; // Action<T>
        }

        private static readonly Logger logger = new Logger(nameof(NetEventBus));
        private readonly Dictionary<Type, Event> events = new Dictionary<Type, Event>();
        private readonly Dictionary<uint, Event> eventsByNetId = new Dictionary<uint, Event>();

        Event<T> GetEntry<T>() where T : struct
        {
            var key = typeof(T);
            if (events.TryGetValue(key, out Event evt))
                return (Event<T>)evt;
            return null;
        }

        private const int eventBitsSmall = 4;
        private const int eventBitsLarge = 10;
        private const int EVENT_STREAM_INITIAL_SIZE = 1024; //1kb.

        private readonly EventStreamList frameEventStreams = new EventStreamList();
        private IStream currentStream = BasicStream.Allocate(EVENT_STREAM_INITIAL_SIZE);

        private int serverFrameID;

        private NetWriteAndSendFrame serverSystem;

        /// <summary>
        /// Declares a networked event type.
        /// </summary>
        /// <typeparam name="T">Event data type.</typeparam>
        public void Declare<T>(uint netId, IEventSerializer<T> serializer) where T : struct
        {
            //generate serializer/deseriazer calls here
            var key = typeof(T);
            if (events.TryGetValue(key, out Event evt))
                throw new Exception($"NetEvent {key.FullName} is being declared twice.");

            var newEvent = new Event<T>();
            newEvent.netId = netId;
            newEvent.serializer = serializer;
            events.Add(key, newEvent);
            eventsByNetId.Add(netId, newEvent);
        }

        /// <summary>
        /// Registers networked events.
        /// </summary>
        /// <typeparam name="T">Event data type.</typeparam>
        /// <param name="callback"></param>
        public void Register<T>(Action<T> callback) where T : struct
        {
            var entry = GetEntry<T>();
            if (entry == null)
                return;
            var listener = new EventListener();
            listener.callback = callback;
            entry.listeners.Add(listener);
        }

        /// <summary>
        /// Unregisters an event handler for networked events.
        /// </summary>
        /// <typeparam name="T">Event data type.</typeparam>
        /// <param name="callback"></param>
        public void Unregister<T>(Action<T> callback) where T : struct
        {
            var entry = GetEntry<T>();
            if (entry == null)
                return;
            entry.listeners.RemoveAll(x => (Action<T>)x.callback == callback);
        }

        /// <summary>
        /// Sends an event to all peers.
        /// </summary>
        /// <param name="eventData">The data you want to send</param>
        /// <typeparam name="T">Event data type.</typeparam>
        public void Send<T>(T eventData) where T : struct
        {
            var entry = GetEntry<T>();
            if (entry == null)
                return;
            // Send via network
            var startOffset = currentStream.PositionBits;
            try
            {
                currentStream.WriteID(entry.netId);
                entry.serializer.Serialize(currentStream, eventData);
            }
            catch (Exception)
            {
                //undo written data
                currentStream.Seek(startOffset);
                throw;
            }
        }

        /// <summary>
        /// Sends an event to a specific peer.
        /// Useful to sync state after a connection or to force-update an event on clients.
        /// Does NOT call local event handlers for this payload.
        /// </summary>
        /// <param name="eventData">The data you want to send</param>
        /// <param name="peer">The peer you want to send this too</param>
        /// <typeparam name="T">Event data type.</typeparam>
        public void CallOnPeer<T>(T eventData, INetTransportPeer peer) where T : struct
        {
            var entry = GetEntry<T>();
            if (entry == null)
                throw new Exception($"Network event {typeof(T)} has not been declared.");

            var perPeerStream = this.frameEventStreams.GetPeerStream(peer);
            var startOffset = perPeerStream.PositionBits;
            try
            {
                perPeerStream.WriteID(entry.netId);
                entry.serializer.Serialize(perPeerStream, eventData);
            }
            catch (Exception)
            {
                //undo written data
                perPeerStream.Seek(startOffset);
                throw;
            }
        }

        void IEventCallOnPeer.CallOnPeer<T>(T eventData, IPeer peer) => CallOnPeer(eventData, (INetTransportPeer)peer);

        public void PeerDisconnected(INetTransportPeer context)
        {
            frameEventStreams.RemovePeer(context);
        }

        // TODO: This is a temporary workaround, that causes NetEventBus to be present before other system OnCreate calls.
        public NetEventBus()
        {
            EventBus.Register(this);
        }

        protected override void OnCreate()
        {
            serverSystem = World.GetExistingSystem<NetWriteAndSendFrame>();
            RegisterNetEvents();
        }
        protected override void OnDestroy()
        {
            EventBus.Unregister(this);
        }

        protected override void OnUpdate()
        {
            if (serverSystem.Enabled)
                NetWriteEvents();
        }

        protected override void OnStopRunning()
        {
            serverSystem = null;
        }

        private void NetWriteEvents()
        {
            Debug.Assert(serverSystem.Enabled);
            if (currentStream.PositionBytes <= 0)
                return; //No Events written this frame
            currentStream.Flip();
            serverFrameID = serverSystem.CurrentFrameID;
            frameEventStreams.Insert(currentStream, serverFrameID);
            frameEventStreams.LimitHistoryByAcks();
            currentStream = BasicStream.Allocate(EVENT_STREAM_INITIAL_SIZE);
        }

        internal void AppendToFrame(INetTransportPeer context, IStreamWriter stream, int baseFrame)
        {
            var numEventsWritten = 0;
            frameEventStreams.ProcessForPeer((s, frameEventStreams) =>
            {
                //NOTE: We prefix each frame with the frameID but relative to the current base frame, to save bits. FrameIDs grow large over time
                //This will most of the time be bitSmall, but we need to support up until ceil(log2(historyLimit))
                var relativeFrameId = baseFrame - s;
                Debug.Assert(relativeFrameId >= 0);
                Debug.Assert(relativeFrameId < baseFrame);
                stream.Write(relativeFrameId, eventBitsSmall, eventBitsLarge);
                stream.WriteStream(frameEventStreams);
                numEventsWritten++;
            }, context, serverSystem.CurrentFrameID);
            stream.Write(-1, eventBitsSmall, eventBitsLarge);
            logger.LogTrace($"Wrote {numEventsWritten} in frame {baseFrame}");
        }

        internal void ProcessEventsFromServer(IStreamReader eventStream, int frameID)
        {
            var numEventsRead = 0;
            var oldestFrameID = int.MaxValue;
            var newestFrameID = 0;
            var eventFrame = eventStream.ReadInt32(eventBitsSmall, eventBitsLarge);
            while (eventFrame > -1)
            {
                var frameEventStream = eventStream.ReadStream();
                if (frameEventStream == null)
                    throw new Exception("Malformed stream, no events attached!");
                if (frameEventStream.LimitBits == 0)
                    throw new NotSupportedException("Empty stream is not supported");
                frameEventStreams.Insert(frameEventStream, frameID - eventFrame);
                eventFrame = eventStream.ReadInt32(eventBitsSmall, eventBitsLarge);
                oldestFrameID = math.min(oldestFrameID, eventFrame);
                newestFrameID = math.max(newestFrameID, eventFrame);
                numEventsRead++;
            }
            if (numEventsRead > 0)
            {
                logger.LogTrace($"Read {numEventsRead} in frame {frameID} oldest was {oldestFrameID} newest was {newestFrameID}");
                //NOTE: If the server stopped resending a certain frame, it must recieved our ack. We can safely drop earlier frames
                frameEventStreams.LimitHistory(oldestFrameID - 1);
            }
        }

        internal void EnsureEventsReplayed(int targetFrameID)
        {
            var numEventsReplayed = 0;
            frameEventStreams.ProcessEvents((streamFrameID, stream) =>
            {
                try
                {
                    while (stream.BitsAvailable())
                    {
                        var eventID = stream.ReadID();
                        var evt = eventsByNetId[eventID];
                        evt.CallFromNetwork(stream);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
                numEventsReplayed++;
            }, targetFrameID);
            logger.LogTrace($"Replayed {numEventsReplayed} in frame {targetFrameID}");
        }

        internal void UpdateLastAck(INetTransportPeer context, int ackedFrame)
        {
            frameEventStreams.UpdateLastAck(context, ackedFrame);
        }

        #region Registration
        internal const string RegistratorFuncName = "_NetEventBus_EventRegistrator_";
        const BindingFlags RegistratorBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        class RegItem
        {
            public Type serializerType;
            public uint netId; // 0 - not assigned
            public bool dynamic;
        };

        private void RegisterNetEvents()
        {
            var types = NBG.Core.AssemblyUtilities.GetAllTypesWithAttribute(typeof(NetEventBusSerializerAttribute)).Select(x => new RegItem() { serializerType = x }).ToList();
            types.Sort((x, y) => x.serializerType.FullName.CompareTo(y.serializerType.FullName)); // Assign ids based on name

            // Determine network ids based on a game-specific protocol override
            var registryType = NBG.Core.AssemblyUtilities.GetSingleDerivedClass(typeof(INetEventBusIDs));
            var overrideIDs = Activator.CreateInstance(registryType) as INetEventBusIDs;
            if (overrideIDs == null)
                throw new Exception($"Could not instantiate event {nameof(INetEventBusIDs)} of type {registryType}");
            
            foreach (var item in types)
            {
                item.netId = overrideIDs.GetId(item.serializerType);
                if (item.netId == 0)
                    item.dynamic = true;
            }

            // Fill the remaining ids automatically
            {
                uint netId = 1;
                foreach (var item in types)
                {
                    if (item.netId != 0)
                        continue; // id already assigned

                    while (types.Exists(x => x.netId == netId))
                        netId++; // look for the next minimum available id

                    item.netId = netId;
                    netId++;
                }
            }

            // Register serializers
            foreach (var item in types)
            {
                try
                {
                    var serializer = Activator.CreateInstance(item.serializerType);
                    var func = item.serializerType.GetMethod(RegistratorFuncName, RegistratorBindingFlags);
                    func.Invoke(serializer, new object[] { this, item.netId });
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to run network event registrator on {item.serializerType.FullName}.", e);
                }
            }

            // Log network event protocol
            {
                var sb = new StringBuilder(4096);
                sb.AppendLine($"NetEventBus protocol:");

                types.Sort((x, y) => x.netId.CompareTo(y.netId)); // Log protocol in netid order
                foreach (var item in types)
                {
                    sb.Append($"  [{item.netId}]");
                    if (item.dynamic)
                        sb.Append("[Dynamic]");
                    sb.AppendLine($" : {item.serializerType.Name}");
                }

                logger.LogInfo(sb.ToString());
            }
        }
        #endregion
    }
}
