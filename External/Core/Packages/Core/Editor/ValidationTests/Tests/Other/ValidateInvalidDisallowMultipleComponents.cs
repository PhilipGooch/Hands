using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBG.Core.Editor.StandardLevelValidators
{
    public class ValidateInvalidDisallowMultipleComponents : ValidationTest
    {
        public override string Name => "There are no game objects with: multiple instances of components that are set to DisallowMultiple; or multiple instances of Transform.";
        public override string Category => "Prefab & Scene";
        public override ValidationTestCaps Caps { get; set; } = ValidationTestCaps.Strict | ValidationTestCaps.ChecksScenes | ValidationTestCaps.ChecksProject;

        private int _errorCount;
        private int _totalCount;

        private readonly HashSet<Type> kSpecialCases = new HashSet<Type>() { typeof(Transform) };

        protected override void OnReset()
        {
            _errorCount = 0;
            _totalCount = 0;
        }

        protected override Result OnRun(ILevel level)
        {
            foreach (var rootGO in ValidationTests.GetAllPrefabsOrAllRootsFromAllScenes(level))
            {
                CheckPrefabRefsRecursive(rootGO);
            }

            return Result.FromCount(_errorCount, _totalCount);
        }

        private void CheckPrefabRefsRecursive(GameObject root)
        {
            // Self
            CountViolations(root.transform);
            ++_totalCount; // Count GameObjects

            // Children
            for (int i = 0; i < root.transform.childCount; i++)
            {
                CheckPrefabRefsRecursive(root.transform.GetChild(i).gameObject);
            }
        }

        private void CountViolations(Transform transform)
        {
            Dictionary<Type, int> compCounter = new Dictionary<Type, int>();
            var allComponents = transform.GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                // Invalid scripts are covered by ThereAreNoMissingScriptsOnBehavioursTest. Skip these here.
                if (comp == null)
                    continue;

                var type = comp.GetType();
                bool hasAttr = Attribute.IsDefined(type, typeof(DisallowMultipleComponent));
                if (!hasAttr && !kSpecialCases.Contains(type))
                    continue;

                if (compCounter.TryGetValue(type, out int existing))
                {
                    compCounter[type] = existing + 1;
                }
                else
                {
                    compCounter.Add(type, 1);
                }
            }

            foreach (var entry in compCounter)
            {
                if (entry.Value > 1)
                {
                    PrintError($"{transform.gameObject.GetFullPath()} has {entry.Value} components of type {entry.Key} but should only have 1", transform);
                    ++_errorCount;
                }
            }
        }
    }
}
