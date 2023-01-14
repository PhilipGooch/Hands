using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NBG.MeshGeneration
{
    public struct TriangleMesh : IDisposable
    {
        public NativeList<float3> vertices;
        public NativeList<int> indices;

        private NativeParallelHashMap<float3, int> vertexIndexMap;
        public int vertexCount => vertices.Length;
        public int triangleIndexCount => indices.Length;

        public TriangleMesh(int size = 4096)
        {
            vertexIndexMap = new NativeParallelHashMap<float3, int>(8192, Allocator.Persistent);
            vertices = new NativeList<float3>(size, Allocator.Persistent);
            indices = new NativeList<int>(size, Allocator.Persistent);
        }
        public void Clear()
        {
            vertexIndexMap.Clear();
            vertices.Clear();
            indices.Clear();
        }
        public void AddVertex(float3 pos)
        {
            if (!vertexIndexMap.TryGetValue(pos, out int vertexIndex))
            {
                vertexIndex = vertices.Length;
                vertexIndexMap.Add(pos, vertexIndex);
                vertices.Add(pos);
            }
            indices.Add(vertexIndex);
        }
        public void Dispose()
        {
            if (vertices.IsCreated)
                vertices.Dispose();
            if (indices.IsCreated)
                indices.Dispose();
            if (vertexIndexMap.IsCreated)
                vertexIndexMap.Dispose();
        }
    }
}
