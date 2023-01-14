using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.MeshGeneration
{
    public struct VoxelChunk : IDisposable
    {

        public int3 pos;
        [ReadOnly]
        public NativeArray<float> voxels;

        public TriangleMesh triangleMesh;

        public int voxelsPerAxis;

        private const float subOctreeThreshold = 0.0f;
        public void Create(int3 pos, int voxelsPerAxis, int vertexBufferSize = 50000)
        {
            this.pos = pos;
            this.voxelsPerAxis = voxelsPerAxis;

            voxels = new NativeArray<float>(voxelsPerAxis * voxelsPerAxis * voxelsPerAxis, Allocator.Persistent);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;

            for (int i = 0; i < voxelsPerAxis; i++)
                for (int j = 0; j < voxelsPerAxis; j++)
                    for (int k = 0; k < voxelsPerAxis; k++)
                        voxels[i + j * voxelsPerAxis + k * cellsPerAxisSquared] = 1.0f;

            triangleMesh = new TriangleMesh(4096);
        }
        public void AddSphere(int3 worldPos, int radius, /*ref SparseOctree sparseOctree,*/ float rate = 0.05f)
        {
            int3 localPos = worldPos - pos;
            float floatRadius = radius;

            int minX = math.max(localPos.x - radius, 0);
            int maxX = math.min(localPos.x + radius, voxelsPerAxis - 1);
            int minY = math.max(localPos.y - radius, 0);
            int maxY = math.min(localPos.y + radius, voxelsPerAxis - 1);
            int minZ = math.max(localPos.z - radius, 0);
            int maxZ = math.min(localPos.z + radius, voxelsPerAxis - 1);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;
            float3 center = new float3(localPos);
            float3 currentPos;


            for (int j = minY; j <= maxY; j++)
            {
                currentPos.y = j;
                for (int k = minZ; k <= maxZ; k++)
                {
                    currentPos.z = k;
                    for (int i = minX; i <= maxX; i++)
                    {
                        currentPos.x = i;
                        int index = i + j * voxelsPerAxis + k * cellsPerAxisSquared;

                        float currentValue = voxels[index];
                        float targetValue = math.distance(currentPos, center) - floatRadius;
                        float finalValue = math.lerp(currentValue, targetValue, rate);

                        if (finalValue < currentValue)
                        {
                            voxels[index] = finalValue;
                            //if (currentValue >= 0.0f && finalValue < 0.5f)
                            //    sparseOctree.Add(pos + currentPos);
                        }
                    }
                }
            }
        }

        public void SubtractSphere(int3 worldPos, int radius, /*ref SparseOctree sparseOctree,*/ float rate = 0.05f, bool modifyOctree = true)
        {
            int3 localPos = worldPos - pos;
            float floatRadius = radius;

            int minX = math.max(localPos.x - radius, 0);
            int maxX = math.min(localPos.x + radius, voxelsPerAxis - 1);
            int minY = math.max(localPos.y - radius, 0);
            int maxY = math.min(localPos.y + radius, voxelsPerAxis - 1);
            int minZ = math.max(localPos.z - radius, 0);
            int maxZ = math.min(localPos.z + radius, voxelsPerAxis - 1);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;
            float3 center = new float3(localPos);
            float3 currentPos;


            for (int j = minY; j <= maxY; j++)
            {
                currentPos.y = j;
                for (int k = minZ; k <= maxZ; k++)
                {
                    currentPos.z = k;
                    for (int i = minX; i <= maxX; i++)
                    {
                        currentPos.x = i;
                        int index = i + j * voxelsPerAxis + k * cellsPerAxisSquared;

                        float oldValue = voxels[index];
                        float targetValue = math.distance(currentPos, center) / floatRadius;
                        float finalValue = oldValue + (1.0f - targetValue) * rate;
                        if (finalValue > oldValue)
                        {
                            voxels[index] = finalValue;
                            //if (finalValue > subOctreeThreshold && modifyOctree)
                            //    sparseOctree.Sub(pos + currentPos);
                        }
                    }
                }
            }
        }
        public void AddPrism(int3 worldPos, int3 side, /*ref SparseOctree sparseOctree,*/ float rate = 0.05f, bool modifyOctree = true)
        {
            int3 localPos = worldPos - pos;

            localPos += 1;
            side -= 2;

            int minX = math.max(localPos.x, 0);
            int maxX = math.min(localPos.x + side.x, voxelsPerAxis - 1);
            int minY = math.max(localPos.y, 0);
            int maxY = math.min(localPos.y + side.y, voxelsPerAxis - 1);
            int minZ = math.max(localPos.z, 0);
            int maxZ = math.min(localPos.z + side.z, voxelsPerAxis - 1);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;

            float3 currentPos;


            for (int j = minY; j <= maxY; j++)
            {
                currentPos.y = j;
                for (int k = minZ; k <= maxZ; k++)
                {
                    currentPos.z = k;
                    for (int i = minX; i <= maxX; i++)
                    {
                        currentPos.x = i;
                        int index = i + j * voxelsPerAxis + k * cellsPerAxisSquared;

                        float currentValue = voxels[index];
                        const float targetValue = -1.0f;
                        float finalValue = math.lerp(currentValue, targetValue, rate);
                        voxels[index] = finalValue;

                        //if (modifyOctree)
                        //    sparseOctree.Add(pos + currentPos);
                    }
                }
            }
        }

        public void CountVoxelsInBox(int3 worldPos, int3 side, ref float emptyVoxels, ref float filledVoxels, BoxCollider box, Transform volumeTransform, float extraSpacing)
        {
            int3 localPos = worldPos - pos;

            localPos += 1;
            side -= 2;

            int minX = math.max(localPos.x, 0);
            int maxX = math.min(localPos.x + side.x, voxelsPerAxis - 1);
            int minY = math.max(localPos.y, 0);
            int maxY = math.min(localPos.y + side.y, voxelsPerAxis - 1);
            int minZ = math.max(localPos.z, 0);
            int maxZ = math.min(localPos.z + side.z, voxelsPerAxis - 1);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;

            float3 currentPos;

            for (int j = minY; j <= maxY; j++)
            {
                currentPos.y = j;
                for (int k = minZ; k <= maxZ; k++)
                {
                    currentPos.z = k;
                    for (int i = minX; i <= maxX; i++)
                    {
                        currentPos.x = i;
                        
                        if (IsPointInsideBox(volumeTransform.TransformPoint(currentPos+pos), box, extraSpacing))
                        {
                            int index = i + j * voxelsPerAxis + k * cellsPerAxisSquared;

                            float currentValue = voxels[index];
                            if (currentValue < 0)
                                filledVoxels++;
                            else
                                emptyVoxels++;
                        }
                    }
                }
            }
        }
        private bool IsPointInsideBox(float3 point, BoxCollider box, float extraSpacing)
        {
            point = box.transform.InverseTransformPoint(point) - box.center;
            float3 half = (float3)box.size * 0.5f + extraSpacing;
            return math.all(math.abs(point) < half);
        }
        public void SubtractPrism(int3 worldPos, int3 side, /*ref SparseOctree sparseOctree,*/ float rate = 0.05f, bool modifyOctree = true)
        {
            int3 localPos = worldPos - pos;

            int minX = math.max(localPos.x, 0);
            int maxX = math.min(localPos.x + side.x, voxelsPerAxis - 1);
            int minY = math.max(localPos.y, 0);
            int maxY = math.min(localPos.y + side.y, voxelsPerAxis - 1);
            int minZ = math.max(localPos.z, 0);
            int maxZ = math.min(localPos.z + side.z, voxelsPerAxis - 1);

            int cellsPerAxisSquared = voxelsPerAxis * voxelsPerAxis;

            float3 currentPos;


            for (int j = minY; j <= maxY; j++)
            {
                currentPos.y = j;
                for (int k = minZ; k <= maxZ; k++)
                {
                    currentPos.z = k;
                    for (int i = minX; i <= maxX; i++)
                    {
                        currentPos.x = i;
                        int index = i + j * voxelsPerAxis + k * cellsPerAxisSquared;

                        float currentValue = voxels[index];
                        const float targetValue = 1.0f;
                        float finalValue = math.lerp(currentValue, targetValue, rate);
                        voxels[index] = finalValue;
                        //if (finalValue > subOctreeThreshold && modifyOctree)
                        //    sparseOctree.Sub(pos + currentPos);
                    }
                }
            }
        }

        public bool IntersectsWithSphere(int3 worldPos, int radius)
        {
            int3 sphereCubeOrigin = worldPos - radius;
            int sphereCubeSide = radius * 2;

            return IntersectsWithPrism(sphereCubeOrigin, sphereCubeSide);
        }

        public bool IntersectsWithPrism(int3 worldPos, int3 side)
        {
            worldPos -= 1;
            side += 2;
            int3 offset = worldPos - pos;

            return
                (offset.x > 0 ? offset.x < voxelsPerAxis : offset.x > -side.x) &&
                (offset.y > 0 ? offset.y < voxelsPerAxis : offset.y > -side.y) &&
                (offset.z > 0 ? offset.z < voxelsPerAxis : offset.z > -side.z);
        }
        public bool IsInside(int3 worldPos)
        {
            return math.all(worldPos > pos & worldPos < (pos + voxelsPerAxis));
        }
        public void Dispose()
        {
            voxels.Dispose();
            triangleMesh.Dispose();
        }
    }
}
