using NBG.Actor;
using Recoil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Impale
{
    internal static class ImpalerUtils
    {
        internal static void SetJointLinearLimit(this ConfigurableJoint joint, float limit)
        {
            var linearLimit = joint.linearLimit;
            linearLimit.limit = limit;
            joint.linearLimit = linearLimit;
        }

        internal static T GetComponentOfType<T>(this Collider other)
        {
            if (other.attachedRigidbody != null)
            {
                other.attachedRigidbody.TryGetComponent<T>(out var objFromRigidbody);
                return objFromRigidbody;
            }
            other.TryGetComponent<T>(out var objFromCollider);
            return objFromCollider;
        }

        /// <summary>
        /// Negative if pivot is inside impaler, positive if outside
        /// </summary>
        /// <param name="pivotPosition">In world space</param>
        /// <param name="impaleDirection">In world space</param>
        /// <param name="impalerStart">In world space</param>
        /// <returns></returns>
        internal static float GetDistanceFromPivotToImpalerStart(Vector3 pivotPosition, Vector3 impaleDirection, Vector3 impalerStart)
        {
            var offset = impalerStart - pivotPosition;
            var distanceFromPivotToImpalerStart = offset.magnitude;
            distanceFromPivotToImpalerStart *= Vector3.Dot(offset, impaleDirection) < 0 ? -1 : 1;
            return distanceFromPivotToImpalerStart;
        }

        /// <summary>
        /// Return rotation and postion needed to align object with a specific normal.
        /// NOTE: if impaler is grabbed by human, then this override doesnt fully work, since human moves the object after
        /// </summary>
        /// <param name="currentRotation">In world space</param>
        /// <param name="normal">In world space</param>
        /// <param name="bodyDirection">In local space</param>
        /// <param name="distanceToAnotherObject"></param>
        /// <param name="normalOrigin">In world space</param>
        /// <returns></returns>
        internal static (Quaternion rotation, Vector3 position) GetAlignmentWithNormalPosAndRot(Quaternion currentRotation, Vector3 normal, Vector3 bodyDirection, float distanceToAnotherObject, Vector3 normalOrigin)
        {
            var rotation = Quaternion.FromToRotation(currentRotation * bodyDirection, -normal) * currentRotation;
            var position = normalOrigin + normal * distanceToAnotherObject;
            return (rotation, position);
        }

        /// <summary>
        /// Since raycast hit is not aligned with object center, need to get somekind of an accurate reference point.
        /// </summary>
        /// <param name="impalePosition">In world space</param>
        /// <param name="impalerTip">In world space</param>
        /// <param name="impaleDirection">In world space</param>
        /// <returns></returns>
        internal static Vector3 ProjectImpalerTipFromHit(Vector3 impalePosition, Vector3 impalerTip, Vector3 impaleDirection)
        {
            var dirToPenFromTip = impalePosition - impalerTip;

            var angleBetweenDirAndTipToPenVector = Vector3.Angle(impaleDirection, -dirToPenFromTip);
            //no need to project if hit point matches impaler center axis
            if (angleBetweenDirAndTipToPenVector < 0.5f)
            {
                return impalerTip;
            }
            float angleBetweenDirToPenAndStart = 180 - (angleBetweenDirAndTipToPenVector + 90);
            var projectedDistanceFromTipToSurface = dirToPenFromTip.magnitude * Mathf.Sin(Mathf.Deg2Rad * angleBetweenDirToPenAndStart) / Mathf.Sin(Mathf.Deg2Rad * 90);

            return impalePosition + impaleDirection.normalized * projectedDistanceFromTipToSurface;
        }

        internal static Vector3 RelativeVelocityToOtherBody(this ReBody thisRebody, ReBody otherBody)
        {
            var relativeVelocity = thisRebody.velocity;
            if (otherBody.BodyExists)
                relativeVelocity -= otherBody.velocity;
            return relativeVelocity;
        }

        /// <summary>
        /// Return Vector3.zero if going away from the impale direction.
        /// </summary>
        /// <param name="velocityToProject">In world space</param>
        /// <param name="imapleDirection">In world space</param>
        /// <returns></returns>
        internal static Vector3 GetProjectedVelocity(Vector3 velocityToProject, Vector3 imapleDirection)
        {
            var velocity = velocityToProject * Time.fixedDeltaTime;
            var projectedVelocity = Vector3.Project(velocity, imapleDirection);
            return projectedVelocity * (Vector3.Dot(projectedVelocity, imapleDirection) > 0 ? 1 : 0);
        }

        internal static void AddKeysCollectionToList(this List<Collider> list, Dictionary<Collider, Connection> dictionary)
        {
            foreach (var pair in dictionary)
            {
                list.Add(pair.Key);
            }
        }

        /// <summary>
        /// Get impaler position if it would be impaled up to its limit.
        /// </summary>
        /// <param name="currentPosition">In world space</param>
        /// <param name="impaleDirection">In world space</param>
        /// <param name="maxDepth"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        internal static Vector3 GetPositionAtDepth(Vector3 currentPosition, Vector3 impaleDirection, float maxDepth, float depth)
        {
            return currentPosition + impaleDirection * (maxDepth + depth);
        }

        /// <summary>
        /// Negative depth if object is penetrating another object, positive if its still outside
        /// </summary>
        /// <param name="impalePos">In world space</param>
        /// <param name="impalerTip">In world space</param>
        /// <param name="impaleDirection">In world space</param>
        /// <param name="castStart">In world space</param>
        /// <returns></returns>
        internal static float CalculateDepth(Vector3 impalePos, Vector3 impalerTip, Vector3 impaleDirection, Vector3 castStart)
        {
            var projectedImpalerTip = ProjectImpalerTipFromHit(impalePos, impalerTip, impaleDirection);
            var projectedCastStart = projectedImpalerTip + ((impalerTip - castStart).magnitude * -impaleDirection);
            float distanceToImpale = (projectedCastStart - impalePos).magnitude;
            float fromCastToTip = (projectedCastStart - projectedImpalerTip).magnitude;
       
            return distanceToImpale - fromCastToTip;
        }

        /// <summary>
        /// Get connected anchor for a newly created joint.
        /// </summary>
        /// <param name="anchor">In world space</param>
        /// <param name="impaleDirection">In world space</param>
        /// <param name="impalerLength"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        internal static Vector3 GetInitialConnectedAnchorPos(Vector3 anchor, Vector3 impaleDirection, float impalerLength, float depth)
        {
            //if its deeper than it should ever be, need to compensate for that
            if (depth < -impalerLength)
            {
                var diff = impalerLength + depth;
                depth += diff;
            }
            return anchor + (impaleDirection * depth);
        }

        internal static Func<RaycastHit, RaycastHit, bool> Comparer = (a, b) => a.distance > b.distance;

        //if colliders are overlapping too much, it means that they were just impaled and need to resolve
        internal static bool CollidersOverlappingTooMuch(this Dictionary<Collider, Connection> impaledObjects, float collidersOverlapTolerance)
        {
            foreach (var colliderA in impaledObjects.Keys)
            {
                if (impaledObjects[colliderA].joint == null)
                    continue;
                var rigA = colliderA.attachedRigidbody;

                foreach (var colliderB in impaledObjects.Keys)
                {
                    if (colliderA == colliderB)
                        continue;

                    if (impaledObjects[colliderB].joint == null)
                        continue;

                    if (Physics.GetIgnoreCollision(colliderB, colliderA))
                        continue;

                    var rigB = colliderB.attachedRigidbody;
                    if (rigA == null && rigB == null)
                        continue;

                    if (rigA != null && rigB != null && ActorSystem.JointModule != null)
                    {
                        if (HasJointConnectionWithoutCollisions(colliderA, colliderB) || HasJointConnectionWithoutCollisions(colliderB, colliderA))
                            continue;
                    }

                    if (Physics.ComputePenetration(colliderA, colliderA.transform.position, colliderA.transform.rotation,
                         colliderB, colliderB.transform.position, colliderB.transform.rotation, out var dir, out var dist))
                    {
                        if (dist > collidersOverlapTolerance)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static bool HasJointConnectionWithoutCollisions(Collider colliderA, Collider colliderB)
        {
            foreach (var joint in colliderA.GetComponentsInParent<Joint>())
            {
                if (joint.connectedBody != null && !joint.enableCollision)
                {
                    foreach (var collider in joint.connectedBody.GetComponentsInChildren<Collider>())
                    {
                        if (collider == colliderB)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
