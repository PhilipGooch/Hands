using UnityEngine;
using Recoil;

public class LinearServo : ActivatableNode
{
    public bool stepper = true;
    public float maxA = 10;
    public float maxVel = 10;

    public float minPos = 0;
    public float maxPos = 1;
    public float rigPos = .5f;

    float stepperPos;
    ConfigurableJoint joint;
    Rigidbody body;
    ReBody reBody;
    ReBody connectedReBody;
    Vector3 rigAnchor;

    protected void Start()
    {
        stepperPos = rigPos;
        body = GetComponent<Rigidbody>();
        reBody = new ReBody(body);
        joint = body.GetComponent<ConfigurableJoint>();
        connectedReBody = new ReBody(joint.connectedBody);

        var halfTravel = (maxPos - minPos) / 2;
        joint.autoConfigureConnectedAnchor = false;
        joint.linearLimit = new SoftJointLimit() { limit = halfTravel };

        rigAnchor = reBody.TransformPoint(joint.anchor - joint.axis * rigPos);
        joint.connectedAnchor = rigAnchor + reBody.TransformDirection(joint.axis * (minPos + halfTravel));
        if (connectedReBody.BodyExists)
            joint.connectedAnchor = connectedReBody.InverseTransformPoint(rigAnchor);

    }

    public void FixedUpdate()
    {
        var axis = reBody.TransformDirection(joint.axis.normalized);
        var currentPos = Vector3.Dot(axis, reBody.TransformPoint(joint.anchor) - connectedReBody.TransformPoint(rigAnchor));

        var targetPos = Mathf.Lerp(minPos, maxPos, ActivationValue);

        if (stepper)
        {
            stepperPos = Servo.MoveStepper(stepperPos, currentPos, targetPos, maxVel);
            Servo.ApplyLinearDynamics(reBody, joint, stepperPos, currentPos, maxA, 0);
        }
        else
            Servo.ApplyLinearDynamics(reBody, joint, targetPos, currentPos, maxA, maxVel);
    }
}
