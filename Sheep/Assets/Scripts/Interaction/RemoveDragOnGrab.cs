using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveDragOnGrab : MonoBehaviour, IGrabNotifications
{
    new Rigidbody rigidbody;
    ReBody reBody;
    float originalDrag;
    float originalAngularDrag;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        originalDrag = reBody.drag;
        originalAngularDrag = reBody.angularDrag;
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            reBody.drag = 0;
            reBody.angularDrag = 0;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            reBody.drag = originalDrag;
            reBody.angularDrag = originalAngularDrag;
        }
    }
}
