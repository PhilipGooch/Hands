//using Drawing;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.Mathematics;
//using UnityEngine;

//namespace Noodles
//{
//    public class GripSurfacePoint : MonoBehaviourGizmos, IGripMarker
//    {
//        //private void OnDrawGizmosSelected()
//        //{
//        //    Gizmos.color = Color.yellow;
//        //    Gizmos.DrawSphere(transform.position, .1f);
//        //}

//        public override void DrawGizmos()
//        {
//            //Draw.WireSphere(transform.position,.1f, new Color(.5f, 1, 0));
//            Draw.Circle(transform.position, transform.forward, .1f, new Color(.5f, 1, 0));
//            Draw.Arrow(transform.position, transform.position + transform.forward * .5f, transform.up, .1f, new Color(.5f, 1, 0));
//        }

//        public HandTargetGrip CalculateGrab(in HandReachInfo reach, bool inCarryable)
//        {
//            var x = (float3)transform.position;
//            var normal = (float3)transform.forward;
//            var worldHandPos = x + normal * reach.radius;
//            if (inCarryable) // create at palm
//            {
//                float3 worldAnchor = transform.position;
//                return HandTargetGrip.FromRelativeHandAnchor(-1, worldAnchor, reach.relativePalmAnchor, quaternion.identity, GrabJointCollision.Collide);
//            }
//            else // create at hand center
//            {
//                float3 worldAnchor = transform.position + transform.forward * reach.radius;
//                return HandTargetGrip.FromRelativeHandAnchor(-1, worldAnchor, float3.zero, quaternion.identity, GrabJointCollision.Collide);
//            }
//        }

//    }
//}