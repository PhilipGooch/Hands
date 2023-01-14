using NUnit.Framework;
using Unity.Jobs;
using UnityEngine.LowLevel;

namespace NBG.Core.GameSystems.Tests
{
    public class GameSystemWithJobsTests
    {
        GameSystemWorld World = null;

        [SetUp]
        public void Setup()
        {
            World = new GameSystemWorld();
        }

        [TearDown]
        public void TearDown()
        {
            World.Dispose();
            World = null;
        }



        class TestData
        {
        }

        class TestGroup : GameSystemGroup
        {
        }

        class WriterSystem1 : GameSystemWithJobs
        {
            public WriterSystem1()
            {
                WritesData(typeof(TestData));
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                return new Job().Schedule(inputDeps);
            }

            public struct Job : IJob
            {
                public void Execute()
                {
                }
            }
        }

        class WriterSystem2 : WriterSystem1 { };

        [UpdateAfter(typeof(WriterSystem1))]
        class ReaderSystem1 : GameSystem
        {
            public ReaderSystem1()
            {
                ReadsData(typeof(TestData));
            }

            protected override void OnUpdate()
            {
            }
        }

        class ReaderSystem2 : ReaderSystem1 { };

        [Test]
        public void GameSystem_DependenciesAreRegistered()
        {
            var child1 = World.CreateSystem<WriterSystem1>();
            Assert.AreEqual(child1.Reads.Count, 0);
            Assert.AreEqual(child1.Writes.Count, 1);

            var child2 = World.CreateSystem<ReaderSystem1>();
            Assert.AreEqual(child2.Reads.Count, 1);
            Assert.AreEqual(child2.Writes.Count, 0);
        }

        //TODO: figure out how to validate if job dependencies are correct
        /*[Test]
        public void GameSystem_ReaderWaitsForWriter()
        {
            var parent = World.CreateSystem<TestGroup>();
            var write1 = World.CreateSystem<WriterSystem1>();
            var read1 = World.CreateSystem<ReaderSystem1>();
            parent.AddSystemToUpdateList(write1);
            parent.AddSystemToUpdateList(read1);
            parent.Update();

            var child1write = write1._state.lastJobHandle;
            var readingDep = World.DependencyManager.GetReadingDependency(typeof(TestData));
            Assert.IsTrue(JobHandle.CheckFenceIsDependencyOrDidSyncFence(readingDep, child1write));
        }

        [Test]
        public void GameSystem_ReadersWaitForWriterButNotEachOther()
        {
            var parent = World.CreateSystem<TestGroup>();
            var write1 = World.CreateSystem<WriterSystem1>();
            var read1 = World.CreateSystem<ReaderSystem1>();
            var read2 = World.CreateSystem<ReaderSystem2>();
            parent.AddSystemToUpdateList(write1);
            parent.AddSystemToUpdateList(read1);
            parent.AddSystemToUpdateList(read2);
            parent.Update();

            var write1handle = write1._state.lastJobHandle;
            var read1handle = World.DependencyManager.GetReadingDependency(typeof(TestData));
            var read2handle = World.DependencyManager.GetReadingDependency(typeof(TestData));
            Assert.IsTrue(JobHandle.CheckFenceIsDependencyOrDidSyncFence(read1handle, write1handle));
            Assert.IsTrue(JobHandle.CheckFenceIsDependencyOrDidSyncFence(read2handle, write1handle));
            Assert.IsFalse(JobHandle.CheckFenceIsDependencyOrDidSyncFence(read1handle, read2handle));
            Assert.IsFalse(JobHandle.CheckFenceIsDependencyOrDidSyncFence(read2handle, read1handle));
        }*/
    }
}
