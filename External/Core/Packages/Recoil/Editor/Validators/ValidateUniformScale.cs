using NBG.Core;
using NBG.Core.Editor;
using UnityEngine;

namespace Recoil.Editor
{
    public class ValidateUniformScale : ValidationTest
    {
        public override string Name => "RigidBody scale is (1,1,1)";
        public override string Category => "Recoil";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

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
                var rbs = rootGO.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (rb.transform.localScale != Vector3.one)
                    {
                        _errorCount++;
                        PrintError($"Non-Uniform Rigid Body found {rb.gameObject.GetFullPath()}", rb);
                    }

                    var current = rb.transform.parent;
                    while (current != null)
                    {
                        if (current.transform.localScale != Vector3.one)
                        {
                            _errorCount++;
                            PrintError($"Parent ({rb.gameObject.GetFullPath()}) of RigidBody with Non-Uniform scale", current);
                        }
                        current = current.parent;
                    }

                    ++_totalCount;
                }
            }

            return Result.FromCount(_errorCount, _totalCount);
        }
    }
}
