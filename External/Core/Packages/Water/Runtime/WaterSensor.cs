using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.Water
{
    [DisallowMultipleComponent]
    public sealed class WaterSensor : MonoBehaviour, IWaterSensor
    {
        public bool Submerged { get { return waterBodies.Count > 0; } }

        List<BodyOfWater> waterBodies = new List<BodyOfWater>(4);      // to generate a list of the water bodies 
        public IReadOnlyList<BodyOfWater> BodiesOfWater => waterBodies;

        /*public bool SampleDepth(float3 worldPos, out float depth, out float3 velocity)
        {
            if (!Submerged)
            {
                depth = 0.0f;
                velocity = Vector3.zero;
                return false;
            }
            else
            {
                var maxDepth = -1f;
                foreach (var body in waterBodies)
                {
                    var inBounds = body.SampleDepth(worldPos, out var bodyDepth, out var maxVelocity);
                    if (bodyDepth > maxDepth)
                    {
                        maxDepth = bodyDepth;
                        velocity = maxVelocity;
                    }
                }
                depth = maxDepth;
                velocity = Vector3.zero;
                return true;
            }
        }*/ // Unused, consider removal

        internal void OnEnterBody(BodyOfWater waterBody)
        {
            waterBodies.Add(waterBody);
        }

        internal void OnLeaveBody(BodyOfWater waterBody)
        {
            waterBodies.Remove(waterBody);
        }
    }
}
