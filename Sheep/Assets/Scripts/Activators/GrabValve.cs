using Recoil;
using UnityEngine;

public class GrabValve : ObjectActivator, IGrabNotifications
{
    [SerializeField]
    float maxRotations = 3f;
    [SerializeField]
    bool invertedRotation = false;
    [SerializeField]
    float reelBackSpeed = 500f;
    [SerializeField]
    float reelBackForce = 50f;
    [SerializeField]
    [Range(0f, 1f)]
    float startActivation = 0f;
    [SerializeField]
    bool preventMovementWhenNotGrabbed = false;
    HingeJoint joint;
    new Rigidbody rigidbody;
    ReBody reBody;

    float innerAngle = 0f;
    float maxAngle;
    float maxAngleLimit;
    float minAngleLimit;
    bool useMotor = false;

    float? forwardHaltAngle = null;
    float? backwardHaltAngle = null;

    bool grabbed = false;
    Quaternion originalRot;
    Quaternion previousRotation;

    Vector3 Axis => originalRot * (invertedRotation ? -joint.axis : joint.axis);

    Quaternion parentRotation
    {
        get
        {
            if (joint.connectedBody)
                return joint.connectedBody.rotation;
            if (transform.parent)
                return transform.parent.rotation;
            return Quaternion.identity;
        }
    }

    void Awake()
    {
        joint = GetComponent<HingeJoint>();
        rigidbody = GetComponent<Rigidbody>();

        useMotor = joint.useMotor;
        var motor = joint.motor;
        motor.force = reelBackForce;
        motor.targetVelocity = invertedRotation ? reelBackSpeed : -reelBackSpeed;
        joint.motor = motor;

        maxAngle = maxRotations * 360f;
        maxAngleLimit = maxAngle;
        minAngleLimit = 0f;
        innerAngle = maxAngle * startActivation;

        originalRot = transform.localRotation;
        previousRotation = originalRot;
    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
    }

    void FixedUpdate()
    {
        UpdateInnerAngle();
        UpdateLimits();
        SnapRotation();
        ActivationAmount = Mathf.Clamp01(innerAngle / maxAngle);
    }

    void UpdateInnerAngle()
    {
        if (preventMovementWhenNotGrabbed && !grabbed)
        {
            // Keep inner angle the same
            return;
        }
        else
        {
            var rotationDiff = transform.localRotation * Quaternion.Inverse(previousRotation);
            var diff = re.GetTwistAngle(rotationDiff, Axis) * Mathf.Rad2Deg;
            innerAngle += diff;
        }
    }

    void UpdateLimits()
    {
        maxAngleLimit = maxAngle;
        minAngleLimit = 0f;

        if (HaltActivatorForwardMovement)
        {
            if (forwardHaltAngle == null)
            {
                forwardHaltAngle = innerAngle;
            }
            maxAngleLimit = Mathf.Min(maxAngle, (float)forwardHaltAngle);
        }
        else
        {
            forwardHaltAngle = null;
        }

        if (HaltActivatorBackwardMovement)
        {
            if (backwardHaltAngle == null)
            {
                backwardHaltAngle = innerAngle;
            }
            minAngleLimit = Mathf.Max(0f, (float)backwardHaltAngle);
        }
        else
        {
            backwardHaltAngle = null;
        }
    }

    void SnapRotation()
    {
        if (preventMovementWhenNotGrabbed && !grabbed)
        {
            reBody.velocity = Vector3.zero;
            reBody.angularVelocity = Vector3.zero;
        }
        innerAngle = Mathf.Clamp(innerAngle, minAngleLimit, maxAngleLimit);

        var localRotation = Quaternion.AngleAxis(innerAngle, Axis) * originalRot;
        var targetRotation = parentRotation * localRotation;
        reBody.rotation = targetRotation;
        previousRotation = localRotation;

        var finalPos = GetLocalConnectedAnchor() - joint.anchor;
        reBody.position = reBody.TransformPoint(finalPos);
    }

    Vector3 GetLocalConnectedAnchor()
    {
        var worldAnchor = joint.connectedAnchor;
        if (joint.connectedBody != null)
        {
            worldAnchor = joint.connectedBody.transform.TransformPoint(worldAnchor);
        }
        return transform.InverseTransformPoint(worldAnchor);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbed = true;
            joint.useMotor = false;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            grabbed = false;
            joint.useMotor = useMotor;
        }
    }

    /*public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        var axis = Axis.normalized;
        var center = joint.anchor;

        var rotationDiff = Quaternion.Inverse(rigidbody.rotation) * vrRot;
        var projectedRotation = Vector3.Project(rotationDiff.QToAngleAxis(), axis).AngleAxisToQuaternion();
        vrRot = rigidbody.rotation * projectedRotation;

        // let's work in local space
        var localPos = rigidbody.InverseTransformPoint(vrPos);

        var projectedAnchor = Vector3.ProjectOnPlane(anchorPos - center, axis);
        var projectedPos = Vector3.ProjectOnPlane(localPos - center, axis);
        projectedPos = projectedPos.normalized * projectedAnchor.magnitude;
        var angle = innerAngle + Vector3.SignedAngle(projectedAnchor, projectedPos, axis);
        var clampedAngle = Mathf.Clamp(angle, minAngleLimit, maxAngleLimit);
        var angleDiff = clampedAngle - angle;
        var rotatedPos = projectedPos.Rotate(axis, angleDiff);
        localPos = center + rotatedPos;

        vrPos = rigidbody.TransformPoint(localPos);
        vrVel = Vector3.zero;
        vrAngular = Vector3.zero;
    }*/
}
