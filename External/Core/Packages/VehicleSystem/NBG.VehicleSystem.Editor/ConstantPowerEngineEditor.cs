using UnityEditor;

namespace NBG.VehicleSystem.Editor
{
    [CustomEditor(typeof(ConstantPowerEngine))]
    public class ConstantPowerEngineEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return EditorApplication.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            var engine = (ConstantPowerEngine)target;

            DrawDefaultInspector();
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Telemetry:");
            EditorGUILayout.LabelField("Gear", $"{engine.Gear}");
            EditorGUILayout.LabelField("Accelerator", $"{engine.Accelerator}");
            EditorGUILayout.LabelField("Speed (m/s)", $"{engine.DebugCurrentSpeedMps}");
            EditorGUILayout.LabelField("Speed (km/h)", $"{engine.DebugCurrentSpeedKph}");
            EditorGUILayout.LabelField("Engine torque (Nm)", $"{engine.DebugEngineTorqueNm}");
            EditorGUILayout.LabelField("Transmission torque (Nm)", $"{engine.DebugTransmissionTorqueNm}");
        }
    }
}
