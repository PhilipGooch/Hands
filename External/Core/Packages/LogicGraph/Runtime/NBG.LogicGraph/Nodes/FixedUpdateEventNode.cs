using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    [NodeCategoryPath("Event")]
    [NodeSerialization("FixedUpdateEvent")]
    class FixedUpdateEventNode : Node, INodeOnInitialize
    {
        public override string Name
        {
            get => "OnFixedUpdate (Sim)";
        }

        public override NodeAPIScope Scope => NodeAPIScope.Sim;

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_flowOutputs.Count == 0);
            
            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            // Verify stack has no args
            var frame = ctx.Peek();
            Debug.Assert(frame.StackBottom == ctx.Stack.Count);
            
            // Nothing to do
            return 0;
        }
    }
}
