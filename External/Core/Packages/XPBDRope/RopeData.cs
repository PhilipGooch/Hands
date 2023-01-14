using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.XPBDRope
{
    internal struct RopeData
    {
        public NativeArray<int> activeBoneCounts;
        public NativeArray<int> totalBoneCounts;
        public NativeArray<int> ropeEndPoints;
        public NativeArray<float> ropeLengths;
        public NativeArray<float> ropeRadii;
        public NativeArray<int> subdivisions;
        public NativeArray<float> segmentLengths;
        public NativeArray<float> segmentInvMass;
        public NativeArray<float> elasticCompliance;
        public NativeArray<float> bendCompliance;
        public NativeArray<float> bendLimit;
        public NativeArray<float> twistLimits;
        public NativeArray<int> connectedBodies;
        public NativeArray<float3> connectedBodyAnchor;
        public NativeArray<float> maxSegmentSeparation;
        public NativeArray<float> staticFriction;
        public NativeArray<float> dynamicFriction;
        public int pointCount;
        public int totalSubdivisions;

        public RopeData(int pointCount, NativeArray<float> staticFriction, NativeArray<float> dynamicFriction,
            NativeArray<int> activeBoneCounts, NativeArray<int> totalBoneCounts, NativeArray<int> ropeEndPoints, NativeArray<float> ropeLengths, NativeArray<float> ropeRadii,
            NativeArray<float> segmentLengths, NativeArray<float> segmentInvMass, NativeArray<float> elasticCompliance, NativeArray<float> bendCompliance, NativeArray<float> bendLimit,
            NativeArray<float> twistLimits, NativeArray<int> connectedBodies, NativeArray<float3> connectedBodyAnchor, NativeArray<float> maxSegmentSeparation)
        {
            this.pointCount = pointCount;
            this.activeBoneCounts = activeBoneCounts;
            this.totalBoneCounts = totalBoneCounts;
            this.ropeEndPoints = ropeEndPoints;
            this.ropeLengths = ropeLengths;
            this.ropeRadii = ropeRadii;
            this.segmentLengths = segmentLengths;
            this.segmentInvMass = segmentInvMass;
            this.elasticCompliance = elasticCompliance;
            this.bendCompliance = bendCompliance;
            this.bendLimit = bendLimit;
            this.twistLimits = twistLimits;
            this.connectedBodies = connectedBodies;
            this.connectedBodyAnchor = connectedBodyAnchor;
            this.maxSegmentSeparation = maxSegmentSeparation;
            this.staticFriction = staticFriction;
            this.dynamicFriction = dynamicFriction;
            subdivisions = new NativeArray<int>(ropeRadii.Length, Allocator.Persistent);
            totalSubdivisions = 0;
        }

        static void RecalculateSubdivisionsInternal(ref int totalSubdivisions, NativeArray<int> ropeBoneCounts, NativeArray<float> segmentLengths,
            NativeArray<int> subdivisions, NativeArray<float> ropeRadii)
        {
            totalSubdivisions = 0;
            int boneStartIndex = 0;
            for (int i = 0; i < ropeBoneCounts.Length; i++)
            {
                float averageLength = 0f;
                for (int x = boneStartIndex; x < boneStartIndex + ropeBoneCounts[i]; x++)
                {
                    averageLength += segmentLengths[x];
                }
                averageLength /= ropeBoneCounts[i];

                subdivisions[i] = (int)math.round(averageLength / ropeRadii[i]);
                if (subdivisions[i] < 3)
                {
                    subdivisions[i] = 3;
                }
                totalSubdivisions += subdivisions[i] * ropeBoneCounts[i] + 1;
                boneStartIndex += ropeBoneCounts[i] + 1;
            }
        }

        public void RecalculateSubdivisions()
        {
            RecalculateSubdivisionsInternal(ref totalSubdivisions, totalBoneCounts, segmentLengths, subdivisions, ropeRadii);
        }

        public int GetRopeIndexForPoint(int point)
        {
            for (int i = 0; i < ropeEndPoints.Length; i++)
            {
                if (point < ropeEndPoints[i])
                {
                    return i;
                }
            }
            Debug.LogError($"Failed to find rope index for point {point}!");
            return 0;
        }

        public int GetInnerSubdivisionCount()
        {
            int sum = 0;
            for (int i = 0; i < activeBoneCounts.Length; i++)
            {
                sum += activeBoneCounts[i] * (subdivisions[i] - 1);
            }
            return sum;
        }

        public bool IsEndPoint(int targetRope, int index)
        {
            return index == ropeEndPoints[targetRope] - 1;
        }

        public bool IsStartPoint(int targetRope, int index)
        {
            return GetStartPoint(targetRope) == index;
        }

        public int GetStartPoint(int targetRope)
        {
            var offset = GetStartPointOffset(targetRope);
            if (targetRope > 0)
            {
                return ropeEndPoints[targetRope - 1] + offset;
            }
            return offset;
        }

        int GetStartPointOffset(int targetRope)
        {
            return totalBoneCounts[targetRope] - activeBoneCounts[targetRope];
        }

        public float GetProgress(int targetRope, int subdivision)
        {
            var maxSubdivisions = subdivisions[targetRope];
            if (maxSubdivisions <= 1)
            {
                return 0;
            }
            return (float)subdivision / (maxSubdivisions - 1);
        }

        public void Dispose()
        {
            activeBoneCounts.Dispose();
            totalBoneCounts.Dispose();
            ropeEndPoints.Dispose();
            ropeLengths.Dispose();
            ropeRadii.Dispose();
            subdivisions.Dispose();
            segmentLengths.Dispose();
            segmentInvMass.Dispose();
            elasticCompliance.Dispose();
            bendCompliance.Dispose();
            bendLimit.Dispose();
            twistLimits.Dispose();
            connectedBodies.Dispose();
            connectedBodyAnchor.Dispose();
            maxSegmentSeparation.Dispose();
            staticFriction.Dispose();
            dynamicFriction.Dispose();
        }
    }
}