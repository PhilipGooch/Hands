using NUnit.Framework;
using UnityEngine;

namespace NBG.Impale.Tests
{
    public class ImpaleTests
    {
        const float floatAccuracy = 0.001f;

        [Test]
        public void ProjectImpalerTipFromHit45DegreesAtVectorZeroTest()
        {
            var tipPos = ImpalerUtils.ProjectImpalerTipFromHit(Vector3.zero, new Vector3(-1, 1, 0), new Vector3(-1, -1, 0));
            Assert.AreEqual(tipPos, Vector3.zero);
        }

        [Test]
        public void ProjectImpalerTipFromHit45DegreesDepthTest()
        {
            var tipPos = ImpalerUtils.ProjectImpalerTipFromHit(new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, -1, 0));
            Assert.Less(Vector3.Distance(tipPos, new Vector3(0, -1, 0)), floatAccuracy);
        }

        [Test]
        public void ProjectImpalerTipFromHit0DegreesTest()
        {
            var tipPos = ImpalerUtils.ProjectImpalerTipFromHit(Vector3.zero, Vector3.zero, new Vector3(0, -1, 0));
            Assert.AreEqual(tipPos, Vector3.zero);
        }

        [Test]
        public void DepthCalculationInsideTest()
        {
            var depth = ImpalerUtils.CalculateDepth(new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(-1, -1, 0), new Vector3(2, 3, 0));
            Assert.AreEqual(depth, -Mathf.Sqrt(2), floatAccuracy);
        }

        [Test]
        public void DepthCalculationAt0Test()
        {
            var depth = ImpalerUtils.CalculateDepth(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(-1, -1, 0), new Vector3(3, 4, 0));
            Assert.AreEqual(depth, 0, floatAccuracy);
        }

        [Test]
        public void DepthCalculationOutsideTest()
        {
            var depth = ImpalerUtils.CalculateDepth(Vector3.zero, new Vector3(1, 3, 0), new Vector3(-1, -1, 0), new Vector3(4, 6, 0));
            Assert.AreEqual(depth, Mathf.Sqrt(8), floatAccuracy);
        }

        [Test]
        public void ConnectedAnchorPosInsideTest()
        {
            var connectedAnchor = ImpalerUtils.GetInitialConnectedAnchorPos(Vector3.zero, new Vector3(-1, -1, 0), 2, -2);
            Assert.Less(Vector3.Distance(connectedAnchor, new Vector3(2, 2, 0)), floatAccuracy);
        }

        [Test]
        public void ConnectedAnchorPosAt0Test()
        {
            var connectedAnchor = ImpalerUtils.GetInitialConnectedAnchorPos(Vector3.zero, new Vector3(-1, -1, 0), 2, 0);
            Assert.Less(Vector3.Distance(connectedAnchor, Vector3.zero), floatAccuracy);
        }

        [Test]
        public void ConnectedAnchorOutsideTest()
        {
            var connectedAnchor = ImpalerUtils.GetInitialConnectedAnchorPos(Vector3.zero, new Vector3(-1, -1, 0), 2, 2);
            Assert.Less(Vector3.Distance(connectedAnchor, new Vector3(-2, -2, 0)), floatAccuracy);
        }

        [Test]
        public void ConnectedAnchorTooDeepTest()
        {
            var connectedAnchor = ImpalerUtils.GetInitialConnectedAnchorPos(Vector3.zero, new Vector3(0, -1, 0), 2, -3);
            Assert.Less(Vector3.Distance(connectedAnchor, new Vector3(0, 4, 0)), floatAccuracy);
        }

        [Test]
        public void GetPositionAtMaxImpaleDepthInsideTest()
        {
            var newPosition = ImpalerUtils.GetPositionAtDepth(new Vector3(2, 2, 0), new Vector3(-1, -1, 0), 4, -1);
            Assert.Less(Vector3.Distance(newPosition, new Vector3(-1, -1, 0)), floatAccuracy);
        }

        [Test]
        public void GetPositionAtMaxImpaleDepthOutsideTest()
        {
            var newPosition = ImpalerUtils.GetPositionAtDepth(new Vector3(4, 4, 0), new Vector3(-1, -1, 0), 4, 1);
            Assert.Less(Vector3.Distance(newPosition, new Vector3(-1, -1, 0)), floatAccuracy);
        }

        [Test]
        public void GetPositionAtMaxImpaleDepthTooDeepTest()
        {
            var newPosition = ImpalerUtils.GetPositionAtDepth(new Vector3(-2, -2, 0), new Vector3(-1, -1, 0), 4, -5);
            Assert.Less(Vector3.Distance(newPosition, new Vector3(-1, -1, 0)), floatAccuracy);
        }

        [Test]
        public void AlignWithNormalPivotOutsideTest()
        {
            //positive -> outside;
            float distanceFromPivotToImpalerStart = 3;
            var newPosition = ImpalerUtils.GetAlignmentWithNormalPosAndRot(Quaternion.Euler(45, 180, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), distanceFromPivotToImpalerStart, Vector3.zero);
            Assert.Less(Quaternion.Angle(newPosition.rotation, Quaternion.Euler(90, 180, 0)), floatAccuracy);
            Assert.Less(Vector3.Distance(newPosition.position, new Vector3(0, 3, 0)), floatAccuracy);
        }

        [Test]
        public void AlignWithNormalPivotInsideTest()
        {
            //negative -> inside;
            float distanceFromPivotToImpalerStart = -1;
            var newPosition = ImpalerUtils.GetAlignmentWithNormalPosAndRot(Quaternion.Euler(45, 180, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1), distanceFromPivotToImpalerStart, Vector3.zero);
            Assert.Less(Quaternion.Angle(newPosition.rotation, Quaternion.Euler(90, 180, 0)), floatAccuracy);
            Assert.Less(Vector3.Distance(newPosition.position, new Vector3(0, -1, 0)), floatAccuracy);
        }

        [Test]
        public void GetDistanceFromInsidePivotToImpalerStartTest()
        {
            var dist = ImpalerUtils.GetDistanceFromPivotToImpalerStart(Vector3.zero, new Vector3(0, -1, 0), new Vector3(0, 1, 0));
            Assert.AreEqual(dist, -1, floatAccuracy);
        }

        [Test]
        public void GetDistanceFromOutsidePivotToImpalerStartTest()
        {
            var dist = ImpalerUtils.GetDistanceFromPivotToImpalerStart(new Vector3(0, 2, 0), new Vector3(0, -1, 0), new Vector3(0, 1, 0));
            Assert.AreEqual(dist, 1, floatAccuracy);
        }
    }
}
