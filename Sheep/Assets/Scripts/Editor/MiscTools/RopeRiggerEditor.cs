using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

public class RopeRiggerEditor : EditorWindow
{
    [SerializeField]
    GameObject currentSelection = null;

    [MenuItem("Tools/Sheep/Rope Rig Generator...")]
    static void Init()
    {
        var window = (RopeRiggerEditor)GetWindow(typeof(RopeRiggerEditor));
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
    float thickness = 0.25f;
    float mass = 1f;
    float angularLimits = 15f;
    float linearSpring = 100000f;
    float linearDamper = 10000f;
    bool createJointForOtherSide = true;
    bool distributeMass = true;
    Vector3 axisModifier = Vector3.up;
    bool useLinearLimits = false;

    void OnGUI()
    {
        if (currentSelection != null)
        {
            thickness = EditorGUILayout.FloatField("Thickness", thickness);
            mass = EditorGUILayout.FloatField("Mass", mass);
            angularLimits = EditorGUILayout.FloatField("Angular Limits", angularLimits);
            useLinearLimits = EditorGUILayout.Toggle("Use Linear Limits", useLinearLimits);
            if (useLinearLimits)
            {
                linearSpring = EditorGUILayout.FloatField("Linear Spring", linearSpring);
                linearDamper = EditorGUILayout.FloatField("Linear Damper", linearDamper);
            }

            createJointForOtherSide = EditorGUILayout.Toggle("Create Joint For Other Side", createJointForOtherSide);
            distributeMass = EditorGUILayout.Toggle("Distribute Mass", distributeMass);

            if (GUILayout.Button("Generate Rig"))
            {
                GenerateRig();
            }

            GUILayout.Space(15f);
            axisModifier = EditorGUILayout.Vector3Field("Axes to use for getting rope axis", axisModifier);
            if (GUILayout.Button("Get Rope Local Axis"))
            {
                PrintRopeLocalAxis(axisModifier);
            }
        }
    }

    void GenerateRig()
    {
        var transform = currentSelection.transform;
        var child = transform.GetChild(0);
        int jointCount = 0;
        Undo.RegisterCompleteObjectUndo(transform.gameObject, "Create Rope Rig Collider");
        while(child != null)
        {
            var length = (child.position - transform.position).magnitude;
            var colliderGO = new GameObject("Collider");
            Undo.RegisterCreatedObjectUndo(colliderGO, "Collider Creation");
            colliderGO.transform.SetParent(transform);
            var capsule = colliderGO.AddComponent<CapsuleCollider>();
            capsule.radius = thickness;
            capsule.height = length;
            colliderGO.transform.position = (transform.position + child.position) / 2f;
            colliderGO.transform.rotation = transform.rotation;
            colliderGO.layer = child.gameObject.layer;

            var parentRig = transform.GetComponentInParent<Rigidbody>();

            AddJoint(transform, parentRig);
            jointCount++;

            transform = child;
            child = GetNextChild(transform);
        }

        if (createJointForOtherSide)
        {
            var lastTransform = transform.parent;
            AddJoint(lastTransform, null);
        }

        if (distributeMass)
        {
            var halfJoints = (jointCount / 2f);
            var lowestMass = mass / halfJoints;
            var rigs = currentSelection.transform.GetComponentsInChildren<Rigidbody>();
            for(int i = 0; i < rigs.Length; i++)
            {
                var distanceFromMiddle = Mathf.Abs(halfJoints - i);
                var progress = distanceFromMiddle / halfJoints;
                var targetMass = Mathf.Lerp(lowestMass, mass, progress);
                rigs[i].mass = targetMass;
            }
        }
    }

    Transform GetNextChild(Transform target)
    {
        if (target.childCount > 0)
        {
            return target.GetChild(0);
        }
        return null;
    }

    ConfigurableJoint AddJoint(Transform target, Rigidbody connectTo)
    {

        var rig = target.GetComponent<Rigidbody>();
        if (rig == null)
        {
            rig = target.gameObject.AddComponent<Rigidbody>();
            Undo.RegisterCreatedObjectUndo(rig, "Add Rigidbody");
        }

        rig.mass = mass;
        var joint = target.gameObject.AddComponent<ConfigurableJoint>();
        Undo.RegisterCreatedObjectUndo(joint, "Add Joint");

        joint.xMotion = joint.zMotion = joint.yMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;


        var highLimit = joint.highAngularXLimit;
        highLimit.limit = angularLimits;
        joint.highAngularXLimit = highLimit;
        var lowLimit = joint.lowAngularXLimit;
        lowLimit.limit = -angularLimits;
        joint.lowAngularXLimit = highLimit;
        var otherLimits = joint.angularYLimit;
        otherLimits.limit = angularLimits;
        joint.angularYLimit = otherLimits;
        joint.angularZLimit = otherLimits;

        if (useLinearLimits)
        {
            joint.yMotion = ConfigurableJointMotion.Limited;
            var linLimit = joint.linearLimit;
            linLimit.limit = 0.001f;
            joint.linearLimit = linLimit;

            var spring = joint.linearLimitSpring;
            spring.spring = linearSpring;
            spring.damper = linearDamper;
            joint.linearLimitSpring = spring;
        }


        joint.rotationDriveMode = RotationDriveMode.Slerp;
        var slerpDrive = joint.slerpDrive;
        slerpDrive.positionSpring = linearSpring;
        slerpDrive.positionDamper = linearDamper;
        joint.slerpDrive = slerpDrive;

        joint.yDrive = slerpDrive;

        joint.connectedBody = connectTo;

        joint.projectionMode = JointProjectionMode.PositionAndRotation;

        return joint;
    }

    void PrintRopeLocalAxis(Vector3 axisModifier)
    {
        if (currentSelection)
        {
            var meshFilter = currentSelection.GetComponentInChildren<MeshFilter>();
            if (meshFilter)
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh)
                {
                    var result = Vector3.Scale(mesh.bounds.size, axisModifier);
                    Debug.Log($"Rope Length: {result.magnitude}");
                    result = result.normalized;
                    Debug.Log($"X: {result.x}   Y: {result.y}   Z:{result.z}");
                }
                else
                {
                    Debug.LogError("No mesh found!");
                }
            }
            else
            {
                Debug.LogError("No mesh filter found!");
            }
        }
        else
        {
            Debug.LogError("Invalid selection!");
        }

    }
}

