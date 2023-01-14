using NBG.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    [ExecuteAlways]
    public class BreakableWallDebug : MonoBehaviour
    {
        public enum BreakableWallDebugType
        {
            DegeneratedPolygon,
            EarClipping,
            CustomPolygon
        }

        public BreakableWallDebugType type;

        public bool next = false;
        public bool regenerate = false;

        private int currentIndexDegeneratedPolygon = 0;
        private int currentIndexEarClipping = 0;
        private int currentCustomPolygon = 0;

        public List<float3> customPolygon = null;

        [ClearOnReload] public static BreakableWallDebug instance;

        private void Awake()
        {
            instance = this;
        }

        public void Update()
        {
            if (next)
            {
                if (type == BreakableWallDebugType.DegeneratedPolygon)
                {
                    BreakableWall wall = GetComponent<BreakableWall>();
                    List<float3> degeneratedVertices = wall.polygon.degeneratedVertices;
                    next = false;
                    currentIndexDegeneratedPolygon++;
                    if (currentIndexDegeneratedPolygon >= degeneratedVertices.Count - 1)
                        currentIndexDegeneratedPolygon = 0;
                }
                else if (type == BreakableWallDebugType.EarClipping)
                {
                    BreakableWall wall = GetComponent<BreakableWall>();
                    next = false;
                    currentIndexEarClipping += 3;
                    if (currentIndexEarClipping >= wall.polygon.triangles.Length - 3)
                        currentIndexEarClipping = 0;
                }
                else if (type == BreakableWallDebugType.CustomPolygon)
                {
                    if (customPolygon != null)
                    {
                        next = false;
                        currentCustomPolygon++;
                        if (currentCustomPolygon >= customPolygon.Count)
                            currentCustomPolygon = 0;
                    }
                }
            }

            if (regenerate)
            {
                BreakableWall wall = GetComponent<BreakableWall>();
                wall.ApplyPolygon(wall.polygon);
                regenerate = false;
            }
        }

        private void OnDrawGizmos()
        {
            BreakableWall wall = GetComponent<BreakableWall>();

            if (wall != null && wall.polygon != null)
            {
                if (type == BreakableWallDebugType.DegeneratedPolygon)
                {
                    List<float3> degeneratedVertices = wall.polygon.degeneratedVertices;

                    Vector3 from = Vector3.zero, to = Vector3.zero;

                    if (degeneratedVertices != null)
                    {
                        if (currentIndexDegeneratedPolygon >= degeneratedVertices.Count)
                        {
                            currentIndexDegeneratedPolygon = 0;
                            return;
                        }
                        for (int i = 0; i <= currentIndexDegeneratedPolygon; i++)
                        {
                            from.x = degeneratedVertices[i].x;
                            from.y = degeneratedVertices[i].y;
                            from.z = -0.5f;

                            to.x = degeneratedVertices[i + 1].x;
                            to.y = degeneratedVertices[i + 1].y;
                            to.z = -0.5f;

                            from = transform.TransformPoint(from);
                            to = transform.TransformPoint(to);

                            if (currentIndexDegeneratedPolygon == i)
                                Gizmos.color = Color.yellow;
                            else
                                Gizmos.color = Color.red;

                            Gizmos.DrawLine(from, to);
                        }
                    }
                }
                else if (type == BreakableWallDebugType.EarClipping)
                {
                    NativeArray<float3> vertices = wall.polygon.nativeVertices;
                    NativeArray<int> triangles = wall.polygon.triangles;

                    Vector3 A = Vector3.zero, B = Vector3.zero, C = Vector3.zero;

                    if (currentIndexEarClipping >= triangles.Length)
                    {
                        currentIndexEarClipping = 0;
                        return;
                    }

                    for (int i = 0; i <= currentIndexEarClipping; i += 3)
                    {
                        int a = triangles[i];
                        int b = triangles[i + 1];
                        int c = triangles[i + 2];

                        A.x = vertices[a].x;
                        A.y = vertices[a].y;
                        A.z = -0.5f;
                        A = transform.TransformPoint(A);

                        B.x = vertices[b].x;
                        B.y = vertices[b].y;
                        B.z = -0.5f;
                        B = transform.TransformPoint(B);

                        C.x = vertices[c].x;
                        C.y = vertices[c].y;
                        C.z = -0.5f;
                        C = transform.TransformPoint(C);

                        if (currentIndexEarClipping == i)
                            Gizmos.color = Color.yellow;
                        else
                            Gizmos.color = Color.red;

                        Gizmos.DrawLine(A, B);
                        Gizmos.DrawLine(B, C);
                        Gizmos.DrawLine(C, A);
                    }
                }
                else if (customPolygon != null && type == BreakableWallDebugType.CustomPolygon)
                {
                    Vector3 from = Vector3.zero, to = Vector3.zero;
                    int countMinusOne = customPolygon.Count - 1;

                    for (int i = 0; i <= currentCustomPolygon; i++)
                    {
                        from.x = customPolygon[i].x;
                        from.y = customPolygon[i].y;
                        from.z = -0.5f;

                        int next = i == countMinusOne ? 0 : i + 1;
                        to.x = customPolygon[next].x;
                        to.y = customPolygon[next].y;
                        to.z = -0.5f;

                        from = transform.TransformPoint(from);
                        to = transform.TransformPoint(to);

                        if (currentIndexDegeneratedPolygon == i)
                            Gizmos.color = Color.yellow;
                        else
                            Gizmos.color = Color.red;

                        Gizmos.DrawLine(from, to);
                    }
                }
            }
        }

        public static void SetCustomPolygon(List<float3> poly)
        {
            if (instance.customPolygon.Count == 0)
            {
                List<float3> copy = new List<float3>(poly);
                instance.customPolygon = copy;
            }
        }
    }
}
