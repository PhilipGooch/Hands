using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Soccer
{
    public class Respawn : MonoBehaviour
    {
        Rigidbody body;
        void Start()
        {
            body = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (body.position.y < -5)
            {
                body.position = new Vector3(0, 5, 0);
                body.velocity = body.angularVelocity = Vector3.zero;
            }
        }
    }
}