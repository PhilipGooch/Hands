using System;
using NBG.Core.Easing;
using Recoil;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    [Serializable]
    public struct PitchToDistanceMap
    {
        [Tooltip("Distance from target when in normal pitch range.")]
        public float distNormal;
        [Tooltip("Distance when camera looks up (pitchMin).")]
        public float distLookUp;
        [Tooltip("Distance when camera looks down (pitchMax)")]
        public float distLookDown;

        [Tooltip("Minimum pitch. Everything below gets mapped to distLookUp")]
        public float pitchMin;
        [Tooltip("Lower bound of normal pitch range. Uses distNormal")]
        public float pitchNormalMin;
        [Tooltip("Upper bound of normal pitch range. Uses distNormal")]
        public float pitchNormalMax;
        [Tooltip("Maximum pitch. Everything above gets mapped to distLookDown")]
        public float pitchMax;

        public static PitchToDistanceMap defaults => new PitchToDistanceMap(true);
        public PitchToDistanceMap OffsetDist(float offset)
        {
            var res = this;
            res.distLookUp += offset;
            res.distNormal += offset;
            res.distLookDown += offset;
            return res;
        }
        public PitchToDistanceMap(bool fakeParamToEnsureAllVariablesAreInitializes)
        {
            distLookUp = 2f;
            distNormal = 2.8f;
            distLookDown = 3.2f;
            pitchMin = math.radians(-33);
            pitchNormalMin = -math.radians(7);
            pitchNormalMax = math.radians(30);
            pitchMax = math.radians(53);
        }
        public float PitchToDist(float pitch)
        {
            if (pitch < pitchNormalMin) return Ease.easeOutSine(distLookUp, distNormal, re.InverseLerp(pitchMin, pitchNormalMin, pitch));
            if (pitch > pitchNormalMax) return Ease.easeOutSine(distLookDown, distNormal, re.InverseLerp(pitchMax, pitchNormalMax, pitch));
            return distNormal;
        }
    }
}


// Maps camera pitch to distance from target
// Default behavior keeps constant distance to target when pitch is within horizontal range
// Camera angles looking up gets closer to target to prevent collision with ground
// Camera angles from above increase distance to give better overview