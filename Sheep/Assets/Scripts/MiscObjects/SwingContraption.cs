using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.XPBDRope;
using Recoil;

public class SwingContraption : MonoBehaviour
{
    [SerializeField]
    Transform platformConnectedAnchorPoint;
    [SerializeField]
    ConfigurableJoint platformJoint;
    [SerializeField]
    ConfigurableJoint liftableObjectJoint;
    [SerializeField]
    Vector3 liftAxis = Vector3.up;
    [SerializeField]
    float maxRopeLength = 5f;

    Vector3 originalAnchor;
    Rigidbody objectToLift;
    ReBody reObjectToLift;
    ReBody platformReBody;

    private void Start()
    {
        objectToLift = liftableObjectJoint.GetComponent<Rigidbody>();
        reObjectToLift = new ReBody(objectToLift);
        platformReBody = new ReBody(platformJoint.connectedBody);
        originalAnchor = platformJoint.anchor;
    }

    float GetLiftProgress()
    {
        var limit = liftableObjectJoint.linearLimit.limit;
        var startPoint = liftableObjectJoint.connectedAnchor - liftAxis * limit;
        var diff = reObjectToLift.TransformPoint(liftableObjectJoint.anchor) - startPoint;
        return Mathf.Clamp01(diff.magnitude / (limit * 2f));
    }

    private void FixedUpdate()
    {
        platformJoint.connectedAnchor = platformReBody.InverseTransformPoint(platformConnectedAnchorPoint.position);
        platformJoint.anchor = originalAnchor + liftAxis * maxRopeLength * GetLiftProgress();
    }
}
