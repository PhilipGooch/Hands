using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Joints.Editor")]
namespace NBG.Joints
{
    /// <summary>
    /// A revolute joint covers a subset of joints that rotates with an axis with features like motor-mode, target rotation, limits (https://en.wikipedia.org/wiki/Revolute_joint).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RevoluteJoint : MonoBehaviour
    {
        private HingeJoint theJoint;

        [SerializeField] internal Vector3 pivot = new Vector3(0.0f, 0.0f, 0.0f);
        [SerializeField] internal Vector3 axis = new Vector3(0.0f, 1.0f, 0.0f);
        [SerializeField] internal Vector3 rotationStart = new Vector3(1.0f, 0.0f, 0.0f);

        [SerializeField] internal bool useLimits;
        [Range(0.0f, 180.0f)]
        [SerializeField] internal float maxAngle;
        [Range(0.0f, -180.0f)]
        [SerializeField] internal float minAngle;

        [SerializeField] internal bool useProgress;
        [Range(0.0f, 1.0f)]
        [SerializeField] internal float progress;

        [SerializeField] internal bool useMotor;
        [SerializeField] internal float force;
        [SerializeField] internal float targetVelocity;

        [SerializeField] internal RevoluteJointProfile profile;

        [SerializeField] internal AttachmentMode attachmentMode;
        [SerializeField] internal Rigidbody attachedBody;

        /// <summary>
        /// Lets you move the target angle of the rotation between the established limits(from min angle to max angle[0f-1f]).
        /// </summary>
        public float Progress
        {
            get
            {
                return progress;
            }

            set
            {
                progress = value;

                if (useProgress)
                {
                    theJoint.useSpring = useProgress;
                    var jointSpring = theJoint.spring;
                    jointSpring.targetPosition = Mathf.Lerp(minAngle, maxAngle, progress);
                    theJoint.spring = jointSpring;
                }
            }
        }

        private void Awake()
        {
            List<Transform> children = new List<Transform>();

            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(0);
                children.Add(child);
                child.transform.parent = null;
            }

            transform.rotation = BuildRotation(transform.rotation * rotationStart, transform.rotation * axis);

            foreach (var child in children)
                child.transform.parent = transform;
        }

        private void Start()
        {
            Vector3 worldPivot = transform.TransformPoint(pivot);

            theJoint = gameObject.AddComponent<HingeJoint>();
            theJoint.autoConfigureConnectedAnchor = false;
            theJoint.axis = Vector3.right;
            theJoint.anchor = transform.InverseTransformPoint(worldPivot);

            if (attachmentMode == AttachmentMode.World)
            {
                theJoint.connectedAnchor = worldPivot;
            }
            else if (attachmentMode == AttachmentMode.Attached)
            {
                theJoint.connectedBody = attachedBody;
                theJoint.connectedAnchor = attachedBody.transform.InverseTransformPoint(worldPivot);
            }

            if (useLimits)
            {
                theJoint.useLimits = true;
                var limits = theJoint.limits;
                limits.max = maxAngle;
                limits.min = minAngle;
                theJoint.limits = limits;

                if (useProgress)
                {
                    theJoint.useSpring = useProgress;
                    var jointSpring = theJoint.spring;
                    jointSpring.spring = profile.spring;
                    jointSpring.damper = profile.damp;
                    jointSpring.targetPosition = Mathf.Lerp(minAngle, maxAngle, progress);
                    theJoint.spring = jointSpring;
                }
            }

            if (useMotor)
            {
                theJoint.useMotor = useMotor;
                var motor = theJoint.motor;
                motor.force = force;
                motor.targetVelocity = targetVelocity;
                theJoint.motor = motor;
            }
        }

        public static Vector3 FixRightAxis(Vector3 up, Vector3 forward)
        {
            return (forward - Vector3.Dot(up, forward) * up).normalized;
        }

        private static Quaternion BuildRotation(Vector3 forwardAxis, Vector3 rightAxis)
        {
            return Quaternion.LookRotation(forwardAxis, Vector3.Cross(forwardAxis, rightAxis));
        }
    }
}