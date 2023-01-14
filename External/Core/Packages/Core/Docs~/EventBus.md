# EventBus
The event bus system can be used to create and subscribe to game wide events.

## Event parameter
Events should be structs. Subscribers will receive copies of the struct.

Example:
```csharp
struct MyEvent
{
    public GameObject what;
    public int howOften;
}
```

## Registering events
Registering requires a function or delegate and will be called whenever the event is fired.
Multiple handlers can be subscribed.

Important: There is no gurantee that events will be called in the same order of registration. 
```csharp
//Declare handler function
void HandleMyEvent(MyEvent e)
{
    Debug.Log("{e.gameObject.name} did a thing {e.howOften} times"); 
}

//Get event bus game system
var eventBus = GameSystemWorldDefault.Instance.GetExistingSystem<EventBus>();
//Register to receive a call to HandleMyEvent when ever a MyEvent event is sent
eventBus.Register<MyEvent>(HandleMyEvent);
```

## Calling events
Calling events requires an instance of the event payload.

```csharp
//Get event bus game system
var eventBus = GameSystemWorldDefault.Instance.GetExistingSystem<EventBus>();
//Register to call HandleMyEvent when ever a MyEvent is called
eventBus.Call<MyEvent>(myEvent);
```

## Unregistering events
Important: Having a handler registered will keep the object with the handler alive. Unregistering is required, if you want to stop receiving calls.
```csharp
//Get event bus game system
var eventBus = GameSystemWorldDefault.Instance.GetExistingSystem<EventBus>();
//Unregister registered event
eventBus.Unregister<MyEvent>(HandleMyEvent);
```

## Best Practices: 
EventBus is designed to allow Game Systems to synchronize events that happen in your game. 
While possible to use it in MonoBehaviours, if you take care of registering/unregistering,
it is encouraged to use register your handlers at the start of the application and unregister rarely (e.g. via systems/managers).
