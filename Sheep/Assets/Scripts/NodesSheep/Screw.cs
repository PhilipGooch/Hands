using UnityEngine;
using Recoil;

public class Screw : MonoBehaviour
{
    public bool destroyOnMin = false;
    public bool destroyOnMax = false;
    public float minRot = 0;
    public float maxRot = 720;
    public float rigRot = .5f;

    public float minPos = 0;
    public float maxPos = 1;
    public float rigPos = .5f;

    //public NodeInputFloat input = new NodeInputFloat() { initialValue = 0 };

    ConfigurableJoint joint;
    Rigidbody body;
    float jointRot;
    float stepperRot;
    Quaternion invRigRotation;


    ConfigurableJoint limitJoint;
    float limitJointCenter;
    Vector3 rigAnchor;
    Vector3 rigAxis;
    protected void OnEnable()
    {
        stepperRot = rigRot * Mathf.Deg2Rad;
        body = GetComponent<Rigidbody>();
        joint = body.GetComponent<ConfigurableJoint>();

        jointRot = rigRot * Mathf.Deg2Rad;
        UpdateJointLimits(rigRot * Mathf.Deg2Rad);

        invRigRotation = Servo.GetInvRigRotation(body, joint, rigRot * Mathf.Deg2Rad);

        rigAnchor = body.TransformPoint(joint.anchor - joint.axis * rigPos);
        rigAxis = body.TransformDirection(joint.axis);
        joint.connectedAnchor = rigAnchor;
        if (joint.connectedBody != null)
        {
            rigAnchor=joint.connectedAnchor = joint.connectedBody.InverseTransformPoint(rigAnchor);
            rigAxis = joint.connectedBody.InverseTransformDirection(rigAxis);
        }
    }

    public void FixedUpdate()
    {
        jointRot = Servo.ReadJointAngle(body, joint, invRigRotation, jointRot);
        UpdateJointLimits(jointRot);


    }
    private void UpdateJointLimits(float currentRot)
    {
        currentRot *= Mathf.Rad2Deg; // convert to degrees

        var value = Mathf.InverseLerp(minRot, maxRot, currentRot);
        var pos = Mathf.Lerp(minPos, maxPos, value);
        if(value== 0 && destroyOnMin || value==1 && destroyOnMax)
        {
            DestroyImmediate(GetComponent<FixWhenNoTrigger>());
            DestroyImmediate(joint);
            DestroyImmediate(limitJoint);
            DestroyImmediate(this);
            return;
        }
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = rigAnchor + rigAxis * (pos - rigPos);
        if (maxRot - minRot < 270) // setup once
        {
            joint.lowAngularXLimit = new SoftJointLimit() { limit = -(maxRot - rigRot) };
            joint.highAngularXLimit = new SoftJointLimit() { limit = -(minRot - rigRot) };

        }
        else
        {
            if (limitJoint != null && (currentRot > limitJointCenter + 80 || currentRot < limitJointCenter - 80))
                DestroyImmediate(limitJoint);
            if (limitJoint == null)
            {
                limitJointCenter = currentRot;
                limitJoint = gameObject.AddComponent<ConfigurableJoint>();
                limitJoint.autoConfigureConnectedAnchor = false;
                limitJoint.anchor = joint.anchor;
                limitJoint.connectedAnchor = joint.connectedAnchor;
                limitJoint.connectedBody = joint.connectedBody;
                limitJoint.axis = joint.axis;
                limitJoint.angularXMotion = ConfigurableJointMotion.Limited;

                var minRange = Mathf.Max(-160, minRot - currentRot);
                var maxRange = Mathf.Min(160, maxRot - currentRot);

                limitJoint.lowAngularXLimit = new SoftJointLimit() { limit = -maxRange };
                limitJoint.highAngularXLimit = new SoftJointLimit() { limit = -minRange };
            }
        }
    }
}
