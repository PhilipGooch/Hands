using UnityEditor;
using UnityEngine;

namespace NBG.VehicleSystem.Editor
{
    [CustomEditor(typeof(PhysicalChassis))]
    public class PhysicalChassisEditor : UnityEditor.Editor
    {
        SerializedProperty _ownRigidbody;
        SerializedProperty useRearWheelsForTurningPivot;
        SerializedProperty turningCenterForwardOffset;
        SerializedProperty turningRadius;
        SerializedProperty maxSpeed;
        SerializedProperty _COM;
        SerializedProperty _axleSettings;

        private void OnEnable()
        {
            _ownRigidbody = serializedObject.FindProperty("_ownRigidbody");
            useRearWheelsForTurningPivot = serializedObject.FindProperty("useRearWheelsForTurningPivot");
            turningCenterForwardOffset = serializedObject.FindProperty("turningCenterForwardOffset");
            turningRadius = serializedObject.FindProperty("turningRadius");
            maxSpeed = serializedObject.FindProperty("maxSpeed");
            _COM = serializedObject.FindProperty("_COM");
            _axleSettings = serializedObject.FindProperty("_axleSettings");
        }

        public override bool RequiresConstantRepaint()
        {
            return !EditorApplication.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            serializedObject.Update();

            var chassis = (PhysicalChassis)target;
            chassis.VerifyIntegrity();

            EditorGUILayout.PropertyField(_ownRigidbody);
            EditorGUILayout.PropertyField(useRearWheelsForTurningPivot);
            if (!useRearWheelsForTurningPivot.boolValue)
                EditorGUILayout.PropertyField(turningCenterForwardOffset);
            EditorGUILayout.PropertyField(turningRadius);
            EditorGUILayout.PropertyField(maxSpeed);
            EditorGUILayout.PropertyField(_COM);
            EditorGUILayout.PropertyField(_axleSettings);

            serializedObject.ApplyModifiedProperties();
        }

        public void OnSceneGUI()
        {
            var chassis = (PhysicalChassis)target;
            Handles.matrix = chassis.Rigidbody != null ? chassis.Rigidbody.transform.localToWorldMatrix : chassis.transform.localToWorldMatrix;

            // Turning circle center
            var tcPos = Vector3.zero;
            tcPos.z += chassis.TurningCenterForwardOffset;
            tcPos.x += chassis.TurningRadius;

            Handles.color = Color.yellow;
            if (!Application.isPlaying)
            {
                Handles.Label(tcPos, "Turning circle center");
                var newTcPos = Handles.PositionHandle(tcPos, Quaternion.identity);
                if (newTcPos != tcPos)
                {
                    Undo.RecordObject(chassis, $"Modify {nameof(PhysicalChassis)} turning center");
                    chassis.TurningCenterForwardOffset = newTcPos.z;
                    chassis.TurningRadius = newTcPos.x;
                    serializedObject.Update();
                }
            }

            // Axles
            if (!Application.isPlaying)
            {
                for (int i = 0; i < chassis.AxleCount; ++i)
                {
                    var axle = chassis.GetAxleSettings(i);

                    if (axle.Guide == null)
                    {
                        var axlePos = Vector3.zero;
                        axlePos.z += axle.ForwardOffset;
                        axlePos.y += axle.VerticalOffset;
                        axlePos.x += axle.HalfWidth;
                        var newAxlePos = Handles.PositionHandle(axlePos, Quaternion.identity);
                        if (newAxlePos != axlePos)
                        {
                            Undo.RecordObject(chassis, $"Modify {nameof(PhysicalChassis)} axle geometry");
                            axle.ForwardOffset = newAxlePos.z;
                            axle.VerticalOffset = newAxlePos.y;
                            axle.HalfWidth = newAxlePos.x;
                            chassis.SetAxleSettings(i, axle);
                            serializedObject.Update();
                        }
                    }
                }
            }
        }
    }
}
