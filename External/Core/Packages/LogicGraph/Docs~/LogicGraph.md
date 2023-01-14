# Logic Graph

Visual scripting toolkit.

## Concept

* LogicGraph is like a class.
* LogicGraph variable is like a member variable.
* Node is like a member function.
* Nodes push and pop values onto stack (see IStack) during execution.
* Execution begins at one of the entry point nodes (see NodeConceptualType.EntryPoint) and follows the flow link. Conceptually it is a left-to-right execution flow.
* Node data inputs reference data outputs of other nodes. Conceptually it is a right-to-left request flow.
* Event nodes bind to native C# events and begin execution once the event is invoked.

## Implementation

### Code generation

* Cecil library is used to emit bindings for functions and events with NodeAPIAttribute.
* Bindings bridge CIL stack with LogicGraph stack: read values on CIL stack and push them to IStack, or vice versa.
* Bindings have a fixed signature and can be invoked efficiently, essentially giving Logic Graph a data-driven way to execute user code.
* Code generation happens after Unity compiles scripts.
* Code generation uses an external tool found at Editor\Data\Tools\ILPostProcessorRunner
* Code generation expects the generator assembly to be called Unity.*.CodeGen

### Supported variable types

* Boolean
* Integer
* Float
* String
* UnityEngine.Vector3
* UnityEngine.Object (tracks references to scene or prefab components)

### Serialization

* Binding references are serialized by assembly, namespace and type name.
* Refactoring bindings can break saved data.
* Variables are serialized to strings.

### IL2CPP

* Generated bindings get a PreserveAttribute preventing code stripping.
* Because method bindings reference the original method, that method is also not stripped.
