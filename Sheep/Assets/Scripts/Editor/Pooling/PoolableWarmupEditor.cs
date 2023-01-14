using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Reflection;

[CustomEditor(typeof(PoolableWarmup))]
public class PoolableWarmupEditor : Editor
{
    SerializedProperty poolableListProperty;
    const string targetFieldPath = "target";

    private void OnEnable()
    {
        poolableListProperty = serializedObject.FindProperty("warmupConfigurations");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Target poolables to warmup and the number of instances to create.");
        EditorGUILayout.PropertyField(poolableListProperty);
        if (GUILayout.Button("Append poolables from scene"))
        {
            AppendScenePoolablesToList();
        }
        serializedObject.ApplyModifiedProperties();
    }

    void AppendScenePoolablesToList()
    {
        var sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();

        Queue<Object> prefabsToCheck = new Queue<Object>(sceneRoots);
        HashSet<Object> checkedPrefabs = new HashSet<Object>();
        while(prefabsToCheck.Count > 0)
        {
            var root = prefabsToCheck.Dequeue();
            checkedPrefabs.Add(root);
            Component[] components;
            if (root is GameObject)
            {
                components = ((GameObject)root).GetComponentsInChildren<Component>();
            }
            else if (root is Component)
            {
                components = ((Component)root).GetComponentsInChildren<Component>();
            }
            else
            {
                continue;
            }

            foreach (var component in components)
            {
                var serializedObj = new SerializedObject(component);
                var prop = serializedObj.GetIterator();
                do
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var objRef = prop.objectReferenceValue;
                        if (objRef != null)
                        {
                            if (typeof(Poolable).IsAssignableFrom(objRef.GetType()))
                            {
                                TryToAppendPoolable(objRef as Poolable);
                            }
                            else
                            {
                                var prefabType = PrefabUtility.GetPrefabAssetType(objRef);
                                if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
                                {
                                    if (!checkedPrefabs.Contains(objRef) && !prefabsToCheck.Contains(objRef))
                                    {
                                        prefabsToCheck.Enqueue(objRef);
                                    }
                                }
                            }
                        }
                    }
                } while (prop.NextVisible(true));
            }
        }
    }

    void TryToAppendPoolable(Poolable target)
    {
        if (target != null)
        {
            var prefabType = PrefabUtility.GetPrefabAssetType(target);
            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
            {
                // We found a reference to a poolable prefab. Add it to our list, if it's not already in the list.
                if (PoolableNotInList(target))
                {
                    Debug.Log($"Added {target.name} to the list of poolables.");
                    poolableListProperty.InsertArrayElementAtIndex(0);
                    var newElement = poolableListProperty.GetArrayElementAtIndex(0);
                    newElement.FindPropertyRelative(targetFieldPath).objectReferenceValue = target;
                    newElement.FindPropertyRelative("count").intValue = 4;
                }
            }
        }
    }

    bool PoolableNotInList(Poolable target)
    {
        for (int i = 0; i < poolableListProperty.arraySize; i++)
        {
            var listEntry = poolableListProperty.GetArrayElementAtIndex(i).FindPropertyRelative(targetFieldPath).objectReferenceValue;
            if (target == listEntry)
            {
                return false;
            }
        }
        return true;
    }
}

[CustomPropertyDrawer(typeof(PoolableWarmup.WarmupConfiguration))]
public class WarmupConfigurationPropertyDrawer : PropertyDrawer
{
    const string targetFieldPath = "target";

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
        var countWidth = 50;
        var spacing = 10;
        var targetRect = new Rect(position.x, position.y, position.width - countWidth - spacing, position.height);
        var countRect = new Rect(position.x + targetRect.width + spacing, position.y, countWidth, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        var targetProperty = property.FindPropertyRelative(targetFieldPath);
        targetProperty.objectReferenceValue = EditorGUI.ObjectField(targetRect, targetProperty.objectReferenceValue, typeof(Poolable), false);
        EditorGUI.PropertyField(countRect, property.FindPropertyRelative("count"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}
