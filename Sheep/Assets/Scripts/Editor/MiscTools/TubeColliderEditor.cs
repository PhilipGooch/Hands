using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

public class TubeColliderEditor : EditorWindow
{
    [SerializeField]
    GameObject currentSelection = null;

    [MenuItem("Tools/Sheep/Tube Collider Generator...")]
    static void Init()
    {
        var window = (TubeColliderEditor)GetWindow(typeof(TubeColliderEditor));
        window.Show();
    }

    private void Awake()
    {
        OnSelectionChange();
    }

    private void OnSelectionChange()
    {
        currentSelection = Selection.activeGameObject;
    }

    float radius = 2f;
    int sides = 24;
    float width = 2f;
    float thickness = 0.25f;


    void OnGUI()
    {
        if (currentSelection != null)
        {
            radius = EditorGUILayout.FloatField("Radius", radius);
            sides = EditorGUILayout.IntField("Sides", sides);
            width = EditorGUILayout.FloatField("Width", width);
            thickness = EditorGUILayout.FloatField("Thickness", thickness);

            if (GUILayout.Button("Generate Colliders"))
            {
                GenerateColliders();
            }
        }
    }

    void GenerateColliders()
    {
        var transform = currentSelection.transform;
        for (int i = 0; i < sides; i++)
        {
            var targetAngle = 360f * ((float)i / sides) * Mathf.Deg2Rad;
            var targetDirection = new Vector3(0, Mathf.Sin(targetAngle), Mathf.Cos(targetAngle)).normalized;
            var targetPos = transform.position + transform.rotation * targetDirection * (radius - thickness / 2f);
            var targetRot = Quaternion.LookRotation(targetDirection);
            var go = new GameObject("Collider");
            Undo.RegisterCreatedObjectUndo(go, "Create Tube Collider");
            go.transform.SetParent(transform);
            go.transform.position = targetPos;
            go.transform.localRotation = targetRot;
            var boxCol = go.AddComponent<BoxCollider>();
            var c = 2 * Mathf.PI * radius;
            var segmentSize = c / sides;
            boxCol.size = new Vector3(width, segmentSize, thickness);
        }
    }
}

