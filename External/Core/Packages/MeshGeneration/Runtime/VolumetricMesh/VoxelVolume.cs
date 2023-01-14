using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[assembly: InternalsVisibleToAttribute("NBG.Voxel.Tests")]
namespace NBG.MeshGeneration
{
    public class VoxelVolume : MonoBehaviour
    {
        public bool initializeOnAwake = false;
        public Material material;
        public bool useChildBoxCollidersToDefineShape = false;

        private const int voxelsPerAxis = 8;

        private class VoxelChunkData
        {
            public VoxelChunk chunk;
            public Mesh mesh;
            public MeshCollider collider;
            public VoxelRenderJob renderJob;
            public JobHandle renderJobHandle;
            public bool pendingJob;
        }

        private List<VoxelChunkData> chunksData;
        private List<VoxelChunkData> pendingJobs;
        private List<VoxelChunkData> runningJobs;
        private List<VoxelChunkData> updateColliders;

        public int iterations = 5;
        public float rate = 0.3f;

        private void Awake()
        {
            if (initializeOnAwake)
                Initialize();
        }

        private void Initialize()
        {
            chunksData = new List<VoxelChunkData>();
            pendingJobs = new List<VoxelChunkData>();
            runningJobs = new List<VoxelChunkData>();
            updateColliders = new List<VoxelChunkData>();

            if (useChildBoxCollidersToDefineShape)
            {
                foreach (var box in gameObject.GetComponentsInChildren<BoxCollider>())
                {
                    float3 pos = box.transform.TransformPoint(box.center - box.size * 0.5f);
                    float3 size = box.transform.TransformVector(box.size);
                    AddPrism(pos, size, 1.0f);
                    Destroy(box.gameObject);
                }
            }
        }
        private void Update()
        {
            ScheduleSimulation();

            CheckJobs();
        }

        private void LateUpdate()
        {
            SchedulePendingJobs();
        }

        private int CreateChunk(int3 pos)
        {
            VoxelChunkData newChunkData = new VoxelChunkData();

            pos = (pos / voxelsPerAxis) * voxelsPerAxis;
            VoxelChunk newChunk = new VoxelChunk();
            newChunk.Create(pos, voxelsPerAxis + 1);
            newChunkData.chunk = newChunk;

            Mesh chunkMesh = new Mesh();
            newChunkData.mesh = chunkMesh;

            GameObject newChunkGO = new GameObject("Chunk " + pos);
            newChunkGO.transform.parent = transform;
            newChunkGO.transform.localScale = Vector3.one;
            newChunkGO.transform.localRotation = quaternion.identity;
            newChunkGO.transform.localPosition = new Vector3(pos.x, pos.y, pos.z);
            newChunkGO.AddComponent<MeshFilter>().sharedMesh = chunkMesh;
            newChunkGO.AddComponent<MeshRenderer>().sharedMaterial = material;

            MeshCollider mc = newChunkGO.AddComponent<MeshCollider>();
            mc.sharedMesh = chunkMesh;
            newChunkData.collider = mc;


            newChunkData.renderJob = new VoxelRenderJob(newChunkData.chunk);

            chunksData.Add(newChunkData);
            return chunksData.Count - 1;
        }

        public void AddSphere(float3 worldCenter, float radius, float rate = 0.05f)
        {
            if (CanSchedulePendingJobs())
            {
                int3 center = Transform(worldCenter);
                int localRadius = Transform(radius);

                localRadius += 1;
                CreateChunksInCubicalSpace(center - localRadius, localRadius * 2);
                localRadius -= 1;

                int chunkCount = chunksData.Count;

                for (int i = 0; i < chunkCount; i++)
                {
                    if (chunksData[i].chunk.IntersectsWithSphere(center, localRadius))
                    {
                        chunksData[i].chunk.AddSphere(center, localRadius, rate);
                        AddPendingJob(i);
                    }
                }
            }
        }

        public void SubtractSphere(float3 worldCenter, float radius, float rate = 0.05f)
        {
            if (CanSchedulePendingJobs())
            {
                int3 center = Transform(worldCenter);
                int localRadius = Transform(radius);

                int chunkCount = chunksData.Count;

                for (int i = 0; i < chunkCount; i++)
                {
                    if (chunksData[i].chunk.IntersectsWithSphere(center, localRadius))
                    {
                        chunksData[i].chunk.SubtractSphere(center, localRadius, rate);
                        AddPendingJob(i);
                    }
                }
            }
        }
        public void AddPrism(float3 worldPos, float3 axisAlignedSide, float rate = 0.05f)
        {
            int3 pos = Transform(worldPos);
            int3 localSide = math.abs(TransformVector(axisAlignedSide));
            AddPrism(pos, localSide, rate);
        }
        public void AddPrism(int3 pos, int3 localSide, float rate = 0.05f, bool modifyOctree = true)
        {
            localSide += 2;
            pos -= 1;
            CreateChunksInCubicalSpace(pos, localSide);
            localSide -= 2;
            pos += 1;

            int chunkCount = chunksData.Count;

            for (int i = 0; i < chunkCount; i++)
            {
                if (chunksData[i].chunk.IntersectsWithPrism(pos, localSide))
                {
                    chunksData[i].chunk.AddPrism(pos, localSide, rate, modifyOctree);
                    AddPendingJob(i);
                }
            }
        }

        public void SubtractPrism(float3 worldPos, float3 axisAlignedSide, float rate = 0.05f, bool modifyOctree = true)
        {
            int3 pos = Transform(worldPos);
            int3 localSide = Transform(axisAlignedSide);

            SubtractPrism(pos, localSide, rate, modifyOctree);
        }

        public void SubtractPrism(int3 pos, int3 localSide, float rate = 0.05f, bool modifyOctree = true)
        {
            int chunkCount = chunksData.Count;

            for (int i = 0; i < chunkCount; i++)
            {
                if (chunksData[i].chunk.IntersectsWithPrism(pos, localSide))
                {
                    chunksData[i].chunk.SubtractPrism(pos, localSide, rate, modifyOctree);
                    AddPendingJob(i);
                }
            }
        }
        private int3 Transform(float3 pos)
        {
            float3 localPos = (transform.InverseTransformPoint(pos));
            return (int3)localPos;
        }
        private int3 TransformVector(float3 pos)
        {
            float3 localPos = (transform.InverseTransformVector(pos));
            return (int3)localPos;
        }

        private float3 TransformDirection(float3 dir)
        {
            return transform.InverseTransformDirection(dir);
        }
        private int Transform(float value)
        {
            return (int)(value);
        }

        private void AddPendingJob(int index)
        {
            pendingJobs.Add(chunksData[index]);
        }

        private bool CanSchedulePendingJobs()
        {
            return runningJobs.Count == 0;
        }
        private void SchedulePendingJobs()
        {
            if (CanSchedulePendingJobs())
            {
                if (pendingJobs.Count > 0)
                {
                    for (int i = 0; i < pendingJobs.Count; i++)
                    {
                        runningJobs.Add(pendingJobs[i]);
                        CreateJobsForChunk(pendingJobs[i]);
                    }
                    pendingJobs.Clear();
                }
            }
        }

        private void ScheduleSimulation()
        {
            if (CanSchedulePendingJobs())
            {
                for (int i = 0; i < chunksData.Count; i++)
                {

                    CreateJobsForChunk(chunksData[i]);
                    runningJobs.Add(chunksData[i]);
                }
            }
        }
        private void CreateJobsForChunk(VoxelChunkData data)
        {
            if (!data.pendingJob)
            {
                data.renderJobHandle = data.renderJob.Schedule();
                data.pendingJob = true;
            }
        }

        List<int> pendingMeshIds = new List<int>();
        private void CheckJobs()
        {
            for (int i = 0; i < runningJobs.Count; i++)
            {
                var runningChunk = runningJobs[i];

                if (runningChunk.renderJobHandle.IsCompleted)
                {
                    runningChunk.renderJobHandle.Complete();
                    runningChunk.pendingJob = false;

                    VoxelRenderer.UpdateMesh(
                        runningChunk.mesh,
                        runningChunk.chunk.triangleMesh);

                    runningChunk.collider.sharedMesh = runningChunk.mesh;

                    pendingMeshIds.Add(runningChunk.mesh.GetInstanceID());
                    updateColliders.Add(runningChunk);

                    runningJobs.RemoveAt(i);
                    i--;
                }
            }
        }
        private void CreateChunksInCubicalSpace(int3 origin, int3 side)
        {
            int3 dest = origin + side;

            int3 originChunk = origin / voxelsPerAxis;
            int3 destChunk = dest / voxelsPerAxis;

            originChunk.x += origin.x < 0 ? -1 : 0;
            originChunk.y += origin.y < 0 ? -1 : 0;
            originChunk.z += origin.z < 0 ? -1 : 0;

            destChunk.x += dest.x < 0 ? -1 : 0;
            destChunk.y += dest.y < 0 ? -1 : 0;
            destChunk.z += dest.z < 0 ? -1 : 0;

            int3 chunkPos;
            for (int i = originChunk.x; i <= destChunk.x; i++)
            {
                chunkPos.x = i * voxelsPerAxis;
                for (int j = originChunk.y; j <= destChunk.y; j++)
                {
                    chunkPos.y = j * voxelsPerAxis;
                    for (int k = originChunk.z; k <= destChunk.z; k++)
                    {
                        chunkPos.z = k * voxelsPerAxis;
                        if (!ChunkExists(chunkPos))
                        {
                            CreateChunk(chunkPos);
                        }
                    }
                }
            }
        }
        private bool ChunkExists(int3 pos)
        {
            int chunkCount = chunksData.Count;
            for (int c = 0; c < chunkCount; c++)
            {
                if (chunksData[c].chunk.pos.Equals(pos))
                    return true;
            }

            return false;
        }

        public float FillPercentage(Bounds bounds, BoxCollider box, float extraSpacing)
        {
            int3 boundsPos = Transform(bounds.center - bounds.extents);
            int3 boundsSide = math.abs(TransformVector(bounds.size));

            int chunkCount = chunksData.Count;

            float fillVoxels = 0.0f;
            float emptyVoxels = 0.0f;

            for (int i = 0; i < chunkCount; i++)
            {
                if (chunksData[i].chunk.IntersectsWithPrism(boundsPos, boundsSide))
                {
                    chunksData[i].chunk.CountVoxelsInBox(
                        boundsPos,
                        boundsSide,
                        ref emptyVoxels,
                        ref fillVoxels,
                        box,
                        transform,
                        extraSpacing
                        );
                }
            }
            return fillVoxels / (fillVoxels + emptyVoxels);
        }

        internal int CountVoxels()
        {
            int count = 0;
            int chunkCount = chunksData.Count;
            for (int i = 0; i < chunkCount; i++)
            {
                var voxels = chunksData[i].chunk.voxels;
                for (int j = 0; j < voxels.Length; j++)
                {
                    if (voxels[j] < 0.0f)
                        count++;
                }
            }
            return count;
        }

        private void OnDestroy()
        {
            foreach (var chunk in chunksData)
            {
                chunk.renderJobHandle.Complete();

                chunk.renderJob.Dispose();
                chunk.chunk.Dispose();
            }

            MarchingCubes.Dispose();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.3f, 0.8f, 1.0f);
            foreach (var collider in GetComponentsInChildren<BoxCollider>())
                Gizmos.DrawCube(collider.transform.TransformPoint(collider.center), collider.transform.TransformVector(collider.size));
        }
    }
}
