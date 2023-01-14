using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorNoteWindow : EditorWindow
{
    EditorNotes notes;
    EditorNotes Notes
    {
        get
        {
            if (notes == null)
            {
                notes = new EditorNotes();
                notes.LoadNotes();
            }
            return notes;
        }
    }
    bool notesChanged = false;
    Vector2 scroll;
    string currentSelectionGUID = string.Empty;

    [MenuItem("Window/Notes")]
    static void Init()
    {
        var window = (EditorNoteWindow)GetWindow(typeof(EditorNoteWindow));
        window.Show();
    }

    private void OnDestroy()
    {
        Notes.SaveNotes();
    }

    private void OnSelectionChange()
    {
        var selectedGUIDS = Selection.assetGUIDs;
        if (selectedGUIDS.Length == 1)
        {
            currentSelectionGUID = selectedGUIDS[0];
        }
        else
        {
            currentSelectionGUID = string.Empty;
        }

        SaveNotes();
    }

    private void OnLostFocus()
    {
        SaveNotes();
    }

    void SaveNotes()
    {
        if (notesChanged)
        {
            Notes.SaveNotes();
            notesChanged = false;
        }

        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Notes");
        if (currentSelectionGUID != string.Empty)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var originalNote = Notes.GetNote(currentSelectionGUID);
            float height = EditorStyles.textArea.CalcHeight(new GUIContent(originalNote), position.width);
            var newNote = EditorGUILayout.TextArea(originalNote, EditorStyles.textArea, GUILayout.MinHeight(height), GUILayout.ExpandHeight(true));
            if (newNote != originalNote)
            {
                Notes.SetNote(currentSelectionGUID, newNote);
                notesChanged = true;
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("Select a single asset to add notes.", MessageType.Info);
        }

    }
}
