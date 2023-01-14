using NUnit.Framework;
using System;
using UnityEngine.LowLevel;

namespace NBG.Core.GameSystems.Tests
{
    public class GameSystemTests
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



        class TestSystem : GameSystem
        {
            public bool Created = false;

            protected override void OnUpdate()
            {
            }

            protected override void OnCreate()
            {
                Created = true;
            }

            protected override void OnDestroy()
            {
                Created = false;
            }
        }

        class DerivedTestSystem : TestSystem
        {
            protected override void OnUpdate()
            {
            }
        }

        class ThrowExceptionSystem : TestSystem
        {
            protected override void OnCreate()
            {
                throw new System.Exception();
            }

            protected override void OnUpdate()
            {
            }
        }

        class NotEvenASystem
        {
        }



        [Test]
        public void Create()
        {
            var system = World.CreateSystem<TestSystem>();
            Assert.AreEqual(system, World.GetExistingSystem<TestSystem>());
            Assert.IsTrue(system.Created);
        }

        [Test]
        public void GameSystem_CheckExistsAfterDestroy_CorrectMessage()
        {
            var destroyedSystem = World.CreateSystem<TestSystem>();
            World.DestroySystem(destroyedSystem);
            Assert.That(() => { destroyedSystem.ShouldRunSystem(); },
                Throws.InvalidOperationException.With.Message.Contains("destroyed"));
        }

        [Test]
        public void GameSystem_CheckExistsBeforeCreate_CorrectMessage()
        {
            var incompleteSystem = new TestSystem();
            Assert.That(() => { incompleteSystem.ShouldRunSystem(); },
                Throws.InvalidOperationException.With.Message.Contains("initialized"));
        }

        [Test]
        public void CreateAndDestroy()
        {
            var system = World.CreateSystem<TestSystem>();
            World.DestroySystem(system);
            Assert.AreEqual(null, World.GetExistingSystem<TestSystem>());
            Assert.IsFalse(system.Created);
        }

        [Test]
        public void GetOrCreateSystemReturnsSameSystem()
        {
            var system = World.GetOrCreateSystem<TestSystem>();
            Assert.AreEqual(system, World.GetOrCreateSystem<TestSystem>());
        }

        [Test]
        public void InheritedSystem()
        {
            var system = World.CreateSystem<DerivedTestSystem>();
            Assert.AreEqual(system, World.GetExistingSystem<DerivedTestSystem>());
            Assert.AreEqual(system, World.GetExistingSystem<TestSystem>());

            World.DestroySystem(system);

            Assert.AreEqual(null, World.GetExistingSystem<DerivedTestSystem>());
            Assert.AreEqual(null, World.GetExistingSystem<TestSystem>());

            Assert.IsFalse(system.Created);
        }

        [Test]
        public void CreateNonSystemThrows()
        {
            Assert.Throws<ArgumentException>(() => { World.CreateSystem(typeof(NotEvenASystem)); });
        }

        [Test]
        public void GetOrCreateNonSystemThrows()
        {
            Assert.Throws<ArgumentException>(() => { World.GetOrCreateSystem(typeof(NotEvenASystem)); });
        }

        [Test]
        public void OnCreateThrowRemovesSystem()
        {
            Assert.Throws<Exception>(() => { World.CreateSystem<ThrowExceptionSystem>(); });
            Assert.AreEqual(null, World.GetExistingSystem<ThrowExceptionSystem>());
        }

        [Test]
        public void DestroySystemTwiceThrows()
        {
            var system = World.CreateSystem<TestSystem>();
            World.DestroySystem(system);
            Assert.Throws<ArgumentException>(() => World.DestroySystem(system));
        }

        [Test]
        public void CreateTwoSystemsOfSameType()
        {
            var systemA = World.CreateSystem<TestSystem>();
            var systemB = World.CreateSystem<TestSystem>();
            // CreateSystem makes a new system
            Assert.AreNotEqual(systemA, systemB);
            // Return first system
            Assert.AreEqual(systemA, World.GetOrCreateSystem<TestSystem>());
        }

        [Test]
        public void CreateTwoSystemsAfterDestroyReturnSecond()
        {
            var systemA = World.CreateSystem<TestSystem>();
            var systemB = World.CreateSystem<TestSystem>();
            World.DestroySystem(systemA);

            Assert.AreEqual(systemB, World.GetExistingSystem<TestSystem>()); ;
        }

        [Test]
        public void CreateTwoSystemsAfterDestroyReturnFirst()
        {
            var systemA = World.CreateSystem<TestSystem>();
            var systemB = World.CreateSystem<TestSystem>();
            World.DestroySystem(systemB);

            Assert.AreEqual(systemA, World.GetExistingSystem<TestSystem>()); ;
        }

        [Test]
        public void UpdateDestroyedSystemThrows()
        {
            var system = World.CreateSystem<TestSystem>();
            World.DestroySystem(system);
            Assert.Throws<InvalidOperationException>(system.Update);
        }
    }
}
