using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace NBG.PlaneDestructionSystem.Tests
{
    [CustomEditor(typeof(BreakableWall))]
    public class BreakableWallEditor : Editor
    {
        private List<MethodInfo> testMethods;
        private static bool pendingPreview;

        private bool isOpen = false;

        private void OnEnable()
        {
            Initialize();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void Initialize()
        {
            testMethods = new List<MethodInfo>();

            Type t = typeof(DestructionTest);
            var methods = t.GetMethods();

            foreach (var method in methods)
            {
                var attributes = method.CustomAttributes;
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeType == typeof(UnityEngine.TestTools.UnityTestAttribute))
                    {
                        testMethods.Add(method);
                        break;
                    }
                }
            } 
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool newIsFolded = EditorGUILayout.Foldout(isOpen, "Test interface");

            if (newIsFolded == true && isOpen == false)
            {
                isOpen = EditorUtility.DisplayDialog("Test interface", "You're opening the testing interface, you can break tests with these options. Are you sure?", "Yes", "No");
            }
            else
                isOpen = newIsFolded;

            if (isOpen)
            {
                BreakableWall wall = target as BreakableWall;
                object[] parameters = new object[] { new DestructionTest.PolygonTest { } };

                GUILayout.BeginVertical();

                for (int i = 0; i < testMethods.Count; i++)
                {
                    var testMethod = testMethods[i];

                    GUILayout.BeginHorizontal();
                    GUI.enabled = !DestructionTest.generate;
                    if (GUILayout.Button("Preview", GUILayout.Width(70)))
                    {
                        pendingPreview = true;
                        IEnumerator enumerator = (IEnumerator)testMethod.Invoke(new DestructionTest(), parameters);
                        wall.StartCoroutine(enumerator);
                    }
                    if (GUILayout.Button("Generate", GUILayout.Width(70)))
                    {
                        pendingPreview = true;
                        DestructionTest.generate = true;
                        IEnumerator enumerator = (IEnumerator)testMethod.Invoke(new DestructionTest(), parameters);
                        wall.StartCoroutine(enumerator);
                    }

                    GUILayout.Label(testMethod.Name);
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
        }

        private void OnSceneGUI(SceneView scene)
        {
            if ((pendingPreview) && !Application.isPlaying && DestructionTest.lastPolygon != null)
            {
                BreakableWall wall = (BreakableWall)target;
                if (DestructionTest.lastPolygon.IsJobComplete())
                {
                    wall.SetPolygonMesh(DestructionTest.lastPolygon);
                    wall.polygon = DestructionTest.lastPolygon;

                    pendingPreview = false;
                }
            }
        }
    }
}
