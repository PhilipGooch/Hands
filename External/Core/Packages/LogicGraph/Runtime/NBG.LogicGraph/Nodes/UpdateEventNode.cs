using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeConceptualType(NodeConceptualType.EntryPoint)]
    [NodeCategoryPath("Event")]
    [NodeSerialization("UpdateEvent")]
    class UpdateEventNode : Node, INodeOnInitialize
    {
        public override string Name
        {
            get => "OnUpdate (View)";
        }

        public override NodeAPIScope Scope => NodeAPIScope.View;

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
