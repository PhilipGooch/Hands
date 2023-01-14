using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockRotation : MonoBehaviour
{
    [SerializeField]
    bool lockX = false;
    [SerializeField]
    bool lockY = false;
    [SerializeField]
    bool lockZ = false;
    [SerializeField]
    Vector3 targetRotation = Vector3.zero;

    private void FixedUpdate()
    {
        var currentRot = transform.eulerAngles;
        if (lockX)
        {
            currentRot.x = targetRotation.x;
        }
        if (lockY)
        {
            currentRot.y = targetRotation.y;
        }
        if (lockZ)
        {
            currentRot.z = targetRotation.z;
        }

        transform.eulerAngles = currentRot;
    }
}
