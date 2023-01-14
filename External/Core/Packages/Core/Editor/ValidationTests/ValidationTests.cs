using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NBG.Core.Editor
{
    public static class ValidationTests
    {
        static ILevelIndexer _levelIndexer;
        public static ILevelIndexer Indexer => _levelIndexer;

        static LooseScenesProxyLevel _looseScenes = new LooseScenesProxyLevel();
        internal static ILevel LooseScenesProxyLevel => _looseScenes;

        // Importance specifying which project-wide validation tests should be run.
        // Test will run if Test.Importance <= RunLevelValidationTestsAtImportance.
        // Specify -1 to disable tests.
        public static int RunProjectWideValidationTestsAtImportance { get; set; } = int.MaxValue;

        static ValidationTests()
        {
            var list = AssemblyUtilities.GetAllDerivedClasses(typeof(ILevelIndexer));
            if (list.Count == 0)
                throw new InvalidOperationException("Could not find an ILevelIndexer.");
            else if (list.Count > 1)
                throw new InvalidOperationException($"Found {list.Count} ILevelIndexer.");

            _levelIndexer = (ILevelIndexer)Activator.CreateInstance(list[0]);
        }



        static List<ValidationTest> _tests;
        public static IEnumerable<ValidationTest> Tests
        {
            get
            {
                if (_tests == null)
                {
                    _tests = new List<ValidationTest>();
                    var types = AssemblyUtilities.GetAllDerivedClasses(typeof(ValidationTest));
                    foreach (var type in types)
                    {
                        var test = (ValidationTest)Activator.CreateInstance(type);
                        _levelIndexer.OnValidationTestInstantiated(test);
                        _tests.Add(test);
                    }
                    SortTests(_tests);
                }
                return _tests;
            }
        }

        public static void RunAllTests(ValidationTestCaps requireCaps, ValidationTestCaps excludeCaps, ILevel context = null)
        {
            foreach (var test in Tests.Where(t => t.HasCaps(requireCaps) && t.DoesntHaveCaps(excludeCaps)))
            {
                test.Reset();
                test.Run(context);
            }
        }

        public static void FixAllTests(ValidationTestCaps requireCaps, ValidationTestCaps excludeCaps, ILevel context = null)
        {
            foreach (var test in Tests.Where(t => t.HasCaps(requireCaps) && t.DoesntHaveCaps(excludeCaps)))
            {
                if (test.CanFix)
                    test.Fix(context);
            }
        }

        public static void ResetAllTests()
        {
            foreach (var test in Tests)
            {
                test.Reset();
            }
        }

        static void SortTests(List<ValidationTest> tests)
        {
            var baseType = typeof(ValidationTest);

            // Check if types are valid
            foreach (var test in tests)
            {
                if (test.RunBefore != null)
                {
                    Debug.Assert(baseType.IsAssignableFrom(test.RunBefore), $"Level Validation Test type error in RunBefore of {test.GetType().Name}: assigned {test.RunBefore.Name}");
                }

                if (test.RunAfter != null)
                {
                    Debug.Assert(baseType.IsAssignableFrom(test.RunAfter), $"Level Validation Test type error in RunAfter of {test.GetType().Name}: assigned {test.RunAfter.Name}");
                }
            }

            // Apply sort commands once
            var original = new List<ValidationTest>(tests);
            foreach (var ot in original)
            {
                if (ot.RunBefore != null)
                {
                    int originalIndex = tests.FindIndex(x => x == ot);
                    int runBeforeIndex = tests.FindIndex(x => x.GetType() == ot.RunBefore);
                    if (runBeforeIndex != -1 && originalIndex > runBeforeIndex)
                    {
                        tests.Remove(ot);
                        tests.Insert(runBeforeIndex, ot); // Insert before
                    }
                }

                if (ot.RunAfter != null)
                {
                    int originalIndex = tests.FindIndex(x => x == ot);
                    int runAfterIndex = tests.FindIndex(x => x.GetType() == ot.RunAfter);
                    if (runAfterIndex != -1 && originalIndex < runAfterIndex)
                    {
                        tests.Remove(ot);
                        runAfterIndex -= 1; // Removed item ahead of the target
                        tests.Insert(runAfterIndex + 1, ot); // Insert after
                    }
                }
            }

            // Verify that the list is sorted correctly
            // There might be setup mistakes (e.g. circular dependencies)
            foreach (var test in tests)
            {
                if (test.RunBefore != null)
                {
                    int originalIndex = tests.FindIndex(x => x == test);
                    int runBeforeIndex = tests.FindIndex(x => x.GetType() == test.RunBefore);
                    Debug.Assert(originalIndex < runBeforeIndex, $"Level Validation Test ordering error in RunBefore of {test.GetType().Name}");
                }

                if (test.RunAfter != null)
                {
                    int originalIndex = tests.FindIndex(x => x == test);
                    int runAfterIndex = tests.FindIndex(x => x.GetType() == test.RunAfter);
                    Debug.Assert(originalIndex > runAfterIndex, $"Level Validation Test ordering error in RunAfter of {test.GetType().Name}");
                }
            }
        }

        public static IEnumerable<Scene> GetAllScenes(ILevel level)
        {
            yield return level.BaseScene;

            foreach (var section in level.Sections)
            {
                yield return section;
            }
        }

        public static IEnumerable<GameObject> GetAllRootsFromAllScenes(ILevel level)
        {
            _saveContextPath = null;
            foreach (var item in GetAllScenes(level))
            {
                foreach (var root in item.GetRootGameObjects())
                {
                    _saveContextGO = root;
                    yield return root;
                }
            }
            _saveContextGO = null;

            GC.Collect();
        }

        public static IEnumerable<GameObject> GetAllPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject mainAsset;
                try
                {
                    mainAsset = PrefabUtility.LoadPrefabContents(path);
                    if (mainAsset == null)
                        continue;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    continue;
                }
                _saveContextGO = mainAsset;
                _saveContextPath = path;
                yield return mainAsset;
                PrefabUtility.UnloadPrefabContents(mainAsset);
            }
            _saveContextGO = null;
            _saveContextPath = null;

            GC.Collect();
        }

        public static IEnumerable<GameObject> GetAllPrefabsOrAllRootsFromAllScenes(ILevel context)
        {
            if (context == null)
            {
                return GetAllPrefabs();
            }
            else
            {
                return GetAllRootsFromAllScenes(context);
            }
        }

        private static GameObject _saveContextGO;
        private static string _saveContextPath;

        /// <summary>
        /// Handles saving of GameObjects or Prefabs acquired via GetAllRootsFromAllScenes, GetAllPrefabs or GetAllPrefabsOrAllRootsFromAllScenes.
        /// </summary>
        public static void SaveChangesInContext()
        {
            try
            {
                if (_saveContextPath != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(_saveContextGO, _saveContextPath);
                }
                else
                {
                    EditorUtility.SetDirty(_saveContextGO);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
