using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[System.Serializable]
public struct UnderwaterParameters
{
    public int blurAmount;
    public float maxViewDistance;
    public float minViewDistance;
    public Color underwaterColor;
    public UnderwaterParameters(int blurAmount, float maxViewDistance, float minViewDistance, Color underwaterColor)
    {
        this.blurAmount = blurAmount;
        this.maxViewDistance = maxViewDistance;
        this.minViewDistance = minViewDistance;
        this.underwaterColor = underwaterColor;
    }
}

public class UnderwaterEffectZone : MonoBehaviour
{
    PointCheckingJob job;
    MeshFilter zoneMeshFilter;

    [SerializeField]
    UnderwaterParameters underwaterParameters = new UnderwaterParameters(4, 10, 1, new Color32(9, 23, 23, 255));
    private void Awake()
    {
        // This works with a regular mesh filter and a ProBuilderMesh.
        // ProBuilderMesh creates a filter and hides it, absolute hackery.
        zoneMeshFilter = GetComponent<MeshFilter>();
        if (zoneMeshFilter != null)
        {
            var zoneMesh = zoneMeshFilter.mesh;
            var vertices = new List<Vector3>();
            zoneMesh.GetVertices(vertices);
            var triangles = zoneMesh.GetTriangles(0);

            var nativeVertices = new NativeArray<float3>(vertices.Count, Allocator.Persistent);
            var nativeTriangles = new NativeArray<int>(triangles, Allocator.Persistent);
            var nativeResult = new NativeArray<int>(2, Allocator.Persistent);
            for (int i = 0; i < vertices.Count; i++)
            {
                nativeVertices[i] = transform.TransformPoint(vertices[i]);
            }

            job = new PointCheckingJob(nativeVertices, nativeTriangles, nativeResult);
        }
        else
        {
            Debug.LogError("There is no mesh filter attached to the Underwater Effect Zone!", this);
        }
    }

    private void OnDestroy()
    {
        if (zoneMeshFilter != null)
        {
            job.Dispose();
        }
    }

    Vector3 left, right;
    // Update is called once per frame
    void LateUpdate()
    {
        if (Player.Instance != null && zoneMeshFilter != null)
        {
            var player = Player.Instance;
            player.GetEyePositions(out left, out right);
            job.firstPoint = left;
            job.secondPoint = right;
            job.Run();
            player.UpdateEyeUnderwaterState(job.result[0] > 0, job.result[1] > 0, underwaterParameters);
        }
    }

    [BurstCompile]
    struct PointCheckingJob : IJob
    {
        [Unity.Collections.ReadOnly]
        NativeArray<float3> vertices;
        [Unity.Collections.ReadOnly]
        NativeArray<int> triangles;

        public float3 firstPoint;
        public float3 secondPoint;

        public NativeArray<int> result;

        public PointCheckingJob(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<int> result)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.result = result;
            firstPoint = 0;
            secondPoint = 0;
        }

        public void Execute()
        {
            result[0] = 1;
            result[1] = 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var plane = new Plane(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]);
                if (plane.GetSide(firstPoint))
                {
                    result[0] = 0;
                }
                if (plane.GetSide(secondPoint))
                {
                    result[1] = 0;
                }

                if (result[0] == 0 && result[1] == 0)
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            vertices.Dispose();
            triangles.Dispose();
            result.Dispose();
        }
    }

    private void OnDrawGizmosSelected()
    {
        var filter = GetComponent<MeshFilter>();
        if (filter != null)
        {
            Gizmos.color = new Color(252f / 255f, 226f / 255f, 5f / 255f, 0.5f);
            Gizmos.DrawMesh(filter.sharedMesh, 0, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
