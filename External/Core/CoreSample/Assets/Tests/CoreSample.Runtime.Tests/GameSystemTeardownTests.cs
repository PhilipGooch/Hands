using NBG.Core.Events;
using NBG.Core.GameSystems;
using NBG.Entities;
using NUnit.Framework;
using Recoil;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace NBG.Core.Tests
{
    public class GameSystemTeardownTests
    {
        [SetUp]
        public void Setup()
        {
            // Initialize entity world
            ManagedWorld.Create(16);
            EntityStore.Create(10, 500);

            // Initialize game system world
            EventBus.Create();
            GameSystemWorldDefault.Create();
            Recoil.RecoilSystems.Initialize(GameSystemWorldDefault.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            // Shutdown game system world
            Recoil.RecoilSystems.Shutdown();
            GameSystemWorldDefault.Destroy();
            EventBus.Destroy();

            // Shutdown entity world
            EntityStore.Destroy();
            ManagedWorld.Destroy();
        }

        [Test]
        public void RegularTestShutsDownCorrectly()
        {
        }

        [UnityTest]
        public IEnumerator UnityTestShutsDownCorrectly()
        {
            yield return null;
        }

        [UnityTest]
        public IEnumerator YieldWaitForFixedUpdateShutsDownCorrectly()
        {
            yield return new WaitForFixedUpdate();
        }
    }
}
