using UnityEngine;
using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.Rendering;
using NBG.Core;

namespace NBG.Clouds
{
    public class CloudRender : MonoBehaviour
    {
        //public static CloudRender instance;
        public static List<CloudRender> all = new List<CloudRender>();

        int[] visibleClouds;
        public int visibleCloudsCount;
        int[] visibleParticles;
        public int visibleParticlesCount;


        // calculated
        public int maxMeshParticles = 256;


        public Vector3[] vertices;
        public Color32[] colors;
        public Vector3[] normals;

        int[] myThreads;

        struct ParticleSort
        {
            public int index;
            public Vector3 worldPos;
            public float distance;

            public override string ToString()
            {
                return string.Format("{0} {1}", index, distance);
            }
        }
        private ParticleSort[] particleSort;


        void OnEnable()
        {
            all.Add(this);
            //instance = this;
            //CloudSystem.Instance.Initialize();

            if (CloudSystem.Instance == null)
            {
                var system = FindObjectOfType<CloudSystem>();
                if (system != null)
                    system.Initialize();
            }
            else
                CloudSystem.Instance.Initialize();

            RenderPipelineManager.beginCameraRendering += BeforeRender;
            //InitializeCloud();
        }

        void OnDisable()
        {
            KillThreads();
            all.Remove(this);
            RenderPipelineManager.beginCameraRendering -= BeforeRender;
        }

        public void InitializeCloud()
        {

            visibleClouds = new int[CloudSystem.Instance.maxClouds];
            visibleParticles = new int[CloudSystem.Instance.maxParticles];

            maxMeshParticles = CloudSystem.Instance.maxParticles / 4;
            vertices = new Vector3[maxMeshParticles * 4];
            colors = new Color32[maxMeshParticles * 4];
            normals = new Vector3[maxMeshParticles * 4];

            particleSort = new ParticleSort[CloudSystem.Instance.maxParticles];
            myThreads = new int[CloudSystem.Instance.maxThreadCount];

            if (helperThreads == null)
            {
                helperThreads = new Thread[CloudSystem.Instance.maxThreadCount];   // SUMO - rethink, allow a maximum number of threads and "activate/disable" them
                helperParams = new ThreadParams[CloudSystem.Instance.maxThreadCount];

                for (int i = 0; i < CloudSystem.Instance.maxThreadCount; i++)
                {
                    helperThreads[i] = new Thread(new ParameterizedThreadStart(Worker));
                    helperParams[i] = new ThreadParams()
                    {
                        start = maxMeshParticles / CloudSystem.Instance.threadCount * (i % CloudSystem.Instance.threadCount),
                        end = maxMeshParticles / CloudSystem.Instance.threadCount * ((i % CloudSystem.Instance.threadCount) + 1),
                        running = i < CloudSystem.Instance.threadCount,
                        abort = false,
                        verts = vertices,
                        cols = colors,
                        normals = normals,
                        psort = particleSort,
                        vpCount = visibleParticlesCount,
                        startHandle = new AutoResetEvent(false),
                        completeHandle = new AutoResetEvent(false)
                    };
                    myThreads[i] = i < CloudSystem.Instance.threadCount ? i : -1;
                    helperThreads[i].Start(helperParams[i]);
                }
            }
            else
            {
                // We have already created the threads, we now need to find some free ones to use.
                for (int i = 0; i < CloudSystem.Instance.threadCount; i++)
                {
                    myThreads[i] = -1;
                    for (int t = 0; t < CloudSystem.Instance.maxThreadCount; t++)
                    {
                        if (helperParams[t].running == false)
                        {
                            helperParams[t].start = maxMeshParticles / CloudSystem.Instance.threadCount * i;
                            helperParams[t].end = maxMeshParticles / CloudSystem.Instance.threadCount * (i + 1);
                            helperParams[t].abort = false;
                            helperParams[t].verts = vertices;
                            helperParams[t].cols = colors;
                            helperParams[t].normals = normals;
                            helperParams[t].psort = particleSort;
                            helperParams[t].vpCount = visibleParticlesCount;
                            myThreads[i] = t;
                            helperParams[t].running = true;
                            break;
                        }
                    }
                }
            }


        }

        void OnDestroy()
        {
            KillThreads();
        }

        // SUMO - Abort all threads.
        public void KillAllThreads()
        {
            if (helperThreads != null)
            {
                for (int t = 0; t < CloudSystem.Instance.maxThreadCount; t++)
                {
                    helperParams[t].abort = true;
                    helperParams[t].startHandle.Set();  // Wake the thread even if not running any work
                }

                // Wait for all the runnings threads to actually finish their work.
                for (int i = 0; i < CloudSystem.Instance.maxThreadCount; i++)
                {
                    if (helperParams[i].running)
                    {
                        helperParams[i].completeHandle.WaitOne(100);
                    }
                }
            }
        }

        public void KillThreads()
        {
            if (helperThreads != null)
            {
                for (int i = 0; i < CloudSystem.Instance.threadCount; i++)
                {
                    for (int t = 0; t < CloudSystem.Instance.maxThreadCount; t++)
                    {
                        if (myThreads[i] == t)
                        {
                            helperParams[t].abort = true;
                            helperParams[t].startHandle.Set();
                            myThreads[i] = -1;
                        }
                    }
                }

                // Wait for all the runnings threads to actually finish their work.
                for (int i = 0; i < CloudSystem.Instance.maxThreadCount; i++)
                {
                    if (helperParams[i].running)
                    {
                        helperParams[i].completeHandle.WaitOne(100);
                    }
                }
                //helperThreads = null;
            }
        }

        class ThreadParams
        {
            public int start;
            public int end;
            public int vpCount;
            public bool running;
            public bool abort;
            public Vector3[] verts;
            public Color32[] cols;
            public Vector3[] normals;
            public Vector3 camX;
            public Vector3 camY;
            public ParticleSort[] psort;
            public AutoResetEvent startHandle;
            public AutoResetEvent completeHandle;
        }
        private void Worker(object options)
        {
            var tp = options as ThreadParams;

#if UNITY_SWITCH
        SwitchDLL.SetThreadToCoresTwoThree();
#endif

            while (true)
            {
                tp.startHandle.WaitOne();
                if (tp.abort)
                {
                    tp.running = false;
                    tp.cols = null;
                    tp.verts = null;
                }
                if (tp.running)
                    BuildMesh(tp.start, tp.end, tp);
                tp.completeHandle.Set();
            }
        }
        static ThreadParams[] helperParams;
        static Thread[] helperThreads;
        //AutoResetEvent waitHandle = new AutoResetEvent(false);

        //http://www.codeproject.com/Articles/80546/Comparison-Sorting-Algorithms-in-C-Explained
        private void ShellSort(ParticleSort[] inputArray, int length)
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

        Vector3 camX;
        Vector3 camY;
        //Vector3 camZ;
        //Vector3 camPos;

        bool updateStarted = false;
        // Update is called once per frame
        public void StartUpdate()
        {
            var camera = GetComponent<Camera>();
            if (camera == null) return;

            camX = camera.transform.right;
            camY = camera.transform.up;
            //camZ = camera.transform.forward;
            //camPos = camera.transform.position;

            CullClouds(camera);
            SortParticles(camera);

            //Profiler.BeginSample("build mesh");
            camX = camera.transform.right;
            camY = camera.transform.up;

            camX = camera.transform.position;


            for (int i = 0; i < CloudSystem.Instance.threadCount; i++)
            {
                if (myThreads[i] != -1)
                {
                    helperParams[myThreads[i]].camX = camX;
                    helperParams[myThreads[i]].camY = camY;
                    helperParams[myThreads[i]].vpCount = visibleParticlesCount;
                    helperParams[myThreads[i]].startHandle.Set();   // Wake the thread
                }
            }
            updateStarted = true;
        }

        void BeforeRender(ScriptableRenderContext context, Camera camera)
        {
            if (camera != GetComponent<Camera>()) return;
            if (!updateStarted) return;
            updateStarted = false;

            bool allThreadsCompleted = true;
            //BuildMesh(maxMeshParticles - maxMeshParticles / CloudSystem.instance.threadCount, maxMeshParticles);
            for (int i = 0; i < CloudSystem.Instance.threadCount; i++)
            {
                if (myThreads[i] != -1 && helperParams[myThreads[i]].running && !helperParams[myThreads[i]].completeHandle.WaitOne(1000))
                    allThreadsCompleted = false;
            }

            if (allThreadsCompleted)
            {

                //Profiler.EndSample();

                //.55ms
                //Profiler.BeginSample("upload mesh");
                CloudSystem.Instance.mesh.vertices = vertices;
                CloudSystem.Instance.mesh.colors32 = colors;
                CloudSystem.Instance.mesh.normals = normals;

                CloudSystem.Instance.mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);// always render
                                                                                                 //CloudSystem.instance.mesh.RecalculateBounds();
                                                                                                 //Profiler.EndSample();

            }

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

        Plane[] mPlanes = new Plane[6];

        #region Cull and sort
        void CullClouds(Camera camera)
        {
            var cameraPos = camera.transform.position;

            var cloudSetOffset = MathUtils.WrapSigned(CloudSystem.Instance.cloudSetOffset - cameraPos, CloudSystem.Instance.cloudSetSize) + cameraPos;

            GeometryUtility.CalculateFrustumPlanes(camera, mPlanes);

            visibleCloudsCount = 0;
            //if (Game.currentLevel != null && Game.currentLevel.noClouds)
            //    return;
            // get camera planes, etc
            for (int i = 0; i < CloudSystem.Instance.maxClouds; i++)
            {
                // get wrapped around world pos
                var cloud = CloudSystem.Instance.cloudsData[i];
                var worldPos = cloud.pos + cloudSetOffset;
                worldPos = MathUtils.WrapSigned(worldPos - cameraPos, CloudSystem.Instance.cloudSetSize) + cameraPos;

                var dist = (worldPos - cameraPos).magnitude;
                if (dist - Mathf.Max(Mathf.Max(cloud.size.x, cloud.size.y), cloud.size.z) > CloudSystem.Instance.farClipEnd) continue; // cloud too far

                if (GeometryUtility.TestPlanesAABB(mPlanes, new Bounds(worldPos, CloudSystem.Instance.cloudsData[i].size)))
                {
                    CloudSystem.Instance.cloudsData[i].worldPos = worldPos;
                    visibleClouds[visibleCloudsCount++] = i;
                }
            }
        }


        void SortParticles(Camera camera)
        {
            var camPos = camera.transform.position;

            var particlesData = CloudSystem.Instance.particlesData;

            visibleParticlesCount = 0;
            for (int i = 0; i < visibleCloudsCount; i++)
            {
                var cloud = CloudSystem.Instance.cloudsData[visibleClouds[i]];
                var start = cloud.startParticle;
                var end = cloud.endParticle;
                for (int p = start; p < end; p++)
                {

                    // calculate
                    var pos = particlesData[p].pos + cloud.worldPos;
                    var distance = (pos - camPos).magnitude;
                    // cull due to distance
                    if (distance <= CloudSystem.Instance.nearClipStart) continue;
                    if (distance >= CloudSystem.Instance.farClipEnd) continue;

                    //particlesData[p].worldPos = pos;
                    //particlesData[p].distance = distance;
                    particleSort[visibleParticlesCount].index = p;
                    particleSort[visibleParticlesCount].worldPos = pos;
                    particleSort[visibleParticlesCount].distance = -distance;
                    visibleParticlesCount++;

                    // animate
                    // particlesData[p].angle = AnimateAngle(particlesData[p].angle);

                }
            }
            if (visibleParticlesCount > CloudSystem.Instance.maxVisibleParticlesCount)
                CloudSystem.Instance.maxVisibleParticlesCount = visibleParticlesCount;
            ShellSort(particleSort, visibleParticlesCount);
        }
        #endregion

        private void BuildMesh(int start, int end, ThreadParams tp)
        {
            Vector3[] verts = tp.verts;
            Vector3[] normals = tp.normals;
            Color32[] cols = tp.cols;

            List<CloudBox> cloudboxes = new List<CloudBox>();
            lock (CloudBox.cloudLock)   // SUMO - take a copy of the box refs so we don't spend too long locked.
            {
                for (int b = 0; b < CloudBox.all.Count; b++)
                    cloudboxes.Add(CloudBox.all[b]);
            }

            var hiddenParticles = Mathf.Clamp(tp.vpCount - maxMeshParticles, 0, tp.vpCount);
            for (int i = start; i < end; i++)
            {
                if (i >= tp.vpCount) // quick and dirty for now
                {
                    verts[i * 4 + 0] =
                    verts[i * 4 + 1] =
                    verts[i * 4 + 2] =
                    verts[i * 4 + 3] = Vector3.zero;
                    continue;
                }

                //.3ms
                var particleS = tp.psort[i + hiddenParticles];
                var idx = particleS.index;

                // read data and animate
                var particle = CloudSystem.Instance.particlesData[idx];
                var size = particle.size;
                //var angle = particlesData[idx].angle = AnimateAngle(particle.angle);
                var angle = particle.angle;
                var color = particle.color;
                var pos = particleS.worldPos;
                var distance = -particleS.distance;

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


                var forward = (tp.camX - pos).normalized;
                var localX = Vector3.Cross(forward, Vector3.up).normalized;
                var localY = Vector3.Cross(forward, localX).normalized;

                var d1 = new Vector3(localY.x * minus + localX.x * plus, localY.y * minus + localX.y * plus, localY.z * minus + localX.z * plus);
                var d2 = new Vector3(localY.x * plus - localX.x * minus, localY.y * plus - localX.y * minus, localY.z * plus - localX.z * minus);
                //d1 = Vector3.one;
                //d2 = Vector3.zero;
                // calculate alpha
                var alpha = color.a;
                // distance clipping
                alpha = CloudSystem.Instance.DistanceClipAlpha(alpha, distance);

                //1.1ms
                for (int b = 0; alpha > 0.01f && b < cloudboxes.Count; b++)
                    alpha *= cloudboxes[b].GetAlpha(pos);

                color.a = alpha;

                //.9ms
                if (alpha > 0.01f)
                {
                    //verts[i * 4 + 0] = pos - d1;
                    //verts[i * 4 + 1] = pos - d2;
                    //verts[i * 4 + 2] = pos + d1;
                    //verts[i * 4 + 3] = pos + d2;

                    // 0.1ms gain on 4k!
                    verts[i * 4 + 0] = new Vector3(pos.x - d1.x, pos.y - d1.y, pos.z - d1.z);
                    verts[i * 4 + 1] = new Vector3(pos.x - d2.x, pos.y - d2.y, pos.z - d2.z);
                    verts[i * 4 + 2] = new Vector3(pos.x + d1.x, pos.y + d1.y, pos.z + d1.z);
                    verts[i * 4 + 3] = new Vector3(pos.x + d2.x, pos.y + d2.y, pos.z + d2.z);

                }
                else
                {
                    verts[i * 4 + 0] = pos;
                    verts[i * 4 + 1] = pos;
                    verts[i * 4 + 2] = pos;
                    verts[i * 4 + 3] = pos;
                }

                cols[i * 4 + 0] =
                    cols[i * 4 + 1] =
                    cols[i * 4 + 2] =
                    cols[i * 4 + 3] = color;

                var d1Norm = d1.normalized;
                var d2Norm = d2.normalized;
                const float angleCoef = 0.33f;

                normals[i * 4 + 0] = Vector3.Lerp(forward, -d1Norm, angleCoef);
                normals[i * 4 + 1] = Vector3.Lerp(forward, -d2Norm, angleCoef);
                normals[i * 4 + 2] = Vector3.Lerp(forward, d1Norm, angleCoef);
                normals[i * 4 + 3] = Vector3.Lerp(forward, d2Norm, angleCoef);
            }
        }

        //     float AnimateAngle(float angle)
        //     {
        //         angle += deltaTime * CloudSystem.instance.rotateSpeed * Random.Range(0.3f, 1);
        //         //angle = Wrap(angle, Mathf.PI*2);
        //         if (CloudSystem.instance.rotateSpeed > 0)
        //             while (angle > Mathf.PI)
        //                 angle -= Mathf.PI * 2;
        //         else
        //             while (angle < -Mathf.PI)
        //                 angle += Mathf.PI * 2;
        //         return angle;
        //     }
    }
}