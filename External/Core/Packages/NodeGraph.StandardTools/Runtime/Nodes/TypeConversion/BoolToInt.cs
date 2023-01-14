namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class BoolToInt : Node
    {
        public NodeInputBool input;
        public NodeOutputInt output;
        public override void Process()
        {
            base.Process();
            output.SetValue(input.value ? 1 : 0);
        }
    }
}
