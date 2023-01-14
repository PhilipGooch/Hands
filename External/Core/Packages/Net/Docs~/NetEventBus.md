# Networked EventBus
Enables events to optionally run on clients in sync with the server.
``NetEventBus`` implements ``IEventBus`` just like the core ``EventBus`` and proxies all calls, so only one call is needed to send an event locally and via network. Calling ``NetEventBus`` API with an event that is not declared as network-enabled will behave as if it was called on ``EventBus``.

## Net-synced events
Events can optionally also be net synced. If they are called on the server, they will also be called on all connected clients.

### Net-syncing an event
To implement an net-synched event, implement INetEventSerializerTyped<> for that event type.

```csharp
class MyEvent
{
    public readonly GameObject what;
    public readonly int howOften;
}

class MyEvent_NetSerializer : INetEventSerializerTyped<MyEvent>
{
    private GameObject go;
    private int howOften;
 
    public void Serialize(INetStreamWriter writer, MyEvent data)
    {
        writer.WriteGameObject(go);
        writer.Write(howOften, 32);
    }

    public MyEvent Deserialize(INetStreamReader reader)
    {
        var data = new MyEvent();
        data.go = reader.ReadGameObject();
        data.howOften = reader.ReadInt32(32);
        return data;
    }
}
```

### Event IDs
Net-synced events are required to be declared upfront with a stable id. One way to do that is create a class that derives from ``INetEventBusIDs``.

Important: Client and Server MUST have the same order and count, or will be incompatible.
Only one derived class of INetEventBusIDs can be declared in the codebase.

```csharp
public class GameSpecificNetEventIDs : INetEventBusIDs
{
    public void DeclareTypes(NetEventBus target)
    {
        target.Declare<MyEvent>(1, new MyEvent_NetSerializer());
    }
}
```

### Send Events to clients directly
The default API broadcasts events to all clients. For clients that join late, it's useful to send an event to a specific peer, to update them about events they may have missed.

```csharp
// Create an event
var myEvent = new MyEvent()
{
    gameObject = this.gameObject,
    howOften = 3;
}
var netEventBus = GameSystemWorldDefault.Instance.GetExistingSystem<NetEventBus>();
//Send to peer only
netEventBus.SendEventToPeer(myEvent, peer);
```

### Join-in-progress
Register for ``NewPeerIsReadyEvent`` to update any new joining clients.

```csharp
void OnNewPeerIsReady(NewPeerIsReadyEvent e)
{
    // Handle join-in-progress
}

//Register events with the eventbus
eventBus.Register<NewPeerIsReadyEvent>(OnNewPeerIsReady);
```

## Best Practices: 
Use ``INetStream.WriteGameObject`` to transmit a ``GameObject`` reference.

NetEventBus supports only a limited amount of event types. Try to stay below 1024 for your game.

Events are send in the same frame as objects. Try to keep the data small. Avoid sending big blobs of data like strings, 
arrays, texture data, etc. Consider using a new Protocol Message for these needs.