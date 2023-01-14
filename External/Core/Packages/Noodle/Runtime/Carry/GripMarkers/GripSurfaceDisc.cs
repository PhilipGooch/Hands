using Drawing;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // surface disc, ideal for buttons - will grab object touching the surface with hand
    public class GripSurfaceDisc : MonoBehaviourGizmos, IGripMarker
    {
        public float radius = .1f;
        public override void DrawGizmos()
        {
            //Draw.WireSphere(transform.position,.1f, new Color(.5f, 1, 0));
            Draw.Circle(transform.position, transform.forward, radius, new Color(1, .75f, 0));
            Draw.Arrow(transform.position, transform.position + transform.forward * .5f, transform.up, .1f, new Color(1, .75f, 0));
        }

        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable, int bodyId, int gripId, in HandCarryData otherHand)
        {
            if (inCarryable)
                throw new System.InvalidOperationException ("This grip marker is designed for buttons, etc, not carryables");
            var x = (float3)transform.position;
            var normal = (float3)transform.forward;
            var handAnchor = inCarryable ? reach.worldPalmPos : reach.actualPos;
            var projection = re.Clamp( re.ProjectOnPlane(handAnchor - x,normal),radius) + x;
            var worldHandPos = projection + normal * reach.radius;

            return HandTargetGrip.FromRelativeHandAnchor(worldHandPos, float3.zero);
        }
        private void OnValidate()
        {
            Debug.Assert(radius > 0,"radius must be greater than 0",this);
        }
    }
}
