using NBG.Core;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.Getter)]
    [NodeHideInUI]
    [NodeSerialization("Variable")]
    class VariableNode : Node, INodeOnInitialize
    {
        public SerializableGuid VariableId
        {
            get => _properties[0].variable.Get(0).Get<SerializableGuid>();
            set
            {
                _properties[0].variable.Get(0).Set<SerializableGuid>(value);
                OnVariableIdUpdated();
            }
        }

        public override string Name
        {
            get
            {
                var variable = (LogicGraphVariable)this.Owner.Variables[VariableId];
                return $"Variable: {variable.Name}";
            }
        }

        void INodeOnInitialize.OnInitialize()
        {
            // Register the id property
            var p = new NodeProperty();
            p.Name = "VariableId";
            p.Type = VariableType.Guid;
            p.Flags |= NodePropertyFlags.Hidden;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);  
        }

        void OnVariableIdUpdated()
        {
            var variable = this.Owner.Variables[VariableId];

            // Register or update the data output
            if (_stackOutputs.Count == 0)
                _stackOutputs.Add(new StackOutput());
            var so = _stackOutputs[0];
            so.Name = "value";
            so.Type = variable.Type;
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            return 0;
        }

        protected override void PlaceOutputOntoStack(ExecutionContext ctx, VariableType type, int refIndex)
        {
            Debug.Assert(refIndex == 0);

            // VariableNode outputs come from LogicGraph variables and are not stack-owned.
            // Lifetime: forever.
            // Duplicate them on top without taking extra ownership.
            var variable = (LogicGraphVariable)this.Owner.Variables[VariableId];
            var handle = variable.variable.Get(0);
            ctx.Stack.Place(type, handle);
        }

        protected override void OnFinishDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            OnVariableIdUpdated();
        }
    }
}
