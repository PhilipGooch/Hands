using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor
{
    [CustomPropertyDrawer(typeof(PIDController))]
    class PIDControllerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
            if (property.isExpanded)
            {
                var pid = (PIDController)property.GetTargetObjectOfProperty();

                EditorGUI.indentLevel++;
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Debug:");
                EditorGUILayout.LabelField("Last Error", pid.DebugLastError.ToString());
                EditorGUILayout.LabelField("Last Output", pid.DebugLastOutput.ToString());
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUI.GetPropertyHeight(property, label);
            return height;
        }
    }
}
