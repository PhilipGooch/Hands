using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimitRotationWhenNotGrabbed : MonoBehaviour, IGrabNotifications
{
    HingeJoint targetJoint;
    JointLimits previousLimits;
    bool usePreviousLimits = false;

    // Start is called before the first frame update
    void Awake()
    {
        targetJoint = GetComponent<HingeJoint>();
    }

    IEnumerator Start()
    {
        // Wait until joint has its limits set up
        yield return new WaitForEndOfFrame();
        PreventMovement();
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            targetJoint.limits = previousLimits;
            targetJoint.useLimits = usePreviousLimits;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            PreventMovement();
        }
    }

    void PreventMovement()
    {
        previousLimits = targetJoint.limits;
        usePreviousLimits = targetJoint.useLimits;
        var limits = targetJoint.limits;
        limits.max = targetJoint.angle;
        limits.min = targetJoint.angle - 0.1f;
        targetJoint.limits = limits;
        targetJoint.useLimits = true;
    }
}
