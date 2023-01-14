using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractableEntity))]
[CanEditMultipleObjects]
public class PhysicalObjectEditor : Editor
{
    SerializedProperty physicalMaterial;
    SerializedProperty handleEventsForChildren;

    SerializedProperty useWaterSystem;
    SerializedProperty floatingSystem;

    void OnEnable()
    {
        physicalMaterial = serializedObject.FindProperty("physicalMaterial");
        handleEventsForChildren = serializedObject.FindProperty("handleEventsForChildren");

        useWaterSystem = serializedObject.FindProperty("useWaterSystem");
        floatingSystem = serializedObject.FindProperty("floatingSystem");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(physicalMaterial);
        var physicalMaterialValue = physicalMaterial.objectReferenceValue as PhysicalMaterial;
        EditorGUILayout.PropertyField(handleEventsForChildren);
        EditorGUILayout.PropertyField(useWaterSystem);
        if (physicalMaterialValue != null && useWaterSystem.boolValue)
        {
            EditorGUILayout.PropertyField(floatingSystem);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
