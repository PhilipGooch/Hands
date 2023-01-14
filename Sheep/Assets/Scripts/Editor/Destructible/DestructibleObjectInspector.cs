using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DestructibleObject))]
public class DestructibleObjectInspector : Editor
{
    private SerializedProperty destroyedWithForceFrom;

    private SerializedProperty objectToSpawn;
    private SerializedProperty parentOverride;

    private SerializedProperty forceToDestroy;
    private SerializedProperty minForceToCountAsDamage;
    private SerializedProperty childForceInheritance;
    private SerializedProperty childForceRandomness;

    //particle effects
    private SerializedProperty destructionEffectPlayMode;
    private SerializedProperty burnDestructionEffectPlayMode;
    private SerializedProperty hitEffectPlayMode;

    private SerializedProperty destructionEffects;
    private SerializedProperty burnDestructionEffects;
    private SerializedProperty hitEffects;

    //configuration
    private SerializedProperty destroyByJointBreak;
    private SerializedProperty destroyedByMultipleHits;
    private SerializedProperty grabSpawnedObjects;
    private SerializedProperty transferJointsToSpawnedObject;
    private SerializedProperty transferSameJointToAllSpawnedRigidbodies;

    //destruction by sheep
    private SerializedProperty sheepDestructionForceMulti;

    DestructibleObject destructibleObject;

    //bool showDebug = false;

    private void OnEnable()
    {
        destructibleObject = (DestructibleObject)target;

        destroyedWithForceFrom = serializedObject.FindProperty("destroyedWithForceFrom");

        destructionEffectPlayMode = serializedObject.FindProperty("destructionEffectPlayMode");
        burnDestructionEffectPlayMode = serializedObject.FindProperty("burnDestructionEffectPlayMode");
        hitEffectPlayMode = serializedObject.FindProperty("hitEffectPlayMode");
        destructionEffects = serializedObject.FindProperty("destructionEffects");
        burnDestructionEffects = serializedObject.FindProperty("burnDestructionEffects");
        hitEffects = serializedObject.FindProperty("hitEffects");

        objectToSpawn = serializedObject.FindProperty("objectToSpawn");
        parentOverride = serializedObject.FindProperty("parentOverride");
        forceToDestroy = serializedObject.FindProperty("forceToDestroy");
        minForceToCountAsDamage = serializedObject.FindProperty("minForceToCountAsDamage");
        childForceInheritance = serializedObject.FindProperty("childForceInheritance");
        childForceRandomness = serializedObject.FindProperty("childForceRandomness");

        destroyByJointBreak = serializedObject.FindProperty("destroyByJointBreak");
        destroyedByMultipleHits = serializedObject.FindProperty("destroyedByMultipleHits");
        grabSpawnedObjects = serializedObject.FindProperty("grabSpawnedObjects");
        transferJointsToSpawnedObject = serializedObject.FindProperty("transferJointsToSpawnedObject");
        transferSameJointToAllSpawnedRigidbodies = serializedObject.FindProperty("transferSameJointToAllSpawnedRigidbodies");

        sheepDestructionForceMulti = serializedObject.FindProperty("sheepDestructionForceMulti");
    }

    private bool displayEffects;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(objectToSpawn);
        EditorGUILayout.PropertyField(parentOverride);

        ParticleEffects();
        Forces();
        Configuration();
        //Turned off foldout, because it makes it very hard to view debug info, you need to constantly fold it out
        /*showDebug = EditorGUILayout.Foldout(showDebug, "Show Debug Info");
        if (showDebug)*/
        DebugArea();

        serializedObject.ApplyModifiedProperties();
    }

    void DebugArea()
    {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Accumulated Force");
        EditorGUILayout.TextArea(destructibleObject.AccumulatedForce.ToString());
        EditorGUILayout.LabelField("Log");
        EditorGUILayout.TextArea(destructibleObject.FormattedLog);
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
    }

    private void ParticleEffects()
    {
        displayEffects = EditorGUILayout.Foldout(displayEffects, "Particle Effects");
        if (displayEffects)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(destructionEffectPlayMode);
            EditorGUILayout.PropertyField(destructionEffects);

            EditorGUILayout.PropertyField(burnDestructionEffectPlayMode);
            EditorGUILayout.PropertyField(burnDestructionEffects);

            EditorGUILayout.PropertyField(hitEffectPlayMode);
            EditorGUILayout.PropertyField(hitEffects);
            EditorGUI.indentLevel--;
        }
    }

    private void Forces()
    {
        GUILayout.Space(10);
        GUILayout.Label("Forces", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(forceToDestroy);
        EditorGUILayout.PropertyField(childForceInheritance);
        EditorGUILayout.PropertyField(childForceRandomness);
        EditorGUILayout.PropertyField(sheepDestructionForceMulti);
        EditorGUILayout.PropertyField(destroyedByMultipleHits);
        if (destroyedByMultipleHits.boolValue)
            IndentedField(minForceToCountAsDamage);

        EditorGUI.indentLevel--;
    }

    private void Configuration()
    {
        GUILayout.Space(10);
        EditorGUI.indentLevel++;
        GUILayout.Label("Configuration", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(destroyedWithForceFrom);
        EditorGUILayout.PropertyField(destroyByJointBreak);

        EditorGUILayout.PropertyField(grabSpawnedObjects);
        EditorGUILayout.PropertyField(transferJointsToSpawnedObject);
        if (transferJointsToSpawnedObject.boolValue)
            IndentedField(transferSameJointToAllSpawnedRigidbodies);

        EditorGUI.indentLevel--;
    }

    void IndentedField(SerializedProperty property)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(property);
        EditorGUI.indentLevel--;
    }
}
