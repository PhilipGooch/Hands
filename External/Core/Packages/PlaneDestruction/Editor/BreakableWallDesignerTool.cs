using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    [CustomEditor(typeof(BreakableWallDesigner))]
    public class BreakableWallDesignerTool : Editor
    {

        const string mainShapeName = "Main shape";
        const string holesName = "Holes";
        const string holeName = "Hole";

        private SerializedProperty thickness;

        public override void OnInspectorGUI()
        {
            BreakableWallDesigner designer = (BreakableWallDesigner)target;

            GenerateStructure(designer);

            EditorGUILayout.PropertyField(thickness, new GUIContent("Thickness"));

            if (GUILayout.Button("Add hole"))
            {
                Transform holes = designer.transform.GetChild(1);
                GameObject newHole = new GameObject(holeName);

                newHole.transform.parent = holes;
                newHole.transform.localPosition = Vector3.zero;
                newHole.transform.localScale = Vector3.one;
            }

            if (!Application.isPlaying)
            {
                designer.SaveData();
                designer.CreatePolygon();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            thickness = serializedObject.FindProperty("thickness");
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private List<float3> gizmoPoints;


        void OnSceneGUI(SceneView scene)
        {
            if (!Application.isPlaying)
            {
                BreakableWallDesigner designer = (BreakableWallDesigner)target;
                designer.ForceGenerateMesh();
            }
        }

        private void GenerateStructure(BreakableWallDesigner designer)
        {
            if (designer.transform.childCount == 0 || designer.transform.GetChild(0).gameObject.name != mainShapeName)
            {
                GameObject mainShape = new GameObject(mainShapeName);
                GameObject holes = new GameObject(holesName);

                mainShape.transform.parent = designer.transform;
                holes.transform.parent = designer.transform;

                mainShape.transform.localPosition = Vector3.zero;
                holes.transform.localPosition = Vector3.zero;

                mainShape.transform.localScale = Vector3.one;
                holes.transform.localScale = Vector3.one;
            }
        }
    }
}
