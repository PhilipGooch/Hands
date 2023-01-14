using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using Unity.Mathematics;
using UnityEngine.Profiling;
using Unity.Jobs;
using Unity.Collections;

namespace NBG.Locomotion
{
    public class ObstacleAvoidance : ILocomotionHandler
    {
        LayerMask obstacleLayers;
        float avoidanceDistanceMultiplier;
        int maxRays;
        float degreeOffset;
        float currentFacingBias;

        /// <summary>
        /// Avoid obstacles such as walls or objects in locomotion. It works by doing raycasts with increasing offsets until we detect a free path. Can also jump over low obstacles.
        /// </summary>
        /// <param name="obstacleLayers"> Layers which will be avoided.</param>
        /// <param name="avoidanceDistanceMultiplier"> The distance for obstacle avoidance will be calculated by multiplying the agent radius with the avoidanceDistanceMultiplier.</param>
        /// <param name="maxRays">Determines how many different directions to check when trying to avoid an obstacle in both directions.</param>
        /// <param name="degreeOffset">How much should we turn with each ray.</param>
        /// <param name="currentFacingBias">Set this from 0 to 1. Determines which direction to prefer when avoiding obstacles - the current input direction or the current facing direction.</param>
        public ObstacleAvoidance(LayerMask obstacleLayers, float avoidanceDistanceMultiplier = 4f, int maxRays = 9, float degreeOffset = 20f, float currentFacingBias = 0f)
        {
            this.obstacleLayers = obstacleLayers;
            this.avoidanceDistanceMultiplier = avoidanceDistanceMultiplier;
            this.maxRays = maxRays;
            this.degreeOffset = degreeOffset;
            this.currentFacingBias = Mathf.Clamp01(currentFacingBias);
        }

        public bool Enabled { get; set; } = true;

        JobHandle ILocomotionHandler.HandleLocomotion(NativeList<LocomotionData> locomotionData, JobHandle dependsOn)
        {
            dependsOn.Complete();
            Profiler.BeginSample("Obstacle Avoidance");
            for (int i = 0; i < locomotionData.Length; i++)
            {
                var inputData = locomotionData[i];
                if (!inputData.Grounded)
                {
                    continue;
                }

                float3 currentDir = math.normalizesafe(inputData.targetVelocity, float3.zero);

                if (IsBlocked(currentDir, inputData, out var needToJump))
                {
                    currentDir = FindFreePath(currentDir, inputData, out needToJump);
                }

                if (needToJump)
                {
                    inputData.jump = needToJump;
                }
                inputData.targetVelocity = currentDir * math.length(inputData.targetVelocity);
                locomotionData[i] = inputData;
            }
            Profiler.EndSample();
            return dependsOn;
        }

        float3 FindFreePath(Vector3 currentDir, LocomotionData inputData, out bool needToJump)
        {
            float3 groundNormal = inputData.GroundNormal;
            float3 currentFacing = inputData.CurrentFacing;
            float3 leftTurn = currentDir;
            float3 rightTurn = currentDir;
            bool leftPathAvailable = false;
            bool rightPathAvailable = false;
            bool leftJump = false;
            bool rightJump = false;

            for (int i = 1; i <= maxRays; i++)
            {
                if (!leftPathAvailable)
                {
                    leftTurn = RotateAround(currentDir, groundNormal, degreeOffset * i);
                    if (!IsBlocked(leftTurn, inputData, out leftJump))
                    {
                        leftPathAvailable = true;
                    }
                }

                if (!rightPathAvailable)
                {
                    rightTurn = RotateAround(currentDir, groundNormal, -degreeOffset * i);
                    if (!IsBlocked(rightTurn, inputData, out rightJump))
                    {
                        rightPathAvailable = true;
                    }
                }

                if (leftPathAvailable && rightPathAvailable)
                {
                    break;
                }
            }

            if (leftPathAvailable && rightPathAvailable)
            {
                // Determine which direction to prefer - the wanted velocity direction or the current facing direction
                var biasedDirection = Vector3.Lerp(currentDir, currentFacing, currentFacingBias);
                if (math.dot(biasedDirection, leftTurn) > math.dot(biasedDirection, rightTurn))
                {
                    needToJump = leftJump;
                    return leftTurn;
                }
                else
                {
                    needToJump = rightJump;
                    return rightTurn;
                }
            }
            else // Only one or no path available
            {
                if (leftPathAvailable)
                {
                    needToJump = leftJump;
                    return leftTurn;
                }
                needToJump = rightJump;
                return rightTurn;
            }
        }

        Vector3 RotateAround(float3 target, float3 normal, float angle)
        {
            return Quaternion.AngleAxis(angle, normal) * target;
        }

        bool IsBlocked(float3 direction, LocomotionData inputData, out bool needToJump)
        {
            needToJump = false;
            var sphereRadius = inputData.AgentSphereRadius;
            var avoidanceDistance = sphereRadius * avoidanceDistanceMultiplier;
            var offsetSize = sphereRadius * 0.25f;
            // Offset is needed to avoid starting the spherecast inside a wall, failing to detect it
            var backwardOffset = -direction * offsetSize;
            // Offset upwards to avoid hitting the floor we're currently on
            var upwardOffset = inputData.GroundNormal * offsetSize;
            var blockedFront = Physics.SphereCast(inputData.position + backwardOffset + upwardOffset, sphereRadius, direction, out var hitInfo, avoidanceDistance + offsetSize, obstacleLayers);

            // Check if we can jump on this
            if (blockedFront)
            {
                return !CanJumpOver(hitInfo.point, inputData, out needToJump);
            }
            else
            {
                return false;
            }
        }

        bool CanJumpOver(float3 point, LocomotionData inputData, out bool needToJump)
        {
            var sphereRadius = inputData.AgentSphereRadius;
            needToJump = false;
            var castDistance = inputData.JumpHeight + sphereRadius * 4f;
            var startPoint = point + math.up() * castDistance;


            if (Physics.CheckSphere(point + math.up() * inputData.JumpHeight, sphereRadius, obstacleLayers))
            {
                return false;
            }

            var upwardsHit = Physics.SphereCast(startPoint, sphereRadius, -Vector3.up, out var upHit, castDistance, obstacleLayers);
            if (upwardsHit)
            {
                var heightDiff = upHit.point.y + sphereRadius - inputData.position.y;
                var canRollOver = heightDiff < sphereRadius / 4f;
                if (canRollOver)
                {
                    return true;
                }
                var canJump = heightDiff < inputData.JumpHeight * 0.95f;
                var diffToObstacle = (point - inputData.position);
                var normalizedDiff = math.normalizesafe(diffToObstacle, float3.zero);
                var distance = math.length(diffToObstacle) - sphereRadius;
                var linearVelocity = Vector3.Dot(Vector3.Project(inputData.velocity, diffToObstacle), normalizedDiff);
                var movingFastEnoughForJump = linearVelocity * inputData.JumpDuration > distance;
                var closeEnoughForJump = distance < sphereRadius * 1.5f && Vector3.Dot(inputData.CurrentFacing, normalizedDiff) > 0.8f;
                needToJump = canJump && (closeEnoughForJump || movingFastEnoughForJump);
                return canJump;
            }
            else // If we hit nothing, that means that this is a wall and we can't jump or pass through here
            {
                return false;
            }
        }

        void ILocomotionHandler.Dispose()
        {
        }
    }
}
