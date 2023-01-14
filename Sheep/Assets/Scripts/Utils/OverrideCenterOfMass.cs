using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Recoil;

public class OverrideCenterOfMass : MonoBehaviour
{
    [SerializeField]
    Vector3 centerOfMass = Vector3.zero;
    [SerializeField]
    Rigidbody rig;
    ReBody reBody;

    private void Start()
    {
        if (rig)
        {
            reBody = new ReBody(rig);
            reBody.centerOfMass = centerOfMass;
            rig.centerOfMass = centerOfMass;
        }
    }

    private void OnValidate()
    {
        if (!rig)
        {
            rig = GetComponent<Rigidbody>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (rig)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rig.TransformPoint(rig.centerOfMass), 0.25f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(rig.TransformPoint(centerOfMass), 0.25f);
        }
    }
}
