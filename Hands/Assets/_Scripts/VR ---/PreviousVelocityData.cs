using System.Collections.Generic;
using UnityEngine;

namespace VR.System
{
    internal class PreviousVelocityData
    {
        Queue<Vector3> previousVelocities = new Queue<Vector3>();
        internal Vector3 PeakVelocity { get; private set; }
        int velocityCacheFrames;

        public PreviousVelocityData(int framesToCache)
        {
            velocityCacheFrames = framesToCache;
        }

        internal void AddVelocity(Vector3 target)
        {
            previousVelocities.Enqueue(target);
            if (previousVelocities.Count > velocityCacheFrames)
            {
                previousVelocities.Dequeue();
            }

            Vector3 currentPeak = Vector3.zero;
            float currentMagnitude = 0f;
            foreach (var velocity in previousVelocities)
            {
                var mag = velocity.sqrMagnitude;
                if (mag > currentMagnitude)
                {
                    currentMagnitude = mag;
                    currentPeak = velocity;
                }
            }

            PeakVelocity = currentPeak;
        }
    }
}
