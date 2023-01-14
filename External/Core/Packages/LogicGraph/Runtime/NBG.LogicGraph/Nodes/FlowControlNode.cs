using System;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.FlowControl)]
    abstract class FlowControlNode : Node
    {
        public override bool HasFlowInput { get { return true; } }
    }
}
