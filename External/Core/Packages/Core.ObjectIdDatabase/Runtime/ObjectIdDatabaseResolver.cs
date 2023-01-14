using NBG.Core.Streams;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBG.Core.ObjectIdDatabase
{
    public sealed class ObjectIdDatabaseResolver
    {
        [ClearOnReload] private static ObjectIdDatabaseResolver _instance = null;
        public static ObjectIdDatabaseResolver instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ObjectIdDatabaseResolver();
                }

                return _instance;
            }
        }

        private readonly Dictionary<Scene, int> scenes = new Dictionary<Scene, int>();
        private readonly Dictionary<int, Scene> sceneIDs = new Dictionary<int, Scene>();

        internal IReadOnlyDictionary<Scene, int> Scenes => scenes;
        internal IReadOnlyDictionary<int, Scene> SceneIDs => sceneIDs;

        private ObjectIdDatabaseResolver() { }

        public void Register(int id, Scene scene)
        {
            scenes[scene] = id;
            sceneIDs[id] = scene;
        }

        public void Unregister(int id)
        {
            if (sceneIDs.TryGetValue(id, out var scene))
            {
                scenes.Remove(scene);
                sceneIDs.Remove(id);
            }
        }

        public void UnregisterAll()
        {
            scenes.Clear();
            sceneIDs.Clear();
        }



        private const ushort BITS_SCENE = 6;
        private const ushort BITS_SMALL = 8;
        private const ushort BITS_LARGE = 16;

        public void WriteGameObject(IStreamWriter writer, GameObject go)
        {
            if (!Scenes.TryGetValue(go.scene, out int sceneID))
            {
                throw new Exception($"GameObject {go.GetFullPath()} is in a scene that is not registered with {nameof(ObjectIdDatabaseResolver)}");
            }

            var db = ObjectIdDatabase.Get(go.scene);
            if (!db.GetIdForGameObject(go, out uint goID))
            {
                throw new Exception($"GameObject {go.GetFullPath()} is not registered in {nameof(ObjectIdDatabase)}.");
            }

            Debug.Assert(goID < Mathf.Pow(2, BITS_LARGE), $"GameObject {go.GetFullPath()} can not be written to stream correctly, because its {nameof(ObjectIdDatabase)} id ({goID}) is too large.", go);

            writer.Write((uint)sceneID, BITS_SCENE);
            writer.Write((uint)goID, BITS_SMALL, BITS_LARGE);
        }

        public GameObject ReadGameObject(IStreamReader reader)
        {
            var sceneID = reader.ReadUInt32(BITS_SCENE);
            var goID = reader.ReadUInt32(BITS_SMALL, BITS_LARGE);
            if (!SceneIDs.TryGetValue((int)sceneID, out var scene))
            {
                throw new Exception($"Scene id={sceneID} is not registered in {nameof(ObjectIdDatabase)}.");
            }

            var db = ObjectIdDatabase.Get(scene);
            if (!db.GetGameObjectForID(goID, out var ret))
            {
                throw new Exception($"GameObject id={goID} is not registered in {nameof(ObjectIdDatabase)} of scene id={sceneID}.");
            }

            return ret;
        }
    }
}
