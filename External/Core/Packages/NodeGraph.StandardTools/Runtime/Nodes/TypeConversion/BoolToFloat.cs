using UnityEngine;

namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class BoolToFloat : Node
    {
        public NodeInputBool input;
        public NodeOutputFloat output;
        [SerializeField] private float multiplier = 1;
        public override void Process()
        {
            base.Process();
            output.SetValue(input.value ? multiplier : 0);
        }
    }
}
