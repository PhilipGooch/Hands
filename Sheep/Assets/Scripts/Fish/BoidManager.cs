//#define USE_COMPUTE_SHADER

using NBG.Core;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : SingletonBehaviour<BoidManager>
{
    [SerializeField]
    BoidSettings settings;
    [SerializeField]
    ComputeShader compute;
    [SerializeField]
    Boid fishPrefab;
    [SerializeField]
    float spawnRadius = .5f;
    [SerializeField]
    [Range(4, 32)]
    int spawnCount = 16;

    readonly List<Boid> boids = new List<Boid>();
    const int threadGroupSize = 1024;
    BoidData[] boidData;
    ComputeBuffer boidBuffer;
    int boidsShaderID;
    int numBoidsShaderID;
    int viewRadiusShaderID;
    int avoidRadiusShaderID;

    [SerializeField]
    List<Transform> spawnTransforms = new List<Transform>();

    // Defining a hard boundary where they will respawn if they exceed it for some reason (this should not happen).
    public float EnvironmentExtent, EnvironmentRadius;

    const bool useComputeShader = false;

    struct BoidData
    {
        public Vector3 position;
        public Vector3 direction;
        public Vector3 flockHeading;
        public Vector3 flockCenter;
        public Vector3 seperationHeading;
        public int numFlockmates;
        public static int Size => sizeof(float) * 3 * 5 + sizeof(int);
    }

    protected override void Awake()
    {
        base.Awake();
        boidsShaderID = Shader.PropertyToID("boids");
        numBoidsShaderID = Shader.PropertyToID("numBoids");
        viewRadiusShaderID = Shader.PropertyToID("viewRadius");
        avoidRadiusShaderID = Shader.PropertyToID("avoidRadius");
    }

    void Start()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            int spawnTransformID = i % spawnTransforms.Count;
            Vector3 pos = spawnTransforms[spawnTransformID].position + Random.insideUnitSphere * spawnRadius;
            Boid boid = Instantiate(fishPrefab, transform);
            boid.name = "Fish " + i;
            boid.transform.position = pos;
            float randomX = Random.Range(0, 359);
            float randomY = Random.Range(0, 359);
            float randomZ = Random.Range(0, 359);
            boid.transform.forward = Quaternion.Euler(randomX, randomY, randomZ) * Vector3.forward;
            boid.Initialize(settings, spawnTransforms[spawnTransformID].position);
            boids.Add(boid);
        }
        boidData = new BoidData[boids.Count];
        boidBuffer = new ComputeBuffer(boids.Count, BoidData.Size);
    }

    void FixedUpdate()
    {
        if (boids.Count > 0)
        {
            for (int i = 0; i < boids.Count; i++)
            {
                boidData[i].position = boids[i].Position;
                boidData[i].direction = boids[i].Forward;
                boidData[i].flockHeading = Vector3.zero;
                boidData[i].flockCenter = Vector3.zero;
                boidData[i].seperationHeading = Vector3.zero;
                boidData[i].numFlockmates = 0;
            }
#if USE_COMPUTE_SHADER
            // GPU
            boidBuffer.SetData(boidData);
            compute.SetBuffer(0, boidsShaderID, boidBuffer);
            compute.SetInt(numBoidsShaderID, boids.Count);
            compute.SetFloat(viewRadiusShaderID, settings.perceptionRadius);
            compute.SetFloat(avoidRadiusShaderID, settings.avoidanceRadius);
            int threadGroups = Mathf.CeilToInt(boids.Count / (float)threadGroupSize);
            compute.Dispatch(0, threadGroups, 1, 1);
            boidBuffer.GetData(boidData);
#else
            // CPU
            for (int i = 0; i < boids.Count; i++)
            {
                for (int j = 0; j < boids.Count; j++)
                {
                    if (i != j)
                    {
                        Vector3 offset = boidData[j].position - boidData[i].position;
                        float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
                        if (sqrDst < settings.perceptionRadius * settings.perceptionRadius)
                        {
                            boidData[i].numFlockmates += 1;
                            boidData[i].flockHeading += boidData[j].direction;
                            boidData[i].flockCenter += boidData[j].position;
                            if (sqrDst < settings.avoidanceRadius * settings.avoidanceRadius)
                            {
                                boidData[i].seperationHeading -= offset / sqrDst;
                            }
                        }
                    }
                }
            }
#endif
            for (int i = 0; i < boids.Count; i++)
            {
                boids[i].FlockHeading = boidData[i].flockHeading;
                boids[i].FlockCenter = boidData[i].flockCenter;
                boids[i].SeperationHeading = boidData[i].seperationHeading;
                boids[i].NumFlockmates = boidData[i].numFlockmates;
                boids[i].UpdateBoid();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, .5f, 1, 0.4f);
        foreach (Transform spawnTransform in spawnTransforms)
        {
            Gizmos.DrawSphere(spawnTransform.position, spawnRadius);
        }
        DebugExtension.DrawCylinder(transform.position + new Vector3(0, EnvironmentExtent, 0),
                                    transform.position - new Vector3(0, EnvironmentExtent, 0),
                                    Color.black, EnvironmentRadius);
    }
}
