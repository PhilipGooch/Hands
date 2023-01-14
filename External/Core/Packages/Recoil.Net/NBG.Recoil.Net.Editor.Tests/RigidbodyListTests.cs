using NBG.Core.Streams;
using NBG.Entities;
using NBG.Net;
using NUnit.Framework;
using Recoil;
using UnityEngine;

namespace NBG.Recoil.Net.Editor.Tests
{
    public class RigidbodyListTests
    {
        [SetUp]
        public void Setup()
        {
            // Initialize entity world
            ManagedWorld.Create(16);
            EntityStore.Create(10, 500);
        }

        [TearDown]
        public void TearDown()
        {
            // Shutdown entity world
            EntityStore.Destroy();
            ManagedWorld.Destroy();
        }

        [Test]
        public void RigidbodyList_CountWorks()
        {
            // Hierarchy
            var rootGo = new GameObject("root");

            var go0 = new GameObject("go0");
            go0.transform.parent = rootGo.transform;
            var rb0 = go0.AddComponent<Rigidbody>();

            RigidbodyRegistration.RegisterHierarchy(rootGo);

            // Checks
            var rbList = RigidbodyList.BuildFrom(rootGo.transform);
            Assert.IsTrue(rbList.Count == 1);
        }

        [Test]
        public void RigidbodyList_KinematicModesWork()
        {
            // Hierarchy
            var rootGo = new GameObject("root");

            var go0 = new GameObject("go0");
            go0.transform.parent = rootGo.transform;
            var rb0 = go0.AddComponent<Rigidbody>();

            RigidbodyRegistration.RegisterHierarchy(rootGo);

            // Checks
            var rbList = RigidbodyList.BuildFrom(rootGo.transform);
            Assert.IsTrue(rb0.isKinematic == false);
            rbList.SetKinematic();
            Assert.IsTrue(rb0.isKinematic == true);
            rbList.ResetKinematic();
            Assert.IsTrue(rb0.isKinematic == false);
        }

        [Test]
        public void RigidbodyList_BasicSerializationWorks()
        {
            // Hierarchy
            var rootGo = new GameObject("root");

            var go0 = new GameObject("go0");
            go0.transform.parent = rootGo.transform;
            var rb0 = go0.AddComponent<Rigidbody>();

            RigidbodyRegistration.RegisterHierarchy(rootGo);

            // Checks
            var rbList = RigidbodyList.BuildFrom(rootGo.transform);

            var rbStreamer = (INetStreamer)rbList;
            var stream = BasicStream.Allocate(1024);

            var position = new Vector3(1, -2, 3);
            var rotation = Quaternion.Euler(15, 20, -25);
            var velocity = new Vector3(-4, 5, 6);
            rb0.position = position;
            rb0.rotation = rotation;
            rb0.velocity = velocity;
            ManagedWorld.main.ResyncPhysXBody(rb0);
            rbStreamer.CollectState(stream);
            rb0.position = Vector3.zero;
            rb0.rotation = Quaternion.identity;
            rb0.velocity = Vector3.zero;
            stream.Flip();
            rbStreamer.ApplyState(stream);
            ManagedWorld.main.WriteState();

            Assert.IsTrue(Mathf.Abs(rb0.position.x - position.x) < 0.005f);
            Assert.IsTrue(Mathf.Abs(rb0.position.y - position.y) < 0.005f);
            Assert.IsTrue(Mathf.Abs(rb0.position.z - position.z) < 0.005f);

            var angle = Quaternion.Angle(rb0.rotation, rotation);
            Assert.IsTrue(angle < 0.005f);

            Assert.IsTrue(rb0.velocity == Vector3.zero); // Velocity is lost when using ApplyState.
        }
    }
}
