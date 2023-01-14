using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using System;
using System.Reflection;
using Unity.Collections;
using System.Collections.Generic;
using NBG.MeshGeneration;

namespace NBG.PlaneDestructionSystem.Tests
{
    public class DestructionTest
    {
        public static bool generate = false;
        public static Polygon2D lastPolygon;

        public struct PolygonTest
        {
            public bool is3D;
            public override string ToString() => is3D ? "3D" : "2D";
        }

        private static readonly PolygonTest[] testTypes = new PolygonTest[] { new PolygonTest { is3D = true }, new PolygonTest { is3D = false } };

        [UnityTest]
        public IEnumerator SquareHoleTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D smallSquare = new Polygon2D(square.vertices);
            smallSquare.Scale(0.5f);

            square.SubtractPolygon(smallSquare, out _, out _, out _);

            return PolygonPerformTest(square, "SquareWithHole", pt.is3D);
        }

        [UnityTest]
        public IEnumerator SimpleSquareTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
               new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
               }
               );

            return PolygonPerformTest(square, "SimpleSquare", pt.is3D);
        }

        [UnityTest]
        public IEnumerator CornerTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
               new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
               }
               );
            square.FillFramePointsList();
            Polygon2D sub = new Polygon2D(square.vertices);
            sub.AddOffset(new float3(0.5f, 0.5f, 0.0f));
            sub.Scale(0.5f);

            square.SubtractPolygon(sub, out _, out _, out _);

            sub.AddOffset(new float3(-1.0f, -1.0f, 0.0f));
            square.SubtractPolygon(sub, out _, out _, out _);

            return PolygonPerformTest(square, "CornerSquare", pt.is3D);
        }

        [UnityTest]
        public IEnumerator EdgeTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
               new float3[] {
                            new float3(0.0f, 0.0f, 0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
               }
               );
            square.FillFramePointsList();
            Polygon2D sub = new Polygon2D(square.vertices);
            sub.AddOffset(new float3(0.5f, 0.0f, 0.0f));
            sub.Scale(0.5f);

            square.SubtractPolygon(sub, out _, out _, out _);

            sub.AddOffset(new float3(-1.0f, 0.0f, 0.0f));
            square.SubtractPolygon(sub, out _, out _, out _);

            return PolygonPerformTest(square, "Edge", pt.is3D);
        }

        [UnityTest]
        public IEnumerator EdgeVertexTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
               new float3[] {
                            new float3(0.0f, 0.0f, 0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
               }
               );
            square.FillFramePointsList();

            Polygon2D triangle = new Polygon2D(
                new float3[]
                {
                new float3(1.0f,0.5f,0.0f),
                new float3(2.0f,1.0f,0.0f),
                new float3(2.0f,0.0f,0.0f)
                }
                );

            square.SubtractPolygon(triangle, out _, out _, out _);

            return PolygonPerformTest(square, "EdgeVertex", pt.is3D);
        }

        [UnityTest]
        public IEnumerator CornerVertexTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
               new float3[] {
                            new float3(0.0f, 0.0f, 0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
               }
               );
            square.FillFramePointsList();

            Polygon2D triangle = new Polygon2D(
                new float3[]
                {
                new float3(1.0f,0.5f,0.0f),
                new float3(2.0f,1.0f,0.0f),
                new float3(2.0f,0.0f,0.0f)
                }
                );

            triangle.AddOffset(new float3(0.0f, 0.5f, 0.0f));

            square.SubtractPolygon(triangle, out _, out _, out _);

            return PolygonPerformTest(square, "CornerVertex", pt.is3D);
        }

        [UnityTest]
        public IEnumerator SquareTwoHoleTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D smallSquare = new Polygon2D(square.vertices);
            smallSquare.Scale(0.25f);
            smallSquare.AddOffset(new float3(-0.25f, 0.0f, 0.0f));

            square.SubtractPolygon(smallSquare, out _, out _, out _);

            smallSquare = new Polygon2D(smallSquare.vertices);
            smallSquare.AddOffset(new float3(0.5f, 0.0f, 0.0f));
            square.SubtractPolygon(smallSquare, out _, out _, out _);

            return PolygonPerformTest(square, "SquareWithTwoHole", pt.is3D);
        }

        [UnityTest]
        public IEnumerator SquareManyHolesTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );
            square.FillFramePointsList();

            Polygon2D bigSquare = new Polygon2D(square.vertices);
            bigSquare.Scale(10.0f);

            const int size = 3;
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                {
                    Polygon2D smallSquare = new Polygon2D(square.vertices);
                    smallSquare.AddOffset(new float3(2.0f * i, 2.0f * j, 0.0f));
                    bigSquare.SubtractPolygon(smallSquare, out _, out _, out _);
                }

            return PolygonPerformTest(bigSquare, "SquareManyHoles", pt.is3D);
        }

        [UnityTest]
        public IEnumerator SquareFourHolesTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );
            square.FillFramePointsList();

            Polygon2D bigSquare = new Polygon2D(square.vertices);
            bigSquare.Scale(10.0f);

            const int size = 2;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < size; j++)
                {
                    Polygon2D smallSquare = new Polygon2D(square.vertices);
                    smallSquare.AddOffset(new float3(2.0f * i, 2.0f * j, 0.0f));
                    bigSquare.SubtractPolygon(smallSquare, out _, out _, out _);
                }

            return PolygonPerformTest(bigSquare, "SquareFourHoles", pt.is3D);
        }

        [UnityTest]
        public IEnumerator TotalMatchingTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );
            square.FillFramePointsList();

            Polygon2D otherSquare = new Polygon2D(square.vertices);

            square.SubtractPolygon(otherSquare, out _, out _, out _);

            return PolygonPerformTest(square, "TotalMatching", pt.is3D);
        }

        [UnityTest]
        public IEnumerator MatchingExternalEdgeTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D square = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );
            square.FillFramePointsList();

            Polygon2D otherSquare = new Polygon2D(square.vertices);
            otherSquare.AddOffset(new float3(1.0f, 0.0f, 0.0f));

            square.SubtractPolygon(otherSquare, out _, out _, out _);

            return PolygonPerformTest(square, "MatchingExternalEdge", pt.is3D);
        }

        [UnityTest]
        public IEnumerator ColinearExternalEdgeTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareB = new Polygon2D(
               new float3[] {
                            new float3(1.0f,0.1f,0.0f),
                            new float3(1.0f, 0.9f, 0.0f),
                            new float3(2.0f, 0.9f, 0.0f),
                            new float3(2.0f, 0.1f, 0.0f)
               }
               );

            squareA.SubtractPolygon(squareB, out _, out _, out _);

            return PolygonPerformTest(squareA, "ColinearExternalEdge", pt.is3D);
        }

        [UnityTest]
        public IEnumerator ColinearInternalEdgeTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareB = new Polygon2D(
               new float3[] {
                            new float3(0.5f,0.1f,0.0f),
                            new float3(0.5f, 0.9f, 0.0f),
                            new float3(1.0f, 0.9f, 0.0f),
                            new float3(1.0f, 0.1f, 0.0f)
               }
               );

            squareA.FillFramePointsList();
            squareA.SubtractPolygon(squareB, out _, out _, out _);

            return PolygonPerformTest(squareA, "ColinearInternalEdge", pt.is3D);
        }

        [UnityTest]
        public IEnumerator ColinearCornerTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareB = new Polygon2D(
               new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 0.5f, 0.0f),
                            new float3(0.5f, 0.5f, 0.0f),
                            new float3(0.5f, 0.0f, 0.0f)
               }
               );

            squareA.FillFramePointsList();
            squareA.SubtractPolygon(squareB, out _, out _, out _);

            return PolygonPerformTest(squareA, "ColinearCorner", pt.is3D);
        }

        [UnityTest]
        public IEnumerator ColinearExternalTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareB = new Polygon2D(
               new float3[] {
                            new float3(-0.5f, 0.0f,0.0f),
                            new float3(-0.5f, 0.5f, 0.0f),
                            new float3(0.0f, 0.5f, 0.0f),
                            new float3(0.0f, 0.0f, 0.0f)
               }
               );

            squareA.FillFramePointsList();
            squareA.SubtractPolygon(squareB, out _, out _, out _);

            return PolygonPerformTest(squareA, "ColinearExternal", pt.is3D);
        }

        [UnityTest]
        public IEnumerator ColinearTriangleTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareB = new Polygon2D(
               new float3[] {
                            new float3(0.5f, 0.5f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f)
               }
               );

            squareA.FillFramePointsList();
            squareA.SubtractPolygon(squareB, out _, out _, out _);

            return PolygonPerformTest(squareA, "ColinearTriangle", pt.is3D);
        }

        [UnityTest]
        public IEnumerator PieceDisappearingWithHoleTest([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D squareA = new Polygon2D(
                new float3[] {
                            new float3(0.0f,0.0f,0.0f),
                            new float3(0.0f, 1.0f, 0.0f),
                            new float3(1.0f, 1.0f, 0.0f),
                            new float3(1.0f, 0.0f, 0.0f)
                }
                );

            Polygon2D squareHole = new Polygon2D(
                 new float3[] {
                                    new float3(0.2f,0.2f, 0.0f),
                                    new float3(0.2f, 0.8f, 0.0f),
                                    new float3(0.8f, 0.8f, 0.0f),
                                    new float3(0.8f, 0.2f, 0.0f)
                 }
                 );

            squareA.FillFramePointsList();
            squareA.SubtractPolygon(squareHole, out _, out _, out _);

            Polygon2D shape = new Polygon2D(
                new float3[] {
                                    new float3(0.7f, 0.4f, 0.0f),
                                    new float3(0.7f, 0.6f, 0.0f),
                                    new float3(1.2f, 0.6f, 0.0f),
                                    new float3(1.2f, 0.4f, 0.0f)
                }
                );

            squareA.SubtractPolygon(shape, out _, out var clipPolygons, out _);

            Assert.AreEqual(clipPolygons.Count, 1, "There's not exactly one clippolygon, there's " + clipPolygons.Count + " clip");

            return PolygonPerformTest(clipPolygons[0], "PieceDisappearingWithHole", pt.is3D);
        }

        [UnityTest]
        public IEnumerator DuplicationBug()
        {
            Polygon2D square = new Polygon2D(
                    new float3[] {
                            new float3(-5.0f,-5.0f,0.0f),
                            new float3(-5.0f, 1.0f, 0.0f),
                            new float3(5.0f, 1.0f, 0.0f),
                            new float3(5.0f, -5.0f, 0.0f)
                    }
                    );

            Polygon2D shape = new Polygon2D(
                    new float3[] {
                            new float3(-3.0f,-10.0f,0.0f),
                            new float3(-3.0f, 3.0f, 0.0f),
                            new float3(3.0f, 3.0f, 0.0f),
                            new float3(3.0f, -10.0f, 0.0f)
                    }
                    );

            square.SubtractPolygon(shape, out _, out List<Polygon2D> dynamicPolygons, out _);

            Assert.AreEqual( 1, dynamicPolygons.Count, "There must be just 1 piece");
            Assert.AreEqual(dynamicPolygons[0].vertices.Count, 4, "There must be 4 vertices");
            yield return null;
        }

        [UnityTest]
        public IEnumerator VertexFail([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D polygon = new Polygon2D(
                new float3[] {
                                    new float3(-5.0f,-5.0f,0.0f),
                                    new float3(-5.0f, 1.0f, 0.0f),
                                    new float3(5.0f, 1.0f, 0.0f),
                                    new float3(5.0f, -5.0f, 0.0f)
                }
            );



            Polygon2D hole1 = new Polygon2D(
                new float3[] { new float3(1f, -0.4f, 0f), new float3(1.1f, -0.4f, 0f), new float3(1.2f, -0.5f, 0f), new float3(1.1f, -0.6f, 0f), new float3(1.05f, -0.55f, 0f), new float3(1f, -0.6f, 0f), new float3(0.9f, -0.5f, 0f) }
            );

            Polygon2D hole2 = new Polygon2D(
                new float3[] { new float3(1.1f, -0.3f, 0f), new float3(1.2f, -0.4f, 0f), new float3(1.1f, -0.5f, 0f), new float3(1f, -0.4f, 0f) }
            );

            polygon.SubtractPolygon(hole1, out _, out _, out _);
            polygon.SubtractPolygon(hole2, out _, out _, out _);

            return PolygonPerformTest(polygon, "VertexFail", pt.is3D);
        }

        [UnityTest]
        public IEnumerator VertexFail2([ValueSource(nameof(testTypes))] PolygonTest pt)
        {
            Polygon2D polygon = new Polygon2D(
                new float3[] {
                                    new float3(-5.0f,-5.0f,0.0f),
                                    new float3(-5.0f, 1.0f, 0.0f),
                                    new float3(5.0f, 1.0f, 0.0f),
                                    new float3(5.0f, -5.0f, 0.0f)
                }
            );



            Polygon2D hole1 = new Polygon2D(
                new float3[] {
                    new float3(-0.8924273f, -1.306268f, 0f),
                    new float3(-0.8924273f, -1.166268f, 0f),
                    new float3(-0.757f, -1.083f, 0f),
                    new float3(-0.7041487f, -1.045699f, 0f),
                    new float3(-0.5741487f, -0.9656991f, 0f),
                    new float3(-0.4441487f, -1.045699f, 0f),
                    new float3(-0.428f, -1.115f, 0f),
                    new float3(-0.3724776f, -1.081028f, 0f),
                    new float3(-0.242f, -1.196f, 0f),
                    new float3(-0.2177681f, -1.3611f, 0f),
                    new float3(-0.3477681f, -1.4311f, 0f),
                    new float3(-0.4350415f, -1.520387f, 0f),
                    new float3(-0.4837384f, -1.562187f, 0f),
                    new float3(-0.6137384f, -1.632187f, 0f),
                    new float3(-0.661f, -1.603f, 0f),
                    new float3(-0.8009202f, -1.528174f, 0f),
                    new float3(-0.785f, -1.372f, 0f)
                }
            );

            Polygon2D hole2 = new Polygon2D(
                new float3[] {
                    new float3(-0.3599032f, -1.197878f, 0f),
                    new float3(-0.2299032f, -1.277878f, 0f),
                    new float3(-0.2299032f, -1.427878f, 0f),
                    new float3(-0.3599032f, -1.497878f, 0f),
                    new float3(-0.4899032f, -1.417879f, 0f),
                    new float3(-0.4899032f, -1.277878f, 0f) }
            );

            polygon.SubtractPolygon(hole1, out _, out _, out _);
            polygon.SubtractPolygon(hole2, out _, out _, out _);

            return PolygonPerformTest(polygon, "VertexFail2", pt.is3D);
        }

        #region Perform test
        private const string polygonPropertyName = "polygon";
        private const string verticesPropertyName = "vertices";
        private const string trianglePropertyName = "triangles";

        private IEnumerator PolygonPerformTest(Polygon2D polygon, string name, bool is3DTest)
        {
            lastPolygon?.Dispose();
            lastPolygon = polygon;

            polygon.EarClipping();
            polygon.Extrusion();

            Assert.IsTrue(polygon.pendingJob, "There should be a pending job.");

            while (!polygon.CheckJob())
                yield return null;

            if (generate)
            {
                DestructionTestsCodegen.GeneratePolygonCode(polygon, name);
                generate = false;
            }
            else
            {
                Type testType = Type.GetType("NBG.PlaneDestructionSystem.Codegen." + name);
                if (testType == null)
                {
                    Debug.LogError("Test type " + name + " has not been generated yet.");
                }
                else
                {
                    PropertyInfo testPolygonProperty = testType.GetProperty(polygonPropertyName);
                    PropertyInfo verticesProperty = testType.GetProperty(verticesPropertyName);
                    PropertyInfo trianglesProperty = testType.GetProperty(trianglePropertyName);

                    var vertices = new NativeArray<float3>((float3[])verticesProperty.GetValue(null, null), Allocator.Temp);
                    var triangles = new NativeArray<int>((int[])trianglesProperty.GetValue(null, null), Allocator.Temp);

                    Polygon2D testPolygon = (Polygon2D)testPolygonProperty.GetValue(null, null);
                    testPolygon.extrudedPolygonVertices = vertices;
                    testPolygon.extrudedTriangles = triangles;

                    if (is3DTest)
                        Assert.IsTrue(polygon.Equals3D(testPolygon), "It's not equal in 3D");
                    else
                        Assert.IsTrue(polygon.Equals2D(testPolygon), "It's not equal in 2D");

                    testPolygon.Dispose();
                }
            }

            polygon.Dispose();
        }
        #endregion
    }
}
