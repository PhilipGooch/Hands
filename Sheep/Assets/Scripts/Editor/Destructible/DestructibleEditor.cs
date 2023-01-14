using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DestructibleEditor : EditorWindow, ISerializationCallbackReceiver
{
    [SerializeField]
    GameObject currentSelection = null;
    [SerializeField]
    List<Joint> selectionJoints = new List<Joint>();
    [SerializeField]
    List<Rigidbody> selectionRigidbodies = new List<Rigidbody>();
    [SerializeField]
    GUIStyle guiStyle = new GUIStyle();

    [MenuItem("Tools/Sheep/Destructible Editor...")]
    static void Init()
    {
        var window = (DestructibleEditor)GetWindow(typeof(DestructibleEditor));
        window.Show();
    }

    private void Awake()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        guiStyle.normal.textColor = Color.white;
        OnSelectionChange();
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    public void OnBeforeSerialize()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public void OnAfterDeserialize()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }


    private void OnSelectionChange()
    {
        currentSelection = Selection.activeGameObject;
        selectionJoints.Clear();
        selectionRigidbodies.Clear();
        if (currentSelection != null)
        {
            selectionJoints.AddRange(currentSelection.GetComponentsInChildren<Joint>());
            selectionRigidbodies.AddRange(currentSelection.GetComponentsInChildren<Rigidbody>());
        }

        Repaint();
    }

    float targetJointStrength = 1000f;
    float targetRigidbodyMass = 10f;
    float targetRigidbodyDensity = 1f;

    bool showJointInfo = true;
    bool showRigidbodyInfo = true;

    void OnGUI()
    {
        if (currentSelection != null)
        {
            EditorGUIUtils.ButtonAndFloatField("Set joint strength", UpdateJointStrength, ref targetJointStrength);
            EditorGUIUtils.ButtonAndFloatField("Set rigidbody mass", UpdateRigidbodyMass, ref targetRigidbodyMass);
            EditorGUIUtils.ButtonAndFloatField("Set mass from density", UpdateRigidbodyDensity, ref targetRigidbodyDensity);

            EditorGUILayout.Space();

            GUILayout.Label("GUI Visibility Settings");
            EditorGUILayout.BeginHorizontal();
            HandleVisibilityToggleChange(ref showJointInfo, GUILayout.Toggle(showJointInfo, "Show Joint Info"));
            HandleVisibilityToggleChange(ref showRigidbodyInfo, GUILayout.Toggle(showRigidbodyInfo, "Show Rigidbody Info"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (GUILayout.Button("Process into destructible"))
            {
                DestructibleProcessor.ProcessDestructible(currentSelection, targetJointStrength);
                OnSelectionChange();
            }
        }
    }

    void UpdateJointStrength(float strength)
    {
        foreach(var joint in selectionJoints)
        {
            Undo.RecordObject(joint, "Adjust joint strength");
            joint.breakForce = strength;
            joint.breakTorque = strength;
        }
        SceneView.RepaintAll();
    }

    void UpdateRigidbodyMass(float mass)
    {
        var allRigs = currentSelection.GetComponentsInChildren<Rigidbody>();
        foreach(var rig in allRigs)
        {
            Undo.RecordObject(rig, "Adjust rigidbody mass");
            rig.mass = mass;
        }
        SceneView.RepaintAll();
    }

    void UpdateRigidbodyDensity(float density)
    {
        var allRigs = currentSelection.GetComponentsInChildren<Rigidbody>();
        foreach (var rig in allRigs)
        {
            Undo.RecordObject(rig, "Adjust rigidbody density");
            rig.SetDensity(density);
            //Somehow unity loses the mass set from setDensity
            rig.mass = rig.mass;
        }
        SceneView.RepaintAll();
    }

    void HandleVisibilityToggleChange(ref bool targetValue, bool newValue)
    {
        if (targetValue != newValue)
        {
            targetValue = newValue;
            // Repaint scene view with newly hidden/visible handles
            SceneView.RepaintAll();
        }
    }

    void OnSceneGUI(SceneView sceneView)
    {
        const float circleRadius = 0.25f;
        var prevColor = Handles.color;
        var cameraPos = sceneView.camera.transform.position;

        Handles.color = Color.cyan;
        if (showJointInfo)
        {
            foreach (var joint in selectionJoints)
            {
                if (joint != null)
                {
                    var rig = joint.GetComponent<Rigidbody>();
                    var firstPos = rig.worldCenterOfMass;
                    var labelPos = firstPos;

                    if (joint.connectedBody != null)
                    {
                        var secondPos = joint.connectedBody.worldCenterOfMass;
                        labelPos = (firstPos + secondPos) / 2f;
                        Handles.DrawWireDisc(firstPos, (cameraPos - firstPos).normalized, circleRadius);
                        Handles.DrawWireDisc(secondPos, (cameraPos - secondPos), circleRadius);
                        Handles.DrawLine(firstPos, secondPos);
                    }
                    else
                    {
                        Handles.color = Color.red;
                        Vector3 offset = -Vector3.up * circleRadius * 2f;
                        labelPos += offset;
                        Handles.DrawWireDisc(firstPos + offset, (cameraPos - firstPos).normalized, circleRadius);
                        Handles.color = Color.cyan;
                    }

                    Handles.Label(labelPos, string.Format("Strength: {0}", joint.breakForce), guiStyle);
                }
            }
        }

        if (showRigidbodyInfo)
        {
            foreach(var rig in selectionRigidbodies)
            {
                if (rig == null)
                    continue;
                Handles.Label(rig.worldCenterOfMass, string.Format("Mass: {0}", rig.mass), guiStyle);
            }
        }

        //Handles.BeginGUI();
        //Handles.EndGUI();

        Handles.color = prevColor;
    }
}
