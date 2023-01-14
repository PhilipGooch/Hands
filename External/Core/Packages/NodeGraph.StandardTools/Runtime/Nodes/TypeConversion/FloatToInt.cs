using UnityEngine;

namespace NBG.NodeGraph
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Utils, subFolder = "Conversion")]
    public class FloatToInt : Node
    {
        public NodeInputFloat input;
        public NodeOutputInt output;
        enum RoundType
        {
            Floor,
            Ceil
        }
        RoundType type = RoundType.Floor;

        public override void Process()
        {
            base.Process();
            output.SetValue(type == RoundType.Floor ? Mathf.FloorToInt(input.value) : Mathf.CeilToInt(input.value));
        }
    }
}
