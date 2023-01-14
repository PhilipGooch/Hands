using Noodles.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.Noodles
{
    [CustomPropertyDrawer(typeof(CarryableAnimation))]
    public class CarryableAnimationDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var separator = 6;
            var width = (position.width - 2 * separator) / 3;
            var r1 = new Rect(position.x, position.y, width, position.height);
            //var kdRect = new Rect(position.x + 35, position.y, 50, position.height);
            var r2 = new Rect(position.x + width + separator, position.y, width, position.height);
            var r3 = new Rect(position.x + 2 * width + 2 * separator, position.y, width, position.height);

            // Draw fields - passs GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(r1, property.FindPropertyRelative("one"), GUIContent.none);
            EditorGUI.PropertyField(r2, property.FindPropertyRelative("reach"), GUIContent.none);
            EditorGUI.PropertyField(r3, property.FindPropertyRelative("two"), GUIContent.none);

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
}
