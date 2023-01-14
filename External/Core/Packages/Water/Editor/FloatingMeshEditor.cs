using UnityEditor;

namespace NBG.Water
{
    [CustomEditor(typeof(FloatingMesh))]
    public class FloatingMeshEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var fm = (FloatingMesh)target;
            var back = fm.Instance?.Backend;
            
            EditorGUI.BeginDisabledGroup(true);
            if (back == null)
            {
                EditorGUILayout.LabelField("Not simulating");
            }
            else
            {
                EditorGUILayout.LabelField("Is submerged?", $"{fm.Submerged}");
                EditorGUILayout.LabelField("Calculated mass (kg)", $"{back.CalculatedMass}");
                EditorGUILayout.LabelField("Calculated volume (m^3)", $"{back.CalculatedVolume}");
                EditorGUILayout.LabelField("Calculated buoyancy multiplier", $"{back.CalculatedBuoyancyMultiplier}");
                EditorGUILayout.LabelField("Original vertex count", $"{back.OriginalVertexCount}");
                EditorGUILayout.LabelField("Optimized vertex count", $"{back.OptimizedVertexCount}");
                EditorGUILayout.LabelField("Triangle count", $"{back.TriangleCount}");
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
