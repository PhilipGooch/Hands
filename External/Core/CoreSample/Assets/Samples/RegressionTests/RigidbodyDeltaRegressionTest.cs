using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NBG.Core;

/// <summary>
/// Add this script on a cube. Add a target transform that has a different position/rotation than the cube. The cube must move to that transform and stay there.
/// </summary>
public class RigidbodyDeltaRegressionTest : MonoBehaviour
{
    [SerializeField]
    Transform targetTransform;

    Rigidbody rig;
    void Start()
    {
        rig = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var posDiff = targetTransform.position - rig.position;
        rig.velocity = posDiff / Time.fixedDeltaTime;

        var rotDiff = targetTransform.rotation * Quaternion.Inverse(rig.rotation);
        rig.angularVelocity = rotDiff.QToAngleAxis() / Time.fixedDeltaTime;
    }
}
