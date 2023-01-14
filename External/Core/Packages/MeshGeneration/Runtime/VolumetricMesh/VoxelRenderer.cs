using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace NBG.MeshGeneration
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public static class VoxelRenderer
    {
        public static void UpdateMesh(Mesh mesh, TriangleMesh triangleMesh)
        {
            Profiler.BeginSample("UpdateMesh");
            mesh.triangles = null;
            mesh.vertices = null;

            mesh.SetVertices<float3>(triangleMesh.vertices, 0, triangleMesh.vertexCount);
            mesh.SetIndices<int>(triangleMesh.indices, 0, triangleMesh.triangleIndexCount, MeshTopology.Triangles, 0);

            Profiler.BeginSample("RecalculateNormals");
            mesh.RecalculateNormals();
            Profiler.EndSample();

            Profiler.EndSample();
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct VoxelRenderJob : IJob, IDisposable
    {
        private const float iso = 0.0f;

        private CubeSampler sampler;
        private VoxelChunk chunk;


        [ReadOnly]
        public NativeArray<int> edgeTable;
        [ReadOnly]
        public NativeArray<int> triTable;
        [ReadOnly]
        public NativeArray<int> indexOffsets;
        [ReadOnly]
        public NativeArray<float3> sampleOffsets;

        public NativeArray<float3> vertlist;

        private int cellsPerAxis;
        private int cellsPerAxisMinusOne;
        private int cellsPerAxisSquared;
        public VoxelRenderJob(VoxelChunk chunk)
        {
            this.chunk = chunk;

            sampler = new CubeSampler();
            edgeTable = MarchingCubes.nativeEdgeTable;
            triTable = MarchingCubes.nativeTriTable;

            cellsPerAxis = chunk.voxelsPerAxis;
            cellsPerAxisMinusOne = cellsPerAxis - 1;
            cellsPerAxisSquared = cellsPerAxis * cellsPerAxis;

            indexOffsets = new NativeArray<int>(8, Allocator.Persistent);
            indexOffsets.CopyFrom(
            new int[]{
                0,
                1,
                1 + cellsPerAxisSquared,
                cellsPerAxisSquared,
                cellsPerAxis,
                cellsPerAxis + 1,
                cellsPerAxis + 1 + cellsPerAxisSquared,
                cellsPerAxis + cellsPerAxisSquared
            }
            );

            sampleOffsets = new NativeArray<float3>(8, Allocator.Persistent);
            sampleOffsets.CopyFrom(new float3[]
            {
                new float3(0.0f, 0.0f, 0.0f),
                new float3(1.0f, 0.0f, 0.0f),
                new float3(1.0f, 0.0f, 1.0f),
                new float3(0.0f, 0.0f, 1.0f),
                new float3(0.0f, 1.0f, 0.0f),
                new float3(1.0f, 1.0f, 0.0f),
                new float3(1.0f, 1.0f, 1.0f),
                new float3(0.0f, 1.0f, 1.0f)
            });

            vertlist = new NativeArray<float3>(12, Allocator.Persistent);

        }
        public void Execute()
        {
            ref NativeArray<float> voxels = ref chunk.voxels;
            ref TriangleMesh triangleMesh = ref chunk.triangleMesh;

            triangleMesh.Clear();

            CubePoint point;
            float3 positionOffset;

            for (int i = 0; i < cellsPerAxisMinusOne; i++)
            {
                positionOffset.x = i;
                for (int j = 0; j < cellsPerAxisMinusOne; j++)
                {
                    positionOffset.y = j;
                    for (int k = 0; k < cellsPerAxisMinusOne; k++)
                    {
                        positionOffset.z = k;
                        int index = i + j * cellsPerAxis + k * cellsPerAxisSquared;

                        for (int x = 0; x < 8; x++)
                        {
                            var sampleOffset = sampleOffsets[x];

                            point.p.x = (sampleOffset.x + positionOffset.x);
                            point.p.y = (sampleOffset.y + positionOffset.y);
                            point.p.z = (sampleOffset.z + positionOffset.z);
                            point.value = voxels[index + indexOffsets[x]];
                            sampler.Set(x, point);
                        }

                        MarchingCubes.Polygonise(sampler, iso, ref triangleMesh, edgeTable, triTable, vertlist);
                    }
                }
            }
        }
        public void Dispose()
        {
            sampler.Dispose();
            if (vertlist.IsCreated)
                vertlist.Dispose();
            if (indexOffsets.IsCreated)
                indexOffsets.Dispose();
            if (sampleOffsets.IsCreated)
                sampleOffsets.Dispose();
        }
    }
}

