using Drawing;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // snaps hand to transform origin:
    // - carryables grab with palm (snap markers for fixed hand positions)
    // - non-carryables grab with hand center (snap markers for fixed hand positions - allow sinking hands into surface a bit)
    public class GripPoint : MonoBehaviourGizmos, IGripMarker
    {
        //private void OnDrawGizmosSelected()
        //{
        //    Gizmos.color = Color.yellow;
        //    Gizmos.DrawSphere(transform.position, .1f);
        //}

        public override void DrawGizmos()
        {
            Draw.WireSphere(transform.position,.1f, new Color(1, .75f, 0));
            //Draw.Circle(transform.position,transform.forward, .1f, new Color(.5f, 1, 0));
            //Draw.Arrow(transform.position, transform.position + transform.forward*.5f, transform.up,.1f, new Color(.5f, 1, 0));
        }

        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable, int bodyId, int gripId, in HandCarryData otherHand)
        {
            float3 x = transform.position;
            if (inCarryable) // create at palm
                return HandTargetGrip.FromRelativeHandAnchor(x, reach.relativePalmAnchor);
            else // create at hand center
                return HandTargetGrip.FromRelativeHandAnchor(x, float3.zero);
        }

    }
}