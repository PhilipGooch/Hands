using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using NBG.Core;

namespace NBG.Core.Editor.Tests
{
    public class BoxBoundsTests
    {
        [Test]
        public void UnitBoundsContainsZeroPoint()
        {
            Vector3 targetPoint = Vector3.zero;
            var bounds = CreateUnitBounds(Vector3.zero);
            AssertContains(bounds, targetPoint);
        }

        [Test]
        public void UnitBoundsDoesNotContainOffsetPoint()
        {
            Vector3 targetPoint = Vector3.one;
            var bounds = CreateUnitBounds(Vector3.zero);
            AssertDoesNotContain(bounds, targetPoint);
        }

        [Test]
        public void OffsetUnitBoundsContainsOffsetPoint()
        {
            Vector3 targetPoint = Vector3.one * 5f;
            var bounds = CreateUnitBounds(Vector3.one * 5f);
            AssertContains(bounds, targetPoint);
        }

        [Test]
        public void OffsetUnitBoundsDoesNotContainZeroPoint()
        {
            Vector3 targetPoint = Vector3.zero;
            var bounds = CreateUnitBounds(Vector3.one * 5f);
            AssertDoesNotContain(bounds, targetPoint);
        }

        [Test]
        public void ScaledBoundsContainsOffsetPoint()
        {
            Vector3 targetPoint = new Vector3(5, 0, 0);
            var bounds = CreateBounds(Vector3.zero, new Vector3(12, 1, 1), Quaternion.identity);
            AssertContains(bounds, targetPoint);
        }

        [Test]
        public void BoundsScaledInDifferentDirectionDoesNotContainOffsetPoint()
        {
            Vector3 targetPoint = new Vector3(5, 0, 0);
            var bounds = CreateBounds(Vector3.zero, new Vector3(1, 12, 1), Quaternion.identity);
            AssertDoesNotContain(bounds, targetPoint);
        }

        [Test]
        public void ScaledRotatedBoundsContainsOffsetPoint()
        {
            Vector3 targetPoint = new Vector3(0, 0, 5);
            var bounds = CreateBounds(Vector3.zero, new Vector3(12, 1, 1), Quaternion.Euler(0, 90, 0));
            AssertContains(bounds, targetPoint);
        }

        [Test]
        public void ScaledBoundsRotatedInWrongDirectionDoesNotContainOffsetPoint()
        {
            Vector3 targetPoint = new Vector3(0, 0, 5);
            var bounds = CreateBounds(Vector3.zero, new Vector3(12, 1, 1), Quaternion.Euler(90, 0, 0));
            AssertDoesNotContain(bounds, targetPoint);
        }

        [Test]
        public void UnitBoundsClosestPointWithZeroPointReturnsZeroPoint()
        {
            Vector3 targetPoint = Vector3.zero;
            var bounds = CreateUnitBounds(Vector3.zero);
            AssertClosestPoint(bounds, targetPoint, Vector3.zero);
        }

        [Test]
        public void UnitBoundsClosestPointNearCornerReturnsCorner()
        {
            Vector3 targetPoint = Vector3.one;
            var bounds = CreateUnitBounds(Vector3.zero);
            AssertClosestPoint(bounds, targetPoint, new Vector3(0.5f, 0.5f, 0.5f));
        }

        [Test]
        public void UnitBoundsClosestPointNearWallReturnsWallCenter()
        {
            Vector3 targetPoint = new Vector3(1, 0, 0);
            var bounds = CreateUnitBounds(Vector3.zero);
            AssertClosestPoint(bounds, targetPoint, new Vector3(0.5f, 0, 0));
        }

        [Test]
        public void ScaledBoundsClosestPointNearCornerReturnsCorner()
        {
            Vector3 targetPoint = new Vector3(5f, 5f, 5f);
            var bounds = CreateBounds(Vector3.zero, new Vector3(3, 1, 1), Quaternion.identity);
            AssertClosestPoint(bounds, targetPoint, new Vector3(1.5f, 0.5f, 0.5f));
        }

        [Test]
        public void OffsetBoundsClosestPointOnOtherSideReturnsOtherWall()
        {
            Vector3 targetPoint = new Vector3(2f, 0f, 0f);
            var bounds = CreateUnitBounds(new Vector3(5f, 0f,0f));
            AssertClosestPoint(bounds, targetPoint, new Vector3(4.5f, 0.0f, 0.0f));
        }

        [Test]
        public void RotatedBoundsPointNearCornerReturnsCorner()
        {
            Vector3 targetPoint = new Vector3(3f, 3f, 0f);
            var bounds = CreateBounds(Vector3.zero, Vector3.one, Quaternion.Euler(0, 45, 0));
            AssertClosestPoint(bounds, targetPoint, new Vector3(new Vector2(0.5f,0.5f).magnitude, 0.5f, 0f));
        }

        [Test]
        public void ScaledBoundsMinPointMatchesCorner()
        {
            var bounds = CreateBounds(Vector3.zero, Vector3.one * 5f, Quaternion.identity);
            AssertPointsEqual(bounds.min, new Vector3(-2.5f, -2.5f, -2.5f));
        }

        [Test]
        public void RotatedBoundsMaxPointMatchesCorner()
        {
            var bounds = CreateBounds(Vector3.zero, Vector3.one, Quaternion.Euler(0, 45, 0));
            AssertPointsEqual(bounds.max, new Vector3(new Vector2(0.5f, 0.5f).magnitude, 0.5f, 0f));
        }

        [Test]
        public void MovedBoundsMinPointReturnsCorner()
        {
            var position = new Vector3(1, 2, 3);
            var bounds = CreateUnitBounds(position);
            AssertPointsEqual(bounds.min, position - Vector3.one * 0.5f);
        }

        [Test]
        public void EncapsulateXPointEncapsulatesPoint()
        {
            var bounds = CreateUnitBounds(Vector3.zero);
            var point = new Vector3(1, 0, 0);

            bounds.Encapsulate(point);

            AssertPointsEqual(bounds.size, new Vector3(1.5f, 1f, 1f));
            AssertPointsEqual(bounds.center, new Vector3(0.25f, 0f, 0f));
            AssertContains(bounds, point);
        }

        [Test]
        public void EncapsulateNegativeYPointEncapsulatesPoint()
        {
            var bounds = CreateUnitBounds(Vector3.zero);
            var point = new Vector3(0, -2, 0);

            bounds.Encapsulate(point);

            AssertPointsEqual(bounds.size, new Vector3(1f, 2.5f, 1f));
            AssertPointsEqual(bounds.center, new Vector3(0f, -0.75f, 0f));
            AssertContains(bounds, point);
        }

        [Test]
        public void EncapsulatePointAlreadyInsideDoesNothing()
        {
            var bounds = CreateUnitBounds(Vector3.zero);
            var point = new Vector3(0.1f, 0.25f, 0.1f);

            bounds.Encapsulate(point);

            AssertPointsEqual(bounds.size, Vector3.one);
            AssertPointsEqual(bounds.center, Vector3.zero);
            AssertContains(bounds, point);
        }

        [Test]
        public void EncapsulatePointRotatedBoundsEncapsulatesCorrectly()
        {
            var bounds = CreateBounds(Vector3.zero, Vector3.one, Quaternion.Euler(0f, 45f, 0f));
            var point = new Vector3(1f, 0f, 1f);

            bounds.Encapsulate(point);

            var dist = Mathf.Sqrt(2f);
            AssertPointsEqual(bounds.size, new Vector3(1f, 1f, 1f + dist - 0.5f));
            AssertPointsEqual(bounds.center, new Vector3(0.323f, 0f, 0.323f));
            AssertContains(bounds, point);
        }

        [Test]
        public void EncapsulateOtherBoundsCompletelyOverlapsBecomesOtherBounds()
        {
            var bounds = CreateUnitBounds(Vector3.zero);
            var otherBounds = CreateBounds(Vector3.zero, new Vector3(2f, 3f, 4f), Quaternion.identity);

            bounds.Encapsulate(otherBounds);

            AssertPointsEqual(bounds.size, otherBounds.size);
            AssertPointsEqual(bounds.center, otherBounds.center);
        }

        [Test]
        public void EncapsulateCubeNearbyBecomesLongBlock()
        {
            var bounds = CreateUnitBounds(Vector3.zero);
            var otherBounds = CreateUnitBounds(new Vector3(1, 0, 0));

            bounds.Encapsulate(otherBounds);

            AssertPointsEqual(bounds.size, new Vector3(2f, 1f, 1f));
            AssertPointsEqual(bounds.center, new Vector3(0.5f, 0f, 0f));
        }

        [Test]
        public void EncapsulateRotatedCubeBecomesLongBlock()
        {
            var bounds = CreateBounds(Vector3.zero, Vector3.one, Quaternion.Euler(0f, 90f, 0f));
            var otherBounds = CreateBounds(new Vector3(0f, 0f, 1f), Vector3.one, Quaternion.Euler(90f, 0f, 0f));

            bounds.Encapsulate(otherBounds);

            AssertPointsEqual(bounds.size, new Vector3(2f, 1f, 1f));
            AssertPointsEqual(bounds.center, new Vector3(0f, 0f, 0.5f));
        }

        void AssertContains(BoxBounds bounds, Vector3 point)
        {
            Assert.IsTrue(bounds.Contains(point), "Bounds did not contain point!");
        }

        void AssertDoesNotContain(BoxBounds bounds, Vector3 point)
        {
            Assert.IsFalse(bounds.Contains(point), "Bounds contained point!");
        }

        void AssertClosestPoint(BoxBounds bounds, Vector3 targetPoint, Vector3 expectedPoint)
        {
            Vector3 result = bounds.ClosestPoint(targetPoint);
            var diff = (expectedPoint - result).magnitude;
            bool pointCorrect = diff < 0.001f;
            Assert.IsTrue(pointCorrect, $"Point incorrect! Expected {expectedPoint} but got {result}");
        }

        void AssertPointsEqual(Vector3 targetPoint, Vector3 expectedPoint)
        {
            var diff = (targetPoint - expectedPoint).magnitude;
            bool pointCorrect = diff < 0.001f;
            Assert.IsTrue(pointCorrect, $"Point incorrect! Expected {expectedPoint} but got {targetPoint}");
        }

        BoxBounds CreateBounds(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            return new BoxBounds(position, scale, rotation);
        }

        BoxBounds CreateUnitBounds(Vector3 position)
        {
            return CreateBounds(position, Vector3.one, Quaternion.identity);
        }
    }
}
