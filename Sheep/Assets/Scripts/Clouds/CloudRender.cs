using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using NBG.Core;
using Unity.Collections;
using Unity.Jobs;

using static CloudBox;
using static CloudSystem;
using Unity.Mathematics;
using Unity.Burst;

public class CloudRender : MonoBehaviour
{
    public static List<CloudRender> all = new List<CloudRender>();
    public int maxMeshParticles = 256;

    int[] visibleClouds;
    public int visibleCloudsCount;
    public int visibleParticlesCount;

    private NativeArray<ParticleSort> particleSort;
    Plane[] mPlanes = new Plane[6];

    public NativeArray<float3> vertices;
    public NativeArray<Color32> colors;
    public NativeArray<float3> normals;

    NativeList<CloudBoxData> cloudBoxData;

    bool initialized = false;

    new Camera camera;

    CreateCloudMeshJobs cloudMeshJobs;
    bool jobCompleted = false;

    CloudSystem system;

    void OnEnable()
    {
        all.Add(this);

        camera = GetComponent<Camera>();

        RenderPipelineManager.beginCameraRendering += BeforeRender;
    }

    void OnDisable()
    {
        // KillThreads();

        DisposeJobData();

        all.Remove(this);
        RenderPipelineManager.beginCameraRendering -= BeforeRender;

    }

    void BeforeRender(ScriptableRenderContext context, Camera camera)
    {
        if (!initialized || camera != this.camera) return;

        if (jobCompleted)
        {
            system.UpdateCloudMesh(vertices, normals, colors);
        }
    }

    public void Initialize(CloudSystem system)
    {
        DisposeJobData();

        this.system = system;

        visibleClouds = new int[system.maxClouds];

        maxMeshParticles = system.maxParticles / 4;

        vertices = new NativeArray<float3>(maxMeshParticles * 4, Allocator.Persistent);
        colors = new NativeArray<Color32>(maxMeshParticles * 4, Allocator.Persistent);
        normals = new NativeArray<float3>(maxMeshParticles * 4, Allocator.Persistent);
        particleSort = new NativeArray<ParticleSort>(system.maxParticles, Allocator.Persistent);
        cloudBoxData = new NativeList<CloudBoxData>(Allocator.Persistent);

        cloudMeshJobs = new CreateCloudMeshJobs()
        {
            nearClipStart = system.nearClipStart,
            nearClipEnd = system.nearClipEnd,
            farClipStart = system.farClipStart,
            farClipEnd = system.farClipEnd,

            maxMeshParticles = maxMeshParticles,

            particlesData = system.particlesData,

            start = 0,
            end = maxMeshParticles,
            verts = vertices,
            cols = colors,
            normals = normals,
            psort = particleSort,
            vpCount = visibleParticlesCount,
            camX = camera.transform.position,
            camY = camera.transform.up
        };

        initialized = true;
    }

    public void Deinitialize()
    {
        DisposeJobData();

        initialized = false;
        system = null;
    }

    void DisposeJobData()
    {
        if (initialized)
        {
            vertices.Dispose();
            normals.Dispose();
            colors.Dispose();
            particleSort.Dispose();
            cloudBoxData.Dispose();
        }
    }

    public void StartUpdate()
    {
        if (camera == null) return;

        CullClouds();
        SortParticles();

        jobCompleted = false;

        cloudBoxData.Clear();
        for (int i = 0; i < CloudBox.all.Count; i++)
            cloudBoxData.Add(CloudBox.all[i].cloudBoxData);

        cloudMeshJobs.cloudBoxData = cloudBoxData;
        cloudMeshJobs.vpCount = visibleParticlesCount;
        cloudMeshJobs.camX = camera.transform.position;
        cloudMeshJobs.camY = camera.transform.up;

        var jobHandle = cloudMeshJobs.Schedule();

        jobHandle.Complete();

        jobCompleted = true;

    }


    #region Sort and Cull

    void CullClouds()
    {
        var cameraPos = camera.transform.position;

        var cloudSetOffset = MathUtils.WrapSigned(system.cloudSetOffset - cameraPos, system.cloudSetSize) + cameraPos;

        GeometryUtility.CalculateFrustumPlanes(camera, mPlanes);

        visibleCloudsCount = 0;
        //if (Game.currentLevel != null && Game.currentLevel.noClouds)
        //    return;
        // get camera planes, etc
        for (int i = 0; i < system.maxClouds; i++)
        {
            // get wrapped around world pos
            var cloud = system.cloudsData[i];
            var worldPos = cloud.pos + cloudSetOffset;
            worldPos = MathUtils.WrapSigned(worldPos - cameraPos, system.cloudSetSize) + cameraPos;

            var dist = (worldPos - cameraPos).magnitude;
            if (dist - Mathf.Max(Mathf.Max(cloud.size.x, cloud.size.y), cloud.size.z) > system.farClipEnd) continue; // cloud too far

            if (GeometryUtility.TestPlanesAABB(mPlanes, new Bounds(worldPos, system.cloudsData[i].size)))
            {

                system.cloudsData[i].worldPos = worldPos;
                visibleClouds[visibleCloudsCount++] = i;
            }
        }
    }

    void SortParticles()
    {
        var camPos = camera.transform.position;

        var particlesData = system.particlesData;

        visibleParticlesCount = 0;
        for (int i = 0; i < visibleCloudsCount; i++)
        {
            var cloud = system.cloudsData[visibleClouds[i]];
            var start = cloud.startParticle;
            var end = cloud.endParticle;
            for (int p = start; p < end; p++)
            {
                // calculate
                var pos = particlesData[p].pos + cloud.worldPos;
                var distance = (pos - camPos).magnitude;
                // cull due to distance
                if (distance <= system.nearClipStart) continue;
                if (distance >= system.farClipEnd) continue;

                //particlesData[p].worldPos = pos;
                //particlesData[p].distance = distance;
                var partSort = particleSort[visibleParticlesCount];

                partSort.index = p;
                partSort.worldPos = pos;
                partSort.distance = -distance;

                particleSort[visibleParticlesCount] = partSort;

                visibleParticlesCount++;

                // animate
                // particlesData[p].angle = AnimateAngle(particlesData[p].angle);

            }
        }
        if (visibleParticlesCount > system.maxVisibleParticlesCount)
            system.maxVisibleParticlesCount = visibleParticlesCount;
        ShellSort(particleSort, visibleParticlesCount);
    }

    private void ShellSort(NativeArray<ParticleSort> inputArray, int length)
    {
        int j;
        ParticleSort temp;// = 0;
        int increment = (length) / 2;
        while (increment > 0)
        {
            for (int index = 0; index < length; index++)
            {
                j = index;
                temp = inputArray[index];

                while ((j >= increment) && inputArray[j - increment].distance > temp.distance)
                {
                    inputArray[j] = inputArray[j - increment];
                    j = j - increment;
                }
                inputArray[j] = temp;
            }
            if (increment / 2 != 0)
                increment = increment / 2;
            else if (increment == 1)
                increment = 0;
            else
                increment = 1;
        }
    }

    [BurstCompatible]
    struct ParticleSort
    {
        public int index;
        public float3 worldPos;
        public float distance;

        public override string ToString()
        {
            return string.Format("{0} {1}", index, distance);
        }
    }

    #endregion

    [BurstCompile]
    struct CreateCloudMeshJobs : IJob
    {
        //from cloud system
        public float nearClipStart;
        public float nearClipEnd;
        public float farClipStart;
        public float farClipEnd;

        public int start;
        public int end;
        public int vpCount;
        public int maxMeshParticles;

        public float3 camX;
        public float3 camY;

        [Unity.Collections.ReadOnly]
        public NativeArray<CloudParticleData> particlesData;

        public NativeArray<float3> verts;
        public NativeArray<Color32> cols;
        public NativeArray<float3> normals;
        [Unity.Collections.ReadOnly]
        public NativeArray<ParticleSort> psort;
        [Unity.Collections.ReadOnly]
        public NativeArray<CloudBoxData> cloudBoxData;

        void IJob.Execute()
        {
            var hiddenParticles = math.clamp(vpCount - maxMeshParticles, 0, vpCount);
            for (int i = start; i < end; i++)
            {
                if (i >= vpCount) // quick and dirty for now
                {
                    verts[i * 4 + 0] = float3.zero;
                    verts[i * 4 + 1] = float3.zero;
                    verts[i * 4 + 2] = float3.zero;
                    verts[i * 4 + 3] = float3.zero;
                    continue;
                }

                //.3ms
                var particles = psort[i + hiddenParticles];
                var idx = particles.index;

                // read data and animate
                var particle = particlesData[idx];
                var size = particle.size;
                //var angle = particlesData[idx].angle = AnimateAngle(particle.angle);
                var angle = particle.angle;
                var color = particle.color;
                var pos = particles.worldPos;
                var distance = -particles.distance;

                //var sin = Mathf.Sin(angle);
                //var cos = Mathf.Cos(angle);

                // .2ms save on 4K
                float sin, cos;
                FastSinCos(angle, out sin, out cos);

                // diagonals (1,1) and (1,-1) rotated by angle and projected to camera space
                //var d1 = (x * (cos - sin) + y * (sin + cos)) * size;
                //var d2 = (x * (cos + sin) + y * (sin - cos)) * size;

                // this rewriting saves .7ms!!! on 4K particles
                var plus = (sin + cos) * size;
                var minus = (cos - sin) * size;


                var forward = math.normalize(camX - pos);
                var localX = math.normalize(math.cross(forward, new float3(0, 1, 0)));
                var localY = math.normalize(math.cross(forward, localX));

                var d1 = new float3(localY.x * minus + localX.x * plus, localY.y * minus + localX.y * plus, localY.z * minus + localX.z * plus);
                var d2 = new float3(localY.x * plus - localX.x * minus, localY.y * plus - localX.y * minus, localY.z * plus - localX.z * minus);
                //d1 = Vector3.one;
                //d2 = Vector3.zero;
                // calculate alpha
                var alpha = color.a;
                // distance clipping
                alpha = DistanceClipAlpha(alpha, distance);



                //1.1ms
                for (int b = 0; alpha > 0.01f && b < cloudBoxData.Length; b++)
                    alpha *= GetAlpha(pos, cloudBoxData[b]);



                color.a = alpha;
                //   Debug.Log($"Job distance {distance} a {alpha}"); 
                //.9ms
                if (alpha > 0.01f)
                {
                    //verts[i * 4 + 0] = pos - d1;
                    //verts[i * 4 + 1] = pos - d2;
                    //verts[i * 4 + 2] = pos + d1;
                    //verts[i * 4 + 3] = pos + d2;

                    // 0.1ms gain on 4k!
                    verts[i * 4 + 0] = new float3(pos.x - d1.x, pos.y - d1.y, pos.z - d1.z);
                    verts[i * 4 + 1] = new float3(pos.x - d2.x, pos.y - d2.y, pos.z - d2.z);
                    verts[i * 4 + 2] = new float3(pos.x + d1.x, pos.y + d1.y, pos.z + d1.z);
                    verts[i * 4 + 3] = new float3(pos.x + d2.x, pos.y + d2.y, pos.z + d2.z);

                }
                else
                {
                    verts[i * 4 + 0] = pos;
                    verts[i * 4 + 1] = pos;
                    verts[i * 4 + 2] = pos;
                    verts[i * 4 + 3] = pos;
                }


                cols[i * 4 + 0] = color;
                cols[i * 4 + 1] = color;
                cols[i * 4 + 2] = color;
                cols[i * 4 + 3] = color;

                var d1Norm = math.normalize(d1);
                var d2Norm = math.normalize(d2);
                const float angleCoef = 0.33f;

                normals[i * 4 + 0] = math.lerp(forward, -d1Norm, angleCoef);
                normals[i * 4 + 1] = math.lerp(forward, -d2Norm, angleCoef);
                normals[i * 4 + 2] = math.lerp(forward, d1Norm, angleCoef);
                normals[i * 4 + 3] = math.lerp(forward, d2Norm, angleCoef);

            }
        }

        public float GetAlpha(float3 pos, CloudBoxData cloudBoxData)
        {
            //var relative = pos-transform.position;
            //var x = 1 - Mathf.Clamp01((Mathf.Abs(relative.x * 2) - innerSize.x) / (outerSize.x - innerSize.x));
            //var y = 1 - Mathf.Clamp01((Mathf.Abs(relative.y * 2) - innerSize.y) / (outerSize.y - innerSize.y));
            //var z = 1 - Mathf.Clamp01((Mathf.Abs(relative.z * 2) - innerSize.z) / (outerSize.z - innerSize.z));
            //var alpha = x * y * z;
            //if (cleanInside) alpha = 1 - alpha;
            //return Mathf.Lerp(1, alpha,fade);

            // improves by .45ms on 4K particles
            var alpha = 0f;
            var relative = pos - cloudBoxData.pos;
            var x = 1 - (math.abs(relative.x * 2) - cloudBoxData.innerSize.x) / (cloudBoxData.outerSize.x - cloudBoxData.innerSize.x);
            if (x > 0)
            {
                var y = 1 - (math.abs(relative.y * 2) - cloudBoxData.innerSize.y) / (cloudBoxData.outerSize.y - cloudBoxData.innerSize.y);
                if (y > 0)
                {
                    var z = 1 - (math.abs(relative.z * 2) - cloudBoxData.innerSize.z) / (cloudBoxData.outerSize.z - cloudBoxData.innerSize.z);
                    if (z > 0)
                    {
                        alpha = math.clamp(x, 0, 1) * math.clamp(y, 0, 1) * math.clamp(z, 0, 1);
                    }
                }
            }
            alpha = 1 - alpha;

            return cloudBoxData.fade * alpha + (1 - cloudBoxData.fade);
        }

        private float DistanceClipAlpha(float alpha, float distance)
        {
            //  Debug.Log($"Job nearClipEnd {nearClipEnd} nearClipStart {nearClipStart} farClipEnd {farClipEnd} farClipStart {farClipStart}");
            if (distance < nearClipEnd)
                alpha *= math.clamp((distance - nearClipStart) / (nearClipEnd - nearClipStart), 0, 1);
            if (distance > farClipStart)
                alpha *= math.clamp((distance - farClipEnd) / (farClipStart - farClipEnd), 0, 1);
            return alpha;
        }

        private void FastSinCos(float x, out float sin, out float cos)
        {
            ////always wrap input angle to -PI..PI
            //if (x < -3.14159265f)
            //    x += 6.28318531f;
            //else
            //    if (x > 3.14159265f)
            //        x -= 6.28318531f;

            //compute sine
            if (x < 0)
                sin = 1.27323954f * x + .405284735f * x * x;
            else
                sin = 1.27323954f * x - 0.405284735f * x * x;

            //compute cosine: sin(x + PI/2) = cos(x)
            x += 1.57079632f;
            if (x > 3.14159265f)
                x -= 6.28318531f;

            if (x < 0)
                cos = 1.27323954f * x + 0.405284735f * x * x;
            else
                cos = 1.27323954f * x - 0.405284735f * x * x;
        }
    }
}
