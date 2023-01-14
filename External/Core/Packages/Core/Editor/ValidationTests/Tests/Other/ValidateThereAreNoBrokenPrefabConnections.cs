using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace NBG.Core.Editor.StandardLevelValidators
{
    public class ValidateThereAreNoBrokenPrefabConnections : ValidationTest
    {
        public override string Name => "There are no broken Prefab references. (These might crash Unity)";
        public override string Category => "Prefab & Scene";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

        public override bool CanAssist => (_hiddenWithErrors.Count > 0);
        public override string AssistTooltip => $"Unhide {_hiddenWithErrors.Count} GameObjects with errors.";
        public override bool CanFix => false;

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
                CheckPrefabRefsRecursive(rootGO);
            }

            return Result.FromCount(_errorCount, _totalCount);
        }

        protected override void OnAssist(ILevel level)
        {
            Unhide();
        }

        private void CheckPrefabRefsRecursive(GameObject go)
        {
            // Self
            if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                if (CheckName(go))
                {
                    ReportGO(go, "Missing Prefab error in Name");
                    ++_errorCount;
                }
                if (CheckPrefabAssetExists(go))
                {
                    ReportGO(go, "Prefab was deleted in project but exists in scene");
                    ++_errorCount;
                }
                if (CheckPrefabAssetConnected(go))
                {
                    ReportGO(go, "Prefab has lost connection");
                    ++_errorCount;
                }
            }
            ++_totalCount; // Count GameObjects

            // Children
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                CheckPrefabRefsRecursive(t.gameObject);
            }
        }

        private bool CheckPrefabAssetExists(GameObject go)
        {
            return PrefabUtility.IsPrefabAssetMissing(go);
        }

        private bool CheckPrefabAssetConnected(GameObject go)
        {
            return PrefabUtility.IsDisconnectedFromPrefabAsset(go);
        }

        /*According to Forums, PrefabUtility.IsPrefabAssetMissing() is not always 100% reliable,
         so lets do the hacky name thing that unity dev mentioned */
        private bool CheckName(GameObject go)
        {
            return go.name.Contains("Missing Prefab");
        }

        private void ReportGO(GameObject go, string prefabError)
        {
            var hidden = ((go.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy);
            if (hidden)
            {
                PrintError($"Prefab Error found: {prefabError} in {go.GetFullPath()} (hidden)", go);
                _hiddenWithErrors.Add(go);
            }
            else
            {
                PrintError($"Prefab Error found: {prefabError} {go.GetFullPath()}", go);
            }
        }

        void Unhide()
        {
            foreach (var go in _hiddenWithErrors)
            {
                PrintLog($"Unhiding {go}", go);
                go.hideFlags &= ~HideFlags.HideInHierarchy;
            }
            _hiddenWithErrors.Clear();
        }
    }
}
