using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetKinematicOnGrab : MonoBehaviour, IGrabNotifications
{
    [SerializeField]
    Rigidbody rig;

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
            rig.isKinematic = true;
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
            rig.isKinematic = false;
    }
}
