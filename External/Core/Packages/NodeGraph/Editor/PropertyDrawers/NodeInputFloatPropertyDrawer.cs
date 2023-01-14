using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomPropertyDrawer(typeof(NodeInputFloat))]
    public class NodeInputFloatPropertyDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // Draw label
            //(property.serializedObject. as NodeInput).name
            label.text = "> " + label.text;
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var valueRect = new Rect(position.x, position.y, position.width, position.height);
            if (property.FindPropertyRelative("connectedNode").objectReferenceValue != null)
                EditorGUI.LabelField(valueRect, string.Format("{0}.{1}", property.FindPropertyRelative("connectedNode").objectReferenceValue.name, property.FindPropertyRelative("connectedSocket").stringValue));

            // Calculate rects


            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
