using NBG.Core;
using NBG.Core.Editor;
using UnityEngine;

namespace NBG.ElementsSystem.Editor
{
    public class ValidateAllElementsSystemObjectsContainsColliders : ValidationTest
    {
        public override string Name => "Validate If All Elements System Objects Contains Colliders";

        public override string Category => "Elements System";

        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

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
                var elementsSystemObjects = rootGO.GetComponentsInChildren<IElementsSystemObject>(true);
                foreach (var elementSystemObject in elementsSystemObjects)
                {
                    if (elementSystemObject is Component elementsSystemComponent)
                    {
                        var collidersInChildren = elementsSystemComponent.GetComponentsInChildren<Collider>();
                        if (collidersInChildren.Length == 0)
                            _errorCount++;
                    }
                    
                    _totalCount++;
                }
            }

            return Result.FromCount(_errorCount, _totalCount);
        }
    }
}