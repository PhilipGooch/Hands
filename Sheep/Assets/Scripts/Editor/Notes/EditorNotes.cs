using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class EditorNotes : ISerializationCallbackReceiver
{
    [System.Serializable]
    public struct NoteReference
    {
        public string guid;
        public string note;

        public NoteReference(string guid, string note)
        {
            this.guid = guid;
            this.note = note;
        }
    }

    Dictionary<string, string> loadedNotes = new Dictionary<string, string>();
    string filePath = Path.Combine(Application.dataPath, "..", "EditorNotes.json");

    [SerializeField]
    List<NoteReference> serializableNotes = new List<NoteReference>();

    public void LoadNotes()
    {
        if (File.Exists(filePath))
        {
            var contents = File.ReadAllText(filePath);
            var savedNotes = JsonUtility.FromJson<EditorNotes>(contents);
            if (savedNotes != null)
            {
                loadedNotes = savedNotes.loadedNotes;
            }
        }
    }

    public void SaveNotes()
    {
        var json = JsonUtility.ToJson(this, true);
        File.WriteAllText(filePath, json);
    }

    public void SetNote(string guid, string note)
    {
        loadedNotes[guid] = note;
    }

    public string GetNote(string guid)
    {
        if (loadedNotes.ContainsKey(guid))
            return loadedNotes[guid];
        return string.Empty;
    }

    public void OnBeforeSerialize()
    {
        serializableNotes.Clear();
        foreach (var note in loadedNotes)
        {
            serializableNotes.Add(new NoteReference(note.Key, note.Value));
        }
    }

    public void OnAfterDeserialize()
    {
        loadedNotes.Clear();
        foreach (var noteRef in serializableNotes)
        {
            loadedNotes.Add(noteRef.guid, noteRef.note);
        }
    }
}
