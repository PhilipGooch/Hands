namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class IntToBool : Node
    {
        public NodeInputInt input;
        public NodeOutputBool output;
        public int threshold = 1;
        public override void Process()
        {
            base.Process();
            output.SetValue(input.value >= threshold);
        }
    }
}
