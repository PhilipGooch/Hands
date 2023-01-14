using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringMovingPlatform : BaseMovingPlatform
{
    [SerializeField]
    float spring = 100f;
    [SerializeField]
    float damping = 10f;

    protected override void SetupJoint()
    {
        base.SetupJoint();
        var drive = joint.xDrive;
        drive.positionSpring = spring;
        drive.positionDamper = damping;
        joint.xDrive = drive;
        joint.targetPosition = new Vector3(-operationRange.magnitude / 2f, 0, 0);
    }
}
