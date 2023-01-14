using System.Collections;
using System.Collections.Generic;
using NBG.PlaneDestructionSystem;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System;
using System.Reflection;
using NBG.MeshGeneration;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace NBG.PlaneDestructionSystem.Tests
{
    public class CutTests
    {
        private const string guidToSceneTest = "95ff644ea24fe5c419da3351bb4c145f";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(AssetDatabase.GUIDToAssetPath(guidToSceneTest), new LoadSceneParameters());
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
#endif
        }

        [UnityTest]
        public IEnumerator CutTest([ValueSource(nameof(SelectedSeeds))] uint seed)
        {
            yield return SetupWall();

            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            Transform transform = (new GameObject("TestCutPoint")).transform;

            transform.parent = wall.transform;
            transform.localPosition = Vector3.zero;

            wall.SetTarget(transform);
            wall.materialType = BreakableWall.MaterialType.Wood;
            wall.cutRadious = 0.15f;
            wall.pour = false;
            wall.rayDistance = 1.0f;
            wall.sides = 6;

            UnityEngine.Random.seed = (int)seed;
            float frequency = UnityEngine.Random.Range(0.01f,0.5f);
            float amplitude = 0.2f;

            Debug.Log(frequency);

            for (float i = 0; i < 500.0f; i++)
            {
                float distance = Mathf.Sin(frequency * i)* amplitude + 1.0f;
                float angleInRad = i * Mathf.Deg2Rad;
                transform.localPosition = new Vector3(Mathf.Cos(angleInRad) * distance, Mathf.Sin(angleInRad) * distance - 0.5f, 0.0f);
                yield return new WaitForSeconds(0.01f);
            }

            yield return new WaitForSeconds(0.2f);

            var pieces = GameObject.FindObjectsOfType<ProceduralPiece>();

            foreach (var piece in pieces)
            {
                Assert.IsFalse(piece.HasVertexFurtherAwayThan(), "Huge piece error detected");
            }

            Assert.NotZero(pieces.Length, "There're no generated pieces");

            foreach (var piece in pieces)
            {
                GameObject.Destroy(piece.gameObject);
            }

            wall.polygon.Dispose();
        }


        static IEnumerable<uint> GetCutSeeds()
        {
            for (uint i = 0; i < 1000; i++)
                yield return i;
        }

        static IEnumerable<uint> SelectedSeeds()
        {
            uint[] seeds = new uint[]
            {
                120,
                209,
                388,
                965,
                982
            };

            for (uint i = 0; i < seeds.Length; i++)
                yield return seeds[i];
        }

        public static IEnumerator SetupWall()
        {
            BreakableWall wall = GameObject.FindObjectOfType<BreakableWall>();

            wall.transform.position = new Vector3(0.0f, 5.0f, 3.0f);
            wall.transform.rotation = Quaternion.identity;

            Polygon2D square = new Polygon2D(
                    new float3[] {
                            new float3(-5.0f,-5.0f,0.0f),
                            new float3(-5.0f, 1.0f, 0.0f),
                            new float3(5.0f, 1.0f, 0.0f),
                            new float3(5.0f, -5.0f, 0.0f)
                    }
                    );

            square.FillFramePointsList();

            wall.ApplyPolygon(square, wallAttachment: BreakableWall.WallAttachment.Bottom);

            while (!wall.polygon.IsJobComplete())
                yield return null;
        }
    }
}
