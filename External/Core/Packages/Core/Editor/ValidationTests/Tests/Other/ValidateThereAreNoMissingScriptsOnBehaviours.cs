//#define NBG_ValidateThereAreNoMissingScriptsOnBehaviours_CAN_FIX
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBG.Core.Editor.StandardLevelValidators
{
    public class ValidateThereAreNoMissingScriptsOnBehaviours : ValidationTest
    {
        public override string Name => "There are no missing scripts on behaviours. (These might crash Unity)";
        public override string Category => "Prefab & Scene";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
        public override bool CanAssist => (_hiddenWithErrors.Count > 0);
        public override string AssistTooltip => $"Unhide {_hiddenWithErrors.Count} GameObjects with errors.";
#if NBG_ValidateThereAreNoMissingScriptsOnBehaviours_CAN_FIX
        public override bool CanFix => true;
#else
        public override bool CanFix => false;
#endif

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
                CheckComponentsRecursive(rootGO);
            }

            return Result.FromCount(_errorCount, _totalCount);
        }

        protected override void OnAssist(ILevel level)
        {
            Unhide();
        }

        private void CheckComponentsRecursive(GameObject go)
        {
            // Self
            var errors = 0;

            var comps = new List<Component>();
            go.GetComponents(comps);
            foreach (var comp in comps)
            {
                if (comp == null)
                {
                    errors++;
                }
            }

            if (errors > 0)
            {
                var hidden = ((go.hideFlags & HideFlags.HideInHierarchy) == HideFlags.HideInHierarchy);
                if (hidden)
                {
                    PrintError($"Found missing Script on {go.GetFullPath()} (hidden)", go);
                    _hiddenWithErrors.Add(go);
                }
                else
                {
                    PrintError($"Found missing Script on {go.GetFullPath()}", go);
                }
            }

            _errorCount += errors;
            ++_totalCount; // Count GameObjects

            // Children
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                CheckComponentsRecursive(t.gameObject);
            }
        }

        protected override void OnFix(ILevel level)
        {
            AssetDatabase.StartAssetEditing();

            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                FixComponentsRecursive(rootGO);
            }

            AssetDatabase.StopAssetEditing();
        }

        void FixComponentsRecursive(GameObject go)
        {
            // Self
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (count > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                ValidationTests.SaveChangesInContext();
            }

            // Children
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                var t = go.transform.GetChild(i);
                FixComponentsRecursive(t.gameObject);
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
