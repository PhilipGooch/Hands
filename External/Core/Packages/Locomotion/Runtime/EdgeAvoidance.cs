//#define DEBUG_LOCOMOTION

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;
using UnityEngine.Profiling;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace NBG.Locomotion
{
    public class EdgeAvoidance : ILocomotionHandler
    {
        struct Config
        {
            public float detectionAngleIncrements;
            public float detectionDistanceMultiplier;
            public float timeToJumpOverEdge;
            public float timeToJumpIntoBottomlessPit;
            public float minDotProductToJump;
            public float maxHeightToJump;
            public float maxClimbableAngle;
            public int sidesToCheck;
        }

        Config config;
        public LayerMask climbableLayers;

        //State
        NativeList<SpherecastCommand> spherecastCommands;
        NativeArray<RaycastHit> raycastHits;
        NativeList<bool> blockedStateInfo;
        NativeList<EdgeJumpData> jumpData;

        struct EdgeJumpData
        {
            public bool workingUpToJump;
            public float accumulatedPushTowardsEdge;
        }

        /// <summary>
        /// Edge detection and avoidance. Agents can be forced to jump over edges if they detect a floor below.
        /// </summary>
        /// <param name="climbableLayers">Layers which are climbable and will be treated as floors.</param>
        /// <param name="maxClimbableAngle">Max angle in degrees that the agent can climb.</param>
        /// <param name="detectionAngleIncrements">How much to turn when detecting an edge. Determines the number of raycasts used. The lower the value, the greater the precision, but worse performance.</param>
        /// <param name="detectionDistanceMultiplier">How far to check for edges. The final detection distance will be calculated by multiplying the agent radius with this value.</param>
        /// <param name="timeToJumpOverEdge">How long does the agent have to move straight into an edge to jump over it.</param>
        /// <param name="minDotProductToJump">How direct does an agent have to face an edge to be considered moving stringt into it. This is the expected dot product value from 0 to 1</param>
        /// <param name="maxHeightToJump">What is the largest height the agent can jump from.</param>
        /// <param name="timeToJumpIntoBottomlessPit">How long does the agent have to move straight into an edge to jump into an infinitely deep pit. If negative, the agent will never jump.</param>
        public EdgeAvoidance(LayerMask climbableLayers, float maxClimbableAngle = 30f, float detectionAngleIncrements = 30f, float detectionDistanceMultiplier = 3f, float timeToJumpOverEdge = 0.5f,
            float minDotProductToJump = 0.85f, float maxHeightToJump = 4f, float timeToJumpIntoBottomlessPit = -1)
        {
            this.climbableLayers = climbableLayers;
            config.detectionAngleIncrements = detectionAngleIncrements;
            config.detectionDistanceMultiplier = detectionDistanceMultiplier;
            config.timeToJumpOverEdge = timeToJumpOverEdge;
            config.timeToJumpIntoBottomlessPit = timeToJumpIntoBottomlessPit;
            config.minDotProductToJump = minDotProductToJump;
            config.maxHeightToJump = maxHeightToJump;
            config.maxClimbableAngle = maxClimbableAngle;
            config.sidesToCheck = (int)(360f / detectionAngleIncrements);
            spherecastCommands = new NativeList<SpherecastCommand>(16, Allocator.Persistent);

            blockedStateInfo = new NativeList<bool>(16, Allocator.Persistent);
            jumpData = new NativeList<EdgeJumpData>(16, Allocator.Persistent);
        }

        public bool Enabled { get; set; } = true;

        JobHandle ILocomotionHandler.HandleLocomotion(NativeList<LocomotionData> locomotionData, JobHandle dependsOn)
        {
            Profiler.BeginSample("Edge Avoidance");
            dependsOn.Complete();
            EnsureCollectionSizesAreValid(locomotionData.Length);
            dependsOn = BatchSpherecasts(locomotionData, dependsOn);
            var edgeAvoidanceJob = new EdgeAvoidanceJob(locomotionData, blockedStateInfo, jumpData, raycastHits, config);
            dependsOn = edgeAvoidanceJob.Schedule(dependsOn);
            Profiler.EndSample();
            return dependsOn;
        }

        void EnsureCollectionSizesAreValid(int neededCount)
        {
            if (neededCount != jumpData.Length)
            {
                jumpData.Clear();
                for (int i = 0; i < neededCount; i++)
                {
                    jumpData.Add(new EdgeJumpData());
                }
            }

            int neededRaycastResults = neededCount * 2 * config.sidesToCheck + neededCount * 2;
            if (raycastHits.Length != neededRaycastResults)
            {
                if (raycastHits.IsCreated)
                {
                    raycastHits.Dispose();
                }
                raycastHits = new NativeArray<RaycastHit>(neededRaycastResults, Allocator.Persistent);
            }
        }

        static float GetEdgeCheckDistance(LocomotionData target, Config config, bool includeVelocity)
        {
            var result = target.AgentSphereRadius * config.detectionDistanceMultiplier;
            if (includeVelocity)
            {
                result += math.length(target.VelocityOnGround) * 0.33f;
            }
            return result;
        }

        JobHandle BatchSpherecasts(NativeList<LocomotionData> locomotionData, JobHandle dependsOn)
        {
            blockedStateInfo.Clear();
            spherecastCommands.Clear();
            dependsOn.Complete();

            for (int i = 0; i < locomotionData.Length; i++)
            {
                var inputData = locomotionData[i];
                var sphereRadius = inputData.AgentSphereRadius;
                var closeCheckDistance = GetEdgeCheckDistance(inputData, config, false);
                var jump = inputData.jump;

                // Check around the agent
                for (int x = 0; x < config.sidesToCheck; x++)
                {
                    if (jump)
                    {
                        blockedStateInfo.Add(true);
                    }
                    else
                    {
                        var targetDir = RotateAround(inputData.CurrentFacing, inputData.GroundNormal, x * config.detectionAngleIncrements);
                        var startPos = inputData.position + targetDir * closeCheckDistance;

                        CreateSpherecastCommands(startPos, targetDir, sphereRadius, closeCheckDistance);
                    }
                }

                // Check further away in the direction we're moving at
                if (jump)
                {
                    blockedStateInfo.Add(true);
                }
                else
                {
                    var targetDir = inputData.CurrentFacing;
                    var farCheckDistance = GetEdgeCheckDistance(inputData, config, true);
                    var startPos = inputData.position + targetDir * farCheckDistance;

                    CreateSpherecastCommands(startPos, targetDir, sphereRadius, farCheckDistance);
                }
            }

            dependsOn = SpherecastCommand.ScheduleBatch(spherecastCommands, raycastHits, 2, dependsOn);
            return dependsOn;
        }

        void CreateSpherecastCommands(float3 startPos, float3 targetDir, float sphereRadius, float checkDistance)
        {
            var smallOffset = sphereRadius * 0.5f;
            var blockedByWall = Physics.CheckSphere(startPos + new float3(0, -1, 0) * smallOffset, sphereRadius, climbableLayers);
            blockedStateInfo.Add(blockedByWall);

            if (!blockedByWall)
            {
                // Check for floor
#if DEBUG_LOCOMOTION
                Debug.DrawRay(startPos, Vector3.up, Color.red);
#endif
                spherecastCommands.Add(new SpherecastCommand(startPos + math.up() * smallOffset, sphereRadius, math.down(), config.maxHeightToJump + smallOffset, climbableLayers));

                var downOffset = new float3(0, -sphereRadius, 0);
                var endPos = startPos + downOffset;
                // Check the path back to the agent
                spherecastCommands.Add(new SpherecastCommand(endPos, sphereRadius, -targetDir, checkDistance + sphereRadius, climbableLayers));
            }
        }

        void ILocomotionHandler.Dispose()
        {
            spherecastCommands.Dispose();
            blockedStateInfo.Dispose();
            jumpData.Dispose();
            if (raycastHits.IsCreated)
            {
                raycastHits.Dispose();
            }
        }

        [BurstCompile]
        struct EdgeAvoidanceJob : IJob
        {
            NativeList<LocomotionData> locomotionData;
            NativeList<bool> blockedStateInfo;
            NativeList<EdgeJumpData> jumpData;
            NativeArray<RaycastHit> raycastHits;
            Config config;

            public EdgeAvoidanceJob(NativeList<LocomotionData> locomotionData, NativeList<bool> blockedStateInfo, NativeList<EdgeJumpData> jumpData,
                NativeArray<RaycastHit> raycastHits, Config config)
            {
                this.locomotionData = locomotionData;
                this.blockedStateInfo = blockedStateInfo;
                this.jumpData = jumpData;
                this.raycastHits = raycastHits;
                this.config = config;
            }

            public void Execute()
            {
                MoveFromEdges();
            }

            void MoveFromEdges()
            {
                int blockedStateIndex = 0;
                int raycastIndex = 0;

                for (int i = 0; i < locomotionData.Length; i++)
                {
                    var inputData = locomotionData[i];
                    if (!inputData.Grounded)
                    {
                        continue;
                    }
                    UpdateJumpState(i);
                    var sphereRadius = inputData.AgentSphereRadius;
                    var closeCheckDistance = GetEdgeCheckDistance(inputData, config, false);
                    var maxHeightToRollOff = Mathf.Tan(config.maxClimbableAngle * Mathf.Deg2Rad) * closeCheckDistance;

                    for (int x = 0; x < config.sidesToCheck; x++)
                    {
                        if (!blockedStateInfo[blockedStateIndex]) // We didn't detect a wall, check if it's a hole
                        {
                            var targetDir = RotateAround(inputData.CurrentFacing, inputData.GroundNormal, x * config.detectionAngleIncrements);
                            var startPos = inputData.position + targetDir * closeCheckDistance;

                            if (DetectEdge(startPos, raycastIndex, sphereRadius, maxHeightToRollOff, out var edgeInfo))
                            {
                                inputData = AvoidEdge(inputData, edgeInfo, i);
                            }
                            raycastIndex += 2;
                        }
                        blockedStateIndex++;
                    }

                    if (!blockedStateInfo[blockedStateIndex])
                    {
                        var targetDir = inputData.CurrentFacing;
                        var farCheckDistance = GetEdgeCheckDistance(inputData, config, true);
                        var startPos = inputData.position + targetDir * farCheckDistance;
                        if (DetectEdge(startPos, raycastIndex, sphereRadius, maxHeightToRollOff, out var edgeInfo))
                        {
                            inputData = AvoidEdge(inputData, edgeInfo, i);
                        }
                        raycastIndex += 2;
                    }
                    blockedStateIndex++;

                    var jData = jumpData[i];
                    if (!jData.workingUpToJump && jData.accumulatedPushTowardsEdge > 0f)
                    {
                        jData.accumulatedPushTowardsEdge -= World.main.dt;
                        jData.accumulatedPushTowardsEdge = Mathf.Clamp(jData.accumulatedPushTowardsEdge, 0f, config.timeToJumpOverEdge);
                        jumpData[i] = jData;
                    }

                    locomotionData[i] = inputData;
                }
            }

            void UpdateJumpState(int index)
            {
                var jData = jumpData[index];
                jData.workingUpToJump = false;
                jumpData[index] = jData;
            }

            bool DetectEdge(float3 checkPos, int raycastIndex, float sphereRadius, float maxHeightToRollOff, out EdgeInfo edgeInfo)
            {
                edgeInfo = new EdgeInfo();
                var floorCastInfo = raycastHits[raycastIndex];
                var backCastInfo = raycastHits[raycastIndex + 1];

                var floorHeight = GetFloorHeight(checkPos, floorCastInfo, sphereRadius);

                // If floor is close enough, don't do anything and just roll off. Otherwise, try jumping
                if (floorHeight > maxHeightToRollOff)
                {
                    // No good way to check if we hit anything, yet.
                    // TODO: in 2021 there is an API for collider ID.
                    if (backCastInfo.distance > 0f)
                    {
                        edgeInfo.edgePoint = backCastInfo.point;
                        edgeInfo.edgeNormal = backCastInfo.normal;
                        edgeInfo.height = floorHeight;
                        return true;
                    }
                }
                return false;
            }

            float GetFloorHeight(float3 castPos, RaycastHit hitInfo, float sphereRadius)
            {
                // No good way to check if we hit anything, yet.
                // TODO: in 2021 there is an API for collider ID.
                if (hitInfo.distance > 0)
                {
                    return Mathf.Clamp(castPos.y - hitInfo.point.y - sphereRadius, 0f, float.MaxValue);
                }
                return float.MaxValue;
            }

            LocomotionData AvoidEdge(LocomotionData inputData, EdgeInfo edge, int agentIndex)
            {
                var jData = jumpData[agentIndex];
                var sphereRadius = inputData.AgentSphereRadius;
                var edgeWithOffset = edge.edgePoint + new float3(0, sphereRadius, 0);
                var diff = Vector3.Project(inputData.position - edgeWithOffset, edge.edgeNormal);
                var distance = diff.magnitude;
                var direction = edge.edgeNormal;

                if (!jData.workingUpToJump)
                {
                    inputData = HandleJumping(inputData, edge, agentIndex);
                }

                if (!inputData.jump)
                {
                    if (distance < sphereRadius)
                    {
                        var correction = sphereRadius - distance;
                        inputData.strafeVelocity -= direction * correction / World.main.dt;
                    }

                    var velocityTowardsEdge = Vector3.Dot(inputData.VelocityOnGround, direction);
                    var offsetDistance = distance - sphereRadius;
                    if (velocityTowardsEdge > offsetDistance)
                    {
                        var correction = velocityTowardsEdge - offsetDistance;
                        inputData.strafeVelocity -= direction * correction;
                    }

                    var inputTowardsEdge = Vector3.Dot(inputData.targetVelocity, direction);
                    if (inputTowardsEdge > 0f)
                    {
                        var stoppingDistance = Mathf.Max(inputTowardsEdge, sphereRadius * 2f);
                        var correctionAmount = Mathf.InverseLerp(stoppingDistance, 0f, offsetDistance);
                        inputData.targetVelocity -= direction * correctionAmount * inputTowardsEdge;
                    }
                }

                return inputData;
            }

            LocomotionData HandleJumping(LocomotionData inputData, EdgeInfo edge, int agentIndex)
            {
                var jData = jumpData[agentIndex];
                var edgeOnFloor = Vector3.ProjectOnPlane(edge.edgeNormal, inputData.GroundNormal).normalized;
                var wantedMovementTowardsEdge = math.dot(math.normalizesafe(inputData.targetVelocity, float3.zero), edgeOnFloor);

                var cliffLowEnough = edge.height < config.maxHeightToJump;
                var canJumpIntoBottomlessPits = config.timeToJumpIntoBottomlessPit >= 0f;
                jData.workingUpToJump = wantedMovementTowardsEdge > config.minDotProductToJump && (cliffLowEnough || canJumpIntoBottomlessPits);

                // Moving straight into the edge, there is something to jump to - try jumping
                if (jData.workingUpToJump)
                {
                    jData.accumulatedPushTowardsEdge += World.main.dt;
                    var canJumpCliff = cliffLowEnough && jData.accumulatedPushTowardsEdge > config.timeToJumpOverEdge;
                    var canJumpPit = canJumpIntoBottomlessPits && jData.accumulatedPushTowardsEdge > config.timeToJumpIntoBottomlessPit;
                    if (canJumpCliff || canJumpPit)
                    {
                        inputData.jump = true;
                        jData.accumulatedPushTowardsEdge = 0f;
                    }
                }

                jumpData[agentIndex] = jData;
                return inputData;
            }
        }

        static float3 RotateAround(float3 target, float3 normal, float angle)
        {
            return math.mul(quaternion.AxisAngle(normal, math.radians(angle)), target);
        }

        public struct EdgeInfo
        {
            public float3 edgePoint;
            public float3 edgeNormal;
            public float height;

            public EdgeInfo(float3 edgePoint, float3 edgeNormal, float height)
            {
                this.edgePoint = edgePoint;
                this.edgeNormal = edgeNormal;
                this.height = height;
            }
        }
    }
}
