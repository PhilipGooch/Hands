using Recoil;
using UnityEngine;
using TMPro;

namespace Tests.UnityJointingWithRecoil
{
    public class JointingTest : MonoBehaviour
    {
        [SerializeField]
        private bool attemptTest;
        [Header("Test resources")]
        [SerializeField]
        private Material failMaterial;
        [SerializeField]
        private Material passMaterial;
        [SerializeField]
        private Transform jointingPosition;
        [SerializeField]
        private Rigidbody rigidbodyToTest;
        [SerializeField]
        private BodyDetector triggerToListenTo;
        [SerializeField]
        private TextMeshPro testDescription;
        [Header("Test parameters")]

        public bool addStrongForceBefore = false;
        public ObjectMoveTest objectMoveCondition;
        public AttachJointTest attachCondition;

        public enum ObjectMoveTest
        {
            UseRecoilImmediate = 0,
            MoveTransform = 1,
            MoveRigidbody = 2,
            MoveRigidAndTransform = 3,
        }

        public enum AttachJointTest
        {
            DontAttach = 0,
            AttachUnmodified = 1,
            AttachWithManualAnchor = 2,
        }


        private MeshRenderer triggerMeshRenderer;
        private Vector3 targetJointPoint;

        private void OnValidate()
        {
            RefreshTextField();
        }

        private void Awake()
        {
            triggerMeshRenderer = triggerToListenTo.GetComponent<MeshRenderer>();
            targetJointPoint = jointingPosition.position;
            triggerToListenTo.OnRigidBodyEnter += EnteredTargetTrigger;
            triggerToListenTo.OnRigidBodyLeave += LeftTargetTrigger;
        }

        public void RefreshTextField()
        {
            testDescription.text = $"Move: {objectMoveCondition}\nJoint: {attachCondition}\nAddStrongForce: {addStrongForceBefore}";
        }

        public void RunTestOnNextFixedUpdate ()
        {
            attemptTest = true;
        }

        private void EnteredTargetTrigger(int bodyId)
        {
            Rigidbody body = ManagedWorld.main.GetRigidbody(bodyId);

            if (body == rigidbodyToTest)
            {
                triggerMeshRenderer.material = passMaterial;
            }
        }

        private void LeftTargetTrigger(int bodyId)
        {
            Rigidbody body = ManagedWorld.main.GetRigidbody(bodyId);

            if (body == rigidbodyToTest)
            {
                triggerMeshRenderer.material = failMaterial;
            }
        }

        private void ApplyObjectMoveTest()
        {
            if (objectMoveCondition == ObjectMoveTest.UseRecoilImmediate)
            {
                int bodyId = ManagedWorld.main.FindBody(rigidbodyToTest);
                ManagedWorld.main.SetVelocity(bodyId, MotionVector.zero);
                ManagedWorld.main.SetBodyPlacementImmediate(bodyId, new Unity.Mathematics.RigidTransform(rigidbodyToTest.rotation, targetJointPoint));

                return;
            }
            if (objectMoveCondition == ObjectMoveTest.MoveRigidAndTransform || objectMoveCondition == ObjectMoveTest.MoveRigidbody)
            {
                rigidbodyToTest.velocity = Vector3.zero;
                rigidbodyToTest.position = targetJointPoint;
            }
            if (objectMoveCondition == ObjectMoveTest.MoveRigidAndTransform || objectMoveCondition == ObjectMoveTest.MoveTransform)
            {
                rigidbodyToTest.transform.position = targetJointPoint;
            }
        }

        private void ApplyObjectJointTest()
        {
            if (attachCondition == AttachJointTest.DontAttach)
            {
                return;
            }

            FixedJoint targetJoint = rigidbodyToTest.gameObject.AddComponent<FixedJoint>();
            targetJoint.breakForce = 10;
            targetJoint.breakTorque = 10;

            if (attachCondition == AttachJointTest.AttachWithManualAnchor)
            {
                targetJoint.autoConfigureConnectedAnchor = false;
                targetJoint.connectedAnchor = targetJointPoint;
            }

        }

        private void FixedUpdate()
        {
            if (addStrongForceBefore)
            {
                rigidbodyToTest.AddForce(new Vector3(0f, 0f, 100f), ForceMode.VelocityChange);
                addStrongForceBefore = false;
            }
            if (attemptTest)
            {
                ApplyObjectMoveTest();
                ApplyObjectJointTest();
                rigidbodyToTest.useGravity = true;

                attemptTest = false;
            }
        }
    }
}
