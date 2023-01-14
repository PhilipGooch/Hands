using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Soccer
{
    public class Friction : MonoBehaviour
    {
        public float friction = 1;
        public float angularFriction = 1;
        Rigidbody body;
        void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            body.velocity = Vector3.MoveTowards(body.velocity, Vector3.zero, friction * Time.fixedDeltaTime);
            body.angularVelocity = Vector3.MoveTowards(body.angularVelocity, Vector3.zero, angularFriction * Time.fixedDeltaTime);
        }
    }
}