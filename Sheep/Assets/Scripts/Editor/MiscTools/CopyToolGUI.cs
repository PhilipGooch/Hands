using UnityEngine;
using UnityEditor;
using System.Globalization;
using System.Collections.Generic;

public class CopyToolWindow : EditorWindow
{
    bool pastePosition = true;
    bool pasteRotation = true;
    bool pasteScale = true;

    struct TransformData
    {
        public List<int> id;
        public Vector3 translation;
        public Quaternion rotation;
        public Vector3 scale;
    }

    List<TransformData> data = new List<TransformData>();

    [MenuItem("Tools/Sheep/Copy Hierarchy Transforms...")]
    static void Init()
    {
        CopyToolWindow window = (CopyToolWindow)GetWindow(typeof(CopyToolWindow));
        window.Show();
    }

    void OnGUI()
    {
        if (Selection.gameObjects.Length == 0)
        {
            GUILayout.Label("Select root game object to copy/paste to.", EditorStyles.boldLabel);
        }
        else if (Selection.gameObjects.Length > 1)
        {
            GUILayout.Label("Select singular root game object to copy/paste to.", EditorStyles.boldLabel);
        }
        else if (Selection.gameObjects.Length == 1)
        {
            // Copy
            if (GUILayout.Button("Copy"))
            {
                Transform root = Selection.gameObjects[0].transform;
                Copy(root);
            }
            // Paste
            if (data.Count > 0)
            {
                pastePosition = GUILayout.Toggle(pastePosition, "Position");
                pasteRotation = GUILayout.Toggle(pasteRotation, "Rotation");
                pasteScale = GUILayout.Toggle(pasteScale, "Scale");
                if (GUILayout.Button("Paste"))
                {
                    Transform root = Selection.gameObjects[0].transform;
                    Paste(root);
                }
            }
        }
    }

    void OnSelectionChange()
    {
        Repaint();
    }

    void Copy(Transform root)
    {
        SaveTransformData(root, new List<int>());
    }

    void SaveTransformData(Transform transform, List<int> id)
    {
        TransformData transformData = new TransformData
        {
            id = id,
            translation = transform.localPosition,
            rotation = transform.localRotation,
            scale = transform.localScale
        };

        data.Add(transformData);

        for (int i = 0; i < transform.childCount; i++)
        {
            List<int> childID = new List<int>(id);
            childID.Add(i);
            SaveTransformData(transform.GetChild(i), childID);
        }
    }

    void Paste(Transform root)
    {
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Transforms in hierarchy changed.");

        foreach (TransformData transformData in data)
        {
            Transform transform = GetTransform(transformData.id, root);

            if (transform)
            {
                if (pastePosition) transform.localPosition = transformData.translation;
                if (pasteRotation) transform.localRotation = transformData.rotation;
                if (pasteScale) transform.localScale = transformData.scale;
            }
        }
    }

    Transform GetTransform(List<int> ID, Transform transform)
    {
        for (int i = 0; i < ID.Count; i++)
        {
            if (i >= transform.childCount)
                return null;

            transform = transform.GetChild(ID[i]);
        }
        return transform;
    }
}
