using Drawing;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // snaps hand to a circle:
    // - carryables grab with palm (e.g. coin)
    // - non-carryables grab with hand center (e.g. steering wheel or valve)
    public class GripCircle : MonoBehaviourGizmos, IGripMarker
    {
        public float radius = .25f;

        public override void DrawGizmos()
        {
            //Draw.WireSphere(transform.position,.1f, new Color(.5f, 1, 0));
            Draw.Circle(transform.position, transform.forward, radius, new Color(1, .75f, 0));
            //Draw.Arrow(transform.position, transform.position + transform.forward * .5f, transform.up, .1f, new Color(.5f, 1, 0));
        }
        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable, int bodyId, int gripId, in HandCarryData otherHand)
        {
            var x = (float3)transform.position;
            var handAnchor = inCarryable ? reach.worldPalmPos : reach.actualPos;
            var projection = math.normalizesafe(re.ProjectOnPlane( handAnchor - x, transform.forward)) * radius + x;

            if (inCarryable) // create at palm
                return HandTargetGrip.FromRelativeHandAnchor(projection, reach.relativePalmAnchor);
            else // create at hand center
                return HandTargetGrip.FromRelativeHandAnchor(projection, float3.zero);
        }
    }
}
