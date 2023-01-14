// #define DebugEar
//#define DEBUG_SUBSTRACT

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

[assembly: InternalsVisibleTo("NBG.PlaneDestructionSystem.Tests.Editor")]

namespace NBG.MeshGeneration
{
    public class Polygon2D
    {
        public List<float3> vertices;

        public List<List<float3>> holes;
        public List<float3> connectedHole;
        public List<float3> bannedConnectedHoleVertices;
        public float3[] initialFramePoints;

        public List<float3> degeneratedVertices;

        public NativeArray<float3> nativeVertices;
        public NativeArray<int> triangles;

        public NativeArray<float3> extrudedPolygonVertices;
        public NativeArray<int> extrudedTriangles;

        public bool pendingJob = false;
        public JobHandle handle;

        private NativeArray<bool> clipTempNativeArray;
        private NativeArray<int> previousTempNativeArray;
        private NativeArray<int> nextTempNativeArray;

        public float reallySmallThreshold = 0.0000001f;

        public bool isEmpty = false;

        public Polygon2D(int vertexCount, float size, float3 offset, bool randomRadius = true)
        {
            float3[] vertices = new float3[vertexCount];

            float angleOffset = randomRadius ? UnityEngine.Random.Range(0.0f, 2.0f * math.PI) : 0;
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = (2.0f * math.PI * ((float)i / vertexCount)) + angleOffset;
                float distance = size;
                vertices[i] = new float3(Round(math.sin(angle) * distance, 100.0f), Round(math.cos(angle) * distance, 100.0f), 0.0f) + offset;
            }

            SetVertices(vertices);
        }

        public Polygon2D(int vertexCount, float minSize, float maxSize, float rotationOffset = 0)
        {
            float3[] vertices = new float3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float angle = (2.0f * math.PI * ((float)i / vertexCount)) + rotationOffset;
                float distance = UnityEngine.Random.Range(minSize, maxSize);
                vertices[i] = new float3(math.sin(angle) * distance, math.cos(angle) * distance, 0.0f);
            }

            SetVertices(vertices);
        }

        public Polygon2D(float3 start, float3 end, float width)
        {
            float3[] vertices = new float3[4];

            float halfWidth = width * 0.5f;

            float3 forward = end - start;
            float3 right = math.cross(new float3(0.0f, 0.0f, -1.0f), math.normalize(forward)) * halfWidth;

            vertices[0] = end - right;
            vertices[1] = end + right;
            vertices[2] = start + right;
            vertices[3] = start - right;

            SetVertices(vertices);
        }

        public Polygon2D(float3[] vertices)
        {
            SetVertices(vertices);
        }

        public Polygon2D(List<float3> vertices)
        {
            SetVertices(vertices);
        }

        public Polygon2D(float3[] vertices, float3[][] inputHoles)
        {
            SetVertices(vertices);
            for (int i = 0; i < inputHoles.Length; i++)
            {
                holes.Add(new List<float3>(inputHoles[i]));
            }
        }

        public void EmptyPolygon()
        {
            isEmpty = true;
            SetVertices(new float3[0]);
        }

        public override bool Equals(object obj)
        {
            Polygon2D other = obj as Polygon2D;

            return Equals2D(other) && Equals3D(other);
        }

        public bool Equals2D(Polygon2D other)
        {
            if (vertices.Count != other.vertices.Count || holes.Count != other.holes.Count)
                return false;

            for (int i = 0; i < vertices.Count; i++)
            {
                if (!ApproximatelyEqual(vertices[i], other.vertices[i]))
                    return false;
            }
            for (int j = 0; j < holes.Count; j++)
            {
                var hole = holes[j];
                var otherHole = other.holes[j];
                for (int i = 0; i < hole.Count; i++)
                    if (!ApproximatelyEqual(hole[i], otherHole[i]))
                        return false;
            }

            return true;
        }

        public bool Equals3D(Polygon2D other)
        {
            if (extrudedPolygonVertices.Length != other.extrudedPolygonVertices.Length ||
                extrudedTriangles.Length != other.extrudedTriangles.Length)
                return false;

            for (int i = 0; i < extrudedPolygonVertices.Length; i++)
                if (!ApproximatelyEqual(extrudedPolygonVertices[i], other.extrudedPolygonVertices[i]))
                    return false;

            for (int i = 0; i < extrudedTriangles.Length; i++)
                if (extrudedTriangles[i] != other.extrudedTriangles[i])
                    return false;

            return true;
        }

        private bool ApproximatelyEqual(float3 a, float3 b)
        {
            const float approximationThreshold = 0.00001f;
            float3 offset = math.abs(a - b);

            if (offset.x > approximationThreshold || offset.y > approximationThreshold || offset.z > approximationThreshold)
            {
                Debug.Log("[" + a + "] != [" + b + "]");
                return false;
            }

            return true;
        }
        public void SetVertices(float3[] vertices)
        {
            Dispose();

            InitializeHoles();

            this.vertices = new List<float3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                this.vertices.Add(vertices[i]);
            }
        }

        public void SetVertices(List<float3> vertices, bool copy = true)
        {
            Dispose();

            InitializeHoles();

            if (copy)
            {
                this.vertices = new List<float3>(vertices);
            }
            else
            {
                this.vertices = vertices;
            }

            if (vertices.Count < 3)
                isEmpty = true;
        }

        private void InitializeHoles()
        {
            if (holes == null)
                holes = new List<List<float3>>();
        }

        public void FillFramePointsList()
        {
            initialFramePoints = new float3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                initialFramePoints[i] = vertices[i];
        }

        private class WeilerAthertonData
        {
            public List<float3> basePolygon;
            public List<int> baseIntersections;

            public List<float3> subtractPolygon;
            public List<int> subtractIntersections;
            public bool[] isInIntersection;
            public bool[] isBasePointInsideOther;

            public int startIndex;

            public WeilerAthertonData(List<float3> baseVertices, List<float3> other)
            {
                basePolygon = new List<float3>(baseVertices.Count + other.Count);
                baseIntersections = new List<int>(basePolygon.Count);
                isInIntersection = null;
                isBasePointInsideOther = null;

                subtractPolygon = new List<float3>(basePolygon.Count);
                subtractIntersections = new List<int>(basePolygon.Count);
            }

            public void Clear()
            {
                basePolygon.Clear();
                baseIntersections.Clear();

                subtractPolygon.Clear();
                subtractIntersections.Clear();
            }
        }

        public void SubtractPolygon(Polygon2D other, out List<Polygon2D> newStaticPolygons, out List<Polygon2D> clipPolygons, out List<Polygon2D> outOfFramePolygons)
        {
            SubtractPolygon(other.vertices, out newStaticPolygons, out clipPolygons, out outOfFramePolygons);
        }

        public void SubtractPolygon(List<float3> other, out List<Polygon2D> newStaticPolygons, out List<Polygon2D> clipPolygons, out List<Polygon2D> outOfFramePolygons)
        {
            SubtractPolygon(vertices, other, out newStaticPolygons, out clipPolygons, out outOfFramePolygons);
        }

        public void SubtractPolygon(List<float3> baseVertices, List<float3> other, out List<Polygon2D> newStaticPolygons, out List<Polygon2D> clipPolygons, out List<Polygon2D> outOfFramePolygons)
        {
            clipPolygons = new List<Polygon2D>(4);
            newStaticPolygons = new List<Polygon2D>(4);
            outOfFramePolygons = new List<Polygon2D>(4);

            WeilerAthertonData data = new WeilerAthertonData(baseVertices, other);

            List<float3> currentHole;
            bool hasintersectedWithHole = false;

            for (int i = 0; i < holes.Count; i++)
            {
                currentHole = holes[i];
                if (WeilerAthertonPrePass(currentHole, other, data))
                {
                    List<List<float3>> mergedHolePolygons = WeilerAthertonSubtractOrAdd(data, true);

                    if (!hasintersectedWithHole && WeilerAthertonPrePass(other, currentHole, data))
                    {
                        List<List<float3>> otherMinusHole = WeilerAthertonSubtractOrAdd(data, false);

                        for (int j = 0; j < otherMinusHole.Count; j++)
                        {
                            if (WeilerAthertonPrePass(otherMinusHole[j], baseVertices, data))
                            {
                                List<List<float3>> otherMinusHoleClippedWithShape = WeilerAthertonClipping(data);
                                for (int k = 0; k < otherMinusHoleClippedWithShape.Count; k++)
                                    clipPolygons.Add(new Polygon2D(otherMinusHoleClippedWithShape[k]));
                            }
                            else
                            {
                                clipPolygons.Add(new Polygon2D(otherMinusHole[j]));
                            }
                        }
                    }

                    // assign resulting addition polygons 
                    bool otherAssigned = false;
                    for (int j = 0; j < mergedHolePolygons.Count; j++)
                    {
                        if (IsPolygonClockwise(mergedHolePolygons[j]))
                        {
                            if (!otherAssigned)
                            {
                                otherAssigned = true;
                                other = mergedHolePolygons[j];
                            }
                            else
                            {
                                Debug.Log("This shouldn't happen never... report this");
                            }
                        }
                        else
                        {
                            mergedHolePolygons[j].Reverse();
                            outOfFramePolygons.Add(new Polygon2D(mergedHolePolygons[j]));
                        }
                    }

                    holes.RemoveAt(i);
                    i--;
                    hasintersectedWithHole = true;
                }
                else
                {
                    if (!hasintersectedWithHole && IsPointInsideInclusive(currentHole, other[0]))
                    {//other is inside this hole so nothing
                        return;
                    }
                    else if (IsPointInsideInclusive(other, currentHole[0]))
                    { //if the existing hole is inside this new shape, remove it
                        holes.RemoveAt(i);
                        i--;
                    }
                }
            }

            List<List<float3>> clippingPolygons = null;
            List<List<float3>> framePolygons = null;

            if (WeilerAthertonPrePass(baseVertices, other, data))
            {
                //clipPolygons.Clear();
                if (!hasintersectedWithHole)
                    clippingPolygons = WeilerAthertonClipping(data);
                framePolygons = WeilerAthertonSubtractOrAdd(data, false);
            }
            else
            {
                if (IsPointInsideExclusive(baseVertices, other[0]))//If other doesn't collide with the border it's a hole
                {
                    holes.Add(other);

                    if (!hasintersectedWithHole)
                    {
                        clipPolygons.Add(new Polygon2D(other));
                    }
                }
            }

            //Create the output polygons
            if (framePolygons != null)
            {
                bool frameFound = false;
                for (int i = 0; i < framePolygons.Count; i++)
                {
                    List<float3> currentPolygon = framePolygons[i];

                    if (currentPolygon.Count > 2)
                    {
                        if (!frameFound && IsinFrame(currentPolygon))
                        {
                            frameFound = true;
                            SetVertices(currentPolygon);
                            for (int j = 0; j < holes.Count; j++)
                            {
                                if (!IsPointInsideInclusive(currentPolygon, holes[j][0]))
                                {
                                    holes.RemoveAt(j);
                                    j--;
                                }
                            }
                        }
                        else
                        {
                            if (IsinFrame(currentPolygon))
                            {
                                newStaticPolygons.Add(new Polygon2D(currentPolygon));
                            }
                            else
                            {
                                outOfFramePolygons.Add(new Polygon2D(currentPolygon));
                            }
                        }
                    }
                }

                if (!frameFound)
                {
                    EmptyPolygon();
                }
            }

            if (clippingPolygons != null)
            {
                for (int i = 0; i < clippingPolygons.Count; i++)
                {
                    if (clippingPolygons[i].Count > 2)
                        clipPolygons.Add(new Polygon2D(clippingPolygons[i]));
                }
            }

            //Check donut case
            for (int i = 0; i < outOfFramePolygons.Count; i++)
            {
                Polygon2D currentPolygon = outOfFramePolygons[i];

                for (int j = 0; j < holes.Count; j++)
                {
                    if (IsPointInsideInclusive(currentPolygon.vertices, holes[j][0]))
                    {
                        currentPolygon.holes.Add(holes[j]);
                        holes.RemoveAt(j);
                        j--;
                    }
                }
            }
        }

        public void BasicAdd(Polygon2D other)
        {
            WeilerAthertonData data = new WeilerAthertonData(vertices, other.vertices);
            if (WeilerAthertonPrePass(vertices, other.vertices, data))
            {
                List<List<float3>> result = WeilerAthertonSubtractOrAdd(data, true);
                if (result.Count > 0)
                {
                    SetVertices(result[0]);
                }
            }
        }

        private static bool WeilerAthertonPrePass(List<float3> baseVertices, List<float3> other, WeilerAthertonData data)
        {
            if (ArePolygonsEqual(baseVertices, other))
                return false;

            data.Clear();

            ModifyVertexOverlaps(baseVertices, other);

            if (BuildIntersectionTable(baseVertices, other, data.basePolygon, data.baseIntersections)) //If intersects somewhere, calculate subtraction
            {
                BuildIntersectionTable(other, baseVertices, data.subtractPolygon, data.subtractIntersections);

                //Find points inside subjectPolygon
                data.isBasePointInsideOther = new bool[data.baseIntersections.Count];
                for (int i = 0; i < data.baseIntersections.Count; i++)
                {
                    if (data.baseIntersections[i] == -1)
                    {
                        data.isBasePointInsideOther[i] = IsPointInsideInclusive(other, data.basePolygon[i]);
                    }
                }

                //Find initial
                data.startIndex = -1;
                int initial = FindInitial(data);

                if (initial == -1)//Shouldn't happen, fix in the future
                {
                    return false;
                }

                float3 startPos = data.basePolygon[initial];

                //Wire table indices and start point
                for (int i = 0; i < data.basePolygon.Count; i++)
                {
                    if (data.baseIntersections[i] != -1)
                    {
                        float best = 999999.0f;
                        int bestBaseIntersection = -1;
                        int bestSubtractIntersection = -1;
                        for (int j = 0; j < data.subtractPolygon.Count; j++)
                        {
                            if (data.subtractIntersections[j] == -1)
                                continue;

                            float distance = math.distance(data.basePolygon[i], data.subtractPolygon[j]);
                            if (distance < best)
                            {
                                bestBaseIntersection = j;
                                bestSubtractIntersection = i;
                                best = distance;
                            }
                        }

                        if (bestBaseIntersection == -1 || bestSubtractIntersection == -1)
                            return false;

                        data.baseIntersections[bestSubtractIntersection] = bestBaseIntersection;
                        data.subtractIntersections[bestBaseIntersection] = bestSubtractIntersection;
                    }

                    if (data.startIndex == -1 && data.basePolygon[i].Equals(startPos))
                        data.startIndex = i;
                }

                //Find IN intersections
                data.isInIntersection = new bool[data.baseIntersections.Count];
                bool started = false;
                int current = data.startIndex;

                for (int i = 0; i < data.baseIntersections.Count; i++)
                {
                    if (data.baseIntersections[current] != -1 && i != 0)
                    {
                        if (started)
                        {
                            started = false;
                        }
                        else
                        {
                            started = true;
                            data.isInIntersection[current] = true;
                        }
                    }
                    current = current == data.baseIntersections.Count - 1 ? 0 : current + 1;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        private List<List<float3>> WeilerAthertonClipping(WeilerAthertonData data)
        {
            //Clipping polygons 
            int current;
            int clipCurrentIndex;
            int clipStart = -2;
            bool buildingClip = false;

            int baseIntersectionsCountMinusOne = data.baseIntersections.Count - 1;
            int subtractIntersectionsCountMinusOne = data.subtractIntersections.Count - 1;

            List<List<float3>> clippingPolygons = new List<List<float3>>();
            List<float3> currentClip = null;

            bool[] baseVisited = new bool[data.basePolygon.Count];

            int iterations = data.baseIntersections.Count;

            for (int k = 0; k < iterations; k++)
            {
                if (data.isInIntersection[k])
                {
                    current = k;
                    bool done = false;

                    for (int i = 0; i < iterations; i++)
                    {
                        int intersection = data.baseIntersections[current];

                        if (baseVisited[current])
                            break;

                        if (!buildingClip)
                        {
                            if (intersection != -1 && data.isInIntersection[current])
                            {
                                buildingClip = true;
                                clipStart = current;
                                currentClip = new List<float3>(10);
                                currentClip.Add(data.basePolygon[current]);
                                baseVisited[current] = true;
                            }
                        }
                        else
                        {
                            currentClip.Add(data.basePolygon[current]);
                            baseVisited[current] = true;

                            if (intersection != -1)
                            {
                                clipCurrentIndex = intersection;
                                for (int j = 0; j < data.subtractIntersections.Count; j++)
                                {
                                    if (j != 0)
                                    {
                                        int clipIntersection = data.subtractIntersections[clipCurrentIndex];
                                        if (clipIntersection == -1)
                                        {
                                            currentClip.Add(data.subtractPolygon[clipCurrentIndex]);
                                        }
                                        else
                                        {
                                            if (clipIntersection == clipStart)
                                            {
                                                buildingClip = false;
                                                clippingPolygons.Add(currentClip);
                                                done = true;
                                            }
                                            else
                                            {
                                                current = clipIntersection;
                                                currentClip.Add(data.basePolygon[current]);
                                                baseVisited[current] = true;
                                            }
                                            break;
                                        }
                                    }

                                    clipCurrentIndex = clipCurrentIndex == subtractIntersectionsCountMinusOne ? 0 : clipCurrentIndex + 1;
                                }

                                if (done)
                                    break;
                            }
                        }

                        current = current == baseIntersectionsCountMinusOne ? 0 : current + 1;
                    }
                }
            }

            return clippingPolygons;
        }
        private List<List<float3>> WeilerAthertonSubtractOrAdd(WeilerAthertonData data, bool add = false)
        {
#if DEBUG_SUBSTRACT
            Debug.Log("--Substraction starting");
#endif
            int current = data.startIndex;
            int clipCurrentIndex;

            List<List<float3>> clippingPolygons = new List<List<float3>>();
            List<float3> currentClip = new List<float3>(10);

            int baseIntersectionCount = data.baseIntersections.Count;

            bool[] visited = new bool[baseIntersectionCount];
            int visitedVertices = 0;

            for (int i = 0; i < baseIntersectionCount; i++)
            {
                int intersection = data.baseIntersections[current];

                if (i != 0 && current == data.startIndex)
                {
                    Decimate(currentClip, 0.2f);
                    clippingPolygons.Add(currentClip);
                    currentClip = new List<float3>();

                    if (visitedVertices == baseIntersectionCount)
                    {
                        break;
                    }
                    else
                    {
                        //If I haven't visited all the vertices, then find the first non visisted one
                        bool found = false;
                        for (int j = 0; j < baseIntersectionCount; j++)
                        {
                            if (!visited[j] && ((data.baseIntersections[j] == -1 && !data.isBasePointInsideOther[j]) || data.isInIntersection[j]))
                            {
                                current = j;
                                intersection = data.baseIntersections[current];
                                data.startIndex = j;
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            break;
                        }
                    }
                }

                currentClip.Add(data.basePolygon[current]);

#if DEBUG_SUBSTRACT
                Debug.Log("-#BP:"+current);
#endif

                visited[current] = true;
                visitedVertices++;

                if (i != 0 && intersection != -1)
                {
                    clipCurrentIndex = intersection;
                    int clipIntersection;

                    for (int j = 0; j < data.subtractIntersections.Count; j++)
                    {
                        if (j != 0)//skip first
                        {
                            clipIntersection = data.subtractIntersections[clipCurrentIndex];

                            if (clipIntersection == -1)
                            {
                                currentClip.Add(data.subtractPolygon[clipCurrentIndex]);
#if DEBUG_SUBSTRACT
                                Debug.Log("-#SP:" + clipCurrentIndex);
#endif
                            }
                            else
                            {
                                if (clipIntersection == data.startIndex)
                                {
                                    current = clipIntersection == 0 ? data.baseIntersections.Count - 1 : clipIntersection - 1;
                                    break;
                                }
                                else
                                {
                                    currentClip.Add(data.subtractPolygon[clipCurrentIndex]);
#if DEBUG_SUBSTRACT
                                    Debug.Log("-#SP:" + clipCurrentIndex);
#endif
                                    current = clipIntersection;
                                    visited[clipIntersection] = true;
                                    visitedVertices++;
                                    break;
                                }
                            }
                        }

                        if (add)
                        {
                            clipCurrentIndex = clipCurrentIndex == data.subtractIntersections.Count - 1 ? 0 : clipCurrentIndex + 1;
                        }
                        else
                        {
                            clipCurrentIndex = clipCurrentIndex == 0 ? data.subtractIntersections.Count - 1 : clipCurrentIndex - 1;
                        }
                    }
                }

                current = current == data.baseIntersections.Count - 1 ? 0 : current + 1;
            }

            return clippingPolygons;
        }
        private static int FindInitial(in WeilerAthertonData data)
        {
            for (int i = 0; i < data.basePolygon.Count; i++)
            {
                if (!data.isBasePointInsideOther[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ArePolygonsEqual(List<float3> A, List<float3> B)
        {
            int Acount = A.Count;
            int Bcount = B.Count;

            if (Acount != Bcount)
                return false;

            for (int i = 0; i < Acount; i++)
                if (!A[i].Equals(B[i]))
                    return false;

            return true;
        }

        private static bool BuildIntersectionTable(List<float3> A, List<float3> B, List<float3> polygonPositions, List<int> intersectionIndex)
        {
            List<IntersectionData> intersectionData = new List<IntersectionData>(10);
            bool intersected = false;

            float3 current;
            float3 next = A[0];

            int ACount = A.Count;
            int ACountMinusOne = ACount - 1;

            for (int i = 0; i < ACount; i++)
            {
                current = next;

                //Add vertex without intersection
                polygonPositions.Add(current);
                intersectionIndex.Add(-1);

                next = A[i == ACountMinusOne ? 0 : i + 1];
                if (SegmentsintersectPositionsAndFixCornerCases(current, next, B, intersectionData, out bool somethingFixed))
                {
                    for (int j = 0; j < intersectionData.Count; j++)
                    {
                        polygonPositions.Add(intersectionData[j].pos);
                        intersectionIndex.Add(intersectionData[j].index);
                        intersected = true;
                    }

                    intersectionData.Clear();
                }

                if (somethingFixed)
                {
                    //reset the loop
                    i = -1;
                    next = A[0];

                    intersectionData.Clear();
                    polygonPositions.Clear();
                    intersectionIndex.Clear();
                }
            }

            return intersected;
        }

        private void Decimate(List<float3> polygon, float threshold)
        {
            int maxIterations = polygon.Count - 2;

            float3 a, b, c;

            for (int i = 0; i < maxIterations; i += 3)
            {
                a = polygon[i];
                b = polygon[i + 1];
                c = polygon[i + 2];
                float distance = math.distance(a, b) + math.distance(b, c);
                if (distance < threshold && !SegmentsintersectExclusive(a, c, polygon))
                {
                    polygon.RemoveAt(i + 1);
                    maxIterations--;
                    i--;
                }
            }

            for (int i = 1; i < maxIterations; i += 3)
            {
                a = polygon[i];
                b = polygon[i + 1];
                c = polygon[i + 2];
                float distance = math.distance(a, b) + math.distance(b, c);
                if (distance < threshold && !SegmentsintersectExclusive(a, c, polygon))
                {
                    polygon.RemoveAt(i + 1);
                    maxIterations--;
                    i--;
                }
            }
        }

        DataPool<HoleData> holeDataPool = new DataPool<HoleData>(32);

        private class DegeneratedConnection
        {
            internal int holeIndex;
            internal int vertex;
            internal int otherHoleIndex;
            internal int otherVertex;
            internal float3 start, end;
            internal bool used = false;

            public DegeneratedConnection(int hole, int vertex, int otherHole, int otherVertex, float3 start, float3 end)
            {
                this.holeIndex = hole;
                this.vertex = vertex;
                this.otherHoleIndex = otherHole;
                this.otherVertex = otherVertex;
                this.start = start;
                this.end = end;
            }
        }


        private void DegeneratePolygon()
        {
            Profiler.BeginSample("DegeneratePolygon2");

            if (degeneratedVertices == null)
                degeneratedVertices = new List<float3>();
            else
                degeneratedVertices.Clear();

            List<float3> silhouette = new List<float3>();

            List<DegeneratedConnection> connections = new List<DegeneratedConnection>();

            //Add silouette vertices
            for (int i = 0; i < vertices.Count; i++)
            {
                silhouette.Add(vertices[i]);
            }

            if (holes != null && holes.Count > 0)
            {

                List<HoleData> holesData = new List<HoleData>();

                for (int i = 0; i < holes.Count; i++)
                {
                    HoleData newHole = holeDataPool.Get();
                    newHole.Create(holes[i], i);
                    holesData.Add(newHole);
                }

                for (int i = 0; i < holes.Count - 1; i++)
                {
                    connections.Add(FindConnection(silhouette, holesData, connections));
                }

                //Next step connect to silhouette
                var silhouetteConnection = ConnectSilhouette(silhouette, holesData, connections);
                BuildDegeneratedPolygon(silhouette, holesData, connections, silhouetteConnection);

                for (int i = 0; i < holesData.Count; i++)
                    holeDataPool.Recycle(holesData[i]);
            }
            else
            {
                for (int i = 0; i < silhouette.Count; i++)
                    degeneratedVertices.Add(silhouette[i]);
            }

            Profiler.EndSample();
        }

        List<int> orderedHoleIndexesByDistance = new List<int>(32);
        List<int> connectedHolesIndexes = new List<int>(32);
        private DegeneratedConnection FindConnection(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections)
        {
            if (connections.Count == 0)
            {
                int startHoleIndex = 0;

                holes[startHoleIndex].isConnected = true;

                CreateClosestHolesList(holes, startHoleIndex, ref orderedHoleIndexesByDistance);
                for (int j = 0; j < orderedHoleIndexesByDistance.Count; j++)
                {
                    int endHoleIndex = orderedHoleIndexesByDistance[j];
                    if (TryToConnect(silhouette, holes, connections, startHoleIndex, endHoleIndex, out DegeneratedConnection connection))
                    {
                        return connection;
                    }
                }
            }
            else
            {
                CreateConnectedHoleList(holes, connections, ref connectedHolesIndexes);
                for (int j = 0; j < connectedHolesIndexes.Count; j++)
                {
                    int startHoleIndex = connectedHolesIndexes[j];
                    CreateClosestHolesList(holes, startHoleIndex, ref orderedHoleIndexesByDistance);
                    for (int k = 0; k < orderedHoleIndexesByDistance.Count; k++)
                    {
                        int endHoleIndex = orderedHoleIndexesByDistance[k];
                        if (TryToConnect(silhouette, holes, connections, startHoleIndex, endHoleIndex, out DegeneratedConnection connection))
                        {
                            return connection;
                        }
                    }
                }
            }

            return null;
        }

        private void CreateClosestHolesList(List<HoleData> holes, int chosenHoleIndex, ref List<int> orderedHoleIndexesByDistance)
        {
            float3 origin = holes[chosenHoleIndex].center;

            List<HoleData> sortedHoles = new List<HoleData>();

            for (int i = 0; i < holes.Count; i++)
            {
                var hole = holes[i];
                if (i != chosenHoleIndex && !hole.isConnected)
                {
                    hole.distanceToCurrentHole = math.distance(hole.center, origin) * 1000.0f;
                    sortedHoles.Add(hole);
                }
            }

            sortedHoles.Sort((a, b) => (int)(a.distanceToCurrentHole - b.distanceToCurrentHole));

            orderedHoleIndexesByDistance.Clear();
            for (int i = 0; i < sortedHoles.Count; i++)
            {
                int theHoleIndex = sortedHoles[i].index;
                if (theHoleIndex != chosenHoleIndex)
                    orderedHoleIndexesByDistance.Add(theHoleIndex);
            }
        }

        private void CreateConnectedHoleList(List<HoleData> holes, List<DegeneratedConnection> connections, ref List<int> connectedHolesIndexes)
        {
            connectedHolesIndexes.Clear();

            if (connections.Count > 0)
            {
                for (int i = connections.Count - 1; i >= 0; i--)
                {
                    int holeIndex = connections[i].otherHoleIndex;
                    connectedHolesIndexes.Add(holeIndex);
                }
                connectedHolesIndexes.Add(connections[0].holeIndex);
            }
        }

        private bool TryToConnect(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections, int startHoleIndex, int endHoleIndex, out DegeneratedConnection connection)
        {
            connection = null;
            var startHoleVertices = holes[startHoleIndex].vertices;
            var endHoleVertices = holes[endHoleIndex].vertices;
            var startHoleVertexCount = startHoleVertices.Count;
            var endHoleVertexCount = endHoleVertices.Count;
            var startHoleBannedVertices = holes[startHoleIndex].bannedVertices;

            for (int i = 0; i < startHoleVertexCount; i++)
            {
                if (startHoleBannedVertices.Contains(i))
                    continue;
                for (int j = 0; j < endHoleVertexCount; j++)
                {
                    if (IsValidConnection(silhouette, holes, connections, startHoleIndex, endHoleIndex, i, j))
                    {
                        connection = new DegeneratedConnection(startHoleIndex, i, endHoleIndex, j, startHoleVertices[i], endHoleVertices[j]);
                        holes[startHoleIndex].bannedVertices.Add(i);
                        holes[endHoleIndex].bannedVertices.Add(j);
                        holes[startHoleIndex].isConnected = true;
                        holes[endHoleIndex].isConnected = true;
                        return true;
                    }
                }
            }

            return false;
        }

        private List<float3> singleSegment = new List<float3>(2);

        private bool IsValidConnection(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections, int startHoleIndex, int endHoleIndex, int startHoleVertexIndex, int endHoleVertexIndex)
        {
            //Requirements for valid connection
            //--Start hole is connected and end hole is not connected
            //--It's not a banned vertex
            //--It doesn't hits start or end hole polygon
            //--It doesn't hit silhouette
            //--It doesn't hit other holes
            //--It doesn't hit connections

            HoleData start = holes[startHoleIndex];
            HoleData end = holes[endHoleIndex];

            if (start.isConnected == false || end.isConnected == true)
                return false;

            if (start.bannedVertices.Contains(startHoleVertexIndex) || end.bannedVertices.Contains(endHoleVertexIndex))
                return false;

            float3 startPosition = start.vertices[startHoleVertexIndex];
            float3 endPosition = end.vertices[endHoleVertexIndex];

            singleSegment.Clear();
            singleSegment.Add(startPosition);
            singleSegment.Add(endPosition);

            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                if (SegmentsintersectArrayInclusive(conn.start, conn.end, singleSegment))
                    return false;
            }

            if (SegmentsintersectArrayInclusive(startPosition, endPosition, start.vertices))
                return false;
            if (SegmentsintersectArrayInclusive(startPosition, endPosition, end.vertices))
                return false;
            if (SegmentsintersectArrayInclusive(startPosition, endPosition, silhouette))
                return false;

            for (int i = 0; i < holes.Count; i++)
            {
                if (i != startHoleIndex && i != endHoleIndex)
                {
                    HoleData holeData = holes[i];
                    if (holeData.CastSegmentToBounds(startPosition, endPosition) && SegmentsintersectExclusive(startPosition, endPosition, holeData.vertices))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        private bool IsValidConnectionToSilhouette(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections, float3 silhouetteVertex, int endHoleIndex, int endHoleVertexIndex)
        {
            //Requirements for valid connection
            //--Start hole is connected and end hole is not connected
            //--It's not a banned vertex
            //--It doesn't hits start or end hole polygon
            //--It doesn't hit silhouette
            //--It doesn't hit other holes
            //--It doesn't hit connections

            HoleData end = holes[endHoleIndex];

            if (end.bannedVertices.Contains(endHoleVertexIndex))
                return false;

            float3 endPosition = end.vertices[endHoleVertexIndex];

            singleSegment.Clear();
            singleSegment.Add(silhouetteVertex);
            singleSegment.Add(endPosition);

            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                if (SegmentsintersectArrayInclusive(conn.start, conn.end, singleSegment))
                    return false;
            }

            if (SegmentsintersectArrayInclusive(silhouetteVertex, endPosition, end.vertices))
                return false;
            if (SegmentsintersectArrayInclusive(silhouetteVertex, endPosition, silhouette))
                return false;

            for (int i = 0; i < holes.Count; i++)
            {
                if (i != endHoleIndex)
                {
                    HoleData holeData = holes[i];

                    //bool boundsCheck = holeData.CastSegmentToBounds(silhouetteVertex, endPosition);
                    //bool polygonCheck = SegmentsintersectExclusive(silhouetteVertex, endPosition, holeData.vertices);
                    //if (!boundsCheck && polygonCheck)
                    //{
                    //    Debug.Log("Bounds error detected = " + holeData.boundingRect + " , " + silhouetteVertex + "," + endPosition);
                    //}

                    //if (boundsCheck && polygonCheck)
                    //{
                    //    return false;
                    //}

                    if (holeData.CastSegmentToBounds(silhouetteVertex, endPosition) && SegmentsintersectExclusive(silhouetteVertex, endPosition, holeData.vertices))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private DegeneratedConnection ConnectSilhouette(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections)
        {
            int silhouetteVertexCount = silhouette.Count;
            for (int i = 0; i < silhouetteVertexCount; i++)
            {
                for (int j = 0; j < holes.Count; j++)
                {
                    List<float3> currentHoleVertices = holes[j].vertices;
                    int currentHoleVerticesCount = currentHoleVertices.Count;
                    for (int k = 0; k < currentHoleVerticesCount; k++)
                    {
                        if (IsValidConnectionToSilhouette(silhouette, holes, connections, silhouette[i], j, k))
                        {
                            DegeneratedConnection connectionToSilhouette = new DegeneratedConnection(-1, i, j, k, silhouette[i], currentHoleVertices[k]);
                            return connectionToSilhouette;
                        }
                    }
                }
            }

            return null;
        }

        private void BuildDegeneratedPolygon(List<float3> silhouette, List<HoleData> holes, List<DegeneratedConnection> connections, DegeneratedConnection silhouetteConnection)
        {
            int silhouetteVertexCount = silhouette.Count;

            for (int i = 0; i < silhouetteVertexCount; i++)
            {
                degeneratedVertices.Add(silhouette[i]);
                if (silhouetteConnection.vertex == i)
                {
                    BuildConnection(connections, holes, silhouetteConnection.otherHoleIndex, silhouetteConnection.otherVertex);
                    degeneratedVertices.Add(silhouette[i]);
                }
            }
        }

        private void BuildConnection(List<DegeneratedConnection> connections, List<HoleData> holes, int holeIndex, int holeVertex)
        {
            List<float3> currentHoleVertices = holes[holeIndex].vertices;
            int currentHoleVertexCount = currentHoleVertices.Count;

            List<DegeneratedConnection> chosenConnections = new List<DegeneratedConnection>();
            for (int i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];
                if (!connection.used)
                {
                    if (connection.holeIndex == holeIndex || connection.otherHoleIndex == holeIndex)
                    {
                        chosenConnections.Add(connection);
                    }
                }
            }

            int currentVertex = holeVertex;
            for (int i = 0; i < currentHoleVertexCount; i++)
            {
                degeneratedVertices.Add(currentHoleVertices[currentVertex]);

                for (int j = 0; j < chosenConnections.Count; j++)
                {
                    DegeneratedConnection connection = chosenConnections[j];

                    if (connection.holeIndex == holeIndex && currentVertex == connection.vertex)
                    {
                        connection.used = true;
                        BuildConnection(connections, holes, connection.otherHoleIndex, connection.otherVertex);
                        degeneratedVertices.Add(currentHoleVertices[currentVertex]);
                        break;
                    }
                    else if (connection.otherHoleIndex == holeIndex && currentVertex == connection.otherVertex)
                    {
                        connection.used = true;
                        BuildConnection(connections, holes, connection.holeIndex, connection.vertex);
                        degeneratedVertices.Add(currentHoleVertices[currentVertex]);
                        break;
                    }
                }

                currentVertex--;
                if (currentVertex == -1)
                    currentVertex = currentHoleVertexCount - 1;
            }
            degeneratedVertices.Add(currentHoleVertices[holeVertex]);
        }

        #region Box colliders
        public List<float4> GenerateBoxColliders(float xWidth = 0.1f)
        {
            List<float4> boxes = new List<float4>(20);

            GetMinMax(vertices, out float minX, out float maxX, out float minY, out float maxY);

            float startY = maxY + 0.5f;
            float endY = minY - 0.5f;

            float halfXWidth = xWidth * 0.5f;
            float startX = minX + halfXWidth;

            int iterations = (int)((maxX - minX) / xWidth) + 1;

            float3 raystart = new float3(0.0f, startY, 0.0f);
            float3 rayend = new float3(0.0f, endY, 0.0f);

            List<IntersectionData> intersections = new List<IntersectionData>(10);

            for (int i = 0; i < iterations; i++)
            {
                raystart.x = startX + (i * xWidth);
                rayend.x = raystart.x;

                intersections.Clear();
                if (degeneratedVertices != null && SegmentsintersectPositions(raystart, rayend, degeneratedVertices, intersections))
                {
                    int intersectionCount = intersections.Count;

                    bool boxStarted = false;

                    float3 startPos = new float3();

                    for (int j = 0; j < intersectionCount; j++)
                    {
                        IntersectionData intersection = intersections[j];

                        if (boxStarted)
                        {
                            boxStarted = false;
                            float height = startPos.y - intersection.pos.y;

                            boxes.Add(new float4(raystart.x, startPos.y - (height * 0.5f), xWidth, height));
                        }
                        else
                        {
                            boxStarted = true;
                            startPos = intersection.pos;
                        }
                    }
                }
            }

            return boxes;
        }

        private void GetMinMax(List<float3> points, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = 99999.0f;
            maxX = -99999.0f;
            minY = 99999.0f;
            maxY = -99999.0f;

            int count = points.Count;

            for (int i = 0; i < count; i++)
            {
                float3 point = points[i];

                minX = point.x < minX ? point.x : minX;
                minY = point.y < minY ? point.y : minY;

                maxX = point.x > maxX ? point.x : maxX;
                maxY = point.y > maxY ? point.y : maxY;
            }
        }
        #endregion

        private void FindSegment(List<float3> degeneratedPolygon, List<HoleData> holes, out int connectedHoleIndex, out int holeIndex, out int holeVertexIndex)
        {
            float3 a, b;
            holeVertexIndex = -1;
            connectedHoleIndex = -1;
            holeIndex = -1;

            int degeneratedPolygonCount = degeneratedPolygon.Count;
            int holesCount = holes.Count;

            for (int i = 0; i < degeneratedPolygonCount; i++)
            {
                a = degeneratedPolygon[i];

                for (int j = 0; j < holesCount; j++)
                {
                    HoleData currentHole = holes[j];
                    List<float3> currentHoleVertices = currentHole.vertices;
                    int currentHoleCount = currentHoleVertices.Count;

                    bool found = false;

                    for (int k = 0; k < currentHoleCount; k += 2)
                    {
                        b = currentHoleVertices[k];
                        holeVertexIndex = k;

                        //This is just checking if bannedConnectedHoleVertices contains a or b
                        bool isBanned = false;
                        int bannedConnectedHoleVerticesCount = bannedConnectedHoleVertices.Count;
                        float3 checkedPoint;
                        for (int t = 0; t < bannedConnectedHoleVerticesCount; t++)
                        {
                            checkedPoint = bannedConnectedHoleVertices[t];

                            if (checkedPoint.x == a.x || checkedPoint.x == b.x)
                            {
                                if ((checkedPoint.x == a.x && checkedPoint.y == a.y) || (checkedPoint.x == b.x && checkedPoint.y == b.y))
                                {
                                    isBanned = true;
                                    break;
                                }
                            }
                        }

                        bool intersectsWithExitHole = SegmentsintersectArrayInclusive(a, b, currentHoleVertices);
                        bool intersectsWithDegeneratedPolygon = SegmentsintersectArrayInclusive(a, b, degeneratedPolygon);
                        bool intersects =
                            isBanned ||
                            intersectsWithDegeneratedPolygon ||
                            intersectsWithExitHole;

                        if (!intersects)
                        {
                            for (int h = 0; h < holesCount && !intersects; h++)
                            {
                                if (h != j)
                                {
                                    HoleData holeData = holes[h];
                                    //bool castBounds = holeData.CastSegmentToBounds(a, b);
                                    //bool castShape = SegmentsintersectExclusive(a, b, holeData.vertices);
                                    //if(!castBounds && castShape)
                                    //{
                                    //    Debug.Log("Mistake detected = " + holeData.boundingRect + " and points a = " + a + ", b = " + b);
                                    //}
                                    intersects = holeData.CastSegmentToBounds(a, b) && SegmentsintersectExclusive(a, b, holeData.vertices);
                                }
                            }

                            if (!intersects)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if (found)
                    {
                        connectedHoleIndex = i;
                        holeIndex = j;
                        return;
                    }
                }
            }
        }

        public void EarClipping()
        {
            if (!pendingJob && !isEmpty)
            {
                if (nativeVertices.IsCreated)
                    nativeVertices.Dispose();

                DegeneratePolygon();

                nativeVertices = new NativeArray<float3>(degeneratedVertices.Count, Allocator.Persistent);

                for (int i = 0; i < degeneratedVertices.Count; i++)
                {
                    nativeVertices[i] = degeneratedVertices[i];
                }

                if (triangles.IsCreated)
                    triangles.Dispose();
                triangles = new NativeArray<int>((nativeVertices.Length - 2) * 3, Allocator.Persistent);

                clipTempNativeArray = new NativeArray<bool>(nativeVertices.Length, Allocator.TempJob);
                previousTempNativeArray = new NativeArray<int>(nativeVertices.Length, Allocator.TempJob);
                nextTempNativeArray = new NativeArray<int>(nativeVertices.Length, Allocator.TempJob);

                PolygonEarClipping earClippingJob = new PolygonEarClipping
                {
                    polygonVertices = nativeVertices,
                    triangles = triangles,
                    clip = clipTempNativeArray,
                    previousArray = previousTempNativeArray,
                    nextArray = nextTempNativeArray
                };
#if DebugEar
            earClippingJob.Run();
#else
                handle = earClippingJob.Schedule();
#endif
                pendingJob = true;
            }
        }

        public void Extrusion(float depth = 0.1f)
        {
            if (isEmpty)
                return;
            if (extrudedPolygonVertices.IsCreated)
                extrudedPolygonVertices.Dispose();
            if (extrudedTriangles.IsCreated)
                extrudedTriangles.Dispose();

            extrudedPolygonVertices = new NativeArray<float3>(nativeVertices.Length * 4, Allocator.Persistent);
            extrudedTriangles = new NativeArray<int>((triangles.Length * 2) + (nativeVertices.Length * 2 * 3), Allocator.Persistent);

            ExtrudePolygon extrudePolygonJob = new ExtrudePolygon
            {
                polygonVertices = nativeVertices,
                triangles = triangles,
                extrudedPolygonVertices = extrudedPolygonVertices,
                extrudedTriangles = extrudedTriangles,
                depth = depth
            };
#if DebugEar
        extrudePolygonJob.Run();
#else
            handle = extrudePolygonJob.Schedule(handle);
#endif
        }

        public bool CheckJob()
        {
            if (pendingJob)
            {
#if !DebugEar
                if (!handle.IsCompleted)
                    return false;
#endif
                pendingJob = false;
                handle.Complete();
                clipTempNativeArray.Dispose();
                previousTempNativeArray.Dispose();
                nextTempNativeArray.Dispose();
                return true;
            }
            return false;
        }

        public bool IsFill(float3 localPosition)
        {
            bool isFill = IsPointInsideInclusive(vertices, localPosition);
            if (isFill)
            {
                int holeCount = holes.Count;
                for (int i = 0; i < holeCount; i++)
                {
                    if (IsPointInsideInclusive(holes[i], localPosition))
                        return false;
                }
            }
            return isFill;
        }

        public static string ToArrayText(List<float3> points)
        {
            float3[] array = new float3[] { new float3(1, 0, 0), new float3(1, 2, 4) };
            string output = "new float3[]{";
            for (int i = 0; i < points.Count; i++)
                output += "new float3(" + points[i].x + "f," + points[i].y + "f," + points[i].z + "f)" + (i == points.Count - 1 ? "}" : ",");
            return output;
        }

        public bool IsJobComplete()
        {
            return handle.IsCompleted;
        }

        public void Dispose()
        {
            handle.Complete();

            if (nativeVertices.IsCreated)
                nativeVertices.Dispose();
            if (triangles.IsCreated)
                triangles.Dispose();

            if (extrudedPolygonVertices.IsCreated)
                extrudedPolygonVertices.Dispose();
            if (extrudedTriangles.IsCreated)
                extrudedTriangles.Dispose();

            if (clipTempNativeArray.IsCreated)
                clipTempNativeArray.Dispose();

            if (previousTempNativeArray.IsCreated)
                previousTempNativeArray.Dispose();

            if (nextTempNativeArray.IsCreated)
                nextTempNativeArray.Dispose();
        }

        #region Math stuff  

        /// <summary>
        /// Get polygon area
        /// </summary>
        public bool IsPolygonValid()
        {
            int ACount = vertices.Count;
            int ACountMinusOne = ACount - 1;

            float3 a;
            float3 b = vertices[0];

            for (int i = 0; i < ACount; i++)
            {
                a = b;
                b = vertices[i == ACountMinusOne ? 0 : i + 1];

                if (SegmentsintersectDiscardingPoints(a, b, vertices))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Is polygon clockwise
        /// </summary>
        private static bool IsPolygonClockwise(List<float3> A)
        {
            return PolygonArea(A) < 0.0f;
        }

        /// <summary>
        /// Get polygon area
        /// </summary>
        public float PolygonArea()
        {
            return PolygonArea(vertices);
        }

        /// <summary>
        /// Get polygon area
        /// </summary>
        private static float PolygonArea(List<float3> A)
        {
            if (A.Count == 0)
                return -1.0f;

            int ACount = A.Count;
            int ACountMinusOne = ACount - 1;
            float area = 0.0f;

            float3 a;
            float3 b = A[0];

            for (int i = 0; i < ACount; i++)
            {
                a = b;
                b = A[i == ACountMinusOne ? 0 : i + 1];

                area += (a.x * b.y) - (b.x * a.y);
            }

            area *= 0.5f;
            return area;
        }

        private static bool IsPointInsideInclusive(List<float3> vertices, float3 point)
        {
            int times = SegmentsintersectCountInclusive(point, point + new float3(10000.0f, 500.0f, 0.0f), vertices);
            return times > 0 && times % 2 == 1;
        }

        private static bool IsPointInsideExclusive(List<float3> vertices, float3 point)
        {
            int times = SegmentsintersectCountExclusive(point, point + new float3(10000.0f, 500.0f, 0.0f), vertices);
            return times > 0 && times % 2 == 1;
        }

        private static bool SegmentsintersectExclusive(float3 a1, float3 a2, List<float3> vertices)
        {
            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float d2x, d2y;

            float3 o2, b2;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            b2 = vertices[0];

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = b2;
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                // d2 = b2 - o2;
                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = (d1y * d2x) - (d1x * d2y);
                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c > 0.0f && c < 1.0f && t > 0 && t < 1.0f;// use >= and <= to include edge points

                    if (intersects)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SegmentsintersectArrayInclusive(float3 a1, float3 a2, List<float3> vertices)
        {
            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float d2x, d2y;

            float3 o2, b2;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            b2 = vertices[0];

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = b2;
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = (d1y * d2x) - (d1x * d2y);
                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c >= 0.0f && c <= 1.0f && t > 0 && t < 1.0f;// use >= and <= to include edge points

                    if (intersects)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //Doesn't checks the segment in vertices if a1 or a2 is inside.
        private static bool SegmentsintersectDiscardingPoints(float3 a1, float3 a2, List<float3> vertices)
        {
            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float d2x, d2y;

            float3 o2, b2;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            b2 = vertices[0];

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = b2;
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                if (b2.Equals(a1) || b2.Equals(a2) || o2.Equals(a1) || o2.Equals(a2))
                    continue;

                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = (d1y * d2x) - (d1x * d2y);
                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c > 0.0f && c < 1.0f && t > 0 && t < 1.0f;// use >= and <= to include edge points

                    if (intersects)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int SegmentsintersectCountExclusive(float3 a1, float3 a2, List<float3> vertices)
        {
            int timesintersected = 0;

            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float d2x, d2y;

            float3 o2, b2;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = vertices[i];
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = (d1y * d2x) - (d1x * d2y);
                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c > 0.0f && c < 1.0f && t > 0.0f && t < 1.0f;// use >= and <= to include edge points

                    if (intersects)
                    {
                        timesintersected++;
                    }
                }
            }

            return timesintersected;
        }

        private static int SegmentsintersectCountInclusive(float3 a1, float3 a2, List<float3> vertices)
        {
            int timesintersected = 0;

            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float d2x, d2y;

            float3 o2, b2;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = vertices[i];
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = (d1y * d2x) - (d1x * d2y);
                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c >= 0.0f && c <= 1.0f && t >= 0.0f && t <= 1.0f;// use >= and <= to include edge points

                    if (intersects)
                    {
                        timesintersected++;
                    }
                }
            }

            return timesintersected;
        }

        private struct IntersectionData
        {
            public float3 pos;
            public int index;
            public float distance;
        }

        private static bool SegmentsintersectPositionsAndFixCornerCases(float3 a1, float3 a2, List<float3> vertices, List<IntersectionData> intersections, out bool verticesFixed)
        {
            verticesFixed = false;

            float d1x = a2.x - a1.x;
            float d1y = a2.y - a1.y;

            float squaredDistanceFirstSegment = (d1x * d1x) + (d1y * d1y);

            float3 d1Normalized = math.normalize(new float3(d1x, d1y, 0.0f));
            float3 up = new float3(0.0f, 0.0f, 1.0f);

            float3 b1, b2;
            float d2x, d2y;

            bool hasintersected = false;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            b2 = vertices[0];

            for (int i = 0; i < verticesCount; i++)
            {
                int nextIndex = i == verticesCountMinusOne ? 0 : i + 1;

                b1 = b2;
                b2 = vertices[nextIndex];

                d2x = b2.x - b1.x;
                d2y = b2.y - b1.y;

                float denominator = ((d1y * d2x) - (d1x * d2y));

                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (a1.x - b1.x)) + (d1x * (b1.y - a1.y))) / denominator;
                    float t = -((d2y * (b1.x - a1.x)) + (d2x * (a1.y - b1.y))) / denominator;

                    float3 intersectionPos = new float3((a1.x + (d1x * t)), (a1.y + (d1y * t)), 0.0f);

                    bool tInRange = t > 0.0f && t < 1.0f;
                    bool cInRange = c > 0.0f && c < 1.0f;
                    bool cIsMax = c == 1.0f;

                    bool intersects = cInRange && tInRange;

                    bool intersectionMatchWithSomeVertex = intersectionPos.Equals(a2) || intersectionPos.Equals(a1) || intersectionPos.Equals(b1) || intersectionPos.Equals(b2);

                    if (intersects && !intersectionMatchWithSomeVertex)
                    {
                        hasintersected = true;

                        IntersectionData data = new IntersectionData() { pos = intersectionPos, distance = t, index = i };

                        if (intersections.Count == 0)
                        {
                            intersections.Add(data);
                        }
                        else
                        {
                            bool found = false;

                            for (int j = 0; j < intersections.Count; j++)
                            {
                                if (data.distance < intersections[j].distance)
                                {
                                    intersections.Insert(j, data);
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                intersections.Add(data);
                        }
                    }
                    else
                    {
                        bool intersectsEndVertexFromList = cIsMax && tInRange;
                        if (intersectsEndVertexFromList)
                        {
                            //Fix position when single vertex has intersected by modifying the vertices array
                            float3 offset = new float3(d2x, d2y, 0.0f);
                            offset = math.normalize(offset) * 0.005f;
                            vertices[nextIndex] += offset;
                            verticesFixed = true;
                            return false;
                        }
                        else if (intersectionMatchWithSomeVertex && intersects)
                        {
                            float3 fixOffset = -math.cross(math.normalizesafe(b2 - b1), up) * 0.01f;
                            vertices[i] += fixOffset;
                            vertices[nextIndex] += fixOffset;

                            verticesFixed = true;
                            return false;
                        }
                        else if (a2.Equals(b1))
                        {
                            float3 fixOffset = math.normalizesafe(a2 - a1) * 0.01f;
                            vertices[i] += fixOffset;
                            verticesFixed = true;
                            return false;
                        }
                        else if (a1.Equals(b2))
                        {
                            float3 fixOffset = math.normalizesafe(b2 - b1) * 0.01f;
                            vertices[nextIndex] += fixOffset;
                            verticesFixed = true;
                            return false;
                        }
                    }
                }
                else
                {
                    //Fix colinear
                    float3 offset = b1 - a1;
                    float3 offset2 = b2 - a1;
                    float3 offsetDir = math.normalizesafe(offset);
                    if (
                        (
                        (offset.x == 0.0f && offset.y == 0) ||
                        ((offsetDir.x * d1Normalized.x) + (offsetDir.y * d1Normalized.y)) == 1.0f // if dot parallel
                        )
                        &&
                        (
                        ((offset.x * offset.x) + (offset.y * offset.y)) <= squaredDistanceFirstSegment ||
                        ((offset2.x * offset2.x) + (offset2.y * offset2.y)) <= squaredDistanceFirstSegment
                        )//if it's less distance
                        )
                    {
                        float3 fixOffset = -math.cross(d1Normalized, up) * 0.01f;
                        vertices[i] += fixOffset;
                        vertices[nextIndex] += fixOffset;

                        verticesFixed = true;
                        return false;
                    }
                }
            }

            return hasintersected;
        }

        private static bool SegmentsintersectPositions(float3 a1, float3 a2, List<float3> vertices, List<IntersectionData> intersections)
        {
            float3 o1 = a1;

            float d1x = a2.x - o1.x;
            float d1y = a2.y - o1.y;

            float3 o2, b2;
            float d2x, d2y;

            bool hasintersected = false;

            int verticesCount = vertices.Count;
            int verticesCountMinusOne = verticesCount - 1;

            b2 = vertices[0];

            for (int i = 0; i < verticesCount; i++)
            {
                o2 = b2;
                b2 = vertices[i == verticesCountMinusOne ? 0 : i + 1];

                d2x = b2.x - o2.x;
                d2y = b2.y - o2.y;

                float denominator = ((d1y * d2x) - (d1x * d2y));

                if (denominator > 0.00000001f || denominator < -0.00000001f)
                {
                    float c = ((d1y * (o1.x - o2.x)) + (d1x * (o2.y - o1.y))) / denominator;
                    float t = -((d2y * (o2.x - o1.x)) + (d2x * (o1.y - o2.y))) / denominator;

                    bool intersects = c > 0.0f && c < 1.0f && t > 0 && t < 1.0f;

                    if (intersects)
                    {
                        hasintersected = true;

                        IntersectionData data = new IntersectionData() { pos = new float3(o1.x + (d1x * t), o1.y + (d1y * t), 0.0f), distance = t, index = i };

                        if (intersections.Count == 0)
                        {
                            intersections.Add(data);
                        }
                        else
                        {
                            bool found = false;

                            for (int j = 0; j < intersections.Count; j++)
                            {
                                if (data.distance < intersections[j].distance)
                                {
                                    intersections.Insert(j, data);
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                                intersections.Add(data);
                        }
                    }
                }
            }

            return hasintersected;
        }

        private static void ModifyVertexOverlaps(List<float3> A, List<float3> B)
        {
            const float distance = 0.01f;
            int rectAngles = 1;

            int ACount = A.Count;
            int BCount = B.Count;

            for (int i = 0; i < ACount; i++)
            {
                float3 a = A[i];
                for (int j = 0; j < BCount; j++)
                {
                    float3 b = B[j];

                    if (a.Equals(b))
                    {
                        float angle = (++rectAngles) * Mathf.PI * 0.5f + Mathf.PI * 0.35f;
                        B[j] += new float3(math.cos(angle) * distance, math.sin(angle) * distance, 0.0f);
                    }
                }
            }
        }
        private bool IsinFrame(List<float3> polygonPoints)
        {
            if (initialFramePoints == null)
                return false;
            for (int k = 0; k < polygonPoints.Count; k++)
            {
                float3 pos = polygonPoints[k];
                for (int i = 0; i < initialFramePoints.Length; i++)
                {
                    if (pos.Equals(initialFramePoints[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void Scale(float scale)
        {
            Scale(vertices, scale);
        }
        public void Scale(List<float3> verticesToScale, float scale)
        {
            if (scale == 1.0f)
                return;
            float3 centralPoint = float3.zero;

            for (int i = 0; i < verticesToScale.Count; i++)
            {
                centralPoint += verticesToScale[i];
            }

            centralPoint /= verticesToScale.Count;

            for (int i = 0; i < verticesToScale.Count; i++)
            {
                verticesToScale[i] = ((verticesToScale[i] - centralPoint) * scale) + centralPoint;
            }
        }
        public void ScaleFromCenter(float scale)
        {
            if (scale == 1.0f)
                return;

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] *= scale;
            }

            for (int i = 0; i < holes.Count; i++)
            {
                var hole = holes[i];

                for (int j = 0; j < hole.Count; j++)
                    hole[j] *= scale;
            }
        }
        public void AddOffset(float3 offset)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] += offset;
            }
        }
        #endregion

        private const float roundValue = 1000.0f;
        private static float Round(float inFloat)
        {
            return math.round(inFloat * roundValue) / roundValue;
        }

        private static float Round(float inFloat, float roundValue)
        {
            return math.round(inFloat * roundValue) / roundValue;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void DebugBounds(Transform transform)
        {
            for (int i = 0; i < holes.Count; i++)
            {
                HoleData data = new HoleData();
                data.Create(holes[i], i);
                data.DebugBounds(transform);
            }
        }
    }
}
