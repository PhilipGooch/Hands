using NBG.Core;
using NBG.Entities;
using Noodles.Animation;
using Recoil;
using System;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{

    public abstract class CarryableBase : MonoBehaviour, IManagedBehaviour
    {
        protected int bodyId;
        public Entity entity;
        public Transform alignTransform; // allows overriding
        protected IGripMarker[] markers;
        protected bool[] usedMarkers;
        protected virtual Spring GetHandJointSpring() => new Spring(250, 20); // wrist joint
        protected virtual Spring GetHandJointSpringDual() => GetHandJointSpring(); // wrist joint
        protected virtual Spring GetWorldJointSpring() => Spring.free;// no world constraint by default new Spring(150, 50); // object to world joint
        protected virtual Spring GetWorldJointSpringDual() => new Spring(150, 50); // object to world joint

        protected virtual CarryAlgorithmSingle GetCarryFunction(in HandCarryData hand) => default;
        protected bool IsValidFirstGrip(in HandCarryData hand)=> alwaysGetFirstAvailableGrip ? hand.gripId == 0 : hand.gripId >= 0;

        protected virtual bool AllowReach(in HandCarryData hand)=>
            IsValidFirstGrip(hand) && (reuseGrips || GetComponentsInChildren<IGripMarker>().Length > 1);
        protected virtual bool AllowTwoHanded(in HandCarryData first, in HandCarryData second) =>
             alwaysGetFirstAvailableGrip ?
                first.gripId == 0 && second.gripId == 1 :
                first.gripId >= 0 && second.gripId >= 0;
        #region Pivot
        public static quaternion HandToPose => quaternion.Euler(math.radians(-90), math.radians(180), 0);
        public static quaternion PoseToHand => math.inverse(HandToPose);

        /// <summary>
        /// Get desired orientation of the pivot for this carryable based on aim and handedness (left,right, two handed)
        /// </summary>
        protected quaternion GetPivotTargetRotation(HandReachInfo reach, Aim aim, bool twoHanded)
        {
            ref var carryDB = ref NoodleAnimationDatabase.instance.carry;
            var type = GetTypeId();
            if (twoHanded && carryDB.Contains(type, CarryableAnimationType.TwoHanded))
            {
                var pivot = carryDB.GetPivotPose01(type, CarryableAnimationType.TwoHanded, aim.pitch01);
                var pivotRot = pivot.ToRotation(reach.left, aim.yaw);
                if (pivot.fkParent > 0)
                    pivotRot = math.mul(reach.worldPalmX.rot, pivotRot);
                return pivotRot;
            }
            else if (carryDB.Contains(type, CarryableAnimationType.Carry))
            {
                var pivot = carryDB.GetPivotPose01(type, CarryableAnimationType.Carry, aim.pitch01);
                var pivotRot = pivot.ToRotation(reach.left, aim.yaw);
                if (pivot.fkParent > 0)
                    pivotRot = math.mul(reach.worldPalmX.rot, pivotRot);
                return pivotRot;
            }
            else
                return new PivotPose().ToRotation(reach.left, aim.yaw);
        }

        #endregion


        public virtual void OnLevelLoaded()
        {
            entity = EntityStore.AddEntity();
            bodyId = ManagedWorld.main.FindBody(GetComponent<Rigidbody>());
            if (bodyId < 0)
            {
                Debug.LogError("Rigidbody not registered, either IPhysicsBody is missing or carryable added where it should not be", this);
                return;
            }
            ManagedWorld.main.BindEntity(entity, bodyId);
            EntityStore.AddComponentObject(entity, this);

            markers = GetComponentsInChildren<IGripMarker>();
            usedMarkers = new bool[markers.Length];
            if (alignTransform == null) alignTransform = transform;

        }

        public int GetTypeId()
        {
            return math.abs(Unity.Burst.BurstRuntime.GetHashCode32(GetType()));
        }
        public static int GetTypeId<T>()
        {
            return math.abs(Unity.Burst.BurstRuntime.GetHashCode32<T>());
        }

        public static CarryableBase GetCarryableFromBodyId(int bodyId)
        {
            if (!World.IsEnvironment(bodyId))
            {
                var entity = World.main.GetEntity(bodyId);
                if (!entity.isNull)
                    return EntityStore.GetComponentObject<CarryableBase>(entity, optional:true);
            }

            return null;
        }



        void IManagedBehaviour.OnAfterLevelLoaded() { }
        void IManagedBehaviour.OnLevelUnloaded()
        {
            if (!entity.isNull)
            {
                EntityStore.RemoveEntity(entity);
                this.entity = default; //Replace with empty struct, so entity.isNull will be true in the future
            }
        }

        /// <summary>allow multiple grabs per grip</summary>
        public virtual bool reuseGrips => false;
        /// <summary>scan for closest or just use first not occupied</summary>
        public virtual bool alwaysGetFirstAvailableGrip => true;
        /// <summary>preserve pivot information from first grab, or re-calculate on converting to two handed</summary>
        public virtual bool useRotationToPivotFromFirstGrip => true;

        /// <summary>
        /// Calculates <see cref="HandTargetGrip"/> based on hand vs carryable collision information 
        /// </summary>
        public bool AcquireCollisionTarget(in HandReachInfo reach, Aim aim, float3 surfacePos, float3 surfaceHandPos, out HandTargetGrip target, in HandCarryData otherHand)
        {
            var pivotTarget = GetPivotTargetRotation(reach, aim, otherHand.allowTwoHanded && otherHand.IsCarrying(bodyId));

            var x = World.main.GetBodyPosition(bodyId);
            target = AcquireCollisionGrip(reach, surfacePos, surfaceHandPos, pivotTarget, -1, x, otherHand.IsCarrying(bodyId),otherHand);
            return true;
        }
        /// <summary>
        /// Scans for the best <see cref="IGripMarker"/> based on aim/hand position using <see cref="AcquireGrip"/>, 
        /// then returns <see cref="HandTargetGrip"/> describing how to make a joint with the carryable.
        /// </summary>
        public bool AcquireGripTarget(in HandReachInfo reach, Aim aim, out HandTargetGrip target, in HandCarryData otherHand)
        {
            var pivotTarget = GetPivotTargetRotation(reach, aim, otherHand.allowTwoHanded && otherHand.IsCarrying(bodyId));
            var alignX = alignTransform.GetRigidWorldTransform();
            var bodyRot = World.main.GetBodyPosition(bodyId).rot;

            target = default(HandTargetGrip);
            var bestError = float.MaxValue;

            for (int gripId = 0; gripId < markers.Length; gripId++)
            {
                if (!reuseGrips && usedMarkers[gripId]) continue; // skip used grips

                var t = markers[gripId].CalculateGrab(reach, inCarryable: true, bodyId: bodyId, gripId: gripId, otherHand: otherHand);
                t.gripId = gripId;
                if (AcquireGrip(ref t, reach, gripId, pivotTarget, alignX, otherHand))
                {
                    t.rotationToPivot = math.mul(t.rotationToPivot, re.invmul(alignX.rot, bodyRot)); //TODO validate
                    //t.rotationToPivot = math.mul(t.rotationToPivot, math.inverse(alignX[gripId].rot));

                    var posError = math.lengthsq(reach.actualPos - t.worldHandPos);
                    var rotError = math.lengthsq(re.invmul(t.rotationToPivot, re.invmul(pivotTarget, bodyRot)).ToAngleAxis());
                    var error = posError * 100 + rotError; // position takes priority
                    if (error < bestError)
                    {
                        bestError = error;
                        target = t;
                    }
                }

                //CheckGrip(reach, alignX, pivotTarget, i, ref target, ref bestError, otherHand);
                if (alwaysGetFirstAvailableGrip)
                    break;
            }

            //NoodleDebug.builder.Line(reach.actualPos, target.worldAnchor, Color.blue);

            return bestError < float.MaxValue;
        }

        public virtual HandTargetGrip AcquireCollisionGrip(in HandReachInfo reach, float3 surfacePos, float3 surfaceHandPos, quaternion pivotTarget, int gripId, RigidTransform gripX, bool twoHanded, in HandCarryData otherHand)
        {
            var target = HandTargetGrip.FromWorldHandPos(surfacePos, surfaceHandPos);
            AcquireDefaultGrip(ref target, gripId, pivotTarget, gripX.rot, CarryablePivotAlign.None, GrabJointCollision.Collide);
            return target;
        }
        public virtual bool AcquireGrip(ref HandTargetGrip target, in HandReachInfo reach, int gripId, quaternion pivotTarget, RigidTransform alignX, in HandCarryData otherHand)
        {
            target = default;
            return false;
        }
        protected static bool AcquireDefaultGrip(ref HandTargetGrip target, int gripId, quaternion pivotTarget, quaternion alignRot, CarryablePivotAlign align, GrabJointCollision collision)
        {
            target.Align(pivotTarget, alignRot, align);
            target.collision = collision;
            target.gripId = gripId;
            return true;
        }
        public static bool AcquireTwoHandedGrip(int bodyId, in HandReachInfo reach, quaternion pivotTarget, int gripId, RigidTransform alignX, ref HandTargetGrip target, CarryablePivotAlign align)
        {
            if (align != CarryablePivotAlign.AlignXFlipY && align != CarryablePivotAlign.AlignXFlipZ)
                throw new System.InvalidOperationException("Unsupported alignment");
            if (gripId >= 0) // using grip
            {
                var desiredSide = reach.left ? -1 : 1; // side of hand

                var relativeAnchor = target.worldAnchor - alignX.pos;
                var anchorInObject = re.invmul(alignX.rot, relativeAnchor);
                anchorInObject = math.normalize(re.right * anchorInObject.x); // snap to horizontal axis
                var anchorInPivot = re.invmul(pivotTarget, math.mul(alignX.rot, anchorInObject));

                // check our alignment to pivot space (character)
                if (desiredSide * anchorInPivot.x > 0.7f // anchor is strongly on same side as hand when seen by character
                    || anchorInPivot.z < 0) // object is away from anchor when seen by character
                {

                    // check if grip is oriented to correct side, flip otherwise
                    target.rotationToPivot = desiredSide * anchorInObject.x > 0 ?
                        quaternion.identity :
                        align == CarryablePivotAlign.AlignXFlipY ? quaternion.RotateY(math.PI) : quaternion.RotateZ(math.PI);
                    target.gripId = gripId;
                    target.collision = GrabJointCollision.IgnoreLower;
                    return true;
                }
            }
            // did not detect matching grip, fallback to default carry
            target = default;
            return false;
        }
        public bool TakeGrip(int gripId)
        {
            if (reuseGrips) return true;
            //Debug.Log($"Take {gripId}");
            if (gripId < 0) return false;
            if (usedMarkers[gripId]) return false;
            usedMarkers[gripId] = true;
            return true;
        }
        public void ReleaseGrip(int gripId)
        {
            if (reuseGrips) return;
            //Debug.Log($"Release {gripId}");
            if (gripId < 0) return;
            usedMarkers[gripId] = false;
        }

        public virtual void SelectMainHand(ref CarryData data)
        {
            var anchorL = Carry.GetAnchorRelativeToPivot(data, true);
            var anchorR = Carry.GetAnchorRelativeToPivot(data, false);
            data.leftMain = anchorL.z > anchorR.z;
        }
        public virtual bool recalculatePivotOffsetTwoHanded => true;
        public virtual void SetupOneHanded(ref HandCarryData hand)
        {
            hand.jointSpring = GetHandJointSpring();
            hand.worldJointSpring = GetWorldJointSpring();
            hand.type = GetTypeId();
            hand.singleCarryFn = GetCarryFunction(hand);
            hand.allowReach = AllowReach(hand);
        }

        public virtual void SetupTwoHanded(ref HandCarryData first, ref HandCarryData second)
        {
            second.jointSpring = first.jointSpring = GetHandJointSpringDual();
            second.worldJointSpring = first.worldJointSpring = GetWorldJointSpringDual();
            second.type = first.type = GetTypeId();
            second.allowTwoHanded = first.allowTwoHanded = AllowTwoHanded(first, second);
        }
        public static void Clear(ref HandCarryData hand)
        {
            hand.type = 0;
            hand.singleCarryFn = default;
            hand.allowTwoHanded = default;
            hand.allowReach = default;
        }



    }

}
