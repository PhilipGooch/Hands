using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using Unity.Mathematics;
using System;

namespace NBG.XPBDRope
{
    [DisallowMultipleComponent]
    public sealed class RopeSegment : MonoBehaviour
    {
        [HideInInspector]
        public float BoneLength => originalLength * lengthMultiplier;
        [SerializeField]
        [HideInInspector]
        public float originalLength = 1f;
        [SerializeField]
        [HideInInspector]
        float lengthMultiplier = 1f;

        public bool fixedPosition = false;
        public float invMassOverride = 0f;
        [HideInInspector]
        public Rigidbody body;
        [HideInInspector]
        public CapsuleCollider capsule;
        [HideInInspector]
        public float radius = 0.5f;
        [HideInInspector]
        public float overlap = 0.0f;
        [HideInInspector]
        public ConfigurableJoint connectionOnPreviousSegment;
        [HideInInspector]
        public ConfigurableJoint connectionToNextSegment;
        [SerializeField]
        [HideInInspector]
        List<ISegmentRigidbodyListener> segmentListeners = new List<ISegmentRigidbodyListener>();

        [SerializeField]
        [HideInInspector]
        RopeSegment prev;
        [SerializeField]
        [HideInInspector]
        RopeSegment next;
        public RopeSegment Prev => prev;
        public RopeSegment Next => next;

        Action onChange;

        public int Id
        {
            get;
            private set;
        }

        public ref Body RecoilBody
        {
            get
            {
                return ref World.main.GetBody(Id);
            }
        }

        // isActiveAndEnabled is randomly true/false on the first frame
        public bool IsActive { get; private set; } = true;

        public bool NeedsReconnection { get; private set; } = false;

        public void Initialize(Action changeCallback)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
            Id = ManagedWorld.main.FindBody(body);

            // Hanging ropes will jiggle if recoil tries to sleep them, as they are kept in an equilibrium through recoil forces.
            // Sleeping in recoil removes those forces and makes the rope stretch, then recoil kicks back in and the cycle repeats.
            World.main.ConfigureBodySleep(Id, false);

            // Deactivate segments that are not active on start, so they will be properly reconnected when enabled
            if (!enabled || !gameObject.activeSelf)
            {
                DeactivateSegment();
            }

            onChange = changeCallback;
        }

        public void Deinitialize()
        {
            onChange = null;
        }

        public void SetPreviousSegment(RopeSegment previous)
        {
            prev = previous;
            previous.next = this;
        }

        public void GetLocalPointPositionAndVelocity(Vector3 localPoint, ref float3 position, ref float3 velocity)
        {
            var rTransform = RecoilBody.x;
            position = math.transform(rTransform, localPoint);
            velocity = World.main.GetLocalPointVelocity(Id, localPoint).linear;
        }

        public void SetBodyVelocity(float3 linear, float3 angular)
        {
            World.main.SetVelocity4(Id, new Velocity4(angular, linear));
        }

        public Vector3 GetConnectionPoint()
        {
            return new Vector3(0f, 0f, -BoneLength / 2f);
        }

        public float GetInvMass()
        {
            if (fixedPosition)
            {
                return 0f;
            }
            else if (invMassOverride > 0f)
            {
                return invMassOverride;
            }
            return RecoilBody.invM;
        }

        public void SetLengthMultiplier(float lengthMultiplier)
        {
            var previousLength = BoneLength;
            this.lengthMultiplier = lengthMultiplier;
            capsule.height = BoneLength + overlap;

            if (connectionOnPreviousSegment)
            {
                connectionOnPreviousSegment.connectedAnchor = new Vector3(0f, 0f, -BoneLength / 2f);
            }
            if (connectionToNextSegment)
            {
                connectionToNextSegment.anchor = new Vector3(0f, 0f, BoneLength / 2f);
            }

            var lengthDiff = previousLength - BoneLength;
            var forward = math.mul(RecoilBody.x.rot, new float3(0, 0, 1));
            var multiplier = 0f;
            if (next == null || !next.IsActive)
            {
                multiplier = -1f;
            }
            else if (prev == null || !prev.IsActive)
            {
                multiplier = 1f;
            }
            

            // Fixed position segments move the whole rope instead of moving themselves
            if (fixedPosition)
            {
                multiplier = -multiplier;
            }

            RecoilBody.x.pos += forward * multiplier * lengthDiff * 0.5f;

            onChange?.Invoke();
        }

        public void ReconnectSegment()
        {
            if (next != null && next.IsActive)
            {
                var nextTransform = next.RecoilBody.x;
                var posOffset = math.mul(nextTransform.rot, -new float3(0, 0, 1)) * ((next.BoneLength + BoneLength) * 0.5f);
                var targetPos = nextTransform.pos + posOffset;
                ManagedWorld.main.SetBodyPlacementImmediate(Id, new RigidTransform(nextTransform.rot, targetPos));
                ManagedWorld.main.SetVelocity(Id, World.main.GetWorldPointVelocity(next.Id, targetPos));
            }

            NeedsReconnection = false;
        }

        public void ActivateSegment()
        {
            if (!IsActive)
            {
                onChange?.Invoke();
                gameObject.SetActive(true);
                IsActive = true;
            }
        }

        public void DeactivateSegment()
        {
            if (IsActive)
            {
                onChange?.Invoke();
                NeedsReconnection = true;
                gameObject.SetActive(false);
                IsActive = false;
            }
        }

        public RigidTransform GetInterpolatedRigidTransform(float dt)
        {
            return World.main.IntegrateBodyPosition(Id, dt);
        }

        public void CollectAllListeners()
        {
            segmentListeners.Clear();
            GetComponents(segmentListeners);
        }

        public void BeforeRopeSolve()
        {
            foreach(var listener in segmentListeners)
            {
                listener.BeforeReadingSegmentRecoilbody(this);
            }
        }

        public void AfterRopeSolve()
        {
            foreach(var listener in segmentListeners)
            {
                listener.AfterWritingSegmentRecoilbody(this);
            }
        }

        void OnValidate()
        {
            CollectAllListeners();
            Debug.Assert(GetComponents<RopeSegment>().Length == 1, "Multiple RopeSegment components on a single object detected!", gameObject);
        }
    }
}