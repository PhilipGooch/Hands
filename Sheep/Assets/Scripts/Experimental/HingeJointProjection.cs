using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HingeJointProjection : MonoBehaviour, IProjectHandAnchor
{
    HingeJoint target;
    Rigidbody body;
    ReBody reBody;

    void Start()
    {
        target = GetComponent<HingeJoint>();
        body = GetComponent<Rigidbody>();
        reBody = new ReBody(body);
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        ProjectAnchorUtils.ProjectHingeJoint(reBody, target, ref vrPos, ref vrRot, ref vrVel, ref vrAngular, anchorPos, anchorRot);
    }
}
