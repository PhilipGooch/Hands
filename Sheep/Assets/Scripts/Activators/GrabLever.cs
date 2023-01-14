using Recoil;
using UnityEngine;

public class GrabLever : ObjectActivator, IGrabNotifications, IProjectHandAnchor
{
    [SerializeField]
    float startAngle = 0f;

    HingeJoint leverJoint;
    Rigidbody leverBody;
    ReBody reBody;
    float leverForce = 0f;
    float minAngle = 0f;
    float maxAngle = 0f;
    Quaternion baseRotation;

    void Start()
    {
        leverJoint = GetComponentInChildren<HingeJoint>();
        leverBody = leverJoint.GetComponent<Rigidbody>();
        reBody = new ReBody(leverBody);
        baseRotation = leverJoint.transform.localRotation;
        leverForce = leverJoint.motor.force;
        minAngle = leverJoint.limits.min;
        maxAngle = leverJoint.limits.max;
        if (maxAngle > 177)
        {
            // For whatever reason unity does not allow a hinge joint to rotate more than 177 degrees. Even if the limit is 180, it will snap to 177
            maxAngle = 177;
        }

        startAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        SetAngle(startAngle);
    }

    void FixedUpdate()
    {
        var angle = leverJoint.angle;
        if (angle > maxAngle)
        {
            angle = maxAngle;
        }

        ActivationAmount = Mathf.Clamp01((angle - minAngle) / (maxAngle - minAngle));
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            SetLeverForce(0f);
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            SetLeverForce(leverForce);
        }
    }

    void SetAngle(float degrees)
    {
        var rotationOffset = Quaternion.Euler(leverJoint.axis * degrees);
        var finalLocalRotation = baseRotation * rotationOffset;
        var localDelta = transform.localRotation * Quaternion.Inverse(baseRotation) * leverJoint.anchor - finalLocalRotation * leverJoint.anchor;
        var worldDelta = reBody.TransformDirection(localDelta);

        reBody.position = reBody.position + worldDelta;
        var worldRotation = finalLocalRotation;
        if (leverBody.transform.parent != null)
        {
            worldRotation = leverBody.transform.parent.rotation * finalLocalRotation;
        }
        reBody.rotation = worldRotation;
    }

    void SetLeverForce(float force)
    {
        var motor = leverJoint.motor;
        motor.force = force;
        leverJoint.motor = motor;
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectAnchorUtils.ProjectHingeJoint(reBody, leverJoint, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
        vrVel = Vector3.zero;
        vrAngular = Vector3.zero;
    }
}
