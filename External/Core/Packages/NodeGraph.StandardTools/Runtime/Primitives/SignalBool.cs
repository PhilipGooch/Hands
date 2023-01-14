namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Primitives)]
    public class SignalBool : Node
    {
        public NodeOutputBool output;
        public override void Process()
        {
            base.Process();
            output.SetValue(output.initialValue);

        }
    }
}