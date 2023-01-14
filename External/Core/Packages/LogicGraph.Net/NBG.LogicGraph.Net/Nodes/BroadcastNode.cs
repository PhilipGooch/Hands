using NBG.LogicGraph.Nodes;
using UnityEngine;

namespace NBG.LogicGraph.Net
{
    [NodeCategoryPath("Flow")]
    [NodeSerialization("Broadcast")]
    class BroadcastNode : Node, INodeOnInitialize, INodeOnUpdate, INodeValidation
    {
        public override string Name => $"Send Broadcast (id: {BroadcastId})";
        public override NodeAPIScope Scope => NodeAPIScope.Sim;
        public override bool HasFlowInput { get { return true; } }

        public int BroadcastId
        {
            get => _properties[0].variable.Get(0).Get<int>();
            private set => _properties[0].variable.Get(0).Set<int>(value);
        }

        private bool _triggered;

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_properties.Count == 0);
            var p = new NodeProperty();
            p.Name = "BroadcastId";
            p.Type = VariableType.Int;
            p.variable = VarHandleContainers.Create(p.Type);
            _properties.Add(p);

            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            _triggered = true; //TODO: networking via a delay-event-bus
            return 0;
        }

        void INodeOnUpdate.OnUpdate(float dt)
        {
            if (_triggered)
            {
                _triggered = false;

                var id = BroadcastId;
                foreach (var node in Owner.Nodes.Values)
                {
                    var receiverNode = node as OnBroadcastNode;
                    if (receiverNode == null)
                        continue;

                    if (receiverNode.BroadcastId == id)
                    {
                        LogicGraph.Traverse(receiverNode, NodeAPIScope.View);
                    }
                }
            }
        }

        string INodeValidation.CheckForErrors()
        {
            foreach (var node in Owner.Nodes.Values)
            {
                var proxyNode = node as BroadcastNode;
                if (proxyNode == null)
                    continue;
                if (proxyNode == this)
                    continue;
                if (proxyNode.BroadcastId == BroadcastId)
                {
                    return "Duplicate broadcast id";
                }
            }
            return null;
        }
    }
}
