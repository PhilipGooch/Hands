using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using Unity.Mathematics;
using NBG.XPBDRope;

namespace Tests
{
    public class RopeSolvingTests
    {
        [Test]
        public void QuatraticCollisionNoMovementNoCollision()
        {
            float3 firstStart = new float3(0, 0, 0);
            float3 secondStart = new float3(5, 5, 5);
            float3 movement = new float3(0, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, movement, secondStart, movement, 1f);

            AssertNoCollisionDetected(collisionPoint);
        }

        [Test]
        public void QuadraticCollisionCrossingWithNoRadiusCollisionDetected()
        {
            float3 firstStart = new float3(1, 0, 0);
            float3 secondStart = new float3(0, 1, 0);
            float3 firstMovement = new float3(-2, 0, 0);
            float3 secondMovement = new float3(0, -2, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0f);

            Assert.AreEqual(0.5f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionCrossingWithRadiusCollisionDetected()
        {
            float3 firstStart = new float3(1, 0, 0);
            float3 secondStart = new float3(0, 1, 0);
            float3 firstMovement = new float3(-2, 0, 0);
            float3 secondMovement = new float3(0, -2, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0.1f);

            Assert.AreEqual(0.464f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionInvertedCrossingWithRadiusCollisionDetected()
        {
            float3 firstStart = new float3(-1, 0, 0);
            float3 secondStart = new float3(0, -1, 0);
            float3 firstMovement = new float3(2, 0, 0);
            float3 secondMovement = new float3(0, 2, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0.1f);

            Assert.AreEqual(0.464f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionHeadOnCollisionWithoutRadiusDetected()
        {
            float3 firstStart = new float3(-1, 0, 0);
            float3 secondStart = new float3(1, 0, 0);
            float3 firstMovement = new float3(2, 0, 0);
            float3 secondMovement = new float3(-2, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0.0f);

            Assert.AreEqual(0.5f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionHeadOnCollisionWithRadiusDetected()
        {
            float3 firstStart = new float3(-1, 0, 0);
            float3 secondStart = new float3(1, 0, 0);
            float3 firstMovement = new float3(2, 0, 0);
            float3 secondMovement = new float3(-2, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0.1f);

            Assert.AreEqual(0.475f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionOnePointIsStationaryCollisionDetected()
        {
            float3 firstStart = new float3(-1, 0, 0);
            float3 secondStart = new float3(0, 0, 0);
            float3 firstMovement = new float3(2, 0, 0);
            float3 secondMovement = new float3(0, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 0.0f);

            Assert.AreEqual(0.5f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionStartInsideObjectAndMoveOutOnlyDetectsMovingOutPoint()
        {
            // Two spheres with a radius of 0.5. Start overlapping
            float3 firstStart = new float3(0.5f, 0, 0);
            float3 secondStart = new float3(0, 0, 0);
            float3 firstMovement = new float3(-2f, 0, 0);
            float3 secondMovement = new float3(0, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 1f);

            Assert.AreEqual(0.75f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        [Test]
        public void QuadraticCollisionOneMovingPointBigRadiusDetected()
        {
            float3 firstStart = new float3(0f, 0, 0);
            float3 secondStart = new float3(15f, 0, 0);
            float3 firstMovement = new float3(5f, 0, 0);
            float3 secondMovement = new float3(0, 0, 0);

            var collisionPoint = XpbdRopeSolver.DetectTwoMovingSphereCollisionTime(firstStart, firstMovement, secondStart, secondMovement, 10f);

            Assert.AreEqual(1f, collisionPoint, 0.01f, "Collision not detected at the correct position!");
        }

        /*[Test]
        public void QuadraticCollisionTestRealValues()
        {
            //WHY start: float3(14.25805f, 8.761748f, -33.63341f) diff: float3(-0.01474953f, -0.002985954f, -0.00453949f) sub: float3(14.28511f, 8.738094f, -33.69222f) otherDiff: float3(0.01775169f, -0.003089905f, -0.005989075f) 1.442888
            float3 firstStart = new float3(14.25805f, 8.761748f, -33.63341f);
            float3 secondStart = new float3(14.28511f, 8.738094f, -33.69222f);
            float3 firstDiff = new float3(-0.01474953f, -0.002985954f, -0.00453949f);
            float3 secondDiff = new float3(0.01775169f, -0.003089905f, -0.005989075f);

            float3 endPosDiff = (firstStart + firstDiff) - (secondStart + secondDiff);
            float endDistance = math.length(endPosDiff);

            var collisionPoint = XpbdRopeSolver.SolveForT(firstStart, firstDiff, secondStart, secondDiff, 0.1f);

            float startDistance = math.length(firstStart - secondStart);
            Assert.Greater(startDistance, 0.1f);
            Assert.LessOrEqual(endDistance, 0.1f, "End distance too far away!");
            Assert.LessOrEqual(collisionPoint, 1f, "Collision point out of bounds!");
        }*/

        [Test]
        public void CapsuleMovingNoDeltaNotMoved()
        {
            float3 capsulePos1 = new float3(0, 0, 0);
            float3 capsulePos2 = new float3(1, 0, 0);

            XpbdRopeSolver.MoveCapsuleAccordingToPoint(ref capsulePos1, ref capsulePos2, 0.5f, new float3(0,0,0));

            Assert.AreEqual(new float3(0, 0, 0), capsulePos1, "Capsule pos1 was moved!");
            Assert.AreEqual(new float3(1, 0, 0), capsulePos2, "Capsule pos2 was moved!");
        }

        [Test]
        public void CapsuleMovingPointAtStartOnlyFirstPointMoved()
        {
            float3 pos1 = new float3(-1, 0, 0);
            float3 pos2 = new float3(1, 0, 0);

            XpbdRopeSolver.MoveCapsuleAccordingToPoint(ref pos1, ref pos2, 0.0f, new float3(0,1,0));

            Assert.AreEqual(new float3(-1, 1, 0), pos1, "Capsule pos1 was not moved correctly!");
            Assert.AreEqual(new float3(1, 0, 0), pos2, "Capsule pos2 was moved!");
        }

        [Test]
        public void CapsuleMovingPointAtEndOnlySecondPointMoved()
        {
            float3 pos1 = new float3(-1, 0, 0);
            float3 pos2 = new float3(1, 0, 0);

            XpbdRopeSolver.MoveCapsuleAccordingToPoint(ref pos1, ref pos2, 1f, new float3(0, 1, 0));

            Assert.AreEqual(new float3(-1, 0, 0), pos1, "Capsule pos1 was moved!");
            Assert.AreEqual(new float3(1, 1, 0), pos2, "Capsule pos2 was not moved correctly!");
        }

        /*[Test]
        public void CapsuleMovingPointInTheMiddleBothPointsMoved()
        {
            float3 pos1 = new float3(-1, 0, 0);
            float3 pos2 = new float3(1, 0, 0);

            XpbdRopeSolver.MoveCapsuleAccordingToPoint(ref pos1, ref pos2, 0.5f, new float3(0, 1, 0));

            Assert.AreEqual(new float3(-1, 1, 0), pos1, "Capsule pos1 was not moved correctly!");
            Assert.AreEqual(new float3(1, 1, 0), pos2, "Capsule pos2 was not moved correctly!");
        }

        [Test]
        public void CapsuleMovingPullEdgeParallelToCapsuleBothPointsMoved()
        {
            float3 pos1 = new float3(-1, 0, 0);
            float3 pos2 = new float3(1, 0, 0);

            XpbdRopeSolver.MoveCapsuleAccordingToPoint(ref pos1, ref pos2, 1f, new float3(1, 0, 0));

            Assert.AreEqual(new float3(0, 0, 0), pos1, "Capsule pos1 was not moved correctly!");
            Assert.AreEqual(new float3(2, 0, 0), pos2, "Capsule pos2 was not moved correctly!");
        }*/

        void AssertCollisionDetected(float collisionPoint)
        {
            Assert.IsTrue(collisionPoint >= 0f && collisionPoint <= 1f, "Collision not detected!");
        }

        void AssertNoCollisionDetected(float collisionPoint)
        {
            Assert.IsTrue(collisionPoint < 0f || collisionPoint > 1f, "Collision detected when there should be none!");
        }
    }
}
