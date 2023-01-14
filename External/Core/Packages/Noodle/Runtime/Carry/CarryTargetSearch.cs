using NBG.Entities;
using Recoil;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // implements target scanning algorithms
   
    public unsafe static class CarryTargetSearch
    {
        public static Collider[] neighbours = new Collider[64];
        static List<int> processedBodyIds = new List<int>();
        static List<IGripMarker> gripMarkers = new List<IGripMarker>();
        public static void ScanTargetInNeighbours(Collider[] neighbours, int hitCount, in HandReachInfo handReach, in Aim aim, ref HandTargetInfo target, bool left, int targetLayers, in HandCarryData otherHand)
        {
            for (int i = 0; i < hitCount; i++)
            {
                var hitCollider = neighbours[i];
                var subject = HandTargetSubject.FromCollider(hitCollider);
                if (CollisionUtils.MatchesLayerMask(targetLayers, hitCollider.gameObject.layer)) // only get closest point if marked as target
                {
                    if(subject.carryable!=null)
                        AcquireTargetInCarryableCollision(handReach, aim, ref target, subject, hitCollider.ClosestPoint(handReach.actualPos), otherHand);
                    else
                        AcquireTargetInCollider(handReach, ref target, subject, hitCollider.ClosestPoint(handReach.actualPos));
                }

                if (!World.IsEnvironment(subject.bodyId))
                {
                    if (!processedBodyIds.Contains(subject.bodyId))
                    {
                        processedBodyIds.Add(subject.bodyId);
                        if (subject.carryable != null)
                            AcquireTargetInCarryableGrips(handReach, aim, ref target, subject, otherHand);
                        else
                            subject.body.GetComponentsInChildren<IGripMarker>(gripMarkers);
                    }
                }
                else
                    hitCollider.GetComponentsInChildren<IGripMarker>(gripMarkers);

                // process grab targets
                if (gripMarkers.Count > 0) 
                    foreach (var grip in gripMarkers)
                        AcquireTargetInGripMarker(-1, grip, handReach, aim, ref target, subject, otherHand);
                gripMarkers.Clear();
            }
            if (otherHand.state == HandState.Hold && !World.IsEnvironment(otherHand.bodyId) && !processedBodyIds.Contains(otherHand.bodyId))
            {
                var subject = HandTargetSubject.FromBody(otherHand.bodyId);
                if(subject.carryable!=null)
                AcquireTargetInCarryableGrips(handReach, aim, ref target, subject, otherHand);
            }
            processedBodyIds.Clear();
            //NoodleDebug.builder.Line(handReach.actualPos, target.grabInfo.worldAnchor, Color.blue);
        }

        public static void ScanTargetOnCollision(Collider hitCollider, float3 hitPos, bool allowGrabWithoutGrip, in HandReachInfo handReach, in Aim aim, ref HandTargetInfo target, bool left, int targetLayers, in HandCarryData otherHand)
        {
            var subject = HandTargetSubject.FromCollider(hitCollider);

            if (subject.carryable != null) // override by carryable
            {
                if (allowGrabWithoutGrip) // if direct collider grab is allowed (pressing against normal, etc)
                    AcquireTargetInCarryableCollision(handReach, aim, ref target, subject, hitPos, otherHand);
                // grip targets can be grabbed always
                AcquireTargetInCarryableGrips(handReach, aim, ref target, subject, otherHand);
            }
            else
            {
                if (allowGrabWithoutGrip) // if direct collider grab is allowed (pressing against normal, etc)
                    AcquireTargetInCollider(handReach, ref target, subject, hitPos);

                // grip targets are scanned always
                if (!World.IsEnvironment(subject.bodyId))
                    subject.body.GetComponentsInChildren<IGripMarker>(gripMarkers);
                else
                    hitCollider.GetComponentsInChildren<IGripMarker>(gripMarkers);
            }

            // process grab targets
            if (gripMarkers.Count > 0) // use grab targets
                foreach (var grip in gripMarkers)
                    AcquireTargetInGripMarker(-1, grip, handReach, aim, ref target, subject, otherHand);
            gripMarkers.Clear();

            target.handDist = 0; // collided :)
        }


        private static void AcquireTargetInCollider(HandReachInfo handReach, ref HandTargetInfo target, HandTargetSubject subject, float3 hitPos)
        {
            var _grabHandPos = re.MoveTowards(hitPos, handReach.actualPos, handReach.radius);
            if (CalculatePointError(handReach, _grabHandPos, out var handDist, out var error, out var ikWeight) &&
                error < target.error)
            {
                target.handDist = handDist;
                target.error = error;
                target.ikWeight = ikWeight;
                target.subject = subject;
                //ALT1: anchor at surface
                //target.grabInfo = HandTargetGrip.FromWorldHandPos(
                //    -1, // no grip
                //    hitPos, _grabHandPos,
                //    quaternion.identity, GrabJointCollision.Collide);
                //ALT2: anchor at hand center
                target.grabInfo = HandTargetGrip.FromRelativeHandAnchor(
                    _grabHandPos, float3.zero);

                target.grabInfo.collision = GrabJointCollision.Collide;
            }
        }
        private static void AcquireTargetInCarryableCollision(in HandReachInfo handReach, in Aim aim, ref HandTargetInfo target, HandTargetSubject subject, float3 hitPos, in HandCarryData otherHand)
        {
            var _grabHandPos = re.MoveTowards(hitPos, handReach.actualPos, handReach.radius);
            if (subject.carryable != null)
            {
                subject.carryable.AcquireCollisionTarget(handReach, aim, hitPos, _grabHandPos, out target.grabInfo, otherHand);
                CalculatePointError(handReach, _grabHandPos, out var handDist, out var error, out var ikWeight);
                target.handDist = handDist;
                target.error = error;
                target.ikWeight = ikWeight;
                target.subject = subject;
            }
        }

        // version independent of collision
        private static bool AcquireTargetInCarryableGrips(in HandReachInfo handReach, in Aim aim, ref HandTargetInfo target, in HandTargetSubject subject, in HandCarryData otherHand)
        {
            // get target from carryable
            if (subject.carryable.AcquireGripTarget(handReach, aim,  out var grabInfo, otherHand))
            {
                CalculateCarryableGripPointError(handReach, grabInfo.worldHandPos, out var handDist, out var error, out var ikWeight);
                if (error < target.error) // replace if better than existing target
                {
                    target.error = error;
                    target.handDist = handDist;
                    target.ikWeight = ikWeight;
                    target.grabInfo = grabInfo;
                    target.subject = subject;
                    return true;
                }
            }
            return false;
        }
        private static bool AcquireTargetInGripMarker(int gripId, IGripMarker marker, HandReachInfo handReach, Aim aim, ref HandTargetInfo target, HandTargetSubject subject, in HandCarryData otherHand)
        {
            var t = marker.CalculateGrab(handReach, subject.carryable != null, subject.bodyId, gripId, otherHand);
            CalculateCarryableGripPointError(handReach, t.worldHandPos, out var handDist, out var error, out var ikWeight);
            if (error < target.error) // replace if better than existing target
            {
                target.error = error;
                target.handDist = handDist;
                target.ikWeight = ikWeight;
                target.grabInfo = t;
                target.subject = subject;
                return true;
            }
            return false;
        }

        public static bool CalculateReachSecondArmPointError(in HandReachInfo handReach, float3 selectPos, out float handDist, out float error, out float ikWeight)
        {
            handDist = math.length(selectPos - handReach.actualPos);
            error = handDist;
            ikWeight = 1;
            return true;
        }
        public static bool CalculateCarryableGripPointError(in HandReachInfo handReach, float3 selectPos, out float handDist, out float error, out float ikWeight)
        {
            if (CalculatePointError(handReach, selectPos, out handDist, out error, out ikWeight))
            {
                // modify error to prefer grip points
                error -= .25f;
                return true;
            }
            return false;
        }

        public static bool CalculatePointError(in HandReachInfo handReach, float3 selectPos, out float handDist, out float error, out float ikWeight)
        {
            NoodleHand.GetReachCapsule(handReach, out var capsuleLen, out var capsule1, out var capsule2, out var capsuleDir);

            // ignore if behind shoulder
            float dotDir = math.dot(selectPos - handReach.shoulderPos, capsuleDir);
            if (dotDir <= 0)
            {
                handDist = error = float.MaxValue;
                ikWeight = 0;
                return false;
            }

            float capsuleDist = math.length(selectPos - re.ProjectPointOnSegment(selectPos, capsule1, capsule2));
            handDist = math.length(selectPos - handReach.actualPos);
            float targetDist = math.length(selectPos - (handReach.targetPos + capsuleDir * .2f)); // use a bit ahead of target
            float coreDist = math.length(re.ProjectOnPlane(selectPos - handReach.shoulderPos, capsuleDir));
            error = capsuleDist * 0 + targetDist * coreDist;

            var weight0 = re.InverseLerp(NoodleHand.capsuleMax, NoodleHand.capsuleMin, capsuleDist); // capsule
            var weight3 = re.InverseLerp(NoodleHand.capsuleMax, NoodleHand.capsuleMin, handDist);
            ikWeight = math.max(weight0, weight3);
            return ikWeight > 0;
        }

    }
}
