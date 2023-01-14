using NBG.MeshGeneration;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace NBG.PlaneDestructionSystem
{
    [CustomEditor(typeof(Polygon2DAsset))]
    public class Polygon2DAssetCreationTool : Editor
    {
        public override void OnInspectorGUI()
        {
            Polygon2DAsset polygonAsset = (Polygon2DAsset)target;
            if (GUILayout.Button("Add point"))
            {
                if (polygonAsset.points == null)
                    polygonAsset.points = new List<float3>();
                polygonAsset.points.Add(float3.zero);
            }

            if (GUILayout.Button("Recenter"))
            {
                List<float3> points = polygonAsset.points;
                float3 center = float3.zero;
                for (int i = 0; i < points.Count; i++)
                {
                    center += points[i];
                }
                center /= points.Count;

                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = points[i] - center;
                }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◄"))
            {
                ApplyOffset(new float3(-0.05f, 0.0f, 0.0f));
            }

            GUILayout.BeginVertical();
            if (GUILayout.Button("▲"))
            {
                ApplyOffset(new float3(0.0f, 0.05f, 0.0f));
            }
            if (GUILayout.Button("▼"))
            {
                ApplyOffset(new float3(0.0f, -0.05f, 0.0f));
            }
            GUILayout.EndVertical();

            if (GUILayout.Button("►"))
            {
                ApplyOffset(new float3(0.05f, 0.0f, 0.0f));
            }
            GUILayout.EndHorizontal();
            base.OnInspectorGUI();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            if (gizmoPoints == null)
                gizmoPoints = new List<float3>(10);
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorUtility.SetDirty(target);
        }

        private List<float3> gizmoPoints;


        void OnSceneGUI(SceneView scene)
        {


            Polygon2DAsset polygonAsset = (Polygon2DAsset)target;

            if (polygonAsset.points != null)
            {
                Camera sceneCamera = scene.camera;

                float3 forward = sceneCamera.transform.forward;
                float3 right = sceneCamera.transform.right;
                float3 up = sceneCamera.transform.up;

                float3 offset = forward * 2.0f + new float3(sceneCamera.transform.position);

                float3 position2D, position3D, newPosition3D, newPosition2DWithoutOffset;

                gizmoPoints.Clear();

                for (int i = 0; i < polygonAsset.points.Count; i++)
                {
                    position2D = polygonAsset.points[i];
                    position3D = offset + position2D.x * right + position2D.y * up;
                    newPosition3D = Handles.PositionHandle(position3D, sceneCamera.transform.rotation);
                    gizmoPoints.Add(newPosition3D);

                    newPosition2DWithoutOffset = newPosition3D - offset;

                    polygonAsset.points[i] = new float3(math.dot(newPosition2DWithoutOffset, right), math.dot(newPosition2DWithoutOffset, up), 0.0f);

                }


                for (int i = 0; i < gizmoPoints.Count - 1; i++)
                {
                    Handles.DrawLine(gizmoPoints[i], gizmoPoints[i + 1]);
                }
                if (gizmoPoints.Count > 1)
                    Handles.DrawLine(gizmoPoints[gizmoPoints.Count - 1], gizmoPoints[0]);

                Handles.color = Color.red;
                Handles.DrawLine(offset + 0.0f * right + 0.1f * up, offset + 0.0f * right - 0.1f * up);
                Handles.DrawLine(offset + 0.1f * right + 0.0f * up, offset + -0.1f * right + 0.0f * up);
            }
        }

        private void ApplyOffset(float3 offset)
        {
            Polygon2DAsset polygonAsset = (Polygon2DAsset)target;
            for (int i = 0; i < polygonAsset.points.Count; i++)
            {
                polygonAsset.points[i] = polygonAsset.points[i] + offset;
            }
        }

    }
}
