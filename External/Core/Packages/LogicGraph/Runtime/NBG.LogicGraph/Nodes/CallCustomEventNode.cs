using NBG.Core;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.Function)]
    [NodeSerialization("CallCustomEvent")]
    class CallCustomEventNode : Node, INodeOnInitialize, INodeObjectContext, INodeVariantHandler, INodeValidation, INodeResettable
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
                    var targetNode = (CustomEventNode)graph.TryGetNode(Variant);
                    if (targetNode != null)
                    {
                        variantName = targetNode.EventName;
                    }
                }
                return $"Call {variantName}";
            }
        }

        public override bool HasFlowInput { get { return true; } }
        public override bool UserDefinedStackInputs { get { return true; } }

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
                p.Name = "EventContext";
                p.Type = VariableType.UnityObject;
                p.Flags |= NodePropertyFlags.Hidden;
                p.variable = VarHandleContainers.Create(p.Type);
                _properties.Add(p);
            }
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            if (ObjectContext != null)
            {
                var container = (ILogicGraphContainer)ObjectContext;
                var graph = container.Graph;
                Debug.Assert(graph != null);
                var targetNode = (CustomEventNode)graph.TryGetNode(Variant);
                if (targetNode != null)
                {
                    LogicGraph.Traverse(targetNode, ctx.Scope);
                }
            }

            // Cleanup stack
            var frame = ctx.Peek();
            foreach (var _ in _stackInputs)
            {
                ctx.Stack.Pop();
            }

            return 0;
        }

        public void SetVariant(INodeVariant variant)
        {
            if (Application.isPlaying)
                throw new System.NotSupportedException();
            Debug.Assert(this.ObjectContext != null);

            Variant = variant.VariantBacking;

            var targetGraph = ((ILogicGraphContainer)this.ObjectContext).Graph;
            var node = (CustomEventNode)targetGraph.GetNode(Variant);
            
            _stackInputs.Clear();
            foreach (var so in node.StackOutputs) // Map target outputs to self inputs
            {
                var si = new StackInput();
                si.Name = so.Name;
                si.Type = so.Type;
                si.variable = VarHandleContainers.Create(so.Type);
                si.debugLastValue = VarHandleContainers.Create(so.Type);
                _stackInputs.Add(si);
            }
        }

        string INodeValidation.CheckForErrors()
        {
            if (ObjectContext == null)
                return $"Target {nameof(LogicGraph)} is missing";

            var container = (ILogicGraphContainer)ObjectContext;
            var graph = container.Graph;
            var targetNode = (CustomEventNode)graph.TryGetNode(Variant);
            if (targetNode == null)
                return $"Target {nameof(CustomEventNode)} is not found";

            if (StackInputs.Count != targetNode.StackOutputs.Count)
                return $"Argument count mismatch";

            for (int i = 0; i < StackInputs.Count; ++i)
            {
                var si = StackInputs[i];
                var so = targetNode.StackOutputs[i];
                if (si.Type != so.Type)
                    return $"Argument type mismatch";
            }

            return null;
        }
    }
}
