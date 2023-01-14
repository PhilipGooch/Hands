using UnityEditor;
using UnityEngine;

namespace Plugs
{
    [CustomEditor(typeof(Hole), true)]
    [CanEditMultipleObjects]
    public class HoleInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var holeType = serializedObject.FindProperty("holeType");
            EditorGUILayout.PropertyField(holeType, true);

            AddProperty(serializedObject, "alignTransform");
            AddProperty(serializedObject, "engageDeepGuides");

            if (holeType.enumValueIndex != (int)HoleType.FreeXRotation && holeType.enumValueIndex != (int)HoleType.NoConstraints)
            {
                var preventSnap = AddProperty(serializedObject, "preventSnap");
                if (!preventSnap.boolValue)
                {
                    AddProperty(serializedObject, "plugDist", "Plugged Max Distance");
                    AddProperty(serializedObject, "unplugDist", "Unplug Distance");
                }
            }

            AddProperty(serializedObject, "disengageDistance", "Guides Engage Max Distance");
            AddProperty(serializedObject, "engageStartMaxAngle");

            serializedObject.ApplyModifiedProperties();
        }

        SerializedProperty AddProperty(SerializedObject baseProperty, string propertyName, string displayName = "")
        {
            var property = baseProperty.FindProperty(propertyName);

            if (string.IsNullOrWhiteSpace(displayName))
                EditorGUILayout.PropertyField(property, true);
            else
                EditorGUILayout.PropertyField(property, new GUIContent(displayName), true);

            return property;
        }
    }
}