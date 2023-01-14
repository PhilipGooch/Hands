using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

namespace NBG.Core.GameSystems.Tests
{
    public class GameSystemGroupTests
    {
        class TestGroup : GameSystemGroup
        {
        }

        class TestSystem : GameSystem
        {
            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void SortOneChildSystem()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<TestSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            CollectionAssert.AreEqual(new[] { child }, parent.Systems);

            newWorld.Dispose();
        }

        [UpdateAfter(typeof(Sibling2System))]
        class Sibling1System : TestSystem
        {
        }
        class Sibling2System : TestSystem
        {
        }
        class Sibling3System : TestSystem
        {
        }

        [Test]
        public void SortTwoChildSystems_CorrectOrder()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child1 = newWorld.CreateSystem<Sibling1System>();
            var child2 = newWorld.CreateSystem<Sibling2System>();
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child2);
            parent.SortSystems();
            CollectionAssert.AreEqual(new TestSystem[] { child2, child1 }, parent.Systems);

            newWorld.Dispose();
        }

        // This test constructs the following system dependency graph:
        // 1 -> 2 -> 3 -> 4 -v
        //           ^------ 5 -> 6
        // The expected results of topologically sorting this graph:
        // - systems 1 and 2 are properly sorted in the system update list.
        // - systems 3, 4, and 5 form a cycle (in that order, or equivalent).
        // - system 6 is not sorted AND is not part of the cycle.
        [UpdateBefore(typeof(Circle2System))]
        class Circle1System : TestSystem
        {
        }
        [UpdateBefore(typeof(Circle3System))]
        class Circle2System : TestSystem
        {
        }
        [UpdateAfter(typeof(Circle5System))]
        class Circle3System : TestSystem
        {
        }
        [UpdateAfter(typeof(Circle3System))]
        class Circle4System : TestSystem
        {
        }
        [UpdateAfter(typeof(Circle4System))]
        class Circle5System : TestSystem
        {
        }
        [UpdateAfter(typeof(Circle5System))]
        class Circle6System : TestSystem
        {
        }

        [Test]
        public void DetectCircularDependency_Throws()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child1 = newWorld.CreateSystem<Circle1System>();
            var child2 = newWorld.CreateSystem<Circle2System>();
            var child3 = newWorld.CreateSystem<Circle3System>();
            var child4 = newWorld.CreateSystem<Circle4System>();
            var child5 = newWorld.CreateSystem<Circle5System>();
            var child6 = newWorld.CreateSystem<Circle6System>();
            parent.AddSystemToUpdateList(child3);
            parent.AddSystemToUpdateList(child6);
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child4);
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child5);
            var e = Assert.Throws<GameSystemSorter.CircularSystemDependencyException>(() => parent.SortSystems());
            // Make sure the cycle expressed in e.Chain is the one we expect, even though it could start at any node
            // in the cycle.
            var expectedCycle = new Type[] { typeof(Circle5System), typeof(Circle3System), typeof(Circle4System) };
            var cycle = e.Chain.ToList();
            bool foundCycleMatch = false;
            for (int i = 0; i < cycle.Count; ++i)
            {
                var offsetCycle = new System.Collections.Generic.List<Type>(cycle.Count);
                offsetCycle.AddRange(cycle.GetRange(i, cycle.Count - i));
                offsetCycle.AddRange(cycle.GetRange(0, i));
                Assert.AreEqual(cycle.Count, offsetCycle.Count);
                if (expectedCycle.SequenceEqual(offsetCycle))
                {
                    foundCycleMatch = true;
                    break;
                }
            }
            Assert.IsTrue(foundCycleMatch);

            newWorld.Dispose();
        }

        class Unconstrained1System : TestSystem
        {
        }
        class Unconstrained2System : TestSystem
        {
        }
        class Unconstrained3System : TestSystem
        {
        }
        class Unconstrained4System : TestSystem
        {
        }

        [Test]
        public void SortUnconstrainedSystems_IsDeterministic()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child1 = newWorld.CreateSystem<Unconstrained1System>();
            var child2 = newWorld.CreateSystem<Unconstrained2System>();
            var child3 = newWorld.CreateSystem<Unconstrained3System>();
            var child4 = newWorld.CreateSystem<Unconstrained4System>();
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child4);
            parent.AddSystemToUpdateList(child3);
            parent.AddSystemToUpdateList(child1);
            parent.SortSystems();
            CollectionAssert.AreEqual(parent.Systems, new TestSystem[] { child1, child2, child3, child4 });

            newWorld.Dispose();
        }

        private class UpdateCountingSystemBase : GameSystem
        {
            public int CompleteUpdateCount = 0;
            protected override void OnUpdate()
            {
                ++CompleteUpdateCount;
            }
        }
        class NonThrowing1System : UpdateCountingSystemBase
        {
        }
        class NonThrowing2System : UpdateCountingSystemBase
        {
        }
        class ThrowingSystem : UpdateCountingSystemBase
        {
            public string ExceptionMessage = "I should always throw!";
            protected override void OnUpdate()
            {
                if (CompleteUpdateCount == 0)
                {
                    throw new InvalidOperationException(ExceptionMessage);
                }
                base.OnUpdate();
            }
        }

        [Test]
        public void SystemInGroupThrows_LaterSystemsRun()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child1 = newWorld.CreateSystem<NonThrowing1System>();
            var child2 = newWorld.CreateSystem<ThrowingSystem>();
            var child3 = newWorld.CreateSystem<NonThrowing2System>();
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child3);
            LogAssert.Expect(LogType.Exception, new Regex(child2.ExceptionMessage));
            parent.Update();
            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(1, child1.CompleteUpdateCount);
            Assert.AreEqual(0, child2.CompleteUpdateCount);
            Assert.AreEqual(1, child3.CompleteUpdateCount);

            newWorld.Dispose();
        }

        [Test]
        public void SystemThrows_SystemNotRemovedFromUpdate()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<ThrowingSystem>();
            parent.AddSystemToUpdateList(child);
            LogAssert.Expect(LogType.Exception, new Regex(child.ExceptionMessage));
            parent.Update();
            LogAssert.Expect(LogType.Exception, new Regex(child.ExceptionMessage));
            parent.Update();
            LogAssert.NoUnexpectedReceived();

            Assert.AreEqual(0, child.CompleteUpdateCount);

            newWorld.Dispose();
        }

        [UpdateAfter(typeof(NonSibling2System))]
        class NonSibling1System : TestSystem
        {
        }
        [UpdateBefore(typeof(NonSibling1System))]
        class NonSibling2System : TestSystem
        {
        }

        [Test]
        public void GameSystemGroup_UpdateAfterTargetIsNotSibling_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<NonSibling1System>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\] attribute on .+NonSibling1System targeting.+NonSibling2System"));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_UpdateBeforeTargetIsNotSibling_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<NonSibling2System>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\] attribute on .+NonSibling2System targeting.+NonSibling1System"));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [UpdateAfter(typeof(NotEvenASystem))]
        class InvalidUpdateAfterSystem : TestSystem
        {
        }
        [UpdateBefore(typeof(NotEvenASystem))]
        class InvalidUpdateBeforeSystem : TestSystem
        {
        }
        class NotEvenASystem
        {
        }

        [Test]
        public void GameSystemGroup_UpdateAfterTargetIsNotSystem_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<InvalidUpdateAfterSystem>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\].+InvalidUpdateAfterSystem.+NotEvenASystem is not a subclass of GameSystemBase"));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_UpdateBeforeTargetIsNotSystem_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<InvalidUpdateBeforeSystem>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\].+InvalidUpdateBeforeSystem.+NotEvenASystem is not a subclass of GameSystemBase"));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [UpdateAfter(typeof(UpdateAfterSelfSystem))]
        class UpdateAfterSelfSystem : TestSystem
        {
        }
        [UpdateBefore(typeof(UpdateBeforeSelfSystem))]
        class UpdateBeforeSelfSystem : TestSystem
        {
        }

        [Test]
        public void GameSystemGroup_UpdateAfterTargetIsSelf_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<UpdateAfterSelfSystem>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\].+UpdateAfterSelfSystem.+cannot be updated after itself."));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_UpdateBeforeTargetIsSelf_LogsWarning()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var child = newWorld.CreateSystem<UpdateBeforeSelfSystem>();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\].+UpdateBeforeSelfSystem.+cannot be updated before itself."));
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_AddNullToUpdateList_QuietNoOp()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            Assert.DoesNotThrow(() => { parent.AddSystemToUpdateList(null); });
            Assert.IsEmpty(parent.Systems);

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_AddSelfToUpdateList_Throws()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            Assert.That(() => { parent.AddSystemToUpdateList(parent); },
                Throws.ArgumentException.With.Message.Contains("to its own update list"));

            newWorld.Dispose();
        }

        class StartAndStopSystemGroup : GameSystemGroup
        {
            public List<int> Operations;
            protected override void OnCreate()
            {
                base.OnCreate();
                Operations = new List<int>(6);
            }

            protected override void OnStartRunning()
            {
                Operations.Add(0);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Operations.Add(1);
                base.OnUpdate();
            }

            protected override void OnStopRunning()
            {
                Operations.Add(2);
                base.OnStopRunning();
            }
        }

        class StartAndStopSystemA : GameSystemGroup
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(10);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(11);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(12);
                base.OnStopRunning();
            }
        }
        class StartAndStopSystemB : GameSystemGroup
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(20);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(21);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(22);
                base.OnStopRunning();
            }
        }
        class StartAndStopSystemC : GameSystemGroup
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(30);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(31);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(32);
                base.OnStopRunning();
            }
        }

        [Test]
        public void GameSystemGroup_OnStartRunningOnStopRunning_Recurses()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<StartAndStopSystemGroup>();
            var childA = newWorld.CreateSystem<StartAndStopSystemA>();
            var childB = newWorld.CreateSystem<StartAndStopSystemB>();
            var childC = newWorld.CreateSystem<StartAndStopSystemC>();
            parent.AddSystemToUpdateList(childA);
            parent.AddSystemToUpdateList(childB);
            parent.AddSystemToUpdateList(childC);
            // child C is always disabled; make sure enabling/disabling the parent doesn't change that
            childC.Enabled = false;

            // first update
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] { 0, 1, 10, 11, 20, 21 });
            parent.Operations.Clear();

            // second update with no new enabled/disabled
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] { 1, 11, 21 });
            parent.Operations.Clear();

            // parent is disabled
            parent.Enabled = false;
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] { 2, 12, 22 });
            parent.Operations.Clear();

            // parent is re-enabled
            parent.Enabled = true;
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] { 0, 1, 10, 11, 20, 21 });
            parent.Operations.Clear();

            newWorld.Dispose();
        }

        class TrackUpdatedSystem : GameSystemWithJobs
        {
            public List<GameSystemBase> Updated;

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                Updated.Add(this);
                return inputDeps;
            }
        }

        [Test]
        public void AddAndRemoveTakesEffectBeforeUpdate()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var childa = newWorld.CreateSystem<TrackUpdatedSystem>();
            var childb = newWorld.CreateSystem<TrackUpdatedSystem>();

            var updates = new List<GameSystemBase>();
            childa.Updated = updates;
            childb.Updated = updates;

            // Add 2 systems & validate Update calls
            parent.AddSystemToUpdateList(childa);
            parent.AddSystemToUpdateList(childb);
            parent.Update();

            // Order is not guaranteed
            Assert.IsTrue(updates.Count == 2 && updates.Contains(childa) && updates.Contains(childb));

            // Remove system & validate Update calls
            updates.Clear();
            parent.RemoveSystemFromUpdateList(childa);
            parent.Update();
            Assert.AreEqual(new GameSystemBase[] { childb }, updates.ToArray());

            newWorld.Dispose();
        }

        // All the ordering constraints below are valid (though some are redundant). All should sort correctly without warnings.
        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        [UpdateBefore(typeof(FirstSystem))]
        class FirstBeforeFirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        [UpdateBefore(typeof(MiddleSystem))] // redundant
        [UpdateBefore(typeof(LastSystem))] // redundant
        class FirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        [UpdateAfter(typeof(FirstSystem))]
        class FirstAfterFirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup))]
        [UpdateAfter(typeof(FirstSystem))] // redundant
        [UpdateBefore(typeof(MiddleSystem))]
        class MiddleAfterFirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup))]
        class MiddleSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup))]
        [UpdateAfter(typeof(MiddleSystem))]
        [UpdateBefore(typeof(LastSystem))] // redundant
        class MiddleBeforeLastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        [UpdateBefore(typeof(LastSystem))]
        class LastBeforeLastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        [UpdateAfter(typeof(FirstSystem))] // redundant
        [UpdateAfter(typeof(MiddleSystem))] // redundant
        class LastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        [UpdateAfter(typeof(LastSystem))]
        class LastAfterLastSystem : TestSystem { }

        [Test]
        public void GameSystemSorter_ValidUpdateConstraints_SortCorrectlyWithNoWarnings()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var systems = new List<TestSystem>
            {
                newWorld.CreateSystem<FirstBeforeFirstSystem>(),
                newWorld.CreateSystem<FirstSystem>(),
                newWorld.CreateSystem<FirstAfterFirstSystem>(),
                newWorld.CreateSystem<MiddleAfterFirstSystem>(),
                newWorld.CreateSystem<MiddleSystem>(),
                newWorld.CreateSystem<MiddleBeforeLastSystem>(),
                newWorld.CreateSystem<LastBeforeLastSystem>(),
                newWorld.CreateSystem<LastSystem>(),
                newWorld.CreateSystem<LastAfterLastSystem>(),
            };
            // Insert in reverse order
            for (int i = systems.Count - 1; i >= 0; --i)
            {
                parent.AddSystemToUpdateList(systems[i]);
            }

            parent.SortSystems();

            CollectionAssert.AreEqual(systems, parent.Systems);
            LogAssert.NoUnexpectedReceived();

            newWorld.Dispose();
        }

        // Invalid constraints
        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        class DummyFirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        class DummyLastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        [UpdateAfter(typeof(DummyLastSystem))] // can't update after an OrderLast without also being OrderLast
        class FirstAfterLastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup))]
        [UpdateBefore(typeof(DummyFirstSystem))] // can't update before an OrderFirst without also being OrderFirst
        class MiddleBeforeFirstSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup))]
        [UpdateAfter(typeof(DummyLastSystem))] // can't update after an OrderLast without also being OrderLast
        class MiddleAfterLastSystem : TestSystem { }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        [UpdateBefore(typeof(DummyFirstSystem))] // can't update before an OrderFirst without also being OrderFirst
        class LastBeforeFirstSystem : TestSystem { }

        [Test] // runtime string formatting
        public void GameSystemSorter_OrderFirstUpdateAfterOrderLast_WarnAndIgnoreConstraint()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var systems = new List<TestSystem>
            {
                newWorld.CreateSystem<FirstAfterLastSystem>(),
                newWorld.CreateSystem<DummyLastSystem>(),
            };
            // Insert in reverse order
            for (int i = systems.Count - 1; i >= 0; --i)
            {
                parent.AddSystemToUpdateList(systems[i]);
            }

            LogAssert.Expect(LogType.Warning, $"Ignoring invalid [UpdateAfter({typeof(DummyLastSystem).FullName})] attribute on {typeof(FirstAfterLastSystem).FullName} because OrderFirst/OrderLast has higher precedence.");
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();

            CollectionAssert.AreEqual(systems, parent.Systems);

            newWorld.Dispose();
        }

        [Test] // runtime string formatting
        public void GameSystemSorter_MiddleUpdateBeforeOrderFirst_WarnAndIgnoreConstraint()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var systems = new List<TestSystem>
            {
                newWorld.CreateSystem<DummyFirstSystem>(),
                newWorld.CreateSystem<MiddleBeforeFirstSystem>(),
            };
            // Insert in reverse order
            for (int i = systems.Count - 1; i >= 0; --i)
            {
                parent.AddSystemToUpdateList(systems[i]);
            }

            LogAssert.Expect(LogType.Warning, $"Ignoring invalid [UpdateBefore({typeof(DummyFirstSystem).FullName})] attribute on {typeof(MiddleBeforeFirstSystem).FullName} because OrderFirst/OrderLast has higher precedence.");
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();
            CollectionAssert.AreEqual(systems, parent.Systems);

            newWorld.Dispose();
        }

        [Test] // runtime string formatting
        public void GameSystemSorter_MiddleUpdateAfterOrderLast_WarnAndIgnoreConstraint()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var systems = new List<TestSystem>
            {
                newWorld.CreateSystem<MiddleAfterLastSystem>(),
                newWorld.CreateSystem<DummyLastSystem>(),
            };
            // Insert in reverse order
            for (int i = systems.Count - 1; i >= 0; --i)
            {
                parent.AddSystemToUpdateList(systems[i]);
            }

            LogAssert.Expect(LogType.Warning, $"Ignoring invalid [UpdateAfter({typeof(DummyLastSystem).FullName})] attribute on {typeof(MiddleAfterLastSystem).FullName} because OrderFirst/OrderLast has higher precedence.");
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();
            CollectionAssert.AreEqual(systems, parent.Systems);

            newWorld.Dispose();
        }

        [Test] // runtime string formatting
        public void GameSystemSorter_OrderLastUpdateBeforeOrderFirst_WarnAndIgnoreConstraint()
        {
            var newWorld = new GameSystemWorld();

            var parent = newWorld.CreateSystem<TestGroup>();
            var systems = new List<TestSystem>
            {
                newWorld.CreateSystem<DummyFirstSystem>(),
                newWorld.CreateSystem<LastBeforeFirstSystem>(),
            };
            // Insert in reverse order
            for (int i = systems.Count - 1; i >= 0; --i)
            {
                parent.AddSystemToUpdateList(systems[i]);
            }

            LogAssert.Expect(LogType.Warning, $"Ignoring invalid [UpdateBefore({typeof(DummyFirstSystem).FullName})] attribute on {typeof(LastBeforeFirstSystem).FullName} because OrderFirst/OrderLast has higher precedence.");
            parent.SortSystems();
            LogAssert.NoUnexpectedReceived();
            CollectionAssert.AreEqual(systems, parent.Systems);

            newWorld.Dispose();
        }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        class OFL_A : TestSystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        class OFL_B : TestSystem
        {
        }

        class OFL_C : TestSystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        class OFL_D : TestSystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        class OFL_E : TestSystem
        {
        }

        [Test]
        public void OrderFirstLastWorks([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 30, 31)] int bits)
        {
            var newWorld = new GameSystemWorld();
            var parent = newWorld.CreateSystem<TestGroup>();

            // Add in reverse order
            if (0 != (bits & (1 << 4))) { parent.AddSystemToUpdateList(newWorld.CreateSystem<OFL_E>()); }
            if (0 != (bits & (1 << 3))) { parent.AddSystemToUpdateList(newWorld.CreateSystem<OFL_D>()); }
            if (0 != (bits & (1 << 2))) { parent.AddSystemToUpdateList(newWorld.CreateSystem<OFL_C>()); }
            if (0 != (bits & (1 << 1))) { parent.AddSystemToUpdateList(newWorld.CreateSystem<OFL_B>()); }
            if (0 != (bits & (1 << 0))) { parent.AddSystemToUpdateList(newWorld.CreateSystem<OFL_A>()); }

            parent.SortSystems();

            // Ensure they are always in alphabetical order
            string prev = null;
            foreach (var sys in parent.Systems)
            {
                var curr = sys.GetType().Name;
                // we know that only the last character will be different
                int len = curr.Length;
                Assert.IsTrue(prev == null || (prev[len - 1] < curr[len - 1]));
                prev = curr;
            }

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_RemoveThenReAddManagedSystem_SystemIsInGroup()
        {
            var newWorld = new GameSystemWorld();

            var group = newWorld.CreateSystem<TestGroup>();
            var sys = newWorld.CreateSystem<TestSystem>();
            group.AddSystemToUpdateList(sys);

            group.RemoveSystemFromUpdateList(sys);
            group.AddSystemToUpdateList(sys);
            // This is where removals are processed
            group.SortSystems();
            var expectedSystems = new List<GameSystemBase> { sys };
            CollectionAssert.AreEqual(expectedSystems, group.Systems);

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_RemoveSystemNotInGroup_Ignored()
        {
            var newWorld = new GameSystemWorld();

            var group = newWorld.CreateSystem<TestGroup>();
            var sys = newWorld.CreateSystem<TestSystem>();
            // group.AddSystemToUpdateList(sys); // the point here is to remove a system _not_ in the group
            group.RemoveSystemFromUpdateList(sys);
            Assert.AreEqual(0, group.m_systemsToRemove.Count);

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_DuplicateRemove_Ignored()
        {
            var newWorld = new GameSystemWorld();

            var group = newWorld.CreateSystem<TestGroup>();
            var sys = newWorld.CreateSystem<TestSystem>();
            group.AddSystemToUpdateList(sys);

            group.RemoveSystemFromUpdateList(sys);
            group.RemoveSystemFromUpdateList(sys);
            var expectedSystems = new List<GameSystemBase> { sys };
            CollectionAssert.AreEqual(expectedSystems, group.m_systemsToRemove);

            newWorld.Dispose();
        }

        class ParentSystemGroup : GameSystemGroup
        {
        }

        class ChildSystemGroup : GameSystemGroup
        {
        }

        [Test]
        public void GameSystemGroup_SortCleanParentWithDirtyChild_ChildIsSorted()
        {
            var newWorld = new GameSystemWorld();

            var parentGroup = newWorld.CreateSystem<ParentSystemGroup>();
            var childGroup = newWorld.CreateSystem<ChildSystemGroup>();
            parentGroup.AddSystemToUpdateList(childGroup); // parent group sort order is dirty
            parentGroup.SortSystems(); // parent group sort order is clean

            var child1 = newWorld.CreateSystem<Sibling1System>();
            var child2 = newWorld.CreateSystem<Sibling2System>();
            childGroup.AddSystemToUpdateList(child1); // child group sort order is dirty
            childGroup.AddSystemToUpdateList(child2);
            parentGroup.SortSystems(); // parent and child group sort orders should be clean

            // If the child group's systems aren't in the correct order, it wasn't recursively sorted by the parent group.
            CollectionAssert.AreEqual(new TestSystem[] { child2, child1 }, childGroup.Systems);

            newWorld.Dispose();
        }

        class NoSortGroup : GameSystemGroup
        {
            public NoSortGroup()
            {
                EnableSystemSorting = false;
            }
        }

        [Test]
        public void GameSystemGroup_SortManuallySortedParentWithDirtyChild_ChildIsSorted()
        {
            var newWorld = new GameSystemWorld();

            var parentGroup = newWorld.CreateSystem<NoSortGroup>();
            var childGroup = newWorld.CreateSystem<ChildSystemGroup>();
            parentGroup.AddSystemToUpdateList(childGroup);

            var child1 = newWorld.CreateSystem<Sibling1System>();
            var child2 = newWorld.CreateSystem<Sibling2System>();
            childGroup.AddSystemToUpdateList(child1); // child group sort order is dirty
            childGroup.AddSystemToUpdateList(child2);
            parentGroup.SortSystems(); // parent and child group sort orders should be clean

            // If the child group's systems aren't in the correct order, it wasn't recursively sorted by the parent group.
            CollectionAssert.AreEqual(new TestSystem[] { child2, child1 }, childGroup.Systems);

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_RemoveFromManuallySortedGroup_Throws()
        {
            var newWorld = new GameSystemWorld();

            var group = newWorld.CreateSystem<NoSortGroup>();
            var sys = newWorld.CreateSystem<TestSystem>();
            group.AddSystemToUpdateList(sys);
            Assert.Throws<InvalidOperationException>(() => group.RemoveSystemFromUpdateList(sys));

            newWorld.Dispose();
        }

        [Test]
        public void GameSystemGroup_DisableAutoSorting_UpdatesInInsertionOrder()
        {
            var newWorld = new GameSystemWorld();

            var noSortGroup = newWorld.CreateSystem<NoSortGroup>();
            var child1 = newWorld.CreateSystem<Sibling1System>();
            var child2 = newWorld.CreateSystem<Sibling2System>();
            var child3 = newWorld.CreateSystem<Sibling3System>();
            noSortGroup.AddSystemToUpdateList(child1);
            noSortGroup.AddSystemToUpdateList(child2);
            noSortGroup.AddSystemToUpdateList(child3);
            // Just adding the systems should cause them to be updated in insertion order
            var expectedUpdateList = new TestSystem[] { child1, child2, child3 };
            CollectionAssert.AreEqual(expectedUpdateList, noSortGroup.Systems);
            for (int i = 0; i < expectedUpdateList.Length; ++i)
            {
                Assert.AreEqual(expectedUpdateList[i], noSortGroup.m_systemsToUpdate[i]);
            }
            // Sorting the system group should have no effect on the update order
            noSortGroup.SortSystems();
            CollectionAssert.AreEqual(new TestSystem[] { child1, child2, child3 }, noSortGroup.Systems);
            for (int i = 0; i < expectedUpdateList.Length; ++i)
            {
                Assert.AreEqual(expectedUpdateList[i], noSortGroup.m_systemsToUpdate[i]);
            }

            newWorld.Dispose();
        }
    }
}
