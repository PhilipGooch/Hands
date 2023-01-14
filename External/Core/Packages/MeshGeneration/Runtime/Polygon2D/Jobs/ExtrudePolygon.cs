using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NBG.MeshGeneration
{
    [BurstCompile]
    public struct ExtrudePolygon : IJob
    {
        [ReadOnly]
        public NativeArray<float3> polygonVertices;
        [ReadOnly]
        public NativeArray<int> triangles;

        [WriteOnly]
        public NativeArray<float3> extrudedPolygonVertices;
        [WriteOnly]
        public NativeArray<int> extrudedTriangles;

        public float depth;

        public void Execute()
        {
            int extrudedVertexStart = polygonVertices.Length;
            float3 offset = new float3(0.0f, 0.0f, depth);

            for (int i = 0; i < polygonVertices.Length; i++)
            {
                extrudedPolygonVertices[i] = polygonVertices[i];
                extrudedPolygonVertices[extrudedVertexStart + i] = polygonVertices[i] + offset;

                extrudedPolygonVertices[extrudedVertexStart * 2 + i] = polygonVertices[i];
                extrudedPolygonVertices[extrudedVertexStart * 3 + i] = polygonVertices[i] + offset;
            }


            int extrudedTrianglesStart = triangles.Length;
            int newIndex;
            //normal triangles
            for (int i = 0; i < triangles.Length; i += 3)
            {
                extrudedTriangles[i] = triangles[i];
                extrudedTriangles[i + 1] = triangles[i + 1];
                extrudedTriangles[i + 2] = triangles[i + 2];

                newIndex = extrudedTrianglesStart + i;
                extrudedTriangles[newIndex] = triangles[i] + extrudedVertexStart;
                extrudedTriangles[newIndex + 1] = triangles[i + 2] + extrudedVertexStart;
                extrudedTriangles[newIndex + 2] = triangles[i + 1] + extrudedVertexStart;
            }

            //this uses the duplicate vertex, thats why we offset + extrudedVertexStart*2
            int edgeIndexStart = triangles.Length * 2;
            for (int i = 0; i < polygonVertices.Length - 1; i++)
            {
                int offset2 = i * 6;
                extrudedTriangles[edgeIndexStart + offset2] = i + extrudedVertexStart * 2;
                extrudedTriangles[edgeIndexStart + 1 + offset2] = i + 1 + extrudedVertexStart + extrudedVertexStart * 2;
                extrudedTriangles[edgeIndexStart + 2 + offset2] = i + 1 + extrudedVertexStart * 2;

                extrudedTriangles[edgeIndexStart + 3 + offset2] = i + extrudedVertexStart * 2;
                extrudedTriangles[edgeIndexStart + 4 + offset2] = i + extrudedVertexStart + extrudedVertexStart * 2;
                extrudedTriangles[edgeIndexStart + 5 + offset2] = i + 1 + extrudedVertexStart + extrudedVertexStart * 2;
            }

            int lastQuadIndex = (polygonVertices.Length - 1) * 6;

            extrudedTriangles[edgeIndexStart + lastQuadIndex] = polygonVertices.Length - 1 + extrudedVertexStart * 2;
            extrudedTriangles[edgeIndexStart + lastQuadIndex + 1] = extrudedVertexStart + extrudedVertexStart * 2;
            extrudedTriangles[edgeIndexStart + lastQuadIndex + 2] = 0 + extrudedVertexStart * 2;

            extrudedTriangles[edgeIndexStart + 3 + lastQuadIndex] = polygonVertices.Length - 1 + extrudedVertexStart * 2;
            extrudedTriangles[edgeIndexStart + 4 + lastQuadIndex] = polygonVertices.Length - 1 + extrudedVertexStart + extrudedVertexStart * 2;
            extrudedTriangles[edgeIndexStart + 5 + lastQuadIndex] = extrudedVertexStart + extrudedVertexStart * 2;
        }
    }
}
