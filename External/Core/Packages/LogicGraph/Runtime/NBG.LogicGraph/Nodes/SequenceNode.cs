using NBG.LogicGraph.Nodes;

namespace NBG.LogicGraph.Nodes
{
    [NodeCategoryPath("Flow")]
    [NodeSerialization("Sequence")]
    class SequenceNode : FlowControlNode, INodeCustomFlow
    {
        public override string Name
        {
            get => "Sequence";
        }

        protected override int OnExecute(ExecutionContext ctx)
        {
            var count = _flowOutputs.Count;
            for (int i = count - 1; i >= 0; --i)
                ctx.Stack.PushInt(i);
            return count;
        }

        protected override string OnDeserialize(IDeserializationContext ctx, SerializableNodeEntry entry)
        {
            // Populate all flow outputs
            foreach (var foEntry in entry.FlowOutputs)
            {
                var fo = new FlowOutput();
                fo.Name = foEntry.Name;
                fo.refNodeGuid = foEntry.Target;
                _flowOutputs.Add(fo);
            }

            return null;
        }

        public void AddCustomFlow()
        {
            var fo = new FlowOutput();
            fo.Name = "out";
            _flowOutputs.Add(fo);
        }

        public void RemoveCustomFlow(int index)
        {
            _flowOutputs.RemoveAt(index);
        }
    }
}
