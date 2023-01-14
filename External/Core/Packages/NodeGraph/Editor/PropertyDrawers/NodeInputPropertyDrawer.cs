using UnityEditor;
using UnityEngine;

namespace NBG.NodeGraph
{
    [CustomPropertyDrawer(typeof(NodeInput))]
    public class NodeInputPropertyDrawer : PropertyDrawer
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

            // Calculate rects
            var valueRect = new Rect(position.x, position.y, position.width, position.height);
            if (property.serializedObject.context != null)
            {
                if (property.FindPropertyRelative("connectedNode").objectReferenceValue == null)
                    EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("initialValue"), GUIContent.none);
                else
                    EditorGUI.LabelField(valueRect, string.Format("{0}.{1}", property.FindPropertyRelative("connectedNode").objectReferenceValue.name, property.FindPropertyRelative("connectedSocket").stringValue));

            }
            else
            {
                EditorGUI.LabelField(valueRect, "Not Connected");
            }
            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
            //var all = System.AppDomain.CurrentDomain.GetAssemblies();
            //foreach (var entry in all)
            //{
            //    var allTypes = entry.ExportedTypes;
            //    var t=allTypes.First().Attributes;
            //    CustomPropertyDrawer.IsDefined(entry, allTypes.First());
            //}


            EditorGUI.EndProperty();
        }
    }
}
