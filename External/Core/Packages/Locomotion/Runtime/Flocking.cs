using Recoil;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Locomotion
{
    public class Flocking : ILocomotionHandler
    {
        struct Configuration
        {
            public float minDistanceToPullTogether;
            public float centerPullFalloffDistance;
            public float separation;
            public float maxFlockDistanceSqr;
        }

        Configuration configuration;

        /// <summary>
        /// Flocking based on booids. Clumps nearby agents together, forces some separation and tries to align the directions of nearby agents.
        /// </summary>
        /// <param name="maxFlockDistance">How close do other agents need to be in order to be considered part of the flock.</param>
        /// <param name="minDistanceToPullTogether">Closest distance to still pull agents together into the center of the flock. This helps avoid flip-flopping between separation and flocking.</param>
        /// <param name="pullFalloffDistance">How quickly does the pull to center stop. This makes the agents stop in a more smooth way when nearing the center of the flock.</param>
        /// <param name="separation">How far should each agent be away from other agents.</param>
        public Flocking(float maxFlockDistance = 2f, float minDistanceToPullTogether = 0.75f, float pullFalloffDistance = 0.1f, float separation = 0.5f)
        {
            configuration.minDistanceToPullTogether = minDistanceToPullTogether;
            configuration.centerPullFalloffDistance = pullFalloffDistance;
            configuration.separation = separation;
            configuration.maxFlockDistanceSqr = maxFlockDistance * maxFlockDistance;
        }

        public bool Enabled { get; set; } = true;

        JobHandle ILocomotionHandler.HandleLocomotion(NativeList<LocomotionData> data, JobHandle dependsOn)
        {
            var job = new FlockingJob(data, configuration);
            dependsOn = job.Schedule(dependsOn);
            return dependsOn;
        }

        void ILocomotionHandler.Dispose()
        {
        }

        [BurstCompile]
        struct FlockingJob : IJob
        {
            NativeList<LocomotionData> data;
            Configuration config;

            public FlockingJob(NativeList<LocomotionData> data, Configuration configuration)
            {
                this.data = data;
                this.config = configuration;
            }

            public void Execute()
            {
                for (int d = 0; d < data.Length; d++)
                {
                    var currentLoco = data[d];
                    if (!currentLoco.Grounded)
                    {
                        continue;
                    }
                    var inputSpeed = math.length(currentLoco.targetVelocity);

                    float3 averageFacing = float3.zero;
                    float3 averagePosition = float3.zero;
                    float3 separationVector = float3.zero;

                    int localFlockSize = 0;

                    for (int i = 0; i < data.Length; i++)
                    {
                        // Ignore our own values
                        if (i == d)
                        {
                            continue;
                        }
                        var otherLoco = data[i];
                        var diff = currentLoco.position - otherLoco.position;
                        if (math.lengthsq(diff) < config.maxFlockDistanceSqr)
                        {
                            localFlockSize++;
                            averageFacing += otherLoco.CurrentFacing;
                            averagePosition += otherLoco.position;

                            if (i != d)
                            {
                                var projectedDiff = Vector3.ProjectOnPlane(diff, currentLoco.GroundNormal);
                                var diffMag = projectedDiff.magnitude;
                                if (diffMag < config.separation)
                                {
                                    var correction = config.separation - diffMag;
                                    separationVector += math.normalize(projectedDiff) * correction;
                                }
                            }
                        }
                    }

                    // If there's no flock, do nothing
                    if (localFlockSize == 0)
                    {
                        continue;
                    }

                    averageFacing = math.normalize(averageFacing / localFlockSize) * inputSpeed;
                    averagePosition /= localFlockSize;

                    var toCenter = Vector3.ProjectOnPlane(averagePosition - currentLoco.position, currentLoco.GroundNormal);
                    // Don't pull flock together if they are already close to avoid flip-flopping on the edge of the min separation
                    // This also creates more organic looking flocks instead of a uniform distrubution
                    var distanceToCenter = toCenter.magnitude;
                    toCenter = toCenter.normalized;
                    if (distanceToCenter < config.minDistanceToPullTogether + config.centerPullFalloffDistance)
                    {
                        // Smoothly reduce speed to zero depending on how close to the center the ball is
                        var falloff = math.clamp(config.minDistanceToPullTogether + config.centerPullFalloffDistance - distanceToCenter, 0f, 1f) / config.centerPullFalloffDistance;
                        toCenter = Vector3.Lerp(toCenter, Vector3.zero, falloff);
                    }
                    //Debug.DrawRay(body.position, separationVector, Color.green);
                    var totalAverage = (currentLoco.targetVelocity + averageFacing) / 2f + re.Clamp(toCenter, 1f) * 2f + separationVector.Clamp(1f);
                    totalAverage = Vector3.ProjectOnPlane(totalAverage, currentLoco.GroundNormal).normalized * math.length(totalAverage);
                    //Debug.DrawRay(body.position, inputData.targetVelocity);
                    //Debug.DrawRay(body.position, totalAverage * 5f, Color.red);
                    currentLoco.targetVelocity = totalAverage;

                    data[d] = currentLoco;
                }
            }
        }
    }
}
