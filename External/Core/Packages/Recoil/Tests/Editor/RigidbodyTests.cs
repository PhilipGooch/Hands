using NUnit.Framework;
using UnityEngine;

namespace NBG.Recoil.Editor.Tests
{
    public class RigidbodyTests
    {
        [Test]
        public void RigidbodyWithoutColliderHasInertiaTensorOfOne()
        {
            var go = new GameObject();
            var rb = go.AddComponent<Rigidbody>();
            Assert.IsTrue(rb.inertiaTensor == Vector3.one);
            GameObject.DestroyImmediate(go);
        }

        [Test]
        public void RigidbodyWithZeroMassHasInertiaTensorOfOne()
        {
            var go = new GameObject();
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.0f;
            Assert.IsTrue(rb.inertiaTensor == Vector3.one);
            GameObject.DestroyImmediate(go);
        }
    }
}
