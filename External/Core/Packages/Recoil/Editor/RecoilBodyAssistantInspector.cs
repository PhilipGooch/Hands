using UnityEditor;

namespace Recoil
{
    [CustomEditor(typeof(RecoilBodyAssistant))]
    public class RecoilBodyAssistantInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var rb = (RecoilBodyAssistant)target;

            if (rb.Id == Recoil.World.environmentId)
            {
                EditorGUILayout.LabelField("Body is not registered");
            }
            else
            {
                var body = rb._bodyDump;
                var energyRecoil = body._lastE;
                var energyPhysX = body._lastEphysX;
                var recoilSleepAllowed = body.sleepAllowed;
                var recoilSleepFrames = body.sleepFrameCounter;
                var physxIsSleeping = rb._physxIsSleeping;

                EditorGUIUtility.labelWidth = 160;
                EditorGUILayout.LabelField("Recoil body id", $"{rb.Id}");
                EditorGUILayout.LabelField("E (according to Recoil)", $"{energyRecoil}");
                EditorGUILayout.LabelField("E (according to PhysX)", $"{energyPhysX}");
                EditorGUILayout.LabelField("Frames Recoil would sleep", $"{recoilSleepFrames}");
                EditorGUILayout.LabelField("Is Recoil sleep allowed?", recoilSleepAllowed ? "YES" : "NO");
                EditorGUILayout.LabelField("Is PhysX sleeping?", physxIsSleeping ? "YES" : "NO");
            }

            Repaint();
        }
    }
}
