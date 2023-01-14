using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VehiclesDemo
{
    public class DebugObjectSpeed : MonoBehaviour
    {
        Rigidbody body;
        bool started;
        void Start()
        {
            body = GetComponent<Rigidbody>();
            started = true;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!started)
            {
                return;
            }

            GUI.color = Color.black;
            Handles.Label(transform.position, "" + body.velocity.magnitude);
        }
#endif
    }
}
