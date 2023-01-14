using UnityEngine;
using System.Collections;

namespace NBG.Clouds
{
    public class CloudSystem : MonoBehaviour
    {
        public static CloudSystem Instance;

        private void OnEnable()
        {
            if (CloudRender.all != null && CloudRender.all.Count > 0)
                Initialize();
        }

        public void Initialize()
        {
            Instance = this;

            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            mesh.name = "Clouds";
            //SetQuality(4);
            InitializeCloud();
            //mesh.MarkDynamic();
        }

        public int maxThreadCount = 4;
        public int threadCount = 4;
        public int maxClouds = 32;
        public int segmentsPerCloud = 4;
        public int particlesPerSegment = 8;
        public Vector3 cloudSetSize = new Vector3(400, 400, 400);
        public float nearClipStart = 5;
        public float nearClipEnd = 10;
        public float farClipStart = 150;
        public float farClipEnd = 200;

        public Vector3 cloudSize = new Vector3(20, 5, 20);
        public Vector3 cloudSegmentSize = new Vector3(15, 10, 15);
        public float particleScale = 15;
        // animation
        public float rotateSpeed = 0.05f;
        public Vector3 moveSpeed = new Vector3(2, 0, 0);

        // calculated
        public int maxParticles = 1024;


        public Color mainColor = new Color32(158, 158, 158, 77);
        public Color tintColor = new Color32(79, 79, 79, 255);
        public float tintCenter = 0.7f;
        public float tintScale = 1;
        public float tintOffset = 0;

        public Vector3 cloudSetOffset; // clouds animation

        private MeshFilter meshFilter;
        public Mesh mesh;

        private int curQuality = -1;

        public void SetQuality(int quality) // 0-4
        {
            if (quality == curQuality)
            {
                return;
            }
            curQuality = quality;
            Debug.Log("CloudSystem quality=" + curQuality);
            /*
                    for (int i = 0; i < CloudRender.all.Count; i++)
                        CloudRender.all[i].KillThreads();
            */
            switch (quality)
            {
                case 0: //off
                    threadCount = 1;
                    maxClouds = 0;
                    segmentsPerCloud = 2;
                    particlesPerSegment = 2;
                    break;
                //case 1://128
                //    threadCount = 1;
                //    maxClouds = 32;
                //    segmentsPerCloud = 2;
                //    particlesPerSegment = 2;
                //    break;
                case 1: //432
                    threadCount = 1;
                    maxClouds = 48;
                    segmentsPerCloud = 3;
                    particlesPerSegment = 3;
                    break;
                case 2://1K
                    threadCount = 1;
                    maxClouds = 64;
                    segmentsPerCloud = 4;
                    particlesPerSegment = 4;
                    break;
                case 3: //2.3K
                    threadCount = 1;
                    maxClouds = 96;
                    segmentsPerCloud = 4;
                    particlesPerSegment = 6;
                    break;
                case 4: //6.1K
                    threadCount = 1;
                    maxClouds = 128;
                    segmentsPerCloud = 6;
                    particlesPerSegment = 8;
                    break;
            }

            // SUMO : lock down to one helper thread on all systems

            InitializeCloud();
        }

        void InitializeCloud()
        {
            // SUMO : Bad thread issue here, we shouldn't generate a new cloudset while there is work being done on existing cloudset.
            CloudRender.all[0].KillAllThreads();

            //maxParticles = CloudSystem.instance.maxClouds * CloudSystem.instance.segmentsPerCloud * CloudSystem.instance.particlesPerSegment;
            maxParticles = maxClouds * segmentsPerCloud * particlesPerSegment;
            Debug.Log("CloudSystem maxParticles=" + maxParticles + " " + CloudRender.all.Count);
            GenerateCloudSet(cloudSetSize);
            for (int i = 0; i < CloudRender.all.Count; i++)
                CloudRender.all[i].InitializeCloud();

            if (CloudRender.all.Count > 0)
            {
                mesh.Clear();
                var uvs = new Vector2[CloudRender.all[0].maxMeshParticles * 4];
                var triangles = new int[CloudRender.all[0].maxMeshParticles * 6];
                for (int i = 0; i < CloudRender.all[0].maxMeshParticles; i++)
                {
                    triangles[i * 6 + 0] = i * 4 + 0;
                    triangles[i * 6 + 1] = i * 4 + 1;
                    triangles[i * 6 + 2] = i * 4 + 2;
                    triangles[i * 6 + 3] = i * 4 + 2;
                    triangles[i * 6 + 4] = i * 4 + 3;
                    triangles[i * 6 + 5] = i * 4 + 0;
                    uvs[i * 4 + 0] = new Vector2(0, 0);
                    uvs[i * 4 + 1] = new Vector2(0, 1);
                    uvs[i * 4 + 2] = new Vector2(1, 1);
                    uvs[i * 4 + 3] = new Vector2(1, 0);
                }

                mesh.vertices = CloudRender.all[0].vertices;
                mesh.colors32 = CloudRender.all[0].colors;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                meshFilter.mesh = mesh;
            }
        }

        public struct CloudData
        {
            public Vector3 worldPos;
            public Vector3 pos;
            public Vector3 size;
            public int startParticle;
            public int endParticle;
        }

        public struct CloudParticleData
        {
            //public Vector3 worldPos;
            //public float distance;
            public Vector3 pos;
            public Color color;
            public float size;
            public float angle;
        }

        #region Generate CloudSet
        public CloudData[] cloudsData;
        public CloudParticleData[] particlesData;
        public int maxVisibleParticlesCount;

        void GenerateCloudSet(Vector3 size)
        {

            cloudsData = new CloudData[maxClouds];
            particlesData = new CloudParticleData[maxParticles];
            for (int i = 0; i < maxClouds; i++)
            {
                var cloudPos = Vector3.Scale(Random.insideUnitSphere, size);//new Vector3(size.x * Random.value, size.y * Random.value, size.z * Random.value);
                GenerateCloud(i, cloudPos);
            }
        }

        void GenerateCloud(int index, Vector3 center)
        {
            var start = index * segmentsPerCloud * particlesPerSegment;
            var end = (index + 1) * segmentsPerCloud * particlesPerSegment;

            cloudsData[index].startParticle = start;
            cloudsData[index].endParticle = end;

            var segmentDepthDisplacement = cloudSize.z / segmentsPerCloud;

            for (int i = 0; i < segmentsPerCloud; i++)
            {
                var position = Vector3.Scale(Random.insideUnitSphere, cloudSize);
                position.z = i * segmentDepthDisplacement;
                BuildCloudPart(position, start + i * particlesPerSegment, start + (i + 1) * particlesPerSegment);
            }

            // calculate bounding sphere for a cloud
            Bounds b = new Bounds(particlesData[start].pos, Vector3.zero);
            for (int i = start; i < end; i++)
            {
                var s = new Vector3(particlesData[i].size / 2, particlesData[i].size / 2, particlesData[i].size / 2);
                b.Encapsulate(particlesData[i].pos - s);
                b.Encapsulate(particlesData[i].pos + s);
            }
            cloudsData[index].pos = b.center + center;
            cloudsData[index].size = b.size;
            // recenter particles
            for (int i = start; i < end; i++)
                particlesData[i].pos -= b.center;

            ColorCloud(start, end);
        }

        void BuildCloudPart(Vector3 pos, int start, int end)
        {
            var particleDisplacement = cloudSegmentSize.z / particlesPerSegment;
            for (int i = start; i < end; i++)
            {
                var offset = Vector3.Scale(Random.insideUnitSphere, cloudSegmentSize);
                offset.z = i * particleDisplacement;
                particlesData[i].pos = pos + offset;
                particlesData[i].size = particleScale * Random.Range(0.6f, 1f);
                particlesData[i].angle = Random.value * Mathf.PI * 2 - Mathf.PI;

            }
        }

        void ColorCloud(int start, int end)
        {
            //var light = Light.GetLights(LightType.Directional, 1)[0];
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = start; i < end; i++)
            {
                var y = particlesData[i].pos.y;
                //var y = Vector3.Dot(-light.transform.forward+Vector3.up, render.particlePositions[i]);// render.particlePositions[i];
                min = Mathf.Min(min, y);
                max = Mathf.Max(max, y);
            }

            for (int i = start; i < end; i++)
            {
                var y = particlesData[i].pos.y;
                //var y = Vector3.Dot(-light.transform.forward + Vector3.up, render.particlePositions[i]);
                var relativeY = (y - min) / (max - min);
                var tint = Mathf.Abs(relativeY - tintCenter) * tintScale + tintOffset;
                particlesData[i].color = Color.Lerp(mainColor, tintColor, tint);
            }
        }

        #endregion

        void LateUpdate()
        {


            var deltaTime = Time.deltaTime;
            cloudSetOffset += moveSpeed * deltaTime;

            for (int b = 0; b < CloudBox.all.Count; b++)
                CloudBox.all[b].ReadPos();

            for (int i = 0; i < CloudRender.all.Count; i++)
                CloudRender.all[i].StartUpdate();
        }

        public void Scroll(Vector3 offset)
        {
            cloudSetOffset += offset;
            //for (int i = 0; i < maxClouds; i++)
            //    clouds[i].Scroll(offset);
        }

        public float DistanceClipAlpha(float alpha, float distance)
        {
            if (distance < nearClipEnd)
                alpha *= Mathf.Clamp01((distance - nearClipStart) / (nearClipEnd - nearClipStart));
            if (distance > farClipStart)
                alpha *= Mathf.Clamp01((distance - farClipEnd) / (farClipStart - farClipEnd));
            return alpha;
        }
    }
}