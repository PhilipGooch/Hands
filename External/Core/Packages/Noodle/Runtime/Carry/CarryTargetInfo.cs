using NBG.Entities;
using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    /// <summary>
    /// Describes how a carryable should be grabbed with each hand
    /// </summary>
    public struct CarryTargetInfo
    {
        public HandTargetInfo handL;
        public HandTargetInfo handR;

        public static CarryTargetInfo Empty => new CarryTargetInfo() { handL = HandTargetInfo.Empty, handR = HandTargetInfo.Empty };
    }
    /// <summary>
    /// Describes single grab target scan result - <see cref="subject"/> is the object to be grabbed, and <see cref="grabInfo"/> specifies how exactly it should be grabbed
    /// </summary>
    public struct HandTargetInfo
    {
        public float handDist;
        public float error; // how preferred the target is
        public float ikWeight; // how strong IK force should be
        public HandTargetSubject subject; // what to grab
        public HandTargetGrip grabInfo; // how to grab


        public static HandTargetInfo Empty => new HandTargetInfo() { error = float.MaxValue };
        public bool isEmpty => error == float.MaxValue;
    }

    /// <summary>
    /// Described the body being grabbed, including <see cref="carryable"/> and <see cref="collider"/>
    /// </summary>
    public struct HandTargetSubject
    {
        public int bodyId; // which body to grab
        public CarryableBase carryable;
        public Rigidbody body;
        public Collider collider;

        public static HandTargetSubject FromCollider(Collider collider)
        {
            var body = collider.attachedRigidbody;
            var carryable = (CarryableBase)null;
            if (body != null) body.TryGetComponent<CarryableBase>(out carryable);
            var result = new HandTargetSubject()
            {
                collider = collider,
                body = body,
                bodyId = ManagedWorld.main.FindBody(body, optional: true),
                carryable = carryable
            };
            return result;
        }
        public static HandTargetSubject FromBody(int bodyId)
        {
            var body = ManagedWorld.main.GetRigidbody(bodyId);
            body.TryGetComponent<CarryableBase>(out var carryable);
            return new HandTargetSubject()
            {
                collider = null,
                body = body,
                bodyId = bodyId,
                carryable = carryable
            };
        }

    }


    public enum CarryablePivotAlign
    {
        None, // carry as grabbed
        Locked, // match align transform rotation to pivot
        AlignXFlipY, // locked, but allow flip on X
        AlignXFlipZ, // locked, but allow flip on X
        AlignYFlipZ,
        XAxis, // match x axis of align to pivot
        YAxis, // match y axis of align to pivot
        ZAxis, // match z axis of align to pivot
        ZNegative, // match z axis of align to pivot
        ZUnsigned, // match z axis of align to pivot
        XUnsigned, // match x axis of align to pivot

    }

    /// <summary>
    /// Describes how the joint between hand and carryable should be created when grabbed, including anchors and orientation.
    /// </summary>
    public struct HandTargetGrip
    {
        public int gripId;
        public GrabJointCollision collision;
        public quaternion rotationToPivot;
        public float3 worldAnchor;
        public float3 relativeHandAnchor;
        public float3 worldHandPos => worldAnchor - relativeHandAnchor;
        public HandTargetGrip( float3 worldAnchor, float3 relativeHandAnchor)
        {
            this.worldAnchor = worldAnchor;
            this.relativeHandAnchor = relativeHandAnchor;
            this.gripId = -1;
            this.rotationToPivot = quaternion.identity;
            this.collision = GrabJointCollision.IgnoreLower;
        }

        public static HandTargetGrip FromRelativeHandAnchor(float3 worldAnchor, float3 relativeHandAnchor)//, GrabJointCollision collision = GrabJointCollision.IgnoreLower)
        {
            return new HandTargetGrip(worldAnchor, relativeHandAnchor);
        }

      
        public static HandTargetGrip FromWorldHandPos(float3 worldAnchor, float3 worldHandPos)//, GrabJointCollision collision = GrabJointCollision.IgnoreLower)
        {
            return new HandTargetGrip(worldAnchor, worldAnchor - worldHandPos);
        }
        public void Align(quaternion pivotTarget, quaternion alignRot, CarryablePivotAlign align)
        {
            rotationToPivot = GetRelativeToPivot(pivotTarget, alignRot, align);
        }
        private static quaternion GetRelativeToPivot(quaternion pivotRot, quaternion bodyRot, CarryablePivotAlign align)
        {
            switch (align)
            {
                case CarryablePivotAlign.Locked: return quaternion.identity;
                case CarryablePivotAlign.AlignXFlipY:
                    return math.rotate(re.invmul(pivotRot, bodyRot), re.right).x >= 0 ?
                    quaternion.identity : quaternion.RotateY(math.PI);
                case CarryablePivotAlign.AlignXFlipZ:
                    return math.rotate(re.invmul(pivotRot, bodyRot), re.right).x >= 0 ?
                    quaternion.identity : quaternion.RotateZ(math.PI);
                case CarryablePivotAlign.AlignYFlipZ:
                    return math.rotate(re.invmul(pivotRot, bodyRot), re.up).y >= 0 ?
                    quaternion.identity : quaternion.RotateZ(math.PI);
                case CarryablePivotAlign.XAxis: return re.GetTwist(re.invmul(pivotRot, bodyRot), re.right);
                case CarryablePivotAlign.YAxis: return re.GetTwist(re.invmul(pivotRot, bodyRot), re.up);
                case CarryablePivotAlign.ZAxis: return re.GetTwist(re.invmul(pivotRot, bodyRot), re.forward);
                case CarryablePivotAlign.ZNegative: return math.mul(re.GetTwist(math.mul(re.invmul(pivotRot, bodyRot), quaternion.RotateY(math.PI)), re.forward), quaternion.RotateY(math.PI));
                case CarryablePivotAlign.XUnsigned:
                    return math.rotate(re.invmul(pivotRot, bodyRot), re.right).x < 0 ?
                        math.mul(re.GetTwist(math.mul(re.invmul(pivotRot, bodyRot), quaternion.RotateY(math.PI)), re.right), quaternion.RotateY(math.PI)) :
                        re.GetTwist(re.invmul(pivotRot, bodyRot), re.right);
                case CarryablePivotAlign.ZUnsigned:
                    return math.rotate(re.invmul(pivotRot, bodyRot), re.forward).z < 0 ?
                        math.mul(re.GetTwist(math.mul(re.invmul(pivotRot, bodyRot), quaternion.RotateY(math.PI)), re.forward), quaternion.RotateY(math.PI)) :
                        re.GetTwist(re.invmul(pivotRot, bodyRot), re.forward);
                case CarryablePivotAlign.None:
                default:
                    return re.invmul(pivotRot, bodyRot);
            }
        }
    }

    public struct RuntimeIKTarget
    {
        public float3 pos; // target hand pos
        public int bodyId;
        public float3 worldBodyAnchor;
        public float3 vel => World.main.GetWorldPointVelocity(bodyId, worldBodyAnchor).linear;
        public float weight;

        public RuntimeIKTarget(int bodyId, float3 pos, float3 worldBodyAnchor, float weight)
        {
            this.bodyId = bodyId;
            this.worldBodyAnchor = worldBodyAnchor;
            this.pos = pos;
            this.weight = weight;
        }
        //public IKTarget(int bodyId, float3 pos, float weight) { this.bodyId = bodyId; this.pos = pos; this.vel = float3.zero; this.weight = weight; }
        //public IKTarget(int bodyId, float3 pos, float weight) { this.bodyId = bodyId; this.pos = pos; this.weight = weight; }
    }


    public struct RuntimeIKTargets
    {
        public RuntimeIKTarget handL;
        public RuntimeIKTarget handR;
        //public RuntimeIKTarget footL;
        //public RuntimeIKTarget footR;

        public RuntimeIKTargets Transform(RigidTransform rigidTransform)
        {
            var result = this;
            result.handL.pos = math.transform(rigidTransform, handL.pos);
            result.handR.pos = math.transform(rigidTransform, handR.pos);
            //result.footL.pos = math.transform(rigidTransform, footL.pos);
            //result.footR.pos = math.transform(rigidTransform, footR.pos);
            return result;
        }

        public RuntimeIKTargets PredictMotion(float3 ragdollVelocity, float dt)
        {
            var result = this;
            result.handL.pos += (handL.vel - ragdollVelocity) * dt;
            result.handR.pos += (handR.vel - ragdollVelocity) * dt;
            //result.footL.pos += (footL.vel - ragdollVelocity) * dt;
            //result.footR.pos += (footR.vel - ragdollVelocity) * dt;
            return result;
        }
    }
}
