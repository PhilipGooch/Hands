using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    public struct CameraConfig
    {
        [Tooltip("Offset on the target point, in PlayerSpace")]
        public float3 targetOffset;
        [Tooltip("Pitch range in degrees")]
        public float pitchRangeDeg;
        [Tooltip("Offset angle on top of any pitch calculation, in Degrees")]
        public float pitchOffsetDeg;
        [Tooltip("Offset angle on top of any yaw calculation, in Degrees")]
        public float yawOffsetDeg; //For Debug purposes, to see character from the side
        [Tooltip("Fixed field of view, when modifyFoVFromPitch is false, angle in degrees")]
        public float fovDeg;

        [Tooltip("Maps pitch to distance, can be used to avoid floor and provide overview when looking down")]
        public PitchToDistanceMap distanceMap;

        [Header("Jump Smoothing")]
        [Tooltip("Falling below this range from last known ground level will move camera along")]
        public float fallTrackingLimit;
        [Tooltip("Jumping within this range allows stationary camera, above it camera will follow to stay in frame")]
        public float jumpTrackingLimit;
        [Tooltip("Ground tracking speed expressed as halflife, used to remember ground Y when character was grounded")]
        public float groundCatchupHalflife;

        [Header("Target following")]
        [Tooltip("Max distance from target for camera smoothing")]
        public float maxTargetOffset;
        [Tooltip("Time in which half of distance to target will be covered")]
        public float targetCatchupHalflife;

        [Header("SpringArm")]
        [Tooltip("Closest zoom in allowed, in world units")]
        public float springMinDist;
        [Tooltip("Not sure how to describe, influences speed on top of springSpeed. Needs to be positive")]
        public float springPeriodExtend;

        public static CameraConfig defaults => new CameraConfig(true);
        public CameraConfig(bool fakeParamToEnsureAllVariablesAreInitializes)
        {
            targetOffset = new float3(0, .7f, 0);
            fovDeg = 80;
            pitchRangeDeg = 40;
            pitchOffsetDeg = 10;
            yawOffsetDeg = 0;
            distanceMap = PitchToDistanceMap.defaults;

            maxTargetOffset = 0.7f;
            targetCatchupHalflife = 0.02f;

            fallTrackingLimit = -.05f;
            jumpTrackingLimit = .7f;
            groundCatchupHalflife = .35f;

            springMinDist = 0.21f;
            springPeriodExtend = .2f;
        }
    }
}