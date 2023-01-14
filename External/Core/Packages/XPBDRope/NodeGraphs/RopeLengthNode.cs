using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.NodeGraph;

namespace NBG.XPBDRope.Nodes
{
    [AddNodeMenuItem(QuickAddNodeMenu.Folder.Physics, subFolder = "XPBD Rope")]
    public class RopeLengthNode : Node
    {
        [SerializeField]
        Rope targetRope;

        public NodeInputFloat length;

        public override void Process()
        {
            base.Process();
            if (targetRope != null)
            {
                length.value = Mathf.Clamp01(length.value);
                targetRope.RopeLengthMultiplier = length.value;
            }
        }

        private void OnValidate()
        {
            if (targetRope == null)
            {
                targetRope = GetComponent<Rope>();
            }
        }
    }
}

