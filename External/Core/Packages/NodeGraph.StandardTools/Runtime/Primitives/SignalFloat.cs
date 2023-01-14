namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Primitives)]
    public class SignalFloat : Node
    {
        public NodeOutputFloat output;
        public override void Process()
        {
            output.SetValue(output.initialValue);

        }
    }
}