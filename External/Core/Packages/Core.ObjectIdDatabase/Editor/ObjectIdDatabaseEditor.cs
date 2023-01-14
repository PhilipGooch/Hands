#define NBG_OBJECT_ID_DB_ENABLE_FIXING
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBG.Core.ObjectIdDatabase.Editor
{
    [InitializeOnLoad]
    static class ObjectIdDatabaseEditor
    {
        static ObjectIdDatabaseEditor()
        {
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        static void OnSceneSaving(Scene scene, string _)
        {
            try
            {
                RefreshSceneDatabase(scene);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save {nameof(ObjectIdDatabase)}: {e.Message}\n{e.StackTrace}");
            }
        }

        [Serializable]
        public struct Entry
        {
            public GameObject gameObject;
            public uint sceneId;
        }
        static void RegisterGameObjects(GameObject go, List<Entry> entries)
        {
            var entry = new Entry();
            entry.gameObject = go;
            entries.Add(entry);
            
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                RegisterGameObjects(go.transform.GetChild(i).gameObject, entries);
            }
        }

        static void CompactSceneDatabase(Scene scene)
        {
            ObjectIdDatabase db = ObjectIdDatabase.Get(scene);
            if (db == null)
            {
                Debug.LogWarning($"Unable to locate ObjectId database in {scene.name}.");
                return;
            }

            db.CompactBaseData();
            EditorUtility.SetDirty(db);
        }

        static void RefreshSceneDatabase(Scene scene)
        {
            var roots = new List<GameObject>(32);
            var changed = false;

#if NBG_OBJECT_ID_DB_ENABLE_FIXING
            // Check and fix multiple database case
            // This might happen when incorrectly merging scenes
            {
                roots.Clear();
                scene.GetRootGameObjects(roots);
                var dbs = roots.Where(go => go.name == ObjectIdDatabase.GameObjectName);
                if (dbs.Count() > 1)
                {
                    Debug.LogWarning($"Multiple ObjectId databases found in {scene.name}. Fixing...");
                    var ordered = dbs.OrderBy(x => x.GetComponent<ObjectIdDatabase>().ObjectIDs.Count).ToList();
                    while (ordered.Count() > 1)
                    {
                        var min = ordered.First();
                        ordered.RemoveAt(0);
                        GameObject.DestroyImmediate(min);
                    }
                }
            }
#endif

            // Locate/create the database
            ObjectIdDatabase db = ObjectIdDatabase.Get(scene);
            if (db == null)
            {
                var go = new GameObject(ObjectIdDatabase.GameObjectName);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
                db = go.AddComponent<ObjectIdDatabase>();
                changed = true;

                // Register self
                var selfID = db.AllocateObjectID();
                Debug.Assert(selfID == 0);
                db.ObjectIDs.Add(go, selfID);
            }

            // Remove missing items
            var missingGOs = db.ObjectIDs.Where(x => x.Key == null).ToList();
            foreach (var toRemove in missingGOs)
                db.ObjectIDs.Remove(toRemove.Key);
            changed |= missingGOs.Any();

            // Gather the entries list
            List<Entry> entries = new List<Entry>(1024);
            roots.Clear();
            scene.GetRootGameObjects(roots);
            foreach (var root in roots)
                RegisterGameObjects(root, entries);

            // Assign ids to those objects which don't have any (are new)
            var count = entries.Count;
            for (int i = 0; i < count; ++i)
            {
                var entry = entries[i];
                if (!db.ObjectIDs.ContainsKey(entry.gameObject))
                {
                    entry.sceneId = db.AllocateObjectID();
                    entries[i] = entry;

                    db.ObjectIDs.Add(entry.gameObject, entry.sceneId);
                    changed = true;
                }
            }

            if (changed)
                EditorUtility.SetDirty(db);
        }

#if NBG_DEVELOPMENT_MENU_COMMANDS
        [MenuItem("No Brakes Games/Development/Object ID DB/Refresh Active Scene Database")]
        static void Development_RefreshActiveSceneDatabase()
        {
            var activeScene = SceneManager.GetActiveScene();
            RefreshSceneDatabase(activeScene);
        }
        
        [MenuItem("No Brakes Games/Development/Object ID DB/Refresh all Open Scenes Database")]
        static void Development_RefreshAllOpenScenesDatabase()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                RefreshSceneDatabase(scene);
            }
        }

        [MenuItem("No Brakes Games/Development/Object ID DB/Check Active Scene Database")]
        static void Development_CheckActiveSceneDatabase()
        {
            var activeScene = SceneManager.GetActiveScene();
            var db = ObjectIdDatabase.Get(activeScene);
            foreach (var entry in db.ObjectIDs)
            {
                Debug.Log($"[GameObject id: {entry.Key.GetFullPath()}] {entry.Value}");
            }
        }
#endif //NBG_DEVELOPMENT_MENU_COMMANDS

        [MenuItem("No Brakes Games/Development/Object ID DB/Compact Active Scene Database")]
        static void Development_CompactActiveSceneDatabase()
        {
            var activeScene = SceneManager.GetActiveScene();
            CompactSceneDatabase(activeScene);
        }
        
        [MenuItem("No Brakes Games/Development/Object ID DB/Compact all Open Scenes Database")]
        static void Development_CompactAllOpenScenesDatabase()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                CompactSceneDatabase(scene);
            }
        }
    }
}
