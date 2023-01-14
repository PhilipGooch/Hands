# Pressure system

A flow system which can transfer and split source input between multiple outputs through a system. Multiple systems can be joined together.
Connecting multiple systems together adds their pressure.

## Editor Usage

### PressureNode

Base system component, each system segment max have it or one of the components which derive from it (PressureSource, PressureValve).
Each pressure node can have an unlimited number of PressurePorts.

<B>Logic Graph</B>
* float Get Pressure - get current node pressure.
* event OnPressureChanged - an event which fires when node pressure changes

### PressurePort

Can connect to a single other port, thus connecting different nodes. Ports with a connection cannot leak pressure.
Ports without a connection can be opened and leak pressure.
System pressure decreases by open port count. So system with a starting pressure of 1 and 2 open ports would have a pressure of 0.5.
Ports can be opened through inspector or Logic Graph.
Ports can be connected through inspector or code.

<B>Logic Graph</B>
* bool Get/Set IsOpen - is port leaking pressure.
* float PortPressure - get current port pressure.
* float LerpedPortPressure - get a lerped pressure changed. Needed since the pressure changes instantly which doesnt work with animations. 
Lerped pressure changes gradually
* event OnPortPressureChanged -  an event which fires when node pressure changes.
* event OnPortLerpedPressureChanged -  an event which fires when the node lerped pressure changes.

### PressureSource

System start node, has a constant pressure value which is spread around the system. This value can be set through inspector or Logic Graph.

<B>Logic Graph</B>
* float Get/Set GeneratedPressure- starting system pressure.

### PressureValve

A node which can block pressure flow through it. This value can be set through inspector or Logic Graph.

<B>Logic Graph</B>
* bool Get/Set Blocked - does this node block pressure flow.

### Example

![Example](resources/systemDemo.png)

Yellow gizmos represent ports, blue spheres are system nodes, node connections are shown via blue lines.

