using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

public class RiggedObjectEditor : EditorWindow, ISerializationCallbackReceiver
{
    [SerializeField]
    GameObject currentSelection = null;
    [SerializeField]
    List<RigidbodyWithJoint> rigidbodiesWithJoint = new List<RigidbodyWithJoint>();

    class RigidbodyWithJoint
    {
        public Rigidbody rig;
        public ConfigurableJoint joint;
        public int depth;

        public RigidbodyWithJoint(Rigidbody rig, ConfigurableJoint joint, int depth)
        {
            this.rig = rig;
            this.joint = joint;
            this.depth = depth;
        }
    }

    [SerializeField]
    GUIStyle guiStyle = new GUIStyle();

    [MenuItem("Tools/Sheep/Rigged Object Editor...")]
    static void Init()
    {
        var window = (RiggedObjectEditor)GetWindow(typeof(RiggedObjectEditor));
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
        rigidbodiesWithJoint.Clear();
        if (currentSelection != null)
        {
            var rigs = currentSelection.GetComponentsInChildren<Rigidbody>();
            foreach (var rig in rigs)
            {
                var joint = rig.GetComponent<ConfigurableJoint>();
                rigidbodiesWithJoint.Add(new RigidbodyWithJoint(rig, joint, GetJointDepth(joint)));
            }
        }

        Repaint();
    }

    int GetJointDepth(ConfigurableJoint joint)
    {
        var parent = joint.connectedBody;
        int depth = 1;
        while (parent != null)
        {
            depth++;
            var parentJoint = parent.GetComponent<ConfigurableJoint>();
            if (parentJoint != null)
            {
                parent = parentJoint.connectedBody;
            }
            else
            {
                parent = null;
            }

            if (depth > 99)
            {
                Debug.LogError("Possible circular joint reference detected in " + joint.name);
                return 1;
            }
        }
        return depth;
    }

    Inertia CalculateInertiaAtPosition(Vector3 position)
    {
        var I = Inertia.zero;
        foreach(var body in rigidbodiesWithJoint)
        {
            I += Inertia.FromRigidAtPointEditor(body.rig, position);
        }
        return I;
    }

    float CalculateInertiaCoefficient(Vector3 position)
    {
        var I = CalculateInertiaAtPosition(position);
        return Mathf.Pow(I.I.m00 / I.mass, inertiaCoefficientScale);
    }

    float frequency = 10000f;
    float dampingSize = 0.5f;
    float maxAngle = 5f;
    float inertiaCoefficientScale = 1f;

    bool showJointInfo = true;
    bool showRigidbodyInfo = true;

    void OnGUI()
    {
        if (currentSelection != null)
        {
            frequency = EditorGUILayout.FloatField("Frequency", frequency);
            dampingSize = EditorGUILayout.FloatField("Damping Size", dampingSize);
            inertiaCoefficientScale = EditorGUILayout.FloatField("Inertia Coefficient Scale", inertiaCoefficientScale);

            if (GUILayout.Button("Apply Settings"))
            {
                UpdateAllJoints();
            }

            EditorGUILayout.Space();

            EditorGUIUtils.ButtonAndFloatField("Max Angle", UpdateMaxAngle, ref maxAngle);

            EditorGUILayout.Space();

            GUILayout.Label("GUI Visibility Settings");
            EditorGUILayout.BeginHorizontal();
            HandleVisibilityToggleChange(ref showJointInfo, GUILayout.Toggle(showJointInfo, "Show Joint Info"));
            HandleVisibilityToggleChange(ref showRigidbodyInfo, GUILayout.Toggle(showRigidbodyInfo, "Show Rigidbody Info"));
            EditorGUILayout.EndHorizontal();
        }
    }

    void UpdateAllJoints()
    {
        foreach(var rigjoint in rigidbodiesWithJoint)
        {
            var joint = rigjoint.joint;
            Undo.RecordObject(joint, "Adjust joint");

            var xInertiaCoef = CalculateInertiaCoefficient(rigjoint.rig.worldCenterOfMass);

            var targetSpring = frequency / xInertiaCoef;
            var targetDamp = targetSpring * dampingSize;
            var xSpring = joint.angularXLimitSpring;
            xSpring.spring = targetSpring;
            xSpring.damper = targetDamp;
            joint.angularXLimitSpring = xSpring;
            joint.angularYZLimitSpring = xSpring;
            var drive = joint.slerpDrive;
            drive.positionSpring = targetSpring;
            drive.positionDamper = targetDamp;
            joint.slerpDrive = drive;
        }
        SceneView.RepaintAll();
    }

    void UpdateMaxAngle(float newAngle)
    {
        foreach(var rigjoint in rigidbodiesWithJoint)
        {
            var joint = rigjoint.joint;
            Undo.RecordObject(joint, "Adjust joint");
            var highLimit = joint.highAngularXLimit;
            highLimit.limit = newAngle;
            joint.highAngularXLimit = highLimit;
            var lowLimit = joint.lowAngularXLimit;
            lowLimit.limit = -newAngle;
            joint.lowAngularXLimit = lowLimit;
            joint.angularYLimit = highLimit;
            joint.angularZLimit = highLimit;
        }
        SceneView.RepaintAll();
    }

    void UpdateRigidbodyMass(float mass)
    {
        var allRigs = currentSelection.GetComponentsInChildren<Rigidbody>();
        foreach (var rig in allRigs)
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

        Handles.color = Color.green;

        foreach (var rigjoint in rigidbodiesWithJoint)
        {
            if (rigjoint != null)
            {
                var rig = rigjoint.rig;
                var joint = rigjoint.joint;
                var infoLabel = new StringBuilder();

                if (showJointInfo)
                {
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

                    infoLabel.Append(string.Format("Spring: {0}\nDamper: {1}\nAngle: {2}\n",
                        joint.angularXLimitSpring.spring,
                        joint.angularXLimitSpring.damper,
                        joint.highAngularXLimit.limit));

                }
                if (showRigidbodyInfo)
                {
                    if (rig != null)
                    {
                        infoLabel.Append(string.Format("Mass: {0}\nInertia Coef: {1}", rig.mass, CalculateInertiaCoefficient(rig.worldCenterOfMass)));
                    }
                }

                if (infoLabel.Length > 0)
                {
                    Handles.Label(rig.worldCenterOfMass, infoLabel.ToString(), guiStyle);
                }
            }
        }
        Handles.color = prevColor;
    }
}
