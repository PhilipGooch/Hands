using NBG.Core;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.Getter)]
    [NodeSerialization("HandleCustomGetter")]
    class HandleCustomGetterNode : Node, INodeOnInitialize, INodeObjectContext, INodeVariantHandler, INodeValidation, INodeResettable
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
                    var targetNode = (CustomGetterNode)graph.TryGetNode(Variant);
                    if (targetNode != null)
                    {
                        variantName = targetNode.OutputName;
                    }
                }
                return $"Get {variantName} (custom)";
            }
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);

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
            if (ObjectContext == null)
                throw new System.InvalidOperationException();

            var container = (ILogicGraphContainer)ObjectContext;
            var graph = container.Graph;
            Debug.Assert(graph != null);
            var node = (CustomGetterNode)graph.TryGetNode(Variant);
            if (node == null)
                throw new System.InvalidOperationException($"Couldn't find target of {this.Name}.");

            var countBefore = ctx.Stack.Count;
            node.Execute(ctx); // NOTE: Data nodes are executed on every request
            Debug.Assert(ctx.Stack.Count == countBefore + 1);

            var frame = ctx.Last(node);
            ctx.Pop(cleanupStack: false);
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
            var node = (CustomGetterNode)targetGraph.GetNode(Variant);

            _stackOutputs.Clear();
            foreach (var si in node.StackInputs) // Map target inputs to self outputs
            {
                var so = new StackOutput();
                so.Name = si.Name;
                so.Type = si.Type;
                _stackOutputs.Add(so);
            }
        }

        string INodeValidation.CheckForErrors()
        {
            if (ObjectContext == null)
                return $"Target {nameof(LogicGraph)} is missing";

            var container = (ILogicGraphContainer)ObjectContext;
            var graph = container.Graph;
            var targetNode = (CustomGetterNode)graph.TryGetNode(Variant);
            if (targetNode == null)
                return $"Target {nameof(CustomGetterNode)} is not found";

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
