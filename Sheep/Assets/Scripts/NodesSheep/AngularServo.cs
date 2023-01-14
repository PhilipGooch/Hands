using UnityEngine;

public class AngularServo : ActivatableNode//, IProjectHandAnchor
{
    public bool stepper = true;
    public float maxA = 10;
    public float maxVel = 10;

    public float minPos = 0;
    public float maxPos = 1;
    public float rigPos = .5f;

    ConfigurableJoint joint;
    Rigidbody body;
    float jointPos;
    float stepperPos;
    Quaternion invRigRotation;

    protected void OnEnable()
    {

        stepperPos = rigPos * Mathf.Deg2Rad;
        body = GetComponent<Rigidbody>();
        joint = body.GetComponent<ConfigurableJoint>();

        jointPos = rigPos * Mathf.Deg2Rad;
        UpdateJointLimits(rigPos * Mathf.Deg2Rad);

        invRigRotation = Servo.GetInvRigRotation(body, joint, rigPos * Mathf.Deg2Rad);
    }

    public void FixedUpdate()
    {
        jointPos = Servo.ReadJointAngle(body, joint, invRigRotation, jointPos);
        UpdateJointLimits(jointPos);

        var targetPos = Mathf.Lerp(minPos, maxPos, ActivationValue) * Mathf.Deg2Rad;
        if (stepper)
        {
            stepperPos = Servo.MoveStepper(stepperPos, jointPos, targetPos, maxVel);
            Servo.ApplyAngularDynamics(body, joint, stepperPos, jointPos, maxA, 0);
        }
        else
            Servo.ApplyAngularDynamics(body, joint, targetPos, jointPos, maxA, maxVel);

    }

    ConfigurableJoint limitJoint;
    float limitJointCenter;
    private void UpdateJointLimits(float currentPos)
    {
        currentPos *= Mathf.Rad2Deg; // convert to degrees
        if (maxPos - minPos < 270) // setup once
        {
            joint.lowAngularXLimit = new SoftJointLimit() { limit = -(maxPos - rigPos) };
            joint.highAngularXLimit = new SoftJointLimit() { limit = -(minPos - rigPos) };

        }
        else
        {
            if (limitJoint != null && (currentPos > limitJointCenter + 80 || currentPos < limitJointCenter - 80))
                DestroyImmediate(limitJoint);
            if (limitJoint == null)
            {
                limitJointCenter = currentPos;
                limitJoint = gameObject.AddComponent<ConfigurableJoint>();
                limitJoint.autoConfigureConnectedAnchor = false;
                limitJoint.anchor = joint.anchor;
                limitJoint.connectedAnchor = joint.connectedAnchor;
                limitJoint.connectedBody = joint.connectedBody;
                limitJoint.axis = joint.axis;
                limitJoint.angularXMotion = ConfigurableJointMotion.Limited;

                var minRange = Mathf.Max(-160, minPos - currentPos);
                var maxRange = Mathf.Min(160, maxPos - currentPos);

                limitJoint.lowAngularXLimit = new SoftJointLimit() { limit = -maxRange };
                limitJoint.highAngularXLimit = new SoftJointLimit() { limit = -minRange };
            }
        }
    }

    //public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    //{
    //    ProjectAnchorUtils.ProjectAngular(body, joint.anchor, joint.axis.normalized, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);

    //}
}
