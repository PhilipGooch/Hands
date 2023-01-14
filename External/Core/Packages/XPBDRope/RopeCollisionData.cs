using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace NBG.XPBDRope
{
    internal struct CollisionData
    {
        public int point;
        public int subdivision;
        public float3 position;
        public float3 normal;

        public CollisionData(int point, int subdivision, float3 position, float3 normal)
        {
            this.point = point;
            this.subdivision = subdivision;
            this.position = position;
            this.normal = normal;
        }

        public static CollisionData Empty()
        {
            return new CollisionData(-1, -1, float3.zero, float3.zero);
        }
    }

    public struct CollisionConstraints
    {
        public int point1;
        public int subdivision1;
        public int point2;
        public int subdivision2;
        public float3 collisionPoint;
        public float3 normal;
        public float minDistance;

        public CollisionConstraints(int point1, int subdivision1, int point2, int subdivision2, float3 collisionPoint, float3 normal, float minDistance)
        {
            this.point1 = point1;
            this.subdivision1 = subdivision1;
            this.point2 = point2;
            this.subdivision2 = subdivision2;
            this.collisionPoint = collisionPoint;
            this.normal = normal;
            this.minDistance = minDistance;
        }

        public static CollisionConstraints CreateEmpty()
        {
            return new CollisionConstraints(-1, -1, -1, -1, float3.zero, float3.zero, 0f);
        }
    }
}
