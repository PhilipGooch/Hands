using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(SheepCheckpoint))]
public class SheepCheckpointEditor : Editor
{
    SerializedProperty previousCheckpointProperty;
    SerializedProperty respawnHeightProperty;

    private void OnEnable()
    {
        previousCheckpointProperty = serializedObject.FindProperty("previousCheckpoints");
        respawnHeightProperty = serializedObject.FindProperty("respawnHeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(respawnHeightProperty);

        EditorGUILayout.BeginVertical("Box");
        GUILayout.Label("Previous Checkpoints");
        var checkpoint = serializedObject.targetObject as SheepCheckpoint;
        var checkpointsInLevel = GameObject.FindObjectsOfType<SheepCheckpoint>();
        var checkpointStrings = checkpointsInLevel.Select(x => x.name).Append("Null").ToArray();
        
        for (int i = 0; i < checkpoint.previousCheckpoints.Count; i++)
        {
            DrawUIForCheckpoint(i, checkpoint.previousCheckpoints, checkpointsInLevel, checkpointStrings);
        }
        if (GUILayout.Button("Add Checkpoint"))
        {
            previousCheckpointProperty.InsertArrayElementAtIndex(previousCheckpointProperty.arraySize);
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.EndVertical();
    }

    void DrawUIForCheckpoint(int index, List<SheepCheckpoint> list, SheepCheckpoint[] levelCheckpoints, string[] checkpointStrings)
    {
        SheepCheckpoint checkpoint = list[index];
        EditorGUILayout.BeginHorizontal();
        var selectedCheckpointId = GetSelectedCheckpointIndex(checkpoint, levelCheckpoints);
        var newCheckpointId = EditorGUILayout.Popup(selectedCheckpointId, checkpointStrings);
        if (newCheckpointId != selectedCheckpointId)
        {
            if (newCheckpointId < levelCheckpoints.Length)
            {
                var serializedCheckpoint = previousCheckpointProperty.GetArrayElementAtIndex(index);
                serializedCheckpoint.objectReferenceValue = levelCheckpoints[newCheckpointId];
            }
            else
            {
                list[index] = null;
            }
        }

        if (GUILayout.Button("Remove"))
        {
            previousCheckpointProperty.DeleteArrayElementAtIndex(index);
        }
        EditorGUILayout.EndHorizontal();
    }

    int GetSelectedCheckpointIndex(SheepCheckpoint checkpoint, SheepCheckpoint[] levelCheckpoints)
    {
        for(int i = 0; i < levelCheckpoints.Length; i++)
        {
            if (levelCheckpoints[i] == checkpoint)
                return i;
        }
        return levelCheckpoints.Length;
    }
}
