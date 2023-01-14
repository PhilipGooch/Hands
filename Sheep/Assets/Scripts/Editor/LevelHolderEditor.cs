using Malee.List;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelHolder))]
public class LevelHolderEditor : Editor
{
	ReorderableList list;
    SerializedProperty mainMenuProperty;
    SerializedProperty bootstrapSceneProperty;

    void OnEnable()
	{
		list = new ReorderableList(serializedObject.FindProperty("chapters"));
        mainMenuProperty = serializedObject.FindProperty("mainMenuScene");
        bootstrapSceneProperty = serializedObject.FindProperty("bootstrapScene");
    }

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		list.DoLayoutList();
        EditorGUILayout.PropertyField(mainMenuProperty);
        EditorGUILayout.PropertyField(bootstrapSceneProperty);
        serializedObject.ApplyModifiedProperties();
	}
}
