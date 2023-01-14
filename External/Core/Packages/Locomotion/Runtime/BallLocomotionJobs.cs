using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Recoil;

namespace NBG.Locomotion
{
    using BallLocomotionData = BallLocomotion.BallLocomotionData;

    internal static class BallLocomotionJobs
    {
        static float3 ProjectOnFloorPlane(BallLocomotionData state, float3 input)
        {
            var normal = math.up();

            if (state.locomotionData.Grounded)
            {
                if (math.abs(math.dot(state.locomotionData.GroundNormal, math.normalizesafe(input, float3.zero))) < 0.999f)
                {
                    normal = state.locomotionData.GroundNormal;
                }
            }

            return Vector3.ProjectOnPlane(input, normal).normalized;
        }

        static float3 GetGroundVelocity(BallLocomotionData state)
        {
            if (state.locomotionData.Grounded && state.groundBodyId != World.environmentId)
            {
                return World.main.GetBody(state.groundBodyId).v.linear;
            }
            return Vector3.zero;
        }

        [BurstCompile]
        internal struct InitializeLocomotionStateJob : IJob
        {
            NativeList<BallLocomotionData> data;

            internal InitializeLocomotionStateJob(NativeList<BallLocomotionData> data)
            {
                this.data = data;
            }

            public void Execute()
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = UpdateLocomotionData(data[i]);
                }
            }

            BallLocomotionData UpdateLocomotionData(BallLocomotionData state)
            {
                state = UpdateGroundStateData(state);
                var wantedVelocity = state.locomotionData.targetVelocity;
                var originalMagnitude = math.length(wantedVelocity);
                wantedVelocity = ProjectOnFloorPlane(state, wantedVelocity) * originalMagnitude;
                var currentFacing = ProjectOnFloorPlane(state, state.locomotionData.CurrentFacing);
                state.locomotionData = new LocomotionData(wantedVelocity, state.locomotionData.strafeVelocity, state.locomotionData.jump, currentFacing, state.locomotionData.groundData, state.locomotionData.agentData);
                return state;
            }

            BallLocomotionData UpdateGroundStateData(BallLocomotionData state)
            {
                var body = World.main.GetBody(state.locomotionData.AgentRecoilId);
                float3 velocityOnGround = body.v.linear;

                var groundData = state.locomotionData.groundData;
                bool grounded = groundData.grounded;
                float3 groundNormal = groundData.groundNormal;

                if (state.Jumping)
                {
                    grounded = false;
                    state.jumpTimer -= World.main.dt;
                    state.jumpTimer = Mathf.Clamp(state.jumpTimer, 0f, float.MaxValue);
                }
                else
                {
                    if (grounded)
                    {
                        if (Vector3.Dot(groundNormal, Vector3.up) < 1f)
                        {
                            state.groundSlope = state.GetSlope(BallLocomotion.GetAngle(groundNormal));
                        }
                        else
                        {
                            state.groundSlope = 1f;
                        }

                        var groundVel = GetGroundVelocity(state);
                        velocityOnGround = Vector3.ProjectOnPlane(velocityOnGround - groundVel, groundNormal);
                    }
                }

                state.locomotionData.groundData = new LocomotionData.GroundData(groundNormal, velocityOnGround, grounded);
                return state;
            }
        }

        [BurstCompile]
        internal struct ExtractLocomotionDataJob : IJob
        {
            [ReadOnly]
            NativeList<BallLocomotionData> data;
            [WriteOnly]
            NativeList<LocomotionData> locomotionData;

            internal ExtractLocomotionDataJob(NativeList<BallLocomotionData> data, NativeList<LocomotionData> locomotionData)
            {
                this.data = data;
                this.locomotionData = locomotionData;
            }

            public void Execute()
            {
                for (int i = 0; i < data.Length; i++)
                {
                    locomotionData.Add(data[i].locomotionData);
                }
            }
        }

        [BurstCompile]
        internal struct ApplyMovementJob : IJob
        {
            NativeList<BallLocomotionData> data;
            [ReadOnly]
            NativeList<LocomotionData> locomotionData;

            internal ApplyMovementJob(NativeList<BallLocomotionData> data, NativeList<LocomotionData> locomotionData)
            {
                this.data = data;
                this.locomotionData = locomotionData;
            }

            public void Execute()
            {
                for (int i = 0; i < data.Length; i++)
                {
                    var state = data[i];
                    state.locomotionData = locomotionData[i];
                    data[i] = UpdateMovement(state);
                }
            }

            BallLocomotionData UpdateMovement(BallLocomotionData state)
            {
                state = UpdateCurrentFacing(state);

                if (state.locomotionData.Grounded)
                {
                    state = MoveOnGround(state);
                }
                else if (state.Jumping)
                {
                    state = MoveInAir(state);
                }

                state = UpdateSpeeds(state);
                return state;
            }

            BallLocomotionData UpdateCurrentFacing(BallLocomotionData state)
            {
                var locoData = state.locomotionData;
                var targetFacing = math.normalizesafe(Vector3.ProjectOnPlane(locoData.targetVelocity, locoData.GroundNormal), locoData.CurrentFacing);
                var vectorDeltaAngle = math.radians(Vector3.SignedAngle(locoData.CurrentFacing, targetFacing, locoData.GroundNormal));
                var maxRotationSpeed = state.rotationSpeed * Mathf.Deg2Rad * World.main.dt * Mathf.InverseLerp(0f, 1f, math.length(locoData.targetVelocity));
                maxRotationSpeed = math.min(maxRotationSpeed, math.abs(vectorDeltaAngle)) * math.sign(vectorDeltaAngle);
                var currentFacing = re.Rotate(locoData.CurrentFacing, locoData.GroundNormal, maxRotationSpeed);
                state.locomotionData = new LocomotionData(locoData.targetVelocity, locoData.strafeVelocity, locoData.jump, currentFacing, locoData.groundData, locoData.agentData);
                return state;
            }

            BallLocomotionData MoveOnGround(BallLocomotionData state)
            {
                var inputData = state.locomotionData;
                var body = World.main.GetBody(inputData.AgentRecoilId);
                var targetVel = inputData.targetVelocity;
                var jump = inputData.jump;
                var targetVelMagnitude = math.length(targetVel);

                if (targetVelMagnitude > inputData.AgentSphereRadius && jump)
                {
                    state = Jump(state, targetVel);
                }
                else
                {
                    var targetMagnitude = targetVelMagnitude;
                    targetVel = state.locomotionData.CurrentFacing * targetMagnitude;
                    targetVel += inputData.strafeVelocity;

                    var groundNormal = inputData.GroundNormal;
                    var currentVel = body.v;
                    currentVel.angular = Vector3.Lerp(currentVel.angular, Vector3.Cross(groundNormal, targetVel) / inputData.AgentSphereRadius, state.groundSlope);
                    var groundVel = GetGroundVelocity(state);
                    // The velocity that we're moving currently
                    var relativeVel = Vector3.ProjectOnPlane(targetVel + groundVel - currentVel.linear, groundNormal);

                    // Make sure we stay with the ground
                    var acc = relativeVel / World.main.dt;
                    acc = re.Clamp(acc, math.length(World.main.gravity)) * state.groundSlope;
                    //reBody.AddForce(acc, ForceMode.Acceleration);
                    World.main.AddLinearVelocity(inputData.AgentRecoilId, acc * World.main.dt);

                    // Apply force to body we're standing on
                    if (state.groundBodyId != World.environmentId)
                    {
                        World.main.ApplyImpulseAtWorldPos(state.groundBodyId, -acc * World.main.dt * body.m, body.x.pos - groundNormal * inputData.AgentSphereRadius);
                    }

                    // Counteract gravity on slopes
                    //reBody.AddTorque(state.collisionResponseTorque, ForceMode.Impulse);
                    World.main.ApplyImpulse(inputData.AgentRecoilId, new ForceVector(state.collisionResponseTorque, float3.zero));
                }

                state.collisionResponseTorque = Vector3.zero;
                return state;
            }

            BallLocomotionData Jump(BallLocomotionData state, float3 targetVelocity)
            {
                ref var body = ref World.main.GetBody(state.locomotionData.AgentRecoilId);
                var currentVel = body.v;
                var v = math.length(World.main.gravity) * state.jumpDuration - currentVel.linear.y;

                var finalVelocity = targetVelocity - currentVel.linear;
                finalVelocity.y = v;

                //reBody.AddForce(finalVelocity, ForceMode.VelocityChange);
                currentVel.linear += finalVelocity;
                body.v = currentVel;
                state.jumpTimer = state.jumpDuration;

                // Apply force to object we were standing on
                if (state.groundBodyId != World.environmentId)
                {
                    World.main.ApplyImpulseAtWorldPos(state.groundBodyId, -finalVelocity * body.m, body.x.pos - math.down() * state.locomotionData.AgentSphereRadius);
                }
                return state;
            }

            // Have control in the air to allow jumping on objects while standing next to them
            BallLocomotionData MoveInAir(BallLocomotionData state)
            {
                ref var body = ref World.main.GetBody(state.locomotionData.AgentRecoilId);
                var currentVel = body.v;
                var finalVelocity = state.locomotionData.CurrentFacing * state.jumpSpeed - currentVel.linear;
                finalVelocity.y = 0f;
                //reBody.AddForce(finalVelocity, ForceMode.VelocityChange);
                currentVel.linear += finalVelocity;
                body.v = currentVel;
                return state;
            }

            BallLocomotionData UpdateSpeeds(BallLocomotionData state)
            {
                var currentFacing = state.locomotionData.CurrentFacing;
                state.currentTurnSpeed = Vector3.SignedAngle(state.previousFacing, currentFacing, Vector3.up) * Mathf.Deg2Rad / World.main.dt;
                state.currentMovementSpeed = Vector3.ProjectOnPlane(state.locomotionData.VelocityOnGround, state.locomotionData.Grounded ? state.locomotionData.GroundNormal : new float3(0, 1, 0)).magnitude;
                state.previousFacing = currentFacing;
                state.locomotionData.strafeVelocity = float3.zero;
                return state;
            }
        }
    }
}
