using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

namespace Tests
{
    public class DestructibleProcessorTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ThreeCubeLineConnectedWithTwoJoints()
        {
            var line = CreateThreeCubeLine();

            DestructibleProcessor.ProcessDestructible(line);

            var joints = line.GetComponentsInChildren<Joint>();
            Assert.AreEqual(2, joints.Length, "Joint count incorrect!");
        }

        [Test]
        public void ThreeCubeLineConnectedInCorrectSequence()
        {
            var line = CreateThreeCubeLine();

            DestructibleProcessor.ProcessDestructible(line);

            var rigidbodies = line.GetComponentsInChildren<Rigidbody>();
            var joints = line.GetComponentsInChildren<Joint>();
            Assert.IsTrue(joints[0].connectedBody == rigidbodies[1] || joints[1].connectedBody == rigidbodies[0]);
            Assert.IsTrue(joints[1].connectedBody == rigidbodies[2] || joints[2].connectedBody == rigidbodies[1]);
        }

        [Test]
        public void TranslatedLineConnectedWithTwoJoints()
        {
            var line = CreateThreeCubeLine();
            line.transform.position = new Vector3(100, 50, -23);

            DestructibleProcessor.ProcessDestructible(line);

            var joints = line.GetComponentsInChildren<Joint>();
            Assert.AreEqual(2, joints.Length, "Joint count incorrect!");
        }

        [Test]
        public void RotatedLineConnectedWithTwoJoints()
        {
            var line = CreateThreeCubeLine();
            line.transform.rotation = Quaternion.Euler(45, -45, 22);

            DestructibleProcessor.ProcessDestructible(line);

            var joints = line.GetComponentsInChildren<Joint>();
            Assert.AreEqual(2, joints.Length, "Joint count incorrect!");
        }

        [Test]
        public void TranslatedAndRotatedLineConnectedWithTwoJoints()
        {
            var line = CreateThreeCubeLine();
            line.transform.position = new Vector3(-10, 22.5f, 11.573f);
            line.transform.rotation = Quaternion.Euler(-35f, 66.7f, 790f);

            DestructibleProcessor.ProcessDestructible(line);

            var joints = line.GetComponentsInChildren<Joint>();
            Assert.AreEqual(2, joints.Length, "Joint count incorrect!");
        }

        [Test]
        public void Cube2x2ConnectedWith12Joints()
        {
            var cube = CreateNCube(2);

            DestructibleProcessor.ProcessDestructible(cube);

            var joints = cube.GetComponentsInChildren<Joint>();
            Assert.AreEqual(12, joints.Length, "Joint count incorrect!");
        }

        [Test]
        public void Cube3x3ConnectedWith54Joints()
        {
            var cube = CreateNCube(3);

            DestructibleProcessor.ProcessDestructible(cube);

            var joints = cube.GetComponentsInChildren<Joint>();
            Assert.AreEqual(54, joints.Length, "Joint count incorrect!");
        }

        GameObject CreateNCube(int n)
        {
            var parent = new GameObject();
            parent.transform.position = Vector3.zero;
            int counter = 0;
            for(int x = 0; x < n; x++)
            {
                for(int y = 0; y < n; y++)
                {
                    for(int z = 0; z < n; z++)
                    {
                        var cube = CreateCube(new Vector3(x, y, z), "Cube" + counter);
                        cube.transform.parent = parent.transform;
                        counter++;
                    }
                }
            }
            return parent;
        }

        GameObject CreateThreeCubeLine()
        {
            var parent = new GameObject();
            parent.transform.position = Vector3.zero;
            for(int i = 0; i < 3; i++)
            {
                var cube = CreateCube(new Vector3(i, 0, 0), "Cube" + i);
                cube.transform.parent = parent.transform;
            }
            return parent;
        }

        GameObject CreateCube(Vector3 position, string name)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = position;
            obj.name = name;
            return obj;
        }
    }
}
