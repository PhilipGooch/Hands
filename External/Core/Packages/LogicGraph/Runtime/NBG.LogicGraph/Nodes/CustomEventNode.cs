using NBG.Core;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    [NodeSerialization("CustomEvent")]
    class CustomEventNode : Node, INodeOnInitialize, INodeCustomIO, INodeVariant
    {
        public override string Name
        {
            get => "Event (custom)";
        }

        SerializableGuid INodeVariant.VariantBacking => ((LogicGraph)Owner).GetNodeId(this);
        string INodeVariant.VariantName => EventName;
        System.Type INodeVariant.VariantHandler => typeof(CallCustomEventNode);

        public string EventName
        {
            get => _properties[0].variable.Get(0).Get<string>();
            private set => _properties[0].variable.Get(0).Set<string>(value);
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_properties.Count == 0);

            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);

            var p = new NodeProperty();
            p.Name = "EventName";
            p.Type = VariableType.String;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            // Verify stack has args
            var frame = ctx.Peek();
            for (int i = 0; i < _stackOutputs.Count; ++i)
            {
                var reverseIndex = _stackOutputs.Count - i - 1;
                var so = _stackOutputs[reverseIndex];
                var index = frame.StackBottom - reverseIndex - 1;
                var handle = ctx.Stack.Peek(index);
                Debug.Assert(handle.Container.Type == so.Type);
            }

            // Nothing to do
            return 0;
        }

        protected override void PlaceOutputOntoStack(ExecutionContext ctx, VariableType type, int refIndex)
        {
            var frame = ctx.Last(this);
            if (frame.Entry != this)
                throw new System.InvalidOperationException($"Trying to get outputs of {this.Name} which is not the entry point of the current execution context.");

            // CustomEventNode outputs come from CallCustomEventNode.
            // Lifetime: entire execution of the node chain, originating at this CustomEventNode.
            // Duplicate them on top without taking extra ownership.
            var index = frame.StackBottom - refIndex - 1;
            var handle = ctx.Stack.Peek(index);
            ctx.Stack.Place(type, handle);
        }

        protected override void OnSerialize(ISerializationContext ctx, SerializableNodeEntry entry)
        {
            // Serialize user defined outputs
            foreach (var so in _stackOutputs)
            {
                var soEntry = new SerializableStackOutputEntry();
                soEntry.Name = so.Name;
                soEntry.Type = so.Type;
                entry.StackOutputs.Add(soEntry);
            }
        }

        protected override string OnDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            // Deserialize user defined outputs
            for (int i = 0; i < entry.StackOutputs.Count; ++i)
            {
                var soEntry = entry.StackOutputs[i];
                var so = new StackOutput();
                so.Name = soEntry.Name;
                so.Type = soEntry.Type;
                _stackOutputs.Add(so);
            }

            return null;
        }

        public void SetEventName(string name)
        {
            EventName = name;
        }

        public void AddCustomIO(string name, VariableType type)
        {
            var so = new StackOutput();
            so.Name = name;
            so.Type = type;
            _stackOutputs.Add(so);
        }

        public void RemoveCustomIO(int index)
        {
            _stackOutputs.RemoveAt(index);
        }

        public void UpdateCustomIO(int index, string name, VariableType type)
        {
            var si = _stackOutputs[index];
            si.Name = name;
            si.Type = type;
        }

        public string GetCustomIOName(int index)
        {
            return _stackOutputs[index].Name;
        }

        public VariableType GetCustomIOType(int index)
        {
            return _stackOutputs[index].Type;
        }

        bool INodeCustomIO.CanAddAndRemove => true;
        public INodeCustomIO.Type CustomIOType => INodeCustomIO.Type.Outputs;
    }
}
