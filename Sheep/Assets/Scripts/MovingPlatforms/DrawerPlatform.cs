using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ObjectWeightTracker))]
public class DrawerPlatform : BaseMovingPlatform, IGrabNotifications
{
    [SerializeField]
    float maxSupportedWeight = 100;
    [SerializeField]
    float fallSpeed = 15;
    [SerializeField]
    float fallAcceleration = 1f;

    ObjectWeightTracker weightTracker;
    float? fixedPosition = null;
    bool currentlyGrabbed = false;

    protected override void Start()
    {
        base.Start();
        weightTracker = GetComponent<ObjectWeightTracker>();
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            currentlyGrabbed = true;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            currentlyGrabbed = false;
            fixedPosition = GetLinearPosition();
        }
    }

    bool IsOverweight()
    {
        // Aggregate masses from a couple of frames
        // We only want the platform to move if the objects weigh it down, not because of a collision
        // This also prevents some nasty stop&go - when the platform starts to move the measured mass becomes a lot lower until the velocities equalize
        // If we used that low mass to check if we are overweight, we would stop moving. Then we would be overweight again, then underweight, then overweight...
        return weightTracker.GetWeight() > maxSupportedWeight;
    }

    void FixedUpdate()
    {
        if (!currentlyGrabbed)
        {
            var currentPos = GetLinearPosition();

            if (IsOverweight())
            {
                ApplyLinearDynamics(currentPos, 0, fallAcceleration, fallSpeed);
                fixedPosition = null;
            }
            else
            {
                if (fixedPosition == null)
                {
                    fixedPosition = GetLinearPosition();
                }
                ApplyLinearDynamics(currentPos, (float)fixedPosition, 999, 999);
            }
        }
    }
}
