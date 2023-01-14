# Plugs And Sockets system

This system helps with connecting a rigidbody (plug) object to another object (socket). That is achieved by creating guiding joints - which let the plug easily slide into the socket, and (aligned the plug is inserted into the socket) by creating a snap joint which holds it in the socket.

## Editor Usage

### Hole

- Type:
    - FreeZRotation: single axis - can be rotated freely around Z.
    - Double: two pins, can be flipped.
    - Fixed: can be slotted in only one rotation.
    - NoConstraints: Any approach rotation is viable.
    - FreeXRotation: single axis - can be rotated freely around X.
- Align Transform: overrides snapping point, allows to separate Hole component from socket setup.
- Engage Deep Guides: creates joints which aling the plug with the socket which makes sliding the plug into the socket a lot easier.
- Prevent Snap: prevents plug from snapping.
- Plugged Max Distance: at what distance from socket pivot plug gets snapped (snap joint gets created) to the socket.
- Unplug Distance: at what distance from socket pivot plug gets unsnapped (snap joint gets destroyed).
- Guides Engage Max Distance: at what distance from the pivot of the hole to the pivot of the plug will the guides be destroyed.
- Engage Start Max Angle: at what maximum angle can the plug start interacting with the socket.

### Plug

- Type: same types as on Hole
- Snap Joint Spring: how strong the snap joint spring should be - heavier plugs require larger values to stay snapped

### Scene Setup

Plug object has to cointain Rigidbody and Plug components. Plugs pivot point is whats treated as a snapping point.
Hole has to have Rigidbody, and a Hole component on the same object. It must also have a trigger collider (usually a sphere) either on the same object or as one of the child objects.
There are arrow gizmos that indicate the current position and orientation of the plugs and holes. Using the ***Align Transform*** field you can override the center and orientation of the plug & hole.

## API

### Hole

Overridable Hole methods to allow for custom logic
```csharp
    public virtual bool CanConnect(Plug plug){}
    public virtual bool CanDisconnect(Plug plug){}
```

Connection state events
```csharp
    public event Action onEngageGuides;
    public event Action onDisengageGuides;
    public event Action onPlugIn;
    public event Action onPlugOut;
```
### Plug

Connection state events
```csharp
    public event Action onEngageGuides;
    public event Action onDisengageGuides;
    public event Action<Hole> onPlugIn;
    public event Action<Hole> onPlugOut;
```

Allows to lock/unlock snapped plugs movement and/or rotation

```csharp
    public void LockSnapJointLinearMovement(){ }
    public void UnlockSnapJointLinearMovement(){}
    public void LockSnapJointAngularMovement(){ }
    public void UnlockSnapJointAngularMovement(){}
```

Allows to change snapped plugs joint spring to change how much mass it can support;
```csharp
    public float SnapJointSpring{ get; set;}
```

Forces plug to fall out of the socket. Destroys both snap and guide joints.
```csharp
    public void ForceDestroySnapAndGuides();
```
