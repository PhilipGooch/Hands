using UnityEngine;

namespace NBG.LogicGraph.Net
{
    [NodeCategoryPath("Flow")]
    [NodeSerialization("OnBroadcast")]
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    class OnBroadcastNode : Node, INodeOnInitialize, INodeValidation
    {
        public override string Name => $"On Broadcast (id: {BroadcastId})";
        public override NodeAPIScope Scope => NodeAPIScope.View;
        public override bool HasFlowInput { get { return false; } }

        public int BroadcastId
        {
            get => _properties[0].variable.Get(0).Get<int>();
            private set => _properties[0].variable.Get(0).Set<int>(value);
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);
            var p = new NodeProperty();
            p.Name = "BroadcastId";
            p.Type = VariableType.Int;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);

            Debug.Assert(_flowOutputs.Count == 0);
            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            // Nothing to do
            return 0;
        }

        string INodeValidation.CheckForErrors()
        {
            int senders = 0;
            foreach (var node in Owner.Nodes.Values)
            {
                var proxyNode = node as BroadcastNode;
                if (proxyNode == null)
                    continue;
                if (proxyNode.BroadcastId == BroadcastId)
                    ++senders;
            }
            
            if (senders == 0)
            {
                return "No senders with this broadcast id";
            }

            return null;
        }
    }
}
