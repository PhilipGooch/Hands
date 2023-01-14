using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System;
using System.Reflection;
using NBG.MeshGeneration;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace NBG.PlaneDestructionSystem.Tests
{
    public class BreakTests
    {
        private const string guidToSceneTest = "95ff644ea24fe5c419da3351bb4c145f";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GUIDToAssetPath(guidToSceneTest), new LoadSceneParameters());
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
            ProceduralPiece.isTesting = true;
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            ProceduralPiece.isTesting = false;
        }

        [UnityTest]
        public IEnumerator DuplicationBug()
        {
            yield return SetupWall();

            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            Polygon2D shape = new Polygon2D(
                    new float3[] {
                            new float3(-3.0f,-10.0f,0.0f),
                            new float3(-3.0f, 3.0f, 0.0f),
                            new float3(3.0f, 3.0f, 0.0f),
                            new float3(3.0f, -10.0f, 0.0f)
                    }
                    );

            float shatterAngle1 = 1.0f;
            float shatterAngle2 = 0.0f;
            float3 pos = float3.zero;

            wall.BreakAndUpdate(
                pos,
                Vector3.forward,
                shape,
                shatterAngle1: shatterAngle1,
                shatterAngle2: shatterAngle2
                );

            yield return new WaitForSeconds(0.1f);

            var pieces = GameObject.FindObjectsOfType<ProceduralPiece>();

            Assert.AreEqual(pieces.Length, 4, "There must be just 4 pieces");

            shape.Dispose();
            wall.polygon.Dispose();

        }

        [UnityTest]
        public IEnumerator AllBreakCases([ValueSource(nameof(GetBreakTests))] Type breakCase)
        {
            return PerformBreakTest(breakCase);
        }

        private static IEnumerable<Type> GetBreakTests()
        {
            var breakTestInterface = typeof(IBreakTest);

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                foreach (var interfaceType in type.GetInterfaces())
                    if (interfaceType == breakTestInterface)
                    {
                        yield return type;
                    }
        }

        private const string shapePropertyName = "shape";
        private const string shatter1PropertyName = "shatter1";
        private const string shatter2PropertyName = "shatter2";
        private const string posPropertyName = "pos";
        private const string wallShapePropertyName = "wallShape";

        private IEnumerator PerformBreakTest(Type testType)
        {
            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            if (testType == null)
            {
                Debug.LogError("Test type " + nameof(testType) + " has not been generated yet.");
            }
            else
            {
                FieldInfo shapeProperty = testType.GetField(shapePropertyName);
                FieldInfo shatter1Property = testType.GetField(shatter1PropertyName);
                FieldInfo shatter2Property = testType.GetField(shatter2PropertyName);
                FieldInfo posProperty = testType.GetField(posPropertyName);
                FieldInfo wallShapeProperty = testType.GetField(wallShapePropertyName);

                Polygon2D shape = new Polygon2D((float3[])shapeProperty.GetValue(null));
                float shatterAngle1 = (float)shatter1Property.GetValue(null);
                float shatterAngle2 = (float)shatter2Property.GetValue(null);
                float3 pos = (float3)posProperty.GetValue(null);

                if (wallShapeProperty != null)
                {
                    float3[] wallShape = (float3[])wallShapeProperty.GetValue(null);
                    yield return SetupWall(wallShape);
                }
                else
                {
                    yield return SetupWall();
                }

                wall.BreakAndUpdate(
                    pos,
                    Vector3.forward,
                    shape,
                    shatterAngle1: shatterAngle1,
                    shatterAngle2: shatterAngle2
                    );

                yield return new WaitForSeconds(0.1f);

                var pieces = GameObject.FindObjectsOfType<ProceduralPiece>();

                foreach (var piece in pieces)
                {
                    Assert.IsFalse(piece.HasVertexFurtherAwayThan(), "Huge piece error detected in " + nameof(testType));
                }

                Assert.NotZero(pieces.Length, "There're no generated pieces in " + nameof(testType));

                foreach (var piece in pieces)
                {
                    GameObject.Destroy(piece.gameObject);
                }

                shape.Dispose();
            }

            wall.polygon.Dispose();
        }

        public static IEnumerator SetupWall(float3[] customWall = null)
        {
            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            wall.transform.position = new Vector3(0.0f, 5.0f, 3.0f);
            wall.transform.rotation = Quaternion.identity;

            Polygon2D square = new Polygon2D(
                    customWall == null ?
                    new float3[] {
                            new float3(-5.0f,-5.0f,0.0f),
                            new float3(-5.0f, 1.0f, 0.0f),
                            new float3(5.0f, 1.0f, 0.0f),
                            new float3(5.0f, -5.0f, 0.0f)
                    }
                    :
                    customWall
                    );

            wall.ApplyPolygon(square, wallAttachment: BreakableWall.WallAttachment.Bottom);

            while (!wall.polygon.IsJobComplete())
                yield return null;
        }
    }
}
