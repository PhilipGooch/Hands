using Drawing;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public class GripAxis : MonoBehaviourGizmos, IGripMarker
    {

        public float radius = .05f;
        public float limitFrom = -1;
        public float limitTo = 1;
        float gripWidth = .4f;
        //private void OnDrawGizmosSelected()
        //{

        //    Gizmos.color = Color.yellow;
        //    for (int i = 0; i <= 10; i++)
        //        Gizmos.DrawSphere(transform.TransformPoint(Vector3.forward * Mathf.Lerp(limitFrom, limitTo, 1f * i / 10)), radius);
        //    Gizmos.DrawLine(transform.TransformPoint(Vector3.forward * limitFrom), transform.TransformPoint(Vector3.forward * limitTo));
        //}

        public override void DrawGizmos()
        {
            var drawRadius = radius < .05 && limitFrom == limitTo ? .05f : radius; // ensure visible
            Draw.WireCylinder(transform.position + limitFrom * transform.forward, transform.forward, limitTo - limitFrom,  drawRadius, new Color(1, .75f, 0));
        }


        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable, int bodyId, int gripId, in HandCarryData otherHand)
        {

            var x = (float3)transform.position;
            var normal = (float3)transform.forward;
            var handAnchor = inCarryable ? reach.worldPalmPos : reach.actualPos;
            var projection = limitFrom == limitTo ? normal * limitFrom +x :
                re.ProjectPointOnSegment(handAnchor-x, normal * limitFrom, normal * limitTo) + x;

            if (otherHand.IsCarrying(bodyId)&&otherHand.gripId==gripId && limitFrom != limitTo) // two handed grab of same grip
            {
                var otherAnchor = Carry.GetWorldAnchor(otherHand);
                if (math.dot(projection - otherAnchor, normal) < 0) normal *= -1;
                projection = otherAnchor + normal * gripWidth;
            }
            if (inCarryable)
            {
                // for carryable anchor core to a position offset from palm by axis radius
                return HandTargetGrip.FromRelativeHandAnchor(projection, reach.relativePalmAnchor+reach.worldPalmDir*radius);
            }
            else
            {
                if(radius>0) throw new System.InvalidOperationException("Non carryable objects can only use thin (radius=0) axes");
                return HandTargetGrip.FromRelativeHandAnchor( projection, float3.zero);
            }
        }

    }
}