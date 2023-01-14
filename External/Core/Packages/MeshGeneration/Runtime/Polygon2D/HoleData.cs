using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.MeshGeneration
{
    internal class HoleData
    {
        internal List<float3> vertices;
        internal float4 boundingRect;//minX, minY, maxX, maxY
        internal float3 center;
        internal bool isConnected = false;
        internal HashSet<int> bannedVertices = new HashSet<int>();
        internal float distanceToCurrentHole = float.MaxValue;
        internal int index;

        internal const float margin = 0.1f;
        public HoleData()
        {
            vertices = new List<float3>(64);
            boundingRect = float4.zero;
        }

        public HoleData(List<float3> holeVertices, int index = 0)
        {
            vertices = new List<float3>(64);
            boundingRect = float4.zero;
            Create(holeVertices, index);
        }

        internal void Create(List<float3> holeVertices, int index = 0)
        {
            this.index = index;
            bannedVertices.Clear();
            vertices.Clear();
            int vertexCount = holeVertices.Count;
            for (int i = 0; i < vertexCount; i++)
                vertices.Add(holeVertices[i]);
            boundingRect = float4.zero;
            CalculateBounds();
            isConnected = false;
            distanceToCurrentHole = float.MaxValue;
        }

        internal void CalculateBounds()
        {
            boundingRect.x = boundingRect.y = float.MaxValue;
            boundingRect.z = boundingRect.w = float.MinValue;

            int vertexCount = vertices.Count;

            for (int i = 0; i < vertexCount; i++)
            {
                float3 currentVertex = vertices[i];
                boundingRect.x = math.min(boundingRect.x, currentVertex.x);
                boundingRect.y = math.min(boundingRect.y, currentVertex.y);
                boundingRect.z = math.max(boundingRect.z, currentVertex.x);
                boundingRect.w = math.max(boundingRect.w, currentVertex.y);
                center += currentVertex;
            }

            center /= vertexCount;


            boundingRect.x -= margin;
            boundingRect.y -= margin;
            boundingRect.z += margin;
            boundingRect.w += margin;
        }

        internal bool CastSegmentToBounds(float3 start, float3 end)
        {
            bool intersects = false;

            float3 offset = end - start;
            float segmentDistance = math.length(offset);
            float3 dir = math.normalize(end - start);
            float3 absDir = math.abs(dir);

            float4 offsets = boundingRect - start.xyxy;
            float4 offsetsAbs = math.abs(offsets);

            intersects = intersects || (start.x >= boundingRect.x && start.y >= boundingRect.y && start.x <= boundingRect.z && start.y <= boundingRect.w);

            bool4 signs = math.sign(offsets) == math.sign(dir.xyxy);

            float3 minXIntersection = start + (offsetsAbs.x / absDir.x) * dir;
            intersects = intersects || (minXIntersection.y >= boundingRect.y && minXIntersection.y <= boundingRect.w && signs.x && math.distance(start, minXIntersection) <= segmentDistance);

            float3 minYIntersection = start + (offsetsAbs.y / absDir.y) * dir;
            intersects = intersects || (minYIntersection.x >= boundingRect.x && minYIntersection.x <= boundingRect.z && signs.y && math.distance(start, minYIntersection) <= segmentDistance);

            float3 maxXIntersection = start + (offsetsAbs.z / absDir.x) * dir;
            intersects = intersects || (maxXIntersection.y >= boundingRect.y && maxXIntersection.y <= boundingRect.w && signs.z && math.distance(start, maxXIntersection) <= segmentDistance);

            float3 maxYIntersection = start + (offsetsAbs.w / absDir.y) * dir;
            intersects = intersects || (maxYIntersection.x >= boundingRect.x && maxYIntersection.x <= boundingRect.z && signs.w && math.distance(start, maxYIntersection) <= segmentDistance);

            return intersects;
        }

        internal void DebugBounds(Transform transform)
        {
            Gizmos.color = Color.blue;
            const float o = -0.4f;
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(boundingRect.x, boundingRect.y, o)), transform.TransformPoint(new Vector3(boundingRect.x, boundingRect.w, o)));
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(boundingRect.x, boundingRect.w, o)), transform.TransformPoint(new Vector3(boundingRect.z, boundingRect.w, o)));
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(boundingRect.z, boundingRect.w, o)), transform.TransformPoint(new Vector3(boundingRect.z, boundingRect.y, o)));
            Gizmos.DrawLine(transform.TransformPoint(new Vector3(boundingRect.z, boundingRect.y, o)), transform.TransformPoint(new Vector3(boundingRect.x, boundingRect.y, o)));
        }
    }
}
