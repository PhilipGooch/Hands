using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using Unity.Mathematics;
using NBG.Core;
using Unity.Collections;

namespace NBG.Locomotion
{
    public class BallLocomotion : MonoBehaviour
    {
        [SerializeField]
        float movementSpeed = 5f;
        [SerializeField]
        float jumpSpeed = 2f;
        [SerializeField]
        float jumpHeight = 1f;
        [SerializeField]
        float allowClimbSlope = 30f;
        public float MaxClimbableSlope => allowClimbSlope;
        [SerializeField]
        float slideSlope = 40f;
        [SerializeField]
        new Rigidbody rigidbody;
        [SerializeField]
        SphereCollider sphere;
        [SerializeField]
        float rotationSpeed = 180f;
        [SerializeField]
        [Range(0f, 0.999f)]
        [Tooltip("How much to align facing with vertical velocity when not grounded")]
        float verticalFacingWhenInAir = 0.5f;
        [SerializeField]
        [Tooltip("Vertical velocity is multiplied by this number when calculating the facing in the air. Lower values will make the agent face the vertical direction more gradually.")]
        float verticalFacingVelocityMultiplier = 0.1f;

        public bool Jumping => state.Jumping;
        public bool OnGround => state.locomotionData.Grounded;
        public float CurrentMovementSpeed => state.currentMovementSpeed;
        public float CurrentTurnSpeed => state.currentTurnSpeed;
        public Vector3 CurrentFacingOnGround => state.locomotionData.CurrentFacing;
        public Vector3 CurrentFacing
        {
            get
            {
                if (state.locomotionData.Grounded)
                {
                    return CurrentFacingOnGround;
                }
                else
                {
                    var verticalVel = new float3(0, reBody.velocity.y * verticalFacingVelocityMultiplier, 0);
                    return math.normalize(math.lerp(CurrentFacingOnGround, verticalVel, verticalFacingWhenInAir));
                }
            }
        }

        ReBody reBody;
        public ReBody Body
        {
            get
            {
                if (!reBody.BodyExists)
                {
                    reBody = new ReBody(rigidbody);
                }
                return reBody;
            }
        }

        float gravityMagnitude;
        public float SphereRadius => sphere.radius * sphere.transform.lossyScale.x;
        float JumpDuration => Mathf.Sqrt(jumpHeight / (0.5f * gravityMagnitude));

        Collider[] sphereCheckHits = new Collider[16];

        // Active state
        internal struct BallLocomotionData
        {
            internal LocomotionData locomotionData;
            internal float currentMovementSpeed;
            internal float currentTurnSpeed;
            internal float jumpTimer;
            internal float groundSlope;
            internal int groundBodyId;
            internal float3 collisionResponseTorque;
            internal float3 previousFacing;
            internal float3 collisionPointNormal;

            internal readonly float allowClimbSlope;
            internal readonly float slideSlope;
            internal readonly float rotationSpeed;
            internal readonly float jumpDuration;
            internal readonly float jumpSpeed;

            internal bool Jumping => jumpTimer > 0f;

            internal BallLocomotionData(LocomotionData locomotionData, float allowClimbSlope, float slideSlope, float rotationSpeed, float jumpDuration, float jumpSpeed)
            {
                this.locomotionData = locomotionData;
                currentMovementSpeed = 0f;
                currentTurnSpeed = 0f;
                jumpTimer = 0f;
                groundSlope = 1f;
                groundBodyId = World.environmentId;
                collisionResponseTorque = float3.zero;
                previousFacing = locomotionData.CurrentFacing;
                collisionPointNormal = float3.zero;
                this.allowClimbSlope = allowClimbSlope;
                this.slideSlope = slideSlope;
                this.rotationSpeed = rotationSpeed;
                this.jumpDuration = jumpDuration;
                this.jumpSpeed = jumpSpeed;
            }

            internal float GetSlope(float angle)
            {
                return Mathf.InverseLerp(slideSlope, allowClimbSlope, angle);
            }
        }

        BallLocomotionData state;


        private void Awake()
        {
            // Need to initialize state in Awake since collision events can happen before Start
            gravityMagnitude = Physics.gravity.magnitude;
            var locomotionData = new LocomotionData(float3.zero, float3.zero, false, transform.forward, new LocomotionData.GroundData(Vector3.up, float3.zero, false), new LocomotionData.AgentData(jumpHeight, JumpDuration, SphereRadius, World.environmentId));
            state = new BallLocomotionData(locomotionData, allowClimbSlope, slideSlope, rotationSpeed, JumpDuration, jumpSpeed);
        }

        private void Start()
        {
            reBody = new ReBody(rigidbody);
            reBody.AllowSleeping = false;
            reBody.maxAngularVelocity = 20;
            // Update recoil Id. Needed to support dynamic instantiation.
            var loco = state.locomotionData;
            state.locomotionData.agentData = new LocomotionData.AgentData(loco.JumpHeight, loco.JumpDuration, loco.AgentSphereRadius, reBody.Id);
        }

        public void SetInput(Vector3 wantedVelocity, bool jump)
        {
            state.locomotionData.targetVelocity = wantedVelocity * movementSpeed;
            state.locomotionData.jump = jump;
        }

        public void SetRotation(Quaternion rotation)
        {
            var wantedFacing = math.mul(rotation, math.forward());
            var groundNormal = state.locomotionData.GroundNormal;
            if (math.abs(math.dot(groundNormal, wantedFacing)) < 0.999f)
            {
                wantedFacing = Vector3.ProjectOnPlane(wantedFacing, groundNormal).normalized;
            }
            var loco = state.locomotionData;
            state.locomotionData = new LocomotionData(loco.targetVelocity, loco.strafeVelocity, loco.jump, wantedFacing, loco.groundData, loco.agentData);
        }

        internal void ApplyState(BallLocomotionData state)
        {
            this.state = state;
        }

        internal BallLocomotionData UpdateGroundInfo(LayerMask groundLayers)
        {
            var normal = Vector3.up;
            Rigidbody groundBody = null;
            var offset = SphereRadius * 0.25f;
            var posWithOffset = Body.position + Vector3.down * offset;
            var finalPos = posWithOffset;
            var hitCount = Physics.OverlapSphereNonAlloc(posWithOffset, SphereRadius, sphereCheckHits, groundLayers);
            for (int i = 0; i < hitCount; i++)
            {
                var groundCollider = sphereCheckHits[i];
                if (Physics.ComputePenetration(sphere, finalPos, Quaternion.identity, groundCollider, groundCollider.transform.position, groundCollider.transform.rotation, out var dir, out var dist))
                {
                    finalPos += dir * dist;
                }
                if (groundBody == null && groundCollider.attachedRigidbody != null)
                {
                    groundBody = groundCollider.attachedRigidbody;
                }
            }

            if (hitCount > 0)
            {
                var diff = finalPos - posWithOffset;
                normal = math.normalizesafe(diff, math.up());
            }

            var loco = state.locomotionData;
            state.locomotionData.groundData = new LocomotionData.GroundData(normal, loco.VelocityOnGround, hitCount > 0);
            state.groundBodyId = groundBody == null ? World.environmentId : ManagedWorld.main.FindBody(groundBody);
            return state;
        }

        internal static float GetAngle(Vector3 normal)
        {
            return Mathf.Acos(normal.y) * Mathf.Rad2Deg;
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            HandleCollision(collision);
        }

        void HandleCollision(Collision collision)
        {
            var normal = collision.GetContact(0).normal;
            var angle = GetAngle(normal);
            var slope = state.GetSlope(angle);

            // Gravity deflect
            if (slope > 0f)
            {
                var impulse = collision.impulse;
                var upwardImpulse = impulse.magnitude / normal.y * Vector3.up;
                state.collisionResponseTorque += -math.cross(upwardImpulse * slope, normal * SphereRadius);
            }
        }

        private void OnValidate()
        {
            if (rigidbody == null)
            {
                rigidbody = GetComponent<Rigidbody>();
            }
            if (sphere == null)
            {
                sphere = GetComponentInChildren<SphereCollider>();
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                DebugExtension.DrawArrow(reBody.position, state.locomotionData.CurrentFacing, Color.green);
            }
        }
    }
}
