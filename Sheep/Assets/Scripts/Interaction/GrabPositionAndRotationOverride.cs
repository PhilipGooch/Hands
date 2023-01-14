using Recoil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabPositionAndRotationOverride : MonoBehaviour, IOverrideGrabAnchor
{
    [SerializeField]
    Collider targetCollider;
    [SerializeField]
    bool enableRotationSnapping = true;
    [SerializeField]
    float maxSnappingAngle = 45f;
    [SerializeField]
    Vector3 snapAngle;

    new Rigidbody rigidbody;
    ReBody reBody;

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
    }

    public bool HandIsWithinSnappingZone(Vector3 currentGrabPosition, Quaternion currentGrabRotation)
    {
        if (targetCollider.bounds.Contains(currentGrabPosition))
        {
            if (enableRotationSnapping)
            {
                var objectRotation = reBody.rotation;
                var grabAngleDiff = Mathf.Abs(Quaternion.Angle(objectRotation, currentGrabRotation * Quaternion.Euler(snapAngle)));
                if (grabAngleDiff < maxSnappingAngle)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        return false;
    }

    public (Vector3 grabPosition, Quaternion grabRotation) Reanchor(Vector3 currentGrabPosition, Quaternion currentGrabRotation)
    {

        var position = currentGrabPosition;
        var rotation = reBody.rotation;

        if (HandIsWithinSnappingZone(currentGrabPosition, currentGrabRotation))
        {
            position = targetCollider.transform.position;
            if (enableRotationSnapping)
            {
                rotation = Quaternion.Euler(snapAngle);
            }
        }

        return (position, rotation);
    }
}
