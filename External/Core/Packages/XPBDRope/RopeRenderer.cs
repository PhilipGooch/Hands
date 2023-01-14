using Recoil;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.XPBDRope
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [DisallowMultipleComponent]
    public class RopeRenderer : MonoBehaviour, NBG.Core.IManagedBehaviour
    {
        [SerializeField]
        int segmentsAround = 6;
        [SerializeField]
        [Range(1, 99)]
        int meshSegmentsForRopeSegment = 1;
        [SerializeField]
        float yUVScale = 1f;
        [SerializeField]
        Vector2 xUVLimits = new Vector2(0, 1);

        Rope rope;
        Mesh mesh;
        MeshFilter filter;

        RopeMeshJob job;
        private JobHandle handle;

        List<float> segmentLengths = new List<float>();
        List<int> meshTriangles = new List<int>();

        private bool rendererInitialized = false;
        private bool RendererActivated { get; set; } = false;

        void NBG.Core.IManagedBehaviour.OnLevelLoaded()
        {
            rope = GetComponent<Rope>();
            filter = GetComponent<MeshFilter>();
            rendererInitialized = true;
        }

        void NBG.Core.IManagedBehaviour.OnAfterLevelLoaded()
        {
            TryActivateRenderer();
        }

        void NBG.Core.IManagedBehaviour.OnLevelUnloaded()
        {
            TryDeactivateRenderer();
        }

        public void OnEnable()
        {
            TryActivateRenderer();
        }

        private void OnDisable()
        {
            TryDeactivateRenderer();
        }

        private void TryActivateRenderer()
        {
            if (RendererActivated || !isActiveAndEnabled || !rendererInitialized || !rope.RopeInitialized)
                return;

            mesh = new Mesh();
            mesh.MarkDynamic();

            RebuildJob(rope.ActiveStartSegment);

            rope.onStartSegmentChanged += RebuildJob;
            RendererActivated = true;
        }

        private void TryDeactivateRenderer()
        {
            if (!RendererActivated || !rendererInitialized)
                return;

            /*
             * We might be unloading with the job still running. We can't dispose until the job is complete
             * NOTE: Unity bug, handle misreports complete when it really isnt.
             * Bug discovered on 2021.1.23f1, Jobs package: 0.8.0-preview.23
             * Tested with Jobs package: 0.11.0-preview.6, bug still present.
             */
            //if (!handle.IsCompleted) 
            handle.Complete();


            Destroy(mesh);
            job.Dispose();

            if (rope.RopeInitialized)
            {
                rope.onStartSegmentChanged -= RebuildJob;
            }

            RendererActivated = false;
        }

        void RebuildJob(RopeSegment lastSegment)
        {
            job.Dispose();
            GetSegmentLengths(rope);
            job = RopeMeshJob.Createjob(meshSegmentsForRopeSegment, segmentsAround,
                rope.RendererRadius, rope.ActiveBoneCount, segmentLengths, yUVScale, xUVLimits);
            job.UpdateMesh(mesh, meshTriangles);
        }

        void GetSegmentLengths(Rope rope)
        {
            segmentLengths.Clear();
            for (int i = rope.FirstActiveBone; i < rope.BoneCount; i++)
            {
                segmentLengths.Add(rope.bones[i].BoneLength);
            }
        }

        private void Update()
        {
            if (!RendererActivated || rope.ActiveBoneCount == 0 || !rope.RopeInitialized)
                return;

            // Interpolation could be done inside the ropeSystem to reuse the calculated data.
            // For now it is much simpler to do it here.
            // But if there's more than one thing that needs the interpolated rope segment positions, move the calculations into the rope system.
            var dt = (float)(Time.timeAsDouble - Time.fixedTimeAsDouble) - World.main.dt;
            var inverseRot = Quaternion.Inverse(transform.rotation);
            var referenceRot = inverseRot * rope.bones[rope.FirstActiveBone].GetInterpolatedRigidTransform(dt).rot;
            bool hasTwistLimits = rope.UseTwistLimits;

            job.meshOffset = float3.zero;
            for (int i = 0; i < rope.ActiveBoneCount; i++)
            {
                var targetBone = rope.bones[rope.FirstActiveBone + i];
                var interpolatedRigid = targetBone.GetInterpolatedRigidTransform(dt);
                var localRot = inverseRot * interpolatedRigid.rot;
                if (hasTwistLimits)
                {
                    // If the rope has twist limits, we can use each segments local rotation for more stable and accurate uvs
                    referenceRot = localRot;
                }
                referenceRot = Quaternion.LookRotation(math.mul(localRot, Vector3.forward), math.mul(referenceRot, Vector3.up));
                job.pos[i] = transform.InverseTransformPoint(interpolatedRigid.pos);
                job.rot[i] = referenceRot;
                //job.rot[i] = chain.bones[i].rotation;
            }

            job.yUVScale = yUVScale;
            job.uvLimits = xUVLimits;

            handle = job.Schedule();
        }

        private void LateUpdate()
        {
            if (!RendererActivated)
                return;

            handle.Complete();
            mesh.SetVertices(job.verts);
            mesh.SetNormals(job.normals);
            mesh.SetUVs(0, job.uvs);
            mesh.RecalculateBounds();
            filter.sharedMesh = mesh;
        }

        void OnValidate()
        {
            Debug.Assert(GetComponents<RopeRenderer>().Length == 1, "Multiple RopeRenderer components on a single object detected!", gameObject);
        }

        [BurstCompile]
        public struct RopeMeshJob : IJob
        {
            int boneCount;
            int ringsPerBone;
            int totalRings;
            int meshSides;
            NativeArray<float2> rotatedRadius;

            [ReadOnly]
            public NativeArray<float3> pos;
            [ReadOnly]
            public NativeArray<quaternion> rot;
            [ReadOnly]
            public NativeArray<float> lengths;

            public NativeArray<float3> verts;

            public NativeArray<float3> normals;

            public NativeArray<float2> uvs;

            public float3 meshOffset;

            public float yUVScale;
            public float2 uvLimits;

            float totalLength;

            public static RopeMeshJob Createjob(int ringsPerBone, int meshSides, float radius, int boneCount, List<float> lengths, float yUVScale, Vector2 uvLimits)
            {
                var rotatedRadius = new NativeArray<float2>(meshSides, Allocator.Persistent);
                for (int i = 0; i < meshSides; i++)
                {
                    var rotateAngle = math.PI * 2 * i / meshSides;
                    var cs = Mathf.Cos(-rotateAngle);
                    var sn = Mathf.Sin(-rotateAngle);
                    rotatedRadius[i] = new Vector2(radius * cs, radius * sn);
                }

                NativeArray<float> segmentLengths = new NativeArray<float>(boneCount, Allocator.Persistent);
                float totalLength = 0f;
                for (int i = 0; i < boneCount; i++)
                {
                    segmentLengths[i] = lengths[i];
                    if (i == 0 || i == boneCount - 1)
                    {
                        segmentLengths[i] += radius;
                    }

                    totalLength += segmentLengths[i];
                }

                // Extra vertex for UV wrapping
                int vertsForSides = meshSides + 1;
                int totalRings = boneCount * ringsPerBone + 1;
                int vertCount = totalRings * vertsForSides + 2 * vertsForSides;

                return new RopeMeshJob()
                {
                    rotatedRadius = rotatedRadius,
                    ringsPerBone = ringsPerBone,
                    totalRings = totalRings,
                    meshSides = meshSides,
                    boneCount = boneCount,
                    pos = new NativeArray<float3>(boneCount, Allocator.Persistent),
                    rot = new NativeArray<quaternion>(boneCount, Allocator.Persistent),
                    verts = new NativeArray<float3>(vertCount, Allocator.Persistent),
                    normals = new NativeArray<float3>(vertCount, Allocator.Persistent),
                    uvs = new NativeArray<float2>(vertCount, Allocator.Persistent),
                    lengths = segmentLengths,
                    totalLength = totalLength,
                    yUVScale = yUVScale,
                    uvLimits = uvLimits
                };
            }
            public void Dispose()
            {
                if (pos.IsCreated)
                {
                    pos.Dispose();
                    rot.Dispose();
                    verts.Dispose();
                    normals.Dispose();
                    uvs.Dispose();
                    rotatedRadius.Dispose();
                    lengths.Dispose();
                }
            }
            public void UpdateMesh(Mesh mesh, List<int> meshTris)
            {
                meshTris.Clear();

                int sideVerts = meshSides + 1;

                int triangleCount = (totalRings - 1) * sideVerts * 6 + (meshSides - 2) * 3 * 2;
                if (meshTris.Capacity < triangleCount)
                {
                    meshTris.Capacity = triangleCount;
                }

                for (var i = 0; i < totalRings - 1; i++)
                {
                    for (var j = 0; j < sideVerts; j++)
                    {
                        var baseI = i * sideVerts;
                        var nextI = baseI + sideVerts;
                        var nextJ = (j + 1) % sideVerts;
                        var i00 = baseI + j;
                        var i01 = baseI + nextJ;
                        var i10 = nextI + j;
                        var i11 = nextI + nextJ;
                        meshTris.Add(i10);
                        meshTris.Add(i01);
                        meshTris.Add(i00);
                        meshTris.Add(i11);
                        meshTris.Add(i01);
                        meshTris.Add(i10);
                    }
                }
                // caps
                {
                    var baseI = totalRings * sideVerts;
                    for (var j = 0; j < meshSides - 2; j++)
                    {
                        meshTris.Add(baseI);
                        meshTris.Add(baseI + j + 1);
                        meshTris.Add(baseI + j + 2);
                    }
                }
                {
                    var baseI = (totalRings + 1) * sideVerts;
                    for (var j = 0; j < meshSides - 2; j++)
                    {
                        meshTris.Add(baseI);
                        meshTris.Add(baseI + j + 2);
                        meshTris.Add(baseI + j + 1);
                    }
                }

                mesh.Clear();
                mesh.SetVertices(verts);
                mesh.SetTriangles(meshTris, 0);
            }



            public void Execute()
            {
                int idx = 0;
                // create simple rope mesh
                float lengthRendered = 0f;

                float3 lastPos = pos[0];

                for (var i = 0; i < totalRings; i++)
                {
                    // Last ring is an extra for the last segment
                    if (i > 0)
                    {
                        var bodyId = (i - 1) / ringsPerBone;
                        lengthRendered += lengths[bodyId] / ringsPerBone;
                    }
                    lastPos = UpdateRing((float)i / (totalRings-1), lastPos, lengthRendered, ref idx);
                }

                // caps
                UpdateRing(0, pos[0], 0f, ref idx);
                UpdateRing(1, pos[pos.Length - 1], totalLength, ref idx);
            }
            private float3 UpdateRing(float t, float3 lastPos, float lengthRendered, ref int idx)
            {
                GetPoint(t, out var center, out var normal, out var binormal);
                var distanceRendered = math.length(center - lastPos);
                //lengthRendered += distanceRendered;
                for (var j = 0; j < meshSides; j++)
                {
                    AddVertex(j, center, normal, binormal, lengthRendered, ref idx);
                }

                // Add extra vertex on start/end for wrapping UVs around
                AddVertex(meshSides, center, normal, binormal, lengthRendered, ref idx);
                return center;
            }

            void AddVertex(int side, float3 center, float3 normal, float3 binormal, float lengthRendered, ref int idx)
            {
                var rotated = rotatedRadius[side % meshSides];
                var offset = rotated.x * normal + rotated.y * binormal;
                var p = center + offset;
                verts[idx] = p;
                normals[idx] = math.normalize(offset);
                var xUV = math.lerp(uvLimits.x, uvLimits.y, (float)side / meshSides);
                var yUV = (totalLength - lengthRendered) * yUVScale;
                uvs[idx] = new float2(xUV, yUV);
                idx++;
            }

            public void GetPoint(float dist, out float3 p, out float3 normal, out float3 binormal)
            {

                // This happens everyframe 
                int rigidIdx = Mathf.FloorToInt(dist * boneCount - .5f);
                float t = dist * boneCount - .5f - rigidIdx;

                var idx0 = Mathf.Clamp(rigidIdx, 0, boneCount - 1);
                var idx1 = Mathf.Clamp(rigidIdx + 1, 0, boneCount - 1);
                var boneLen0 = lengths[idx0];
                var boneLen1 = lengths[idx1];

                // special cases for ends
                if (rigidIdx == -1)
                {

                    var m0 = math.float3x3(rot[idx0]);
                    normal = m0.c0;
                    binormal = m0.c1;
                    p = pos[idx0] + m0.c2 * boneLen0 * (t - 1);
                }
                else if (rigidIdx == boneCount - 1)
                {
                    var m0 = math.float3x3(rot[idx0]);
                    normal = m0.c0;
                    binormal = m0.c1;
                    p = pos[idx0] + m0.c2 * boneLen0 * t;
                }
                else
                {

                    // hermite
                    var t2 = t * t;
                    var t3 = t2 * t;
                    var h00 = 2 * t3 - 3 * t2 + 1;
                    var h10 = t3 - 2 * t2 + t;
                    var h01 = -2 * t3 + 3 * t2;
                    var h11 = t3 - t2;

                    var m0 = math.float3x3(rot[idx0]);
                    var m1 = math.float3x3(rot[idx1]);
                    var m0p = m0.c2 * boneLen0;// *2/3;
                    var m1p = m1.c2 * boneLen1;// *2/3;
                    p = h00 * pos[idx0] + h10 * m0p + h01 * pos[idx1] + h11 * m1p;

                    //h00 = 1 - t;
                    //h01 = t;
                    normal = h00 * m0.c0 + h01 * m1.c0;
                    binormal = h00 * m0.c1 + h01 * m1.c1;

                    normal = math.normalize(normal);
                    binormal = math.normalize(binormal);
                }
                p -= meshOffset;
            }
        }
    }
}