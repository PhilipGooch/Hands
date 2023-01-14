using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tether : MonoBehaviour
{
    [SerializeField]
    float maxDistance = 5f;
    [SerializeField]
    float returnSpeed = 1f;
    [SerializeField]
    bool tetherVertically = false;

    new Rigidbody rigidbody;
    ReBody reBody;
    Vector3 tetherPosition;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        tetherPosition = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var diff = tetherPosition - transform.position;
        var direction = diff.normalized;
        var distance = diff.magnitude;
        if (distance > Mathf.Epsilon)
        {
            var wantedVelocity = direction * Mathf.Min(returnSpeed / Time.fixedDeltaTime, distance);
            var currentVel = reBody.velocity;
            if (!tetherVertically)
            {
                wantedVelocity.y = currentVel.y;
            }
            var lerpSpeed = Mathf.Pow(Mathf.Clamp01(distance / maxDistance), 8);
            reBody.velocity = Vector3.Lerp(currentVel, wantedVelocity, lerpSpeed);
        }
    }
}
