//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UIElements;

//[CustomPropertyDrawer(typeof(NoodleDesignTimePose))]
//public class NoodlePoseDrawer : PropertyDrawer
//{
//    // Draw the property inside the given rect
//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        // Using BeginProperty / EndProperty on the parent property means that
//        // prefab override logic works on the entire property.
//        EditorGUI.BeginProperty(position, label, property);

//        // Draw label
//        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

//        // Don't make child fields be indented
//        var indent = EditorGUI.indentLevel;
//        EditorGUI.indentLevel = 0;


//        //// Calculate rects
//        //var separator = 6;
//        //var width = (position.width - 2 * separator )/ 3 ;
//        //var kpRect = new Rect(position.x, position.y, width, position.height);
//        ////var kdRect = new Rect(position.x + 35, position.y, 50, position.height);
//        //var kdRect = new Rect(position.x + width+separator, position.y, width, position.height);
//        //var maxRect = new Rect(position.x + 2*width + 2*separator, position.y, width, position.height);

//        //// Draw fields - passs GUIContent.none to each so they are drawn without labels
//        //EditorGUI.PropertyField(kpRect, property.FindPropertyRelative("kp"), GUIContent.none);
//        //EditorGUI.PropertyField(kdRect, property.FindPropertyRelative("kd"), GUIContent.none);
//        //EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("maxSpring"), GUIContent.none);

//        var v1 = property.FindPropertyRelative("v1");
//        var v2 = property.FindPropertyRelative("v2");
//        var v3 = property.FindPropertyRelative("v3");
//        var v4 = property.FindPropertyRelative("v4");
//        var hipsPitch = v1.FindPropertyRelative("x");
//        var waisPitch = v1.FindPropertyRelative("y");
//        var chstPitch = v1.FindPropertyRelative("z");
//        var headPitch = v1.FindPropertyRelative("w");
//        var v1Labels = new GUIContent[] { new GUIContent("hips"), new GUIContent("waist"), new GUIContent("chest"), new GUIContent("head") };
//        var v1values = new float[] { hipsPitch.floatValue, waisPitch.floatValue, chstPitch.floatValue, headPitch.floatValue };
//        EditorGUI.MultiFloatField(position, v1Labels, v1values);
//        hipsPitch.floatValue = v1values[0];
//        waisPitch.floatValue = v1values[1];
//        chstPitch.floatValue = v1values[2];
//        headPitch.floatValue = v1values[3];

//        // Set indent back to what it was
//        EditorGUI.indentLevel = indent;

//        EditorGUI.EndProperty();
//    }


//}
