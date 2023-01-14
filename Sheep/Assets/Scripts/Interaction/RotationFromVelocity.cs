using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;

public class RotationFromVelocity : MonoBehaviour
{
    [SerializeField]
    Vector3 rotationAxis = new Vector3(1, 0, 0);
    [SerializeField]
    bool projectVelocityToAxis = false;
    [SerializeField]
    Vector3 velocityAxis = new Vector3(1,0,0);
    [SerializeField]
    Rigidbody target;
    [SerializeField]
    float objectRadius = 0.5f;

    float objectLength = 0f;

    ReBody reBody;

    private void Start()
    {
        objectLength = objectRadius * 2f * Mathf.PI;
        reBody = new ReBody(target);
    }

    // Update is called once per frame
    void Update()
    {
        if (reBody.BodyExists)
        {
            var velocity = reBody.velocity;
            var direction = 1f;
            if (projectVelocityToAxis)
            {
                var worldAxis = reBody.TransformDirection(velocityAxis);
                velocity = Vector3.Project(velocity, worldAxis);
                direction = Mathf.Sign(Vector3.Dot(velocity, worldAxis));
            }
            var distance = velocity.magnitude * Time.deltaTime;
            var rotations = direction * distance / objectLength;

            transform.Rotate(rotationAxis * 360f * rotations);
        }
    }
}
