using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace NBG.MeshGeneration
{
    public static class MeshCutting
    {
        public static void Cut(Mesh mesh, CutPlane plane, out Mesh piece1, out Mesh piece2)
        {
            var sides = new NativeList<CutPlane.Side>(1024, Allocator.Temp);

            var newVertices = new NativeList<float3>(1024, Allocator.Temp);
            var newNormals = new NativeList<float3>(1024, Allocator.Temp);
            var newTriangles = new NativeList<int>(1024, Allocator.Temp);

            var otherNewVertices = new NativeList<float3>(1024, Allocator.Temp);
            var otherNewNormals = new NativeList<float3>(1024, Allocator.Temp);
            var otherNewTriangles = new NativeList<int>(1024, Allocator.Temp);

            var closing = new NativeList<float3>(1024, Allocator.Temp);
            var otherClosing = new NativeList<float3>(1024, Allocator.Temp);

            piece1 = new Mesh();
            piece2 = new Mesh();

            ProfilerBeginSampleDetailed("Cuttable.A");
            var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
            var meshData = meshDataArray[0];
            var meshVertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
            meshData.GetVertices(meshVertices);
            var meshNormals = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
            meshData.GetNormals(meshNormals);
            var meshTriangles = new NativeArray<int>(meshData.GetSubMesh(0).indexCount, Allocator.Temp);
            meshData.GetIndices(meshTriangles, 0);

            EndProfilerSampleDetailed();

            ProfilerBeginSampleDetailed("Cuttable.B");
            for (int i = 0; i < meshVertices.Length; i++)
            {
                sides.Add(plane.CalculateSide(meshVertices[i]));
            }
            EndProfilerSampleDetailed();

            ProfilerBeginSampleDetailed("Cuttable.C");
            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                int v1Index = meshTriangles[i];
                int v2Index = meshTriangles[i + 1];
                int v3Index = meshTriangles[i + 2];

                var p1 = meshVertices[v1Index];
                var p2 = meshVertices[v2Index];
                var p3 = meshVertices[v3Index];

                var n1 = meshNormals[v1Index];
                var n2 = meshNormals[v2Index];
                var n3 = meshNormals[v3Index];

                var p1sign = sides[v1Index];
                var p2sign = sides[v2Index];
                var p3sign = sides[v3Index];

                BuildSide(CutPlane.Side.Positive, plane, newVertices, newNormals, newTriangles, closing, p1, p2, p3, n1, n2, n3, p1sign, p2sign, p3sign);
                BuildSide(CutPlane.Side.Negative, plane, otherNewVertices, otherNewNormals, otherNewTriangles, otherClosing, p1, p2, p3, n1, n2, n3, p1sign, p2sign, p3sign);
            }
            EndProfilerSampleDetailed();

            if (closing.Length != 0)
            {
                ProfilerBeginSampleDetailed("Cuttable.CollapseDoubles");
                CollapseDoubles(closing);
                EndProfilerSampleDetailed();

                ProfilerBeginSampleDetailed("Cuttable.CloseConvexPolygon");
                CloseConvexPolygon(newVertices, newNormals, newTriangles, closing, plane.normal);
                EndProfilerSampleDetailed();
            }

            if (otherClosing.Length != 0)
            {
                ProfilerBeginSampleDetailed("Cuttable.CollapseDoubles");
                CollapseDoubles(otherClosing);
                EndProfilerSampleDetailed();

                ProfilerBeginSampleDetailed("Cuttable.CloseConvexPolygon");
                CloseConvexPolygon(otherNewVertices, otherNewNormals, otherNewTriangles, otherClosing, -plane.normal);
                EndProfilerSampleDetailed();
            }

            ProfilerBeginSampleDetailed("Cuttable.D");
            piece1.SetVertices<float3>(newVertices);
            piece1.SetNormals<float3>(newNormals);
            piece1.SetIndices<int>(newTriangles, MeshTopology.Triangles, 0);
            //piece1.RecalculateNormals();

            piece2.SetVertices<float3>(otherNewVertices);
            piece2.SetNormals<float3>(otherNewNormals);
            piece2.SetIndices<int>(otherNewTriangles, MeshTopology.Triangles, 0);
            //piece2.RecalculateNormals();
            EndProfilerSampleDetailed();

            ///
            meshDataArray.Dispose();
            closing.Dispose();

            otherNewTriangles.Dispose();
            otherNewNormals.Dispose();
            otherNewVertices.Dispose();

            newTriangles.Dispose();
            newNormals.Dispose();
            newVertices.Dispose();

            sides.Dispose();
        }

        private static CutPlane BuildSide(CutPlane.Side chosenSide, CutPlane plane, NativeList<float3> newVertices, NativeList<float3> newNormals, NativeList<int> newTriangles, NativeList<float3> closing, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 n1, Vector3 n2, Vector3 n3, CutPlane.Side p1side, CutPlane.Side p2side, CutPlane.Side p3side)
        {
            Assert.AreNotEqual(chosenSide, CutPlane.Side.InsideCut, "This method should never receive InsideCut as side input");

            if (p1side == p2side && p1side == p3side && p1side == chosenSide)
            {
                int firstIndex = newVertices.Length;

                newVertices.Add(p1);
                newVertices.Add(p2);
                newVertices.Add(p3);

                newNormals.Add(n1);
                newNormals.Add(n2);
                newNormals.Add(n3);

                newTriangles.Add(firstIndex);
                newTriangles.Add(firstIndex + 1);
                newTriangles.Add(firstIndex + 2);
            }
            else
            {
                int numberOfVertexOutside = 0;
                numberOfVertexOutside += p1side != chosenSide ? 1 : 0;
                numberOfVertexOutside += p2side != chosenSide ? 1 : 0;
                numberOfVertexOutside += p3side != chosenSide ? 1 : 0;

                if (numberOfVertexOutside != 3)
                {
                    if (numberOfVertexOutside == 1)
                    {
                        float3 outP;
                        float3 innerP1;
                        float3 innerP2;

                        float3 outNormal, innerNormal1, innerNormal2;

                        if (p1side != chosenSide)
                        {
                            outP = p1;
                            innerP1 = p2;
                            innerP2 = p3;

                            outNormal = n1;
                            innerNormal1 = n2;
                            innerNormal2 = n3;
                        }
                        else if (p2side != chosenSide)
                        {
                            outP = p2;
                            innerP1 = p3;
                            innerP2 = p1;

                            outNormal = n2;
                            innerNormal1 = n3;
                            innerNormal2 = n1;
                        }
                        else
                        {
                            outP = p3;
                            innerP1 = p1;
                            innerP2 = p2;

                            outNormal = n3;
                            innerNormal1 = n1;
                            innerNormal2 = n2;
                        }

                        if (plane.IntersectionPoint(chosenSide, out float3 intersectionPoint1, out float3 intersectionNormal1, outP, innerP1, outNormal, innerNormal1) &&
                            plane.IntersectionPoint(chosenSide, out float3 intersectionPoint2, out float3 intersectionNormal2, outP, innerP2, outNormal, innerNormal2))
                        {
                            int firstIndex = newVertices.Length;

                            //Main piece
                            newVertices.Add(intersectionPoint1);
                            newVertices.Add(innerP1);
                            newVertices.Add(innerP2);
                            newVertices.Add(intersectionPoint2);

                            newNormals.Add(intersectionNormal1);
                            newNormals.Add(innerNormal1);
                            newNormals.Add(innerNormal2);
                            newNormals.Add(intersectionNormal2);

                            newTriangles.Add(firstIndex);
                            newTriangles.Add(firstIndex + 1);
                            newTriangles.Add(firstIndex + 2);

                            newTriangles.Add(firstIndex + 2);
                            newTriangles.Add(firstIndex + 3);
                            newTriangles.Add(firstIndex + 0);

                            //Closing
                            closing.Add(intersectionPoint1);
                            closing.Add(intersectionPoint2);
                        }
                    }
                    else if (numberOfVertexOutside == 2)
                    {
                        float3 inP;
                        float3 outP1;
                        float3 outP2;

                        float3 inNormal, outNormal1, outNormal2;


                        if (p1side == chosenSide)
                        {
                            inP = p1;
                            outP1 = p2;
                            outP2 = p3;

                            inNormal = n1;
                            outNormal1 = n2;
                            outNormal2 = n3;
                        }
                        else if (p2side == chosenSide)
                        {
                            inP = p2;
                            outP1 = p3;
                            outP2 = p1;

                            inNormal = n2;
                            outNormal1 = n3;
                            outNormal2 = n1;
                        }
                        else
                        {
                            inP = p3;
                            outP1 = p1;
                            outP2 = p2;

                            inNormal = n3;
                            outNormal1 = n1;
                            outNormal2 = n2;
                        }

                        if (plane.IntersectionPoint(chosenSide, out float3 intersectionPoint1, out float3 intersectionNormal1, inP, outP1, inNormal, outNormal1) &&
                            plane.IntersectionPoint(chosenSide, out float3 intersectionPoint2, out float3 intersectionNormal2, inP, outP2, inNormal, outNormal2))
                        {
                            int firstIndex = newVertices.Length;

                            newVertices.Add(inP);
                            newVertices.Add(intersectionPoint1);
                            newVertices.Add(intersectionPoint2);

                            newNormals.Add(inNormal);
                            newNormals.Add(intersectionNormal1);
                            newNormals.Add(intersectionNormal2);

                            newTriangles.Add(firstIndex);
                            newTriangles.Add(firstIndex + 1);
                            newTriangles.Add(firstIndex + 2);

                            closing.Add(intersectionPoint1);
                            closing.Add(intersectionPoint2);
                        }
                    }
                }

            }

            return plane;
        }

        [BurstCompile] //TODO: call with function pointers (or directly when in Unity 2021.2+)
        private static void CollapseDoubles(NativeList<float3> closing)
        {
            int closingCount = closing.Length;
            for (int i = 0; i < closingCount; i++)
            {
                var selectedPos = closing[i];
                for (int j = 0; j < i; j++)
                {
                    if (math.distancesq(selectedPos, closing[j]) < 1e-5f)
                    {
                        selectedPos = closing[j];
                        closing[i] = selectedPos;
                        break;
                    }
                }
            }
        }

        [BurstCompile] //TODO: call with function pointers (or directly when in Unity 2021.2+)
        private static void CloseConvexPolygon(NativeList<float3> vertices, NativeList<float3> newNormals, NativeList<int> triangles, NativeList<float3> closing, float3 faceNormal)
        {
            int startClosingIndex = vertices.Length;

            int closingCount = closing.Length;
            for (int i = 0; i < closingCount; i++)
            {
                vertices.Add(closing[i]);
                newNormals.Add(faceNormal);
            }

            int startVertex = startClosingIndex;
            var a = vertices[startVertex];

            for (int i = 2; i < closing.Length; i += 2)
            {
                var b = closing[i];
                var c = closing[i + 1];
                int currentVertex = startClosingIndex + i;
                int nextVertex = startClosingIndex + i + 1;

                bool switchTriangle = math.dot(math.cross(b - a, c - a), faceNormal) < 0.0f;

                triangles.Add(startVertex);
                triangles.Add(switchTriangle ? currentVertex : nextVertex);
                triangles.Add(switchTriangle ? nextVertex : currentVertex);
            }
        }

        [System.Diagnostics.Conditional("CUTTABLE_DETAILED_PROFILING")]
        static void ProfilerBeginSampleDetailed(string name) => Profiler.BeginSample(name);
        [System.Diagnostics.Conditional("CUTTABLE_DETAILED_PROFILING")]
        static void EndProfilerSampleDetailed() => Profiler.EndSample();
    }
}