using NBG.Core;
using NBG.Core.Editor;
using System;
using Object = UnityEngine.Object;

namespace NBG.Net.Legacy.Editor
{
    public class RemoveObsoleteNetworkingScripts : ValidationTest
    {
        public override string Name => $"Migrate (delete) outdated Networking Scripts";
        public override string Category => "Migration";
        public override int Importance => ImportanceUrgent;
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;
        public override bool CanFix => true;
        private static Type[] allObsoleteTypes = new Type[] { typeof(LEGACYNetScope), typeof(LEGACYNetBody), typeof(LEGACYNetIdentity) };

        private int _errorCount;
        private int _totalCount;

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
        }

        protected override Result OnRun(ILevel level)
        {
            _totalCount = 0;

            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                foreach (var type in allObsoleteTypes)
                {
                    var components = rootGO.GetComponentsInChildren(type, true);
                    foreach (var comp in components)
                    {
                        PrintError($"Legacy type {type.Name} found on {comp.gameObject.GetFullPath()}", comp);
                    }
                    _errorCount += components.Length;
                    _totalCount++;
                }
            }
            return Result.FromCount(_errorCount, _totalCount);
        }

        protected override void OnFix(ILevel level)
        {
            var countDeleted = 0;

            foreach (var type in allObsoleteTypes)
            {
                foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
                {
                    var components = rootGO.GetComponentsInChildren(type, true);
                    foreach (var comp in components)
                    {
                        Object.DestroyImmediate(comp, true);
                    }

                    if (components.Length > 0)
                        ValidationTests.SaveChangesInContext();
                }
                PrintLog($"Deleted {countDeleted} {type.Name}.", null);
            }
        }
    }
}
