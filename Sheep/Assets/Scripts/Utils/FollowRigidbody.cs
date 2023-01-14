using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;

public class FollowRigidbody : MonoBehaviour
{
    [SerializeField]
    Rigidbody targetBody;
    ReBody reBody;

    Vector3 relativePosition;

    void Start()
    {
        reBody = new ReBody(targetBody);
        relativePosition = reBody.InverseTransformPoint(transform.position);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = reBody.TransformPoint(relativePosition);
    }
}
