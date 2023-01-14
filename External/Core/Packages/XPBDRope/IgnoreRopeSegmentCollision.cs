using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.XPBDRope
{
    public class IgnoreRopeSegmentCollision
    {
        List<Collider> collidersToIgnore = new List<Collider>();
        RopeSegment currentSegment;

        public IgnoreRopeSegmentCollision(Rigidbody targetBody)
        {
            if (targetBody != null)
            {
                targetBody.GetComponentsInChildren(collidersToIgnore);
            }
        }

        public void UpdateCollisionIgnore(RopeSegment newSegment)
        {
            if (newSegment != currentSegment)
            {
                foreach (var collider in collidersToIgnore)
                {
                    if (currentSegment != null)
                    {
                        Physics.IgnoreCollision(collider, currentSegment.capsule, false);
                    }
                    if (newSegment != null)
                    {
                        Physics.IgnoreCollision(collider, newSegment.capsule, true);
                    }
                }

                currentSegment = newSegment;
            }
        }
    }
}

