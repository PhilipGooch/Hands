using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StickyObject))]
public class StickyInspector : Editor
{

    bool showAdvanced;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("General Setup", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var stickyAllAround = CreatePropertyField("stickyAllAround");
        if (!stickyAllAround.boolValue)
        {
            EditorGUI.indentLevel++;
            CreatePropertyField("normal");
            CreatePropertyField("stickTollerance");
            EditorGUI.indentLevel--;
        }

        CreatePropertyField("sticksToASingleObject");
        CreatePropertyField("shouldUnstickOnItsOwn");
        CreatePropertyField("stickinessCooldown");
        CreatePropertyField("stickyLayers");

        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("Forces", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        CreatePropertyField("forceToStick");
        CreatePropertyField("unstickForce");
        CreatePropertyField("unstickTorque");
        CreatePropertyField("stuckObjAcumulatedForceMulti");
        EditorGUI.indentLevel--;

        EditorGUILayout.LabelField("Sounds", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        CreatePropertyField("onStickSound");
        CreatePropertyField("onUnstuckSound");
        EditorGUI.indentLevel--;

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Config");
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            CreatePropertyField("minimumDistanceBetweenContacts");
            CreatePropertyField("feedbackCooldown");
            
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
