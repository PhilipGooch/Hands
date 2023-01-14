using UnityEditor;

namespace NBG.VehicleSystem.Editor
{
    [CustomEditor(typeof(InternalCombustionEngine))]
    public class InternalCombustionEngineEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return EditorApplication.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            var engine = (InternalCombustionEngine)target;

            DrawDefaultInspector();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Telemetry:");
            EditorGUILayout.LabelField("Gear", $"{engine.Gear}");
            EditorGUILayout.LabelField("Accelerator", $"{engine.Accelerator}");
            var chassis = engine.Attachment as IChassis;
            if (chassis != null)
            {
                EditorGUILayout.LabelField("Speed (m/s)", $"{chassis.CurrentSpeed}");
                EditorGUILayout.LabelField("Speed (km/h)", $"{chassis.CurrentSpeed * 3.6f}");
            }
            EditorGUILayout.LabelField("Engine speed (rpm)", $"{engine.DebugEngineRPM}");
            EditorGUILayout.LabelField("Engine torque (Nm)", $"{engine.DebugEngineTorqueNm}");
            EditorGUILayout.LabelField("Transmission torque (Nm)", $"{engine.DebugTransmissionTorqueNm}");
            EditorGUILayout.LabelField("Wheel speed (rpm)", $"{engine.DebugWheelRPM}");
        }
    }
}
