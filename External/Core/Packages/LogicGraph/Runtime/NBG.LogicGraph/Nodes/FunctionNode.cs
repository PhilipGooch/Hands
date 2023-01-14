using System;
using UnityEngine;

namespace NBG.LogicGraph.Nodes
{
    [NodeSerialization("Function")]
    class FunctionNode : BindingNode, INodeBinding
    {
        private UserlandMethodBinding MethodBinding => (UserlandMethodBinding)binding;

        public override string Name => binding.Description;
        public override NodeAPIScope Scope => MethodBinding.NodeAPI.Scope;

        public override bool HasFlowInput
        {
            get
            {
                if (MethodBinding.NodeAPI.Flags.HasFlag(NodeAPIFlags.ForceFlowNode))
                    return true;
                return !MethodBinding.HasReturnValues;
            }
        }

        void INodeBinding.OnDeserializedBinding(UserlandBinding binding_)
        {
            var binding = (UserlandMethodBinding)binding_;

            Debug.Assert(_flowOutputs.Count == 0);
            Debug.Assert(_stackInputs.Count == 0);
            Debug.Assert(_stackOutputs.Count == 0);

            this.binding = binding;

            if (binding.IsStatic)
            {
                Debug.Assert(this.ObjectContext == null);
            }

            if (HasFlowInput)
            {
                var fo = new FlowOutput();
                fo.Name = "out";
                _flowOutputs.Add(fo);
            }

            if (binding.Source.ReturnType != typeof(void))
            {
                var so = new StackOutput();
                so.Name = "ret";
                so.Type = VariableTypes.FromSystemType(binding.Source.ReturnType);
                _stackOutputs.Add(so);
            }

            for (int i = 0; i < binding.Parameters.Count; ++i)
            {
                var parameter = binding.Parameters[i];
                var p = parameter.pi;
                if (p.IsOut)
                {
                    var so = new StackOutput();
                    so.Name = p.Name;
                    so.Type = VariableTypes.FromSystemType(p.ParameterType.GetElementType());
                    //p.Position //TODO: can't assume parameter list is ordered?
                    _stackOutputs.Add(so);
                }
                else
                {
                    var si = new StackInput();
                    si.Name = p.Name;
                    si.Type = VariableTypes.FromSystemType(p.ParameterType);
                    si.BackingType = p.ParameterType;
                    si.variants = parameter.variants;
                    si.variable = VarHandleContainers.Create(si.Type);
                    si.debugLastValue = VarHandleContainers.Create(si.Type);
                    _stackInputs.Add(si);
                }
            }
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            var stack = ctx.Stack;

            var targetIsValid = binding.IsStatic || (this.ObjectContext != null);
            if (targetIsValid)
            {
                MethodBinding.Func(this.ObjectContext, stack);
                // Return values are now on stack
            }

            return 0;
        }

        protected override void PlaceOutputOntoStack(ExecutionContext ctx, VariableType type, int refIndex)
        {
            // FunctionNode outputs come from the binding function and are stack-owned.
            // FunctionNodes with return values are only considered for the data-flow.
            // Do not duplicate them.
            var node = this;

            if (node.HasFlowInput)
            {
                var frame = ctx.Last(this);

                // FunctionNode in flow mode outputs come from the original invocation of the node.
                // Lifetime: entire execution of the node chain, originating at this FunctionNode.
                // Duplicate them on top without taking extra ownership.
                var index = frame.StackBottom + refIndex;
                var handle = ctx.Stack.Peek(index);
                ctx.Stack.Place(type, handle);
            }
            else
            {
                node.Execute(ctx); // NOTE: Data nodes are executed on every request

                var frame = ctx.Last(this);
                
                // Only leave the one handle we are interested it on stack
                for (int i = 0; i < node.StackOutputs.Count; ++i)
                {
                    var reverseIndex = node.StackOutputs.Count - i - 1;
                    if (reverseIndex != refIndex)
                    {
                        var offset = (reverseIndex > refIndex) ? 0 : 1;
                        ctx.Stack.Pop(offset);
                    }
                }

                ctx.Pop(cleanupStack: false);
            }
        }
    }
}
