using NBG.Core.Editor;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NBG.Core
{
    internal class ProjectValidationNUnit
    {
        public struct Item
        {
            public string name;
            public ValidationTest test;

            public override string ToString() => name;
        }

        [TestCaseSource(nameof(GetStrictProjectValidationTests))]
        public void ProjectValidationTests(Item item)
        {
            if (item.name == null)
            {
                Debug.Log($"No project-wide tests are available.");
                return;
            }

            item.test.Reset();
            item.test.Run(null);

            Debug.Log($"Test result count: {item.test.StatusCount}");
            if (item.test.Status == ValidationTestStatus.OK)
                Assert.Pass();
            else
                Assert.Fail();
        }

        static IEnumerable<Item> GetStrictProjectValidationTests()
        {
            var valid = 0;

            ValidationTestCaps requiredCaps = ValidationTestCaps.StrictProjectScope | ValidationTestCaps.ChecksProject;

            var tests = ValidationTests.Tests.Where(t => t.HasCaps(requiredCaps) && t.Importance <= ValidationTests.RunProjectWideValidationTestsAtImportance);
            foreach (var test in tests)
            {
                ++valid;
                var item = new Item();
                item.name = $"[Project] {test.Name}";
                item.test = test;
                yield return item;
            }

            if (valid == 0)
            {
                var emptyItem = new Item();
                yield return emptyItem;
            }
        }
    }
}
