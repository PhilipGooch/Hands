using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NBG.Joints.Editor")]

namespace NBG.Joints
{
    /// <summary>
    /// A prismatic joint covers a subset of joints that moves linearly from point A to B (https://en.wikipedia.org/wiki/Prismatic_joint).
    /// </summary>
    [RequireComponent(typeof(Rigidbody)), DisallowMultipleComponent()]
    public class PrismaticJoint : MonoBehaviour
    {
        [SerializeField] AttachmentMode attachmentMode;
        [SerializeField] Rigidbody attachedBody;

        [SerializeField] internal Vector3 start;
        [SerializeField] internal Vector3 end;

        [Range(0, 1)]
        [SerializeField] float progress;
        public float Progress
        {
            get
            {
                return progress;
            }

            set
            {
                progress = value;
                SetTargetPosition(progress);
            }
        }

        public float RangeOfMotion { get { return Vector3.Distance(start, end); } }

        [SerializeField] PrismaticJointProfile profile;

        public bool overrideProfile;
        public float springOverride, damperOverride;

        private ConfigurableJoint theJoint;

        private void Awake()
        {
            theJoint = gameObject.AddComponent<ConfigurableJoint>();
            theJoint.autoConfigureConnectedAnchor = false;

            Vector3 localOffset = (end - start);
            Vector3 worldOffset = transform.TransformVector(localOffset);
            Vector3 localOffsetScaled = new Vector3(
                localOffset.x * transform.localScale.x,
                localOffset.y * transform.localScale.y,
                localOffset.z * transform.localScale.z);

            theJoint.axis = (localOffsetScaled).normalized;
            theJoint.secondaryAxis = Vector3.zero;

            theJoint.xMotion = ConfigurableJointMotion.Limited;
            theJoint.yMotion = ConfigurableJointMotion.Locked;
            theJoint.zMotion = ConfigurableJointMotion.Locked;

            theJoint.angularXMotion = ConfigurableJointMotion.Locked;
            theJoint.angularYMotion = ConfigurableJointMotion.Locked;
            theJoint.angularZMotion = ConfigurableJointMotion.Locked;

            theJoint.anchor = start;

            if (attachmentMode == AttachmentMode.World)
            {
                theJoint.connectedAnchor = transform.TransformPoint(start + localOffset * 0.5f);
            }
            else if (attachmentMode == AttachmentMode.Attached)
            {
                theJoint.connectedBody = attachedBody;
                theJoint.connectedAnchor = attachedBody.transform.InverseTransformPoint(transform.TransformPoint(start + localOffset * 0.5f));
            }

            var linearLimit = theJoint.linearLimit;
            linearLimit.limit = worldOffset.magnitude * 0.5f;
            theJoint.linearLimit = linearLimit;

            SetTargetPosition(progress);

            float mass = GetComponent<Rigidbody>().mass;
            var drive = theJoint.xDrive;
            if (overrideProfile)
            {
                drive.positionDamper = damperOverride;
                drive.positionSpring = springOverride;
            }
            else
            {
                drive.positionDamper = profile.damp * mass;
                drive.positionSpring = profile.spring * mass;
            }

            theJoint.xDrive = drive;
        }

        /// <summary>
        /// Moves the object through the path using the progress with values between 0 (start) and 1 (end).
        /// </summary>
        /// <param name="progress">Value between 0 and 1 that represents the proportional position between start and end.</param>
        public void SetTargetPosition(float progress)
        {
            this.progress = progress;
            if (theJoint != null) // joint not available in edit time
            {
                var pos = theJoint.targetPosition;
                float halfDistance = RangeOfMotion * 0.5f;
                pos.x = Mathf.Lerp(halfDistance, -halfDistance, progress);
                theJoint.targetPosition = pos;
            }
        }
        /// <summary>
        /// Destroys the joint
        /// </summary>
        public void DestroyJoint()
        {
            var theJoint = GetComponent<ConfigurableJoint>();
            if (theJoint)
                Destroy(theJoint);
            Destroy(this);
        }
    }
}