using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using System;
using System.Reflection;
using Unity.Collections;
using System.Collections.Generic;

namespace NBG.MeshGeneration.Tests
{
    public class HoleBoundsTest
    {
        [Test]
        public void BoundsGeneration()
        {
            List<float3> vertices = new List<float3>();

            vertices.Add(new float3(4f, 4f, 0f));
            vertices.Add(new float3(5f, 5f, 0f));
            vertices.Add(new float3(6f, 4f, 0f));
            vertices.Add(new float3(5f, 3f, 0f));

            HoleData hole = new HoleData(vertices);

            Assert.AreEqual(hole.boundingRect, new float4(4f - HoleData.margin, 3f - HoleData.margin, 6f + HoleData.margin, 5f + HoleData.margin));
        }

        [Test]
        public void Casts()
        {
            List<float3> vertices = new List<float3>();

            vertices.Add(new float3(4f, 4f, 0f));
            vertices.Add(new float3(5f, 5f, 0f));
            vertices.Add(new float3(6f, 4f, 0f));
            vertices.Add(new float3(5f, 3f, 0f));

            HoleData hole = new HoleData(vertices);

            Assert.IsTrue(hole.CastSegmentToBounds(new float3(4f, 4f, 0f), new float3(4f, 10f, 0f)));
            Assert.IsTrue(hole.CastSegmentToBounds(new float3(4f, 10f, 0f), new float3(4f, 4f, 0f)));
            Assert.IsTrue(hole.CastSegmentToBounds(new float3(14f, 4f, 0f), new float3(4f, 4f, 0f)));
            Assert.IsTrue(hole.CastSegmentToBounds(new float3(2f, 2f, 0f), new float3(10f, 10f, 0f)));
            Assert.IsTrue(hole.CastSegmentToBounds(new float3(4f, 10f, 0f), new float3(4f, 1f, 0f)));
            Assert.IsTrue(hole.CastSegmentToBounds(new float3(14f, 4f, 0f), new float3(1f, 4f, 0f)));


            Assert.IsFalse(hole.CastSegmentToBounds(new float3(10f, 4f, 0f), new float3(4f, 10f, 0f)));
            Assert.IsFalse(hole.CastSegmentToBounds(new float3(0f, 0f, 0f), new float3(0f, 10f, 0f)));
            Assert.IsFalse(hole.CastSegmentToBounds(new float3(0f, 0f, 0f), new float3(10f, 0f, 0f)));

            CastAssert(new float4(-4.889092f, -0.6087302f, -4.049635f, -0.1338793f), new float3(0.2417707f, 0.01227908f, 0f), new float3(-5.151737f, -0.2748535f, 0f));
            CastAssert(new float4(-3.759551f, -0.8800972f, -2.746665f, -0.03961772f), new float3(0.2417707f, 0.01227908f, 0f), new float3(-5.151737f, -0.2748535f, 0f));
            CastAssert(new float4(2.89426f, -1.076409f, 3.27426f, -0.7164092f), new float3(6.215196f, 1.677929f, 0f), new float3(2.663888f, -1.156889f, 0f));

        }

        public void CastAssert(float4 bounds, float3 a, float3 b)
        {
            HoleData hole = new HoleData(new List<float3>());
            hole.boundingRect = bounds;
            Assert.IsTrue(hole.CastSegmentToBounds(a, b), "Bounds cast should be true");
        }
    }
}
