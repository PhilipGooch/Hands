namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class IntToFloat : Node
    {
        public NodeInputInt input;
        public NodeOutputFloat output;

        public override void Process()
        {
            base.Process();
            output.SetValue(input.value);
        }
    }
}
