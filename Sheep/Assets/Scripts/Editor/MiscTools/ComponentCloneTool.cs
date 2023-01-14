using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ComponentCloneTool : EditorWindow
{
    List<Component> components = new List<Component>();
    List<bool> componentSelections = new List<bool>();
    Dictionary<GameObject, List<Component>> clipboard = new Dictionary<GameObject, List<Component>>();

    [MenuItem("Tools/Sheep/Component Clone...")]
    static void Init()
    {
        ComponentCloneTool window = (ComponentCloneTool)GetWindow(typeof(ComponentCloneTool));
        window.Show();
    }

    void OnEnable()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshComponents();
    }

    void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnGUI()
    {
        GUILayout.Label("COMPONENT CLONE TOOL", EditorStyles.boldLabel);
        if (Selection.gameObjects.Length == 0)
        {
            GUILayout.Label("Select game object to copy/paste to.", EditorStyles.label);
        }
        else if (Selection.gameObjects.Length > 1)
        {
            GUILayout.Label("Select singular game object to copy/paste to.", EditorStyles.label);
        }
        else if (Selection.gameObjects.Length == 1)
        {
            GameObject selectedGameObject = Selection.gameObjects[0];
            GUILayout.Label(selectedGameObject.name, EditorStyles.boldLabel);
            for (int i = 0; i < components.Count; i++)
            {
                componentSelections[i] = GUILayout.Toggle(componentSelections[i], components[i].GetType().Name);
            }
            if (GUILayout.Button("Select All"))
            {
                SelectAll();
            }
            else if(GUILayout.Button("Deselect All"))
            {
                DeselectAll();
            }
            else if(GUILayout.Button("Copy Selected"))
            {
                CopySelected(selectedGameObject);
            }
            else if (GUILayout.Button("Paste"))
            {
                Paste(selectedGameObject);
            }
        }
        GUILayout.Label("CLIPBOARD", EditorStyles.boldLabel);
        foreach (KeyValuePair<GameObject, List<Component>> gameObjectClip in clipboard)
        {
            GameObject gameObject = gameObjectClip.Key;
            List<Component> components = gameObjectClip.Value;
            if (components.Count > 0)
            {
                GUILayout.Label(gameObject.name, EditorStyles.boldLabel);
            }
            foreach (Component component in components)
            {
                GUILayout.Label(component.GetType().Name, EditorStyles.label);
            }
        }
        if (GUILayout.Button("Clear"))
        {
            ClearClipboard();
        }
    }

    void OnSelectionChange()
    {
        RefreshComponents();
    }

    void OnHierarchyChange()
    {
        RefreshComponents();
    }

    void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (mode == OpenSceneMode.Single)
        {
            ClearClipboard();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            ClearClipboard();
        }
    }

    void RefreshComponents()
    {
        GameObject selectedGameObject = Selection.gameObjects.Length > 0 ? Selection.gameObjects[0] : null;
        GetAllComponents(components, selectedGameObject);
        ResetComponentSelections(components, componentSelections);
        Repaint();
    }

    void GetAllComponents(List<Component> components, GameObject gameObject)
    {
        components.Clear();
        if (gameObject)
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                components.Add(component);
            }
        }
    }

    void ResetComponentSelections(List<Component> components, List<bool> componentSelections)
    {
        componentSelections.Clear();
        foreach (Component component in components)
        {
            componentSelections.Add(false);
        }
    }

    void CopySelected(GameObject selectedGameObject)
    {
        if (!selectedGameObject)
        {
            return;
        }
        if (!clipboard.ContainsKey(selectedGameObject))
        {
            clipboard.Add(selectedGameObject, new List<Component>());
        }
        clipboard[selectedGameObject].Clear();
        for (int i = 0; i < componentSelections.Count; i++)
        {
            Component component = components[i];
            bool selected = componentSelections[i];
            if (selected)
            {
                clipboard[selectedGameObject].Add(component);
            }
        }
    }

    void Paste(GameObject selectedGameObject)
    {
        if (!selectedGameObject)
        {
            return;
        }

        // Registering transforms of gameObject hierarchy with Undo. This allows an undo back to its previous transform. 
        Undo.RegisterFullObjectHierarchyUndo(selectedGameObject, selectedGameObject.name);

        foreach (KeyValuePair<GameObject, List<Component>> clip in clipboard)
        {
            List<Component> components = clip.Value;
            foreach (Component component in components)
            {
                Type type = component.GetType();
                Component existingComponent = selectedGameObject.GetComponent(type);
                if (type == typeof(Transform))
                {
                    EditorUtility.CopySerialized(component, existingComponent);
                    continue;
                }
                Component copy = Undo.AddComponent(selectedGameObject, type);
                if (copy)
                {
                    EditorUtility.CopySerialized(component, copy);
                }
                else if (EditorUtility.DisplayDialog("Only one " + component.GetType().Name + " allowed on gameobject.",
                                                     "Would you like to overwrite " + component.GetType().Name + "?",
                                                     "Overwrite", "Ignore"))
                {
                    EditorUtility.CopySerialized(component, existingComponent);
                }
            }
        }
    }

    void ClearClipboard()
    {
        clipboard.Clear();
    }

    void SelectAll()
    {
        for (int i = 0; i < components.Count; i++)
        {
            componentSelections[i] = GUILayout.Toggle(true, components[i].GetType().Name);
        }
    }

    void DeselectAll()
    {
        for (int i = 0; i < components.Count; i++)
        {
            componentSelections[i] = GUILayout.Toggle(false, components[i].GetType().Name);
        }
    }
}
