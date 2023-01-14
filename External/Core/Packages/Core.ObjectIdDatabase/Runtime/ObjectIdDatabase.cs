using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NBG.Core.ObjectIdDatabase.Editor")]

namespace NBG.Core.ObjectIdDatabase
{
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/UnityEngineObject.bindings.cs
    class CompareUnityObjectsByInstanceId : IEqualityComparer<UnityEngine.Object>
    {
        public bool Equals(UnityEngine.Object x, UnityEngine.Object y)
        {
            // Compare instance ids to avoid going into Unity overrides which are main-thread only
            return x.GetHashCode() == y.GetHashCode();
        }

        public int GetHashCode(UnityEngine.Object obj)
        {
            // Internally gets instance id
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Database that assigns every gameobject in a scene a unique integer. 
    /// </summary>
    public sealed class ObjectIdDatabase : MonoBehaviour, ISerializationCallbackReceiver
    {
        public const string GameObjectName = "NBG_OBJECT_ID_DATABASE";
        public const uint InvalidId = uint.MaxValue;

        private Dictionary<GameObject, uint> _objectIDs = new Dictionary<GameObject, uint>(new CompareUnityObjectsByInstanceId());
        private Dictionary<uint, GameObject> _objects = new Dictionary<uint, GameObject>();
        internal Dictionary<GameObject, uint> ObjectIDs => _objectIDs;

        private Dictionary<GameObject, uint> _compactedObjectIDs = new Dictionary<GameObject, uint>(new CompareUnityObjectsByInstanceId());
        private Dictionary<uint, GameObject> _compactedObjects = new Dictionary<uint, GameObject>();

        [SerializeField] internal uint _nextID = 0;
        internal uint AllocateObjectID()
        {
            var id = _nextID;
            _nextID++;
            return id;
        }

        /// <summary>
        /// Returns an objectId for a given GameObject.
        /// </summary>
        /// <param name="go">The GameObject you are looking for</param>
        /// <param name="objectId">The objectId associated with that GameObject</param>
        /// <returns>true if found, otherwise false</returns>
        public bool GetIdForGameObject(GameObject go, out uint objectId)
        {
            if (_objectIDs.TryGetValue(go, out objectId))
            {
                return true;
            }
            else
            {
                objectId = InvalidId;
                return false;
            }
        }

        /// <summary>
        /// Returns the GameObject for a given objectId.
        /// </summary>
        /// <param name="objectId">The objectId you are looking for</param>
        /// <param name="go">The gameObject associated with that objectId</param>
        /// <returns>true if found, otherwise false</returns>
        public bool GetGameObjectForID(uint objectId, out GameObject go)
        {
            return _objects.TryGetValue(objectId, out go);
        }

        /// <summary>
        /// Returns a compacted objectId for a given GameObject.
        /// </summary>
        /// <param name="go">The GameObject you are looking for</param>
        /// <param name="objectId">The objectId associated with that GameObject</param>
        /// <returns>true if found, otherwise false</returns>
        public bool GetCompactedIdForGameObject(GameObject go, out uint compactedObjectId)
        {
            if (_compactedObjectIDs.TryGetValue(go, out compactedObjectId))
            {
                return true;
            }
            else
            {
                compactedObjectId = InvalidId;
                return false;
            }
        }

        /// <summary>
        /// Returns the GameObject for a given compacted objectId.
        /// </summary>
        /// <param name="objectId">The objectId you are looking for</param>
        /// <param name="go">The gameObject associated with that objectId</param>
        /// <returns>true if found, otherwise false</returns>
        public bool GetGameObjectForCompactedID(uint compactedObjectId, out GameObject go)
        {
            return _compactedObjects.TryGetValue(compactedObjectId, out go);
        }

        static List<GameObject> _rootsBuffer = new List<GameObject>(32);
        public static ObjectIdDatabase Get(Scene scene)
        {
            if (!scene.IsValid())
                return null;

            scene.GetRootGameObjects(_rootsBuffer);
            var go = _rootsBuffer.Find(x => x.name == GameObjectName);
            _rootsBuffer.Clear();

            if (go != null)
            {
                var db = go.GetComponent<ObjectIdDatabase>();
                Debug.Assert(db != null, $"{nameof(ObjectIdDatabase)} not found on {GameObjectName}");
                if (db != null)
                {
                    if (!db.IsPopulated())
                        db.Repopulate();
                    return db;
                }
            }

            return null;
        }

        #region SerializationCallbackReceiver
        [Serializable]
        struct GameObjectEntry
        {
            public GameObject gameObject;
            public uint sceneId;
        }

        [SerializeField] List<GameObjectEntry> _sceneIDEntries = new List<GameObjectEntry>();
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (!IsPopulated())
                return; // Dictionary is lazily populated. Do not wipe it if ObjectIdDatabase was not accessed at all.

            _sceneIDEntries.Clear();
            _sceneIDEntries.Capacity = _objectIDs.Count;
            foreach (var kvp in _objectIDs)
            {
                Debug.Assert(!_sceneIDEntries.Any(e => e.sceneId == kvp.Value)); // Sanity check for duplicate ids
                var entry = new GameObjectEntry();
                entry.gameObject = kvp.Key;
                entry.sceneId = kvp.Value;
                _sceneIDEntries.Add(entry);
            }
            
        }
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _objectIDs.Clear();
        }
        
        bool IsPopulated()
        {
            return _objectIDs.Any();
        }

        void Repopulate()
        {
            try
            {
                _objectIDs.Clear();
                foreach (var entry in _sceneIDEntries)
                {
                    Debug.Assert(!_objectIDs.Any(e => e.Value == entry.sceneId)); // Sanity check for duplicate ids
                    if (entry.gameObject == null)
                    {
                        Debug.LogWarning($"ObjectIdDatabase contains a null gameobject with id {entry.sceneId}");
                    }
                    else
                    {
                        _objectIDs.Add(entry.gameObject, entry.sceneId);
                        _objects.Add(entry.sceneId, entry.gameObject);
                    }
                }

                GetCompactedVersion(_objectIDs, out _compactedObjectIDs, out _compactedObjects);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to populate ObjectIdDatabase. {e.Message}");
            }
        }

        static void GetCompactedVersion(Dictionary<GameObject, uint> objectIds, out Dictionary<GameObject, uint> outObjectIds, out Dictionary<uint, GameObject> outObjects)
        {
            var newObjectIDs = new Dictionary<GameObject, uint>(new CompareUnityObjectsByInstanceId());
            var newObjects = new Dictionary<uint, GameObject>();

            uint counter = 0;
            foreach (var pair in objectIds)
            {
                var id = ++counter;
                newObjectIDs[pair.Key] = id;
                newObjects[id] = pair.Key;
            }

            outObjectIds = newObjectIDs;
            outObjects = newObjects;
        }

        internal void CompactBaseData()
        {
            if (!IsPopulated())
                Repopulate();
            GetCompactedVersion(_objectIDs, out _objectIDs, out _objects);
        }
        #endregion
    }
}
