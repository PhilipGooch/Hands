using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;

public class GrabPlunger : ObjectActivator
{
    [SerializeField]
    [Tooltip("Defines the amplitude of plunger movement")]
    float distance = 1f;
    ConfigurableJoint joint;
    Rigidbody ourRig;
    ReBody reBody;
    ReBody otherReBody;

    Vector3 endPos;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        ourRig = GetComponent<Rigidbody>();
        reBody = new ReBody(ourRig);

        var otherRig = joint.connectedBody;
        otherReBody = new ReBody(otherRig);
        joint.autoConfigureConnectedAnchor = false;
        var scale = Vector3.Project(transform.lossyScale, joint.axis).magnitude;
        var halfDelta = reBody.rotation * joint.axis * distance * 0.5f * scale;
        var worldPosCenter = reBody.TransformPoint(joint.anchor) + halfDelta;
        if (otherReBody.BodyExists)
        {
            joint.connectedAnchor = otherReBody.InverseTransformPoint(worldPosCenter);
            endPos = otherReBody.InverseTransformPoint(worldPosCenter + halfDelta);

        }
        else
        {
            joint.connectedAnchor = worldPosCenter;
            endPos = worldPosCenter + halfDelta;
        }


        var limit = joint.linearLimit;
        limit.limit = distance * 0.5f * scale;
        joint.linearLimit = limit;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var currentPos = reBody.TransformPoint(joint.anchor);
        var otherPos = endPos;
        if (otherReBody.BodyExists)
        {
            otherPos = otherReBody.TransformPoint(endPos);
        }
        // Make sure relative position does not get scaled
        var linearPos = Vector3.Project(reBody.InverseTransformDirection(otherPos - currentPos), joint.axis).magnitude;
        var linearLimit = joint.linearLimit.limit;
        var maxLength = linearLimit * 2;
        var progress = 1f - (linearPos / maxLength);
        ActivationAmount = progress;
    }
}
