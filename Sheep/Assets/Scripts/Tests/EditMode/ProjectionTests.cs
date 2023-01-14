using UnityEngine;
using NUnit.Framework;
using Recoil;
using NBG.Entities;

namespace Tests
{
    public class ProjectionTests
    {
        [SetUp]
        public void SetUp()
        {
            ManagedWorld.Create(16);
            EntityStore.Create(10, 500);
        }

        [TearDown]
        public void TearDown()
        {
            EntityStore.Destroy();
            ManagedWorld.Destroy();
        }

        [Test]
        public void AngularProjectionSnapsPositionOntoPlane()
        {
            var rig = CreateRigidbody();
            var vrPos = new Vector3(10, 20, 30);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = new Vector3(0, 20, 30).normalized * anchorPos.magnitude;

            ProjectAngularPositionOnlyXAxis(rig, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularProjectionOnRotatedObjectSnapsPositionOntoPlane()
        {
            var rig = CreateRigidbody();
            RotateRig(rig, Quaternion.Euler(0, 90, 0));
            var vrPos = new Vector3(10, 20, 30);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = new Vector3(10, 20, 0).normalized * anchorPos.magnitude;

            ProjectAngularPositionOnlyXAxis(rig, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularProjectionOnDiagonalAnchorPosSnapsPositionOntoPlane()
        {
            var rig = CreateRigidbody();
            var vrPos = new Vector3(10, 20, 30);
            var anchorPos = new Vector3(0, 2, 2);
            var expectedPos = new Vector3(0, 20, 30).normalized * anchorPos.magnitude;

            ProjectAngularPositionOnlyXAxis(rig, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularProjectionAxisOffsetAnchorPosSnapsPositionOntoPlane()
        {
            var rig = CreateRigidbody();
            var vrPos = new Vector3(10, 20, 30);
            var anchorPos = new Vector3(2, 0, 2);
            var expectedPos = new Vector3(2, 0, 0) + new Vector3(0, 20, 30).normalized * 2f;

            ProjectAngularPositionOnlyXAxis(rig, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularProjectionRotatedBodyIdenticalToVrRotationNotRotated()
        {
            var rig = CreateRigidbody();
            RotateRig(rig, Quaternion.Euler(0, 90, 0));
            var vrRotation = Quaternion.Euler(0, 90, 0);
            var anchorRot = Quaternion.identity;
            var expectedRot = Quaternion.Euler(0, 90, 0);

            ProjectAngularRotationOnlyXAxis(rig, ref vrRotation, anchorRot);

            VerifyAnglesAreIdentical(expectedRot, vrRotation);
        }

        [Test]
        public void AngularProjectionRotatedBodyAndRotatedVrPositionInPlaneNotRotated()
        {
            var rig = CreateRigidbody();
            RotateRig(rig, Quaternion.Euler(0, 90, 0));
            var vrRotation = Quaternion.Euler(45, 90, 0);
            var anchorRot = Quaternion.identity;
            var expectedRot = Quaternion.Euler(45, 90, 0);

            ProjectAngularRotationOnlyXAxis(rig, ref vrRotation, anchorRot);

            VerifyAnglesAreIdentical(expectedRot, vrRotation);
        }

        [Test]
        public void AngularHingeProjectionSnapsPositionOntoPlane()
        {
            var rig = CreateRigidbody();
            var hinge = CreateHingeJoint(rig.gameObject);
            var vrPos = new Vector3(10, 20, 30);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = new Vector3(0, 20, 30).normalized * anchorPos.magnitude;

            ProjectHingePositionOnly(rig, hinge, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularHingeProjectionWith90MaxLimitSnapsPositionToLimit()
        {
            var rig = CreateRigidbody();
            var hinge = CreateHingeJoint(rig.gameObject);
            SetHingeLimits(hinge, -180, 90);
            var vrPos = new Vector3(0, -5, -5);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = new Vector3(0, -5, 0).normalized * anchorPos.magnitude;

            ProjectHingePositionOnly(rig, hinge, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularHingeProjectionWith45MinLimitSnapsPositionTolimit()
        {
            var rig = CreateRigidbody();
            var hinge = CreateHingeJoint(rig.gameObject);
            SetHingeLimits(hinge, -45, 180);
            var vrPos = new Vector3(0, 5, 0);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = Quaternion.AngleAxis(-45, hinge.axis) * anchorPos;

            ProjectHingePositionOnly(rig, hinge, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularHingeProjectionWith90MaxLimitOnRotatedObjectSnapsPositionToLimit()
        {
            var rig = CreateRigidbody();
            var hinge = CreateHingeJoint(rig.gameObject);
            SetHingeLimits(hinge, -180, 90);
            RotateRig(rig, Quaternion.Euler(90, 0, 0)); // rotate to limit
            // Since the object is rotated, the vr pos has to be in the bottom left corner in order to generate further clockwise rotation
            var vrPos = new Vector3(0, -5, -5);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = new Vector3(0, -5, 0).normalized * anchorPos.magnitude;

            ProjectHingePositionOnly(rig, hinge, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        [Test]
        public void AngularHingeProjectionWith45MinLimitOnRotatedObjectSnapsPositionToLimit()
        {
            var rig = CreateRigidbody();
            var hinge = CreateHingeJoint(rig.gameObject);
            SetHingeLimits(hinge, -45, 180);
            RotateRig(rig, Quaternion.Euler(-30, 0, 0)); // Rotate near limit

            var vrPos = new Vector3(0, 5, -5);
            var anchorPos = new Vector3(0, 0, 2);
            var expectedPos = Quaternion.AngleAxis(-45, hinge.axis) * anchorPos;

            ProjectHingePositionOnly(rig, hinge, ref vrPos, anchorPos);

            VerifyVectorsAreIdentical(expectedPos, vrPos);
        }

        void VerifyVectorsAreIdentical(Vector3 expectedPos, Vector3 actualPos)
        {
            var diff = expectedPos - actualPos;
            Assert.Less(diff.magnitude, 0.01f, string.Format("Projected position too far away! Wanted position: {0} actual position: {1}", expectedPos, actualPos));
        }

        void VerifyAnglesAreIdentical(Quaternion expectedAngle, Quaternion actualAngle)
        {
            var deltaAngle = Quaternion.Angle(expectedAngle, actualAngle);
            Assert.Less(deltaAngle, 0.1f, string.Format("Rotations are different! Expected: {0} Actual: {1}", expectedAngle.eulerAngles, actualAngle.eulerAngles));
        }

        void ProjectAngularRotationOnlyXAxis(Rigidbody body, ref Quaternion vrRot, Quaternion anchorRot)
        {
            Vector3 vrPos = Vector3.zero;
            Vector3 vrVel = Vector3.zero;
            Vector3 vrAngular = Vector3.zero;
            Vector3 anchorPos = Vector3.zero;
            ProjectAnchorUtils.ProjectAngular(body, body.transform.position, new Vector3(1, 0, 0), ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
        }

        void ProjectAngularPositionOnlyXAxis(Rigidbody body, ref Vector3 vrPos, Vector3 anchorPos)
        {
            Quaternion vrRot = Quaternion.identity;
            Vector3 vrVel = Vector3.zero;
            Vector3 vrAngular = Vector3.zero;
            ProjectAnchorUtils.ProjectAngular(body, body.transform.position, new Vector3(1, 0, 0), ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, Quaternion.identity);
        }

        void ProjectHingePositionOnly(Rigidbody body, HingeJoint hinge, ref Vector3 vrPos, Vector3 anchorPos)
        {
            Quaternion vrRot = Quaternion.identity;
            Vector3 vrVel = Vector3.zero;
            Vector3 vrAngular = Vector3.zero;
            ProjectAnchorUtils.ProjectHingeJoint(body, hinge, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, Quaternion.identity);
        }

        Rigidbody CreateRigidbody()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.position = Vector3.zero;
            var rig = obj.AddComponent<Rigidbody>();
            rig.useGravity = false;
            RigidbodyRegistration.RegisterHierarchy(obj);
            return rig;
        }

        void RotateRig(Rigidbody target, Quaternion rotation)
        {
            var id = ManagedWorld.main.FindBody(target);
            ManagedWorld.main.SetBodyPlacementImmediate(id, new Unity.Mathematics.RigidTransform(rotation, Vector3.zero));
        }

        HingeJoint CreateHingeJoint(GameObject target)
        {
            var hinge = target.AddComponent<HingeJoint>();
            hinge.axis = new Vector3(1, 0, 0);
            hinge.anchor = Vector3.zero;
            return hinge;
        }

        void SetHingeLimits(HingeJoint hinge, float min, float max)
        {
            hinge.useLimits = true;
            var limits = hinge.limits;
            limits.min = min;
            limits.max = max;
            hinge.limits = limits;
        }
    }
}

