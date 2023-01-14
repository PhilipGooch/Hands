using UnityEditor;
using UnityEngine;

namespace NBG.Impale
{
    [CustomEditor(typeof(Impaler), false)]
    [CanEditMultipleObjects]
    public class ImpalerInspector : Editor
    {
        bool showAdvanced = false;
        public override void OnInspectorGUI()
        {
            DrawInspector();
        }

        protected virtual void DrawInspector()
        {
            AddProperty(serializedObject, "impalerCollider");
            AddProperty(serializedObject, "impaleStartLocal");
            AddProperty(serializedObject, "impaleDirection");

            EditorGUILayout.LabelField("Impaler Dimensions", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var shapeEnum = AddProperty(serializedObject, "impalerShape");
            if ((ImpalerShape)shapeEnum.enumValueIndex == ImpalerShape.Capsule)
                AddProperty(serializedObject, "impalerRadius");
            else if ((ImpalerShape)shapeEnum.enumValueIndex == ImpalerShape.Box)
                AddProperty(serializedObject, "impalerDimensions");
            AddProperty(serializedObject, "impalerLength");
            EditorGUI.indentLevel--;

            AddProperty(serializedObject, "impaledObjectsCount");
            AddProperty(serializedObject, "jointBreakForce");
            AddProperty(serializedObject, "minVelocityToStartImpale");


            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                AddProperty(serializedObject, "affectedLayers");
                AddProperty(serializedObject, "lockRotationAroundImpaleAxis");
                AddProperty(serializedObject, "preventImpalingOtherImpalers");
                AddProperty(serializedObject, "velocityDeadzone");
                AddProperty(serializedObject, "validHitDot");
                AddProperty(serializedObject, "collidersOverlapTolerance");
                var aling = AddProperty(serializedObject, "alignWithHitNormal");
                if (aling.boolValue)
                {
                    EditorGUI.indentLevel++;
                    AddProperty(serializedObject, "alignWithNormalAnimDuration", "Full Align With Normal Duration");
                    AddProperty(serializedObject, "alignWithNormalMulti");
                    EditorGUI.indentLevel--;
                }

                var multiHitImpaling = AddProperty(serializedObject, "multiHitImpaling");
                if (multiHitImpaling.boolValue)
                {
                    EditorGUI.indentLevel++;
                    AddProperty(serializedObject, "maxHitCount");
                    AddProperty(serializedObject, "hitCooldown", "Cooldown Between Hits");
                    AddProperty(serializedObject, "minVelocityToCountAsHit");
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }


            serializedObject.ApplyModifiedProperties();
        }

        protected SerializedProperty AddProperty(SerializedObject baseProperty, string propertyName, string displayName = "")
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