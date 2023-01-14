using NBG.Core.Editor;
using UnityEngine;

namespace NBG.Core.ObjectIdDatabase.Editor
{
    public class ValidateObjectIdDatabase : ValidationTest
    {
        public override string Name => "All GameObjects have an id in the scene ObjectId database (save scene to fix)";
        public override string Category => "Scene";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes;

        protected override Result OnRun(ILevel level)
        {
            var errors = 0;

            foreach (var scene in ValidationTests.GetAllScenes(level))
            {
                var db = ObjectIdDatabase.Get(scene);
                if (db == null)
                {
                    PrintError($"Found no ObjectId database in scene: {scene}", null);
                    errors++;
                    continue;
                }

                var roots = scene.GetRootGameObjects();
                foreach (var root in roots)
                {
                    errors += CheckComponentsRecursive(root, db);
                }
            }

            return new Result
            {
                Status = (errors == 0) ? ValidationTestStatus.OK : ValidationTestStatus.Failure,
                Count = errors
            };
        }

        int CheckComponentsRecursive(GameObject go, ObjectIdDatabase db)
        {
            // Self
            var errors = 0;

            if (!db.GetIdForGameObject(go, out _))
            {
                PrintError($"Found a GameObject without a scene id: {go.GetFullPath()}", go);
                errors++;
            }

            // Children
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                errors += CheckComponentsRecursive(t.gameObject, db);
            }

            return errors;
        }
    }
}
