using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using Unity.Mathematics;

namespace NBG.XPBDRope
{
    public class RopeStartConnector
    {
        Rope target;
        ConfigurableJoint joint;
        bool fixPosition;
        bool firstAdjustment;

        RopeSegment previousSegment;

        public void Enable(Rope target, ConfigurableJoint joint, bool fixPosition)
        {
            this.target = target;
            this.joint = joint;
            this.fixPosition = fixPosition;
            firstAdjustment = true;

            if (joint != null)
            {
                target.onStartSegmentChanged += UpdateJoint;
            }
        }

        public void Disable()
        {
            if (joint != null)
            {
                target.onStartSegmentChanged -= UpdateJoint;
            }
        }

        void UpdateJoint(RopeSegment segment)
        {
            if (segment != null)
            {
                joint.connectedBody = segment.body;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = segment.GetConnectionPoint();

                if (fixPosition && previousSegment != segment)
                {
                    segment.fixedPosition = true;
                    if (previousSegment != null)
                    {
                        previousSegment.fixedPosition = false;
                    }
                }

                // When rope is enabled, allow segments to be moved
                if (firstAdjustment)
                {
                    var startBody = target.BodyStartIsAttachedTo;
                    if (fixPosition || (startBody != null && startBody.isKinematic))
                    {
                        MoveRope(target.ActiveStartSegment);
                    }
                    firstAdjustment = false;
                }

            }
            else
            {
                joint.connectedBody = null;
            }

            previousSegment = segment;
        }

        void MoveRope(RopeSegment segment)
        {
            if (segment != null)
            {
                var connectedAnchorWorldPos = segment.body.TransformPoint(joint.connectedAnchor);
                var anchorWorldPos = joint.transform.TransformPoint(joint.anchor);
                float3 diff = anchorWorldPos - connectedAnchorWorldPos;

                for (int i = target.FirstActiveBone; i < target.BoneCount; i++)
                {
                    var otherSegment = target.bones[i];
                    if (!otherSegment.fixedPosition || segment == target.ActiveStartSegment)
                    {
                        var body = otherSegment.body;
                        var reBody = otherSegment.RecoilBody;
                        if (!body.isKinematic)
                        {
                            var placement = reBody.x;
                            placement.pos += diff;
                            ManagedWorld.main.SetBodyPlacementImmediate(otherSegment.Id, placement);
                        }
                    }
                }

                var attachedBody = target.BodyEndIsAttachedTo;
                if (attachedBody)
                {
                    if (!attachedBody.isKinematic)
                    {
                        var id = ManagedWorld.main.FindBody(attachedBody);
                        var placement = World.main.GetBodyPosition(id);
                        placement.pos += diff;
                        ManagedWorld.main.SetBodyPlacementImmediate(id, placement);
                    }
                }
            }
        }
    }
}

