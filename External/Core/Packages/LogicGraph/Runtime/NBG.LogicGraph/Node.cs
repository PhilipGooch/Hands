using NBG.Core;
using NBG.LogicGraph.Nodes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NBG.LogicGraph
{
    // Serialization: strict order
    class FlowOutput
    {
        public string Name;
        public SerializableGuid refNodeGuid;
    }

    // Serialization: strict order
    class StackInput
    {
        public string Name;
        public VariableType Type;
        
        public Type BackingType;
        public IVariantProvider variants;

        // Constant
        public IVarHandleContainer variable;

        // Reference
        public SerializableGuid refNodeGuid;
        public int refIndex;

        // Debug
        public IVarHandleContainer debugLastValue;
    }

    // Serialization: strict order
    class StackOutput
    {
        public string Name;
        public VariableType Type;
    }

    [Flags]
    enum NodePropertyFlags
    {
        None = 0,
        Hidden = (1 << 0),
    }

    // Serialization: name match
    class NodeProperty
    {
        public string Name;
        public VariableType Type;
        public NodePropertyFlags Flags;

        public IVarHandleContainer variable;
    }

    /// <summary>
    /// LogicGraph node.
    /// </summary>
    public interface INode
    {
        string Name { get; }
    }

    abstract partial class Node : INode
    {
        public ILogicGraph Owner { get; internal set; }

        /// <summary>
        /// Frame index of last activation.
        /// Based on Time.frameCount.
        /// </summary>
        public int DebugLastActivatedFrameIndex { get; protected set; }

        public virtual string Name { get { return this.GetType().Name; } }
        public virtual NodeAPIScope Scope => NodeAPIScope.Generic;
        public virtual bool HasFlowInput { get { return false; } }

        /// <summary>
        /// Deserializer will create new stack input entries.
        /// If UserDefinedStackInputs is true, it will try to match them instead.
        /// </summary>
        public virtual bool UserDefinedStackInputs { get { return false; } }

        protected List<FlowOutput> _flowOutputs = new List<FlowOutput>();
        protected List<StackInput> _stackInputs = new List<StackInput>();
        protected List<StackOutput> _stackOutputs = new List<StackOutput>();
        protected List<NodeProperty> _properties = new List<NodeProperty>();

        public IReadOnlyList<FlowOutput> FlowOutputs => _flowOutputs;
        public IReadOnlyList<StackInput> StackInputs => _stackInputs;
        public IReadOnlyList<StackOutput> StackOutputs => _stackOutputs;
        public IReadOnlyList<NodeProperty> Properties => _properties;

        /// <summary>
        /// Runs the node function.
        /// </summary>
        /// <param name="ctx">Current execution context.</param>
        /// <returns>The number of flow output indices pushed onto stack. FlowControlNodes can select which outputs to activate.</returns>
        protected abstract int OnExecute(ExecutionContext ctx);

        internal int Execute(ExecutionContext ctx)
        {
            var frame = ctx.Push(this);
            ProcessStackInputs(ctx);
            DebugLastActivatedFrameIndex = UnityEngine.Time.frameCount;
            return OnExecute(ctx);
        }

        protected virtual void PlaceOutputOntoStack(ExecutionContext ctx, VariableType type, int refIndex)
        {
            throw new NotImplementedException();
        }

        private void ProcessStackInputs(ExecutionContext ctx)
        {
            var frame = ctx.Peek();

            // Process stack inputs
            for (int i = 0; i < _stackInputs.Count; ++i)
            {
                var reverseIndex = _stackInputs.Count - i - 1;
                var si = _stackInputs[reverseIndex];
                if (si.refNodeGuid == SerializableGuid.empty)
                {
                    // Constant value
                    ctx.Stack.Place(si.Type, si.variable.Get(0));
                }
                else //TODO(optimization): don't have to call the same node dependency multiple times
                {
                    // Reference value
                    var node = (Node)frame.Graph.GetNode(si.refNodeGuid);
                    node.PlaceOutputOntoStack(ctx, si.Type, si.refIndex);

                    DebugStoreLastValue(ctx.Stack, si);
                }
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void DebugStoreLastValue(Stack stack, StackInput si)
        {
            // Debug (store last value)
            var stackHandle = stack.Peek(stack.Count - 1);
            var debugHandle = si.debugLastValue.Get(0);
            VarHandleContainers.Copy(stackHandle, debugHandle);
        }

        internal SerializableNodeEntry Serialize(ISerializationContext ctx)
        {
            var entry = new SerializableNodeEntry();
            entry.NodeType = SerializationUtils.GetSerializableNodeName(this.GetType());
            entry.Properties = new List<SerializableNodePropertyEntry>();
            entry.FlowOutputs = new List<SerializableFlowOutputEntry>();
            entry.StackInputs = new List<SerializableStackInputEntry>();
            entry.StackOutputs = new List<SerializableStackOutputEntry>();
            OnSerialize(ctx, entry);

            foreach (var fo in _flowOutputs)
            {
                var foEntry = new SerializableFlowOutputEntry();
                foEntry.Name = fo.Name;
                foEntry.Target = fo.refNodeGuid;
                entry.FlowOutputs.Add(foEntry);
            }
            
            foreach (var si in _stackInputs)
            {
                var siEntry = new SerializableStackInputEntry();
                siEntry.Name = si.Name; //TODO: should this be serialized when UserDefinedStackInputs is false?
                siEntry.Type = si.Type;
                siEntry.Constant = si.variable.Get(0).Serialize(ctx);
                siEntry.ReferenceTarget = si.refNodeGuid;
                siEntry.ReferenceIndex = si.refIndex;
                entry.StackInputs.Add(siEntry);
            }

            foreach (var p in _properties)
            {
                var pEntry = new SerializableNodePropertyEntry();
                pEntry.Name = p.Name;
                pEntry.Type = p.Type;
                pEntry.Value = p.variable.Get(0).Serialize(ctx);
                entry.Properties.Add(pEntry);
            }

            return entry;
        }

        internal string Deserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            var deserializationError = OnDeserialize(ctx, entry);
            if (deserializationError != null)
                return deserializationError;

            // Deserialize and match flow outputs
            for (int i = 0; i < entry.FlowOutputs.Count; ++i)
            {
                var foEntry = entry.FlowOutputs[i];
                var fo = _flowOutputs[i];
                if (fo.Name != foEntry.Name)
                    UnityEngine.Debug.LogWarning($"Node {this.GetType().Name} flow output name mismatch. Expected '{fo.Name}', deserialized '{foEntry.Name}'.");
                fo.refNodeGuid = foEntry.Target;
            }

            // Deserialize and match stack inputs
            for (int i = 0; i < entry.StackInputs.Count; ++i)
            {
                var siEntry = entry.StackInputs[i];
                StackInput si;
                if (UserDefinedStackInputs)
                {
                    si = new StackInput();
                    si.Name = siEntry.Name;
                    si.Type = siEntry.Type;
                    si.variable = VarHandleContainers.Create(siEntry.Type);
                    si.debugLastValue = VarHandleContainers.Create(siEntry.Type);
                    _stackInputs.Add(si);
                }
                else
                {
                    si = _stackInputs[i]; // Expects inputs to exist, and matches bindings
                    if (si.Name != siEntry.Name)
                        UnityEngine.Debug.LogWarning($"Node {this.GetType().Name} stack input name mismatch. Expected '{si.Name}', deserialized '{siEntry.Name}'.");
                }
                si.variable.Get(0).Deserialize(ctx, siEntry.Constant);
                si.refNodeGuid = siEntry.ReferenceTarget;
                si.refIndex = siEntry.ReferenceIndex;
            }

            OnDeserializeProperties(ctx, entry);
            OnFinishDeserialize(ctx, entry);
            return null;
        }

        internal void RemoveLinksToNode(SerializableGuid id)
        {
            for (int i = 0; i < _flowOutputs.Count; ++i)
            {
                var fo = _flowOutputs[i];
                if (fo.refNodeGuid == id)
                    fo.refNodeGuid = SerializableGuid.empty;
            }

            for (int i = 0; i < _stackInputs.Count; ++i)
            {
                var si = _stackInputs[i];
                if (si.refNodeGuid == id)
                    si.refNodeGuid = SerializableGuid.empty;
            }
        }

        protected virtual void OnSerialize(ISerializationContext ctx, SerializableNodeEntry entry)
        {
        }

        // Return error string on failure
        protected virtual string OnDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            return null;
        }

        protected virtual void OnDeserializeProperties(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            // Deserialize and match properties
            for (int i = 0; i < entry.Properties.Count; ++i)
            {
                var pEntry = entry.Properties[i];
                var p = _properties.Find(x => x.Name == pEntry.Name); // Expects properties to exist
                p.variable.Get(0).Deserialize(ctx, pEntry.Value);
            }
        }

        protected virtual void OnFinishDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
        }
    }

    static class SpecialNodeTypes
    {
        [ClearOnReload(newInstance: true)]
        static List<Type> _types = new List<Type>();

        public static IReadOnlyList<Type> Types => _types;

        static SpecialNodeTypes()
        {
            Initialize();
        }

        [ExecuteOnReload]
        static void Initialize()
        {
            var derived = GetAllDerivedClasses(typeof(Node));
            foreach (var type in derived)
            {
                if (type.IsAbstract)
                    continue;
                if (type.IsSubclassOf(typeof(BindingNode)))
                    continue;

                _types.Add(type);
            }
        }

        public static List<Type> GetAllDerivedClasses(this Type baseClass)
        {
            var result = new List<Type>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
#if NET_4_6
                if (asm is System.Reflection.Emit.AssemblyBuilder)
                    continue;
#endif
                if (asm.IsDynamic)
                    continue; // GetExportedTypes does not work on dynamic assemblies

                if (baseClass.IsInterface)
                {
                    foreach (var type in asm.GetTypes())
                    {
                        foreach (var interfaceType in type.GetInterfaces())
                        {
                            if (baseClass == interfaceType)
                            {
                                result.Add(type);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.IsSubclassOf(baseClass))
                            result.Add(type);
                    }
                }
            }

            return result;
        }
    }
}
