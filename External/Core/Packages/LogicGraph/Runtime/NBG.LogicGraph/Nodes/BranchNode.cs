using NBG.LogicGraph.Nodes;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeCategoryPath("Flow")]
    [NodeSerialization("Branch")]
    class BranchNode : FlowControlNode, INodeOnInitialize
    {
        public override string Name
        {
            get => "Branch";
        }

        void INodeOnInitialize.OnInitialize()
        {
            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_stackInputs.Count == 0);

            var fo0 = new FlowOutput();
            fo0.Name = "true";
            _flowOutputs.Add(fo0);

            var fo1 = new FlowOutput();
            fo1.Name = "false";
            _flowOutputs.Add(fo1);

            var si = new StackInput();
            si.Name = "condition";
            si.Type = VariableType.Bool;
            si.variable = VarHandleContainers.Create(VariableType.Bool);
            si.debugLastValue = VarHandleContainers.Create(VariableType.Bool);
            _stackInputs.Add(si);
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            var condition = ctx.Stack.PopBool();
            ctx.Stack.PushInt(condition ? 0 : 1);
            return 1;
        }
    }
}
