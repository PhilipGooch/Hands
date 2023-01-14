using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    // This debugger is more of an example of the ISegmentRigidbodyListener interface - not very efficient.
    public class RopeSegmentDebugger : MonoBehaviour, ISegmentRigidbodyListener
    {
        Vector3 previousPosition;
        Vector3 nextPosition;
        float radius = 0f;

        public void BeforeReadingSegmentRecoilbody(RopeSegment target)
        {
            previousPosition = target.RecoilBody.x.pos + target.RecoilBody.v.linear * Time.fixedDeltaTime;
            radius = target.radius;
        }

        public void AfterWritingSegmentRecoilbody(RopeSegment target)
        {
            nextPosition = target.RecoilBody.x.pos + target.RecoilBody.v.linear * Time.fixedDeltaTime;
        }

        void OnDrawGizmos()
        {
            if (radius > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(previousPosition, radius);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(nextPosition, radius);
            }
        }
    }
}

