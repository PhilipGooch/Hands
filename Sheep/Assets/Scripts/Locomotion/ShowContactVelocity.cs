using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowContactVelocity : MonoBehaviour
{
    Vector3 groundVelocity;
    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionStay(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        //DebugLines.DrawRay(collision.GetContact(0).point, collision.relativeVelocity);
        if (collision.rigidbody)
        {
            //DebugLines.DrawRay(collision.GetContact(0).point, collision.rigidbody.GetPointVelocity(collision.GetContact(0).point) + Vector3.up);
            groundVelocity = collision.rigidbody.GetPointVelocity(collision.GetContact(0).point);
        }

    }

    private void FixedUpdate()
    {
        DebugLines.DrawRay(transform.position, groundVelocity+Vector3.up);
           groundVelocity = Vector3.zero;
    }
}
