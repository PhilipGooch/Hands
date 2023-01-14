using NBG.Core;
using NBG.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace Recoil.Editor
{
    public class MigrateCenterOfMassOverride : ValidationTest
    {
        public override string Name => $"Migrate CenterOfMassOverride to {nameof(RigidbodySettingsOverride)}";
        public override string Category => "Migration";
        public override int Importance => ImportanceUrgent;
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
        public override bool CanFix => true;

        private int _errorCount;
        private int _totalCount;

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
        }

        protected override Result OnRun(ILevel level)
        {
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                var components = rootGO.GetComponentsInChildren<LEGACYCenterOfMassOverride>(true);
                foreach (var comp in components)
                {
                    PrintError($"Legacy CenterOfMassOverride found on {comp.gameObject.GetFullPath()}", comp);
                }
                _errorCount += components.Length;
                _totalCount += 1;
            }

            return Result.FromCount(_errorCount, _totalCount);
        }

        protected override void OnFix(ILevel level)
        {
            AssetDatabase.StartAssetEditing();

            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                var components = rootGO.GetComponentsInChildren<LEGACYCenterOfMassOverride>();
                foreach (var comp in components)
                {
                    var go = comp.gameObject;
                    var rso = go.GetComponent<RigidbodySettingsOverride>();
                    if (rso == null)
                    {
                        rso = go.AddComponent<RigidbodySettingsOverride>();
                    }
                    rso.Settings.centerOfMassOverride = comp.target;
                    GameObject.DestroyImmediate(comp, true);
                }

                if (components.Length > 0)
                    ValidationTests.SaveChangesInContext();
            }

            AssetDatabase.StopAssetEditing();
        }
    }
}
