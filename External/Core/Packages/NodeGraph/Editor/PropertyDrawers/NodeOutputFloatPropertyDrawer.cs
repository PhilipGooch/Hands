using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomPropertyDrawer(typeof(NodeOutputFloat))]
    public class NodeOutputFloatPropertyDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Draw label
            label.text += " >";
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var valueRect = new Rect(position.x, position.y, position.width, position.height);

            //if (property.FindPropertyRelative("connectedNode").objectReferenceValue == null)
            //EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("initialValue"), GUIContent.none);
            //else
            //    EditorGUI.LabelField(valueRect, string.Format("{0}.{1}", property.FindPropertyRelative("connectedNode").objectReferenceValue.name, property.FindPropertyRelative("connectedSocket").stringValue));

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
