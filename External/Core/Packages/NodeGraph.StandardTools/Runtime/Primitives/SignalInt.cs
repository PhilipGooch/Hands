namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Primitives)]
    public class SignalInt : Node
    {
        public NodeOutputInt output;
        public override void Process()
        {
            base.Process();
            output.SetValue(output.initialValue);
        }
    }
}