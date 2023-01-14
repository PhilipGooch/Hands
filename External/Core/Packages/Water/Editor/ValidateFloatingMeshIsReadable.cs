using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NBG.Core.Editor;
using NBG.Core;

namespace NBG.Water
{
    public class ValidateFloatingMeshIsReadable : ValidationTest
    {
        public override string Name => "Meshes used by FloatingMesh are marked Readable";
        public override string Category => "Water System";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

        public override bool CanFix => true;

        private int _errorCount;
        private int _totalCount;
        List<GameObject> _hiddenWithErrors = new List<GameObject>();

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
            _hiddenWithErrors.Clear();
        }

        protected override Result OnRun(ILevel level)
        {
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                CheckGORecursive(rootGO, fix: false);
            }

            return Result.FromCount(_errorCount, _totalCount);
        }

        protected override void OnFix(ILevel level)
        {
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                CheckGORecursive(rootGO, fix: true);
            }
        }

        private void CheckGORecursive(GameObject go, bool fix)
        {
            ++_totalCount;

            var fm = go.GetComponent<FloatingMesh>();
            if (fm != null)
            {
                var mesh = fm.Instance.GetFinalMesh();
                if (mesh != null)
                {
                    if (!mesh.isReadable)
                    {
                        if (fix)
                        {
                            string path = AssetDatabase.GetAssetPath(mesh);
                            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                        else
                        {
                            PrintError($"{nameof(FloatingMesh)} is using a non-readable Mesh: {mesh} {go.GetFullPath()}", go);
                            ++_errorCount;
                        }
                    }
                }
            }

            // Children
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                CheckGORecursive(t.gameObject, fix);
            }
        }
    }
}
