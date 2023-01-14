using NBG.Core;
using UnityEngine;
using UnityEngine.Assertions;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    [NodeSerialization("HandleCustomOutput")]
    class HandleCustomOutputNode : Node, INodeOnInitialize, INodeOnEnable, INodeOnDisable, INodeObjectContext, INodeVariantHandler, INodeValidation, INodeResettable
    {
        public SerializableGuid Variant
        {
            get => _properties[0].variable.Get(0).Get<SerializableGuid>();
            private set => _properties[0].variable.Get(0).Set<SerializableGuid>(value);
        }
        public UnityEngine.Object ObjectContext
        {
            get => _properties[1].variable.Get(0).Get<UnityEngine.Object>();
            set => _properties[1].variable.Get(0).Set<UnityEngine.Object>(value);
        }

        public override string Name
        {
            get
            {
                var variantName = "<unknown>";
                if (ObjectContext != null)
                {
                    var graph = ((ILogicGraphContainer)ObjectContext).Graph;
                    Debug.Assert(graph != null);
                    var targetNode = (CustomOutputNode)graph.TryGetNode(Variant);
                    if (targetNode != null)
                    {
                        variantName = targetNode.OutputName;
                    }
                }
                return $"On {variantName} (custom)";
            }
        }

        CustomOutputNode.Listener _listener;

        CustomOutputNode.Listener Listener
        {
            get
            {
                if (_listener == null)
                {
                    _listener = new CustomOutputNode.Listener();
                    _listener.source = ((ILogicGraphContainer)((INodeObjectContext)this).ObjectContext).Graph;
                    Debug.Assert(_listener.source != null);
                    _listener.target = this;
                    _listener.variant = Variant;
                }
                return _listener;
            }
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_properties.Count == 0);

            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);

            // Name
            {
                var p = new NodeProperty();
                p.Name = "Variant";
                p.Type = VariableType.Guid;
                p.variable = VarHandleContainers.Create(p.Type);
                _properties.Add(p);
            }

            // Context
            {
                var p = new NodeProperty();
                p.Name = "OutputContext";
                p.Type = VariableType.UnityObject;
                p.Flags |= NodePropertyFlags.Hidden;
                p.variable = VarHandleContainers.Create(p.Type);
                _properties.Add(p);
            }
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

        public void SetVariant(INodeVariant variant)
        {
            if (Application.isPlaying)
                throw new System.NotSupportedException();
            Debug.Assert(this.ObjectContext != null);

            Variant = variant.VariantBacking;

            var targetGraph = ((ILogicGraphContainer)this.ObjectContext).Graph;
            var node = (CustomOutputNode)targetGraph.GetNode(Variant);

            _stackOutputs.Clear();
            foreach (var si in node.StackInputs) // Map target inputs to self outputs
            {
                var so = new StackOutput();
                so.Name = si.Name;
                so.Type = si.Type;
                _stackOutputs.Add(so);
            }
        }

        void INodeOnEnable.OnEnable()
        {
            // Bind
            Assert.IsNotNull(Listener);
            CustomOutputNode._listeners.Add(Listener);
        }

        void INodeOnDisable.OnDisable()
        {
            // Unbind
            Assert.IsNotNull(Listener);
            CustomOutputNode._listeners.Remove(Listener);
        }

        string INodeValidation.CheckForErrors()
        {
            if (ObjectContext == null)
                return $"Target {nameof(LogicGraph)} is missing";

            var container = (ILogicGraphContainer)ObjectContext;
            var graph = container.Graph;
            var targetNode = (CustomOutputNode)graph.TryGetNode(Variant);
            if (targetNode == null)
                return $"Target {nameof(CustomOutputNode)} is not found";

            if (StackOutputs.Count != targetNode.StackInputs.Count)
                return $"Argument count mismatch";

            for (int i = 0; i < StackOutputs.Count; ++i)
            {
                var so = StackOutputs[i];
                var si = targetNode.StackInputs[i];
                if (so.Type != si.Type)
                    return $"Argument type mismatch";
            }

            return null;
        }
    }
}
