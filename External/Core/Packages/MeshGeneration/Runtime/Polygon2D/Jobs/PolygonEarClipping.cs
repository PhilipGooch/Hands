using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace NBG.MeshGeneration
{
    [BurstCompile(CompileSynchronously = true)]
    public struct PolygonEarClipping : IJob
    {
        [ReadOnly]
        public NativeArray<float3> polygonVertices;
        [WriteOnly]
        public NativeArray<int> triangles;

        public NativeArray<int> previousArray;
        public NativeArray<int> nextArray;
        public NativeArray<bool> clip;


        public void Execute()
        {
            int previous = 0, next = 0, current = 0;
            int end = clip.Length - 1;
            int remainingVertex = polygonVertices.Length;
            int firstIndex = 0;
            int trianglesIndex = 0;

            for (int i = 0; i < polygonVertices.Length; i++)
            {
                if ((i - 1) < 0)
                {
                    previousArray[i] = 0;
                }
                else
                {
                    previousArray[i] = i - 1;
                }

                if ((i + 1) > end)
                {
                    nextArray[i] = 0;
                }
                else
                {
                    nextArray[i] = i + 1;
                }

                clip[i] = false;
            }

            for (int z = 0; z < polygonVertices.Length * 2.0f; z++)
            {
                current = firstIndex;
                for (int i = 0; i < remainingVertex; i++)
                {
                    if (clip[current])
                    {
                        current = nextArray[current];
                        continue;
                    }

                    previous = previousArray[current];
                    next = nextArray[current];

                    float3 previousPos = polygonVertices[previous];
                    float3 nextPos = polygonVertices[next];
                    float3 currentPos = polygonVertices[current];

                    float3 vector1 = nextPos - currentPos;
                    float3 vector2 = previousPos - currentPos;

                    //Is ear vertex
                    bool isEarAngle = IsRight(vector1, vector2);

                    bool canCloseTriangle = true;

                    int currentEdgeIndex = firstIndex;

                    //Check if intersects with some existing edge
                    if (isEarAngle)
                    {
                        for (int j = 0; j < remainingVertex; j++)
                        {
                            int nextEdgeIndex = nextArray[currentEdgeIndex];
                            float3 edge1 = polygonVertices[currentEdgeIndex];
                            float3 edge2 = polygonVertices[nextEdgeIndex];

                            bool intersects = SegmentsIntersect(previousPos, nextPos, edge1, edge2);

                            bool pointIsInsideTriangle = IsPointInsideTriangle(currentPos, nextPos, previousPos, edge1);
                            canCloseTriangle = !(intersects || pointIsInsideTriangle);

                            if (!canCloseTriangle)
                            {
                                break;
                            }

                            currentEdgeIndex = nextEdgeIndex;
                        }
                    }

                    if (isEarAngle && canCloseTriangle)
                    {
                        clip[current] = true;
                        remainingVertex--;
                        if (current == firstIndex)
                        {
                            firstIndex = next;
                        }

                        previousArray[next] = previous;
                        nextArray[previous] = next;

                        //Add triangle
                        triangles[trianglesIndex] = current;
                        triangles[trianglesIndex + 1] = next;
                        triangles[trianglesIndex + 2] = previous;
                        trianglesIndex += 3;
                        break;
                    }
                    else
                    {
                        current = next;
                    }
                }
            }

#if DebugEar
        // Debug.Log("#earTotal vertex count: " + polygonVertices.Length);
        // Debug.Log("#earRemaining vertices: " + remainingVertex);
        // Debug.Log("#earFinal triangle count : [" + (trianglesIndex / 3) + "] expected " + triangles.Length / 3);
#endif

        }

        public bool SegmentsIntersect(float3 a1, float3 a2, float3 b1, float3 b2)
        {
            if (a1.Equals(b1) || a1.Equals(b2) || a2.Equals(b1) || a2.Equals(b2))
                return false;
            float2 o1 = a1.xy;
            float2 d1 = a2.xy - o1;

            float2 o2, d2;

            o2 = b1.xy;
            d2 = b2.xy - o2;

            float denominator = (d1.y * d2.x - d1.x * d2.y);
            if (math.abs(denominator) > float.MinValue)
            {
                float c = (d1.y * (o1.x - o2.x) + d1.x * (o2.y - o1.y)) / denominator;
                float t = -(d2.y * (o2.x - o1.x) + d2.x * (o1.y - o2.y)) / denominator;

                bool intersects = c >= 0.0f && c <= 1.0f && t >= 0 && t <= 1.0f;// use >= and <= to include edge points

                if (intersects)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsPointInsideTriangle(float3 p1, float3 p2, float3 p3, float3 p)
        {
            float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

            float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
            float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
            float c = 1 - a - b;

            return a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f;
        }

        public bool IsRight(float3 v1, float3 v2)
        {
            return math.dot(math.cross(new float3(0.0f, 0.0f, -1.0f), v1), v2) > 0.000000001f;
        }
    }
}
