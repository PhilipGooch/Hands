using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Friction : MonoBehaviour
{
    Rigidbody body;
    ReBody reBody;
    public float frictionA=5;
    public float angularFrictionA = 5;
    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        reBody = new ReBody(body);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var stopA = -reBody.velocity / Time.fixedDeltaTime;
        reBody.AddForce(Vector3.ClampMagnitude( stopA, frictionA), ForceMode.Acceleration);
        var stopAngular = -reBody.angularVelocity / Time.fixedDeltaTime;
        reBody.AddTorque(Vector3.ClampMagnitude(stopAngular, angularFrictionA), ForceMode.Acceleration);
    }
}
