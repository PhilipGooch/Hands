using NBG.LogicGraph;
using Recoil;
using System;
using UnityEngine;

public class SliderActivator : ObjectActivator, IProjectHandAnchor
{
    [NodeAPI("OnRawValueChange")]
    public event Action<float> onRawValueChange;

    [SerializeField]
    float maxAcceleration = 10;
    [SerializeField]
    float maxVelocity = 2;
    [SerializeField]
    float maxDistance = 1;
    [SerializeField]
    [Range(0f, 1f)]
    float startActivation = 0.5f;
    [SerializeField]
    int snapPositionCount = 3;

    float distancePerSnap = 0f;

    float snapTarget;

    ConfigurableJoint joint;
    Rigidbody body;
    ReBody reBody;
    ReBody connectedBody;
    Vector3 rigAnchor;

    protected override void OnEnable()
    {
        base.OnEnable();

        body = GetComponent<Rigidbody>();
        joint = body.GetComponent<ConfigurableJoint>();

        if (snapPositionCount > 1)
        {
            distancePerSnap = maxDistance / (snapPositionCount - 1);
        }
        else
        {
            distancePerSnap = 0;
        }

        var halfTravel = maxDistance / 2;
        joint.autoConfigureConnectedAnchor = false;
        joint.linearLimit = new SoftJointLimit() { limit = halfTravel };

        rigAnchor = body.TransformPoint(joint.anchor);
        joint.connectedAnchor = rigAnchor + body.TransformDirection(joint.axis * (halfTravel));
        if (joint.connectedBody != null)
        {
            rigAnchor = joint.connectedBody.InverseTransformPoint(rigAnchor);
            joint.connectedAnchor = joint.connectedBody.InverseTransformPoint(joint.connectedAnchor);
        }
        else
        {
            //Dont see any scenario where slider would not have a parent, but ....
            Debug.Assert(transform.parent != null, "Slider has not parent");
            rigAnchor = transform.parent.InverseTransformPoint(rigAnchor);
        }

        body.MovePosition(body.position + body.TransformDirection(joint.axis * startActivation));

        snapTarget = 100000;// make sure won't snap to this
        UpdateSnapTarget(startActivation);
    }

    void Start()
    {
        // Can't use reBody OnEnable since it won't be properly initialized
        // Setting reBody positions/rotation outside FixedUpdate will also not work
        reBody = new ReBody(body);
        connectedBody = new ReBody(joint.connectedBody);
    }

    public void FixedUpdate()
    {
        var axis = reBody.TransformDirection(joint.axis.normalized);
        float currentPos;

        if (connectedBody.BodyExists)
        {
            currentPos = Vector3.Dot(axis, reBody.TransformPoint(joint.anchor) - connectedBody.TransformPoint(rigAnchor));
        }
        else
        {
            currentPos = Vector3.Dot(axis, reBody.TransformPoint(joint.anchor) - transform.parent.TransformPoint(rigAnchor));
        }

        UpdateSnapTarget(currentPos);

        Servo.ApplyLinearDynamics(reBody, joint, snapTarget, currentPos, maxAcceleration, maxVelocity);
    }

    private void UpdateSnapTarget(float currentPos)
    {
        var snapTargetDist = Mathf.Abs(currentPos - snapTarget);
        // find where to snap
        var bestTarget = snapTarget;
        var bestDist = snapTargetDist;
        for (int i = 0; i < snapPositionCount; i++)
        {
            var dist = Mathf.Abs(currentPos - distancePerSnap * i);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = distancePerSnap * i;
            }
        }
        if (bestDist < snapTargetDist / 2)
        {
            snapTarget = bestTarget;
            ActivationAmount = Mathf.InverseLerp(0f, maxDistance, snapTarget);
        }

        onRawValueChange?.Invoke(Mathf.InverseLerp(0f, maxDistance, currentPos));
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectAnchorUtils.ProjectLinear(reBody, joint.anchor, joint.axis.normalized, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
    }
}
