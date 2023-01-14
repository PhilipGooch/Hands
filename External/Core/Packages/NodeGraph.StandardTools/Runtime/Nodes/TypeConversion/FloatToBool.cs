namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class FloatToBool : Node
    {
        public NodeInputFloat input;
        public NodeOutputBool output;
        public float threshold = 1;
        public override void Process()
        {
            base.Process();
            output.SetValue(input.value >= threshold);
        }
    }
}
