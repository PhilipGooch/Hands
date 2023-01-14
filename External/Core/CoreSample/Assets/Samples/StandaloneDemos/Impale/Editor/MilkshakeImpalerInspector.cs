using NBG.Impale;
using UnityEditor;
using UnityEngine;

namespace CoreSample.ImpaleDemo
{
    [CustomEditor(typeof(MilkshakeImpaler), false)]
    [CanEditMultipleObjects]
    public class MilkshakeImpalerInspector : Editor
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
            AddProperty(serializedObject, "ignorePlayers");
            //Milkshake specific properties:
            AddProperty(serializedObject, "impaleOnlyIfGrabbed");
            AddProperty(serializedObject, "allowPullOutIfGrabbed");
            //    AddProperty(serializedObject, "ignoreTags");


            var multiHitImpaling = AddProperty(serializedObject, "multiHitImpaling");
            if (multiHitImpaling.boolValue)
            {
                EditorGUI.indentLevel++;
                AddProperty(serializedObject, "maxHitCount");
                AddProperty(serializedObject, "hitCooldown", "Cooldown Between Hits");
                AddProperty(serializedObject, "minVelocityToCountAsHit");
                EditorGUI.indentLevel--;
            }

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced");
            if (showAdvanced)
            {
                EditorGUI.indentLevel++;
                AddProperty(serializedObject, "lockRotationAroundImpaleAxis");
                AddProperty(serializedObject, "affectedLayers");
                AddProperty(serializedObject, "preventImpalingOtherImpalers");
                AddProperty(serializedObject, "velocityDeadzone");

                AddProperty(serializedObject, "alignWithHitNormal");
                AddProperty(serializedObject, "alignWithNormalAnimDuration");

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private SerializedProperty AddProperty(SerializedObject baseProperty, string propertyName, string displayName = "")
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
