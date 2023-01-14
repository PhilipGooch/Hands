using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ObjectWeightTracker))]
public class PressurePlateActivator : ObjectActivator
{
    [SerializeField]
    float weightToActivate = 50f;
    [SerializeField]
    Vector3 pressDistance = new Vector3(0, 0.125f, 0f);
    ObjectWeightTracker bodyTracker;

    ConfigurableJoint joint;
    bool isPressed = false;

    private void Awake()
    {
        bodyTracker = GetComponent<ObjectWeightTracker>();
        SetupJoint();
    }

    void SetupJoint()
    {
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.anchor = Vector3.zero;

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = transform.position;
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        var linearLimit = joint.linearLimit;
        linearLimit.limit = pressDistance.magnitude;
        joint.linearLimit = linearLimit;

        joint.axis = transform.up;
        joint.secondaryAxis = transform.forward;
    }

    void UpdateJointDrive(bool pressed)
    {
        var drive = joint.xDrive;
        const float maxSpring = 1000000f;
        drive.positionSpring = pressed ? 0f : maxSpring;
        drive.positionDamper = maxSpring / 1000f;
        joint.xDrive = drive;
    }

    private void FixedUpdate()
    {
        var currentWeight = bodyTracker.GetWeight();
        if (!isPressed)
        {
            isPressed = currentWeight > weightToActivate;
        }
        else
        {
            // Stay pressed until we lose all weight
            isPressed = currentWeight > 0;
        }
        ActivationAmount = isPressed ? 1f : 0f;
        UpdateJointDrive(isPressed);
    }
}
