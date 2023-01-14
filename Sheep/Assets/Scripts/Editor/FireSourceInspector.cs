using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FireSource))]

public class FireSourceInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CreatePropertyField("initialFireZone");
        CreatePropertyField("igniteEntityOnStart");

        var generateThreat = CreatePropertyField("generateThreat");
        if (generateThreat.boolValue)
        {
            EditorGUI.indentLevel++;
            CreatePropertyField("threatRangeMultiplier");
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    SerializedProperty CreatePropertyField(string name)
    {
        var obj = serializedObject.FindProperty(name);
        EditorGUILayout.PropertyField(obj);
        return obj;
    }
}
