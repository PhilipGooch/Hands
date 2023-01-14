using NBG.Core.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.Core
{
    internal class LevelValidationNUnit
    {
        public struct Item
        {
            public string name;
            public string baseScenePath;
            public ValidationTest test;
            
            public override string ToString() => name;
        }

        [TestCaseSource(nameof(GetStrictLevelValidationTests))]
        public void LevelValidationTests(Item item)
        {
            if (item.baseScenePath == null)
            {
                Debug.Log($"No levels are enabled for testing.");
                return;
            }

            ValidationTests.Indexer.OpenLevel(item.baseScenePath);

            var error = false;
            var status = ValidationTests.Indexer.GetLevelLoadStatus(out error);
            Assert.IsFalse(error);
            Debug.Log($"LevelLoadStatus: {status}");

            if (item.test != null)
            {
                item.test.Reset();
                item.test.Run(ValidationTests.Indexer.CurrentLevel);

                Assert.IsTrue(item.test.Status == ValidationTestStatus.OK);
                Debug.Log($"Test result count: {item.test.StatusCount}");
            }
        }

        static IEnumerable<Item> GetStrictLevelValidationTests()
        {
            var levels = ValidationTests.Indexer;
            var count = levels.LevelBaseScenes.Length;
            var valid = 0;

            ValidationTestCaps requiredCaps = ValidationTestCaps.StrictScenesScope | ValidationTestCaps.ChecksScenes;

            for (int i = 0; i < count; ++i)
            {
                var importanceLimit = levels.RunLevelValidationTestsAtImportance[i];
                if (importanceLimit >= 0)
                {
                    // Test level load itself (e.g. OnValidate failures)
                    {
                        var item = new Item();
                        item.name = $"[{levels.LevelNames[i]}] Load and OnValidate";
                        item.baseScenePath = levels.LevelBaseScenes[i];
                        yield return item;
                    }

                    var tests = ValidationTests.Tests.Where(t => t.HasCaps(requiredCaps) && t.Importance <= importanceLimit);
                    foreach (var test in tests)
                    {
                        ++valid;
                        var item = new Item();
                        item.name = $"[{levels.LevelNames[i]}] {test.Name}";
                        item.baseScenePath = levels.LevelBaseScenes[i];
                        item.test = test;
                        yield return item;
                    }
                }
            }

            if (valid == 0)
            {
                var emptyItem = new Item();
                yield return emptyItem;
            }
        }
    }
}
