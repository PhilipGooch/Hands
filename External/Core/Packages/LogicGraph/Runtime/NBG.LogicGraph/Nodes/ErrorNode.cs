namespace NBG.LogicGraph.Nodes
{
    class ErrorNode : Node, INodeValidation
    {
        public override string Name => "Internal Error";

        public string ErrorMsg;
        public SerializableNodeEntry Backup;

        protected override int OnExecute(ExecutionContext ctx)
        {
            throw new System.InvalidOperationException("Trying to execute an ErrorNode! Please fix the graph.");
        }

        public string CheckForErrors()
        {
            return ErrorMsg;
        }
    }
}
