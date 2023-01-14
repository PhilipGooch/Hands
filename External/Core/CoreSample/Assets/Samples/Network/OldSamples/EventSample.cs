using System;
using CoreSample.Network;
using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Core.ObjectIdDatabase;
using NBG.Core.Streams;
using UnityEngine;
using UnityEngine.Scripting;

namespace NBG.Net.Systems
{
    public class EventSample : MonoBehaviour, INetBehavior
    {
        public void Start()
        {
            var events = EventBus.Get();
            DebugUI.DebugUI.Get().RegisterAction("Test event with gameObject", "Network", TriggerNetEventHandle);
            DebugUI.DebugUI.Get().RegisterAction("Test event with gameObject on peers only", "Network", TriggerNetEventOnPeerOnlyHandle);
            events.Register<GameObjectEvent>(SomeEventWithGO);
            events.Register<GameObjectEvent>(SomeEventWithGOAndEx);
            events.Register<GameObjectEvent>(SomeEventWithGO);
            events.Register<IntEvent>(IntEvent);
        }

        bool isClient = false;
        void INetBehavior.OnNetworkAuthorityChanged(NetworkAuthority authority)
        {
            isClient = (authority == NetworkAuthority.Client);
        }

        private void TriggerNetEventHandle()
        {
            var events = EventBus.Get();
            events.Send(new GameObjectEvent(this.gameObject));
            events.Send(new IntEvent() { someInt = 5 });
        }

        private void TriggerNetEventOnPeerOnlyHandle()
        {
            if (isClient)
            {
                Debug.LogError("This is a server-only event");
                return;
            }

            var netEventBus = GameSystemWorldDefault.Instance.GetExistingSystem<NetEventBus>();
            foreach (var peer in NetGame.Peers.GetReadyPeers())
            {
                netEventBus.CallOnPeer(new IntEvent() { someInt = 5 }, peer);
            }
        }

        private void SomeEventWithGO(GameObjectEvent evt)
        {
            Debug.Log($"Got an event with {evt.Go}");
        }

        private void SomeEventWithGOAndEx(GameObjectEvent evt)
        {
            Debug.Log($"Got an event with {evt.Go} and will throw exception");
            throw new Exception("YOLO!");
        }

        private void IntEvent(IntEvent evt)
        {
            Debug.Log($"Got int event with value: {evt.someInt}");
        }
    }

    // Example GameObjectEvent is always networked and has the NetEventBusSerializer attribute applied directly to its only serializer.
    public struct GameObjectEvent
    {
        public GameObject Go { get; private set; }

        public GameObjectEvent(GameObject testGO)
        {
            this.Go = testGO;
        }
    }

    [NetEventBusSerializer]
    public class GameObjectEvent_Serializer : IEventSerializer<GameObjectEvent>
    {
        public GameObjectEvent Deserialize(IStreamReader reader)
        {
            var data = new GameObjectEvent(ObjectIdDatabaseResolver.instance.ReadGameObject(reader));
            return data;
        }

        public void Serialize(IStreamWriter writer, GameObjectEvent data)
        {
            ObjectIdDatabaseResolver.instance.WriteGameObject(writer, data.Go);
        }
    }

    // Example IntEvent is optionally networked and has the NetEventBusSerializer attribute applied to a derived serialized, which may or may not be present.
    public struct IntEvent
    {
        public int someInt;
    }

    public class IntEvent_Serializer : IEventSerializer<IntEvent>
    {
        public IntEvent Deserialize(IStreamReader reader)
        {
            var data = new IntEvent();
            data.someInt = reader.ReadInt32(32);
            return data;
        }

        public void Serialize(IStreamWriter writer, IntEvent data)
        {
            writer.Write(data.someInt, 32);
        }
    }

    [NetEventBusSerializer]
    public class Net_IntEvent_Serializer : IntEvent_Serializer
    {

    }

    [Preserve] //IL2CPP
    public class NetSampleEventIDs : INetEventBusIDs
    {
        // Id overriding example
        public uint GetId(Type eventSerializerType)
        {
            if (eventSerializerType == typeof(IntEvent_Serializer))
            {
                return 7;
            }

            return 0;
        }
    }
}
