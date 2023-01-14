using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    // This is more of an example of the IRopeSegmentCreationListener - not a very efficient debugger
    public class RopeDebugger : MonoBehaviour, IRopeSegmentCreationListener
    {
        public void AfterSegmentCreation(RopeSegment target)
        {
            target.gameObject.AddComponent<RopeSegmentDebugger>();
        }

        public void BeforeSegmentCreation(GameObject target)
        {
        }
    }
}

