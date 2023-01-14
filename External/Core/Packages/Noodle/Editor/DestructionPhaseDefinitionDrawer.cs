using UnityEditor;
using UnityEngine;

namespace Noodles.Animation
{
    /// <summary>
    /// Visualizes debug data for PhysicalAnimationTrack properties.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicalAnimationTrack))]
    public class DestructionPhaseDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            GUIContent sampleGUIContent = new GUIContent(property.FindPropertyRelative("name").stringValue);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("frames"), sampleGUIContent, true);

            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProperty = property.FindPropertyRelative("frames");
            if (!typeProperty.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight; // single line if not expanded
            }
            else
            {
                return (2.5f + Mathf.Max(1, typeProperty.arraySize)) * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing); // if expanded, one line for the label, 1.5 for list borders and add/remove , and one for each element
            }
        }
    }
}
