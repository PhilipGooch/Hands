using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Noodles
{
    // will create a joint between surface of the object and palm position
    [Obsolete]
    public class GripWithPalm : MonoBehaviour
    {
        new Collider collider;

        private void Awake()
        {
            collider = GetComponent<Collider>();
        }
        public void CalculateGrab(float3 shoulderPos, float3 poseHandPos, float3 actualHandPos, float3 actualPalmPos, float3 actualPalmDir, float handRadius, out float3 grabHandPos, out float3 grabHandAnchor, out float3 worldObjectAnchor, out float3 worldSurfaceAnchor)
        {
            worldObjectAnchor = worldSurfaceAnchor = collider.ClosestPoint(actualPalmPos);
            grabHandAnchor = actualPalmPos - actualHandPos;
            grabHandPos = worldObjectAnchor - grabHandAnchor;
        }
    }
}
