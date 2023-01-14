using Recoil;
using System;
using UnityEngine;

public abstract class FlickSwitchBase : MonoBehaviour
{
    [SerializeField]
    [HideInInspector]
    private Joint joint;

    [SerializeField]
    protected int snapPositionCount = 3;
    [SerializeField]
    protected int startSnapPosition = 1;

    [Range(0, 180)]
    [SerializeField]
    protected int rotationArcDegrees = 120;
    [SerializeField]
    protected float switchLength = 1;
    [SerializeField]
    protected bool invertValue;

    protected int lastSnapPosition;

    protected Vector3 anchorRotationAxis;
    protected Vector3 flickDirection;

    private float totalArcLength;
    private float snapAreaArcLength;
    private Rigidbody anchor;
    ReBody reAnchor;

    protected Transform Parent => transform.parent ? transform.parent : transform;

    //Animation
    [SerializeField]
    private float switchAnimationDuration = 0.1f;
    private Quaternion startAnimationRotation;
    private Quaternion goalAnimationRotation;
    private float animationTimeSpent = 0;
    protected bool runAnimation { get; private set; }

    protected abstract void StartSetup();
    protected abstract void UpdateActivation(int step);

    private void OnValidate()
    {
        if (joint == null)
            joint = GetComponentInChildren<Joint>();
    }

    private void Start()
    {
        anchor = joint.connectedBody;
        reAnchor = new ReBody(anchor);
        lastSnapPosition = startSnapPosition;
        totalArcLength = GetArcLength(rotationArcDegrees);
        snapAreaArcLength = (totalArcLength / (snapPositionCount - 1) / 2) * 0.5f;

        StartSetup();
    }

    private void FixedUpdate()
    {
        if (runAnimation)
        {
            if (animationTimeSpent < switchAnimationDuration)
            {
                reAnchor.rotation = Quaternion.Lerp(startAnimationRotation, goalAnimationRotation, animationTimeSpent / switchAnimationDuration);
            }
            else
            {
                reAnchor.rotation = goalAnimationRotation;
                runAnimation = false;
            }
            animationTimeSpent += Time.deltaTime;
        }
    }

    protected void UpdateSnap(float signedHandPosition, Vector3 axis)
    {
        var pos = GetSnapPositionFromHandPosition(signedHandPosition);
        if (pos.canSnap && pos.snapStep != lastSnapPosition)
        {
            var degrees = GetDegreesFromStep(pos.snapStep);

            SetAngle(degrees, axis);

            lastSnapPosition = pos.snapStep;

            UpdateActivation(pos.snapStep);
        }
    }

    private (bool canSnap, int snapStep) GetSnapPositionFromHandPosition(float handPosition)
    {
        var half = totalArcLength / 2;
        var clampedHandPosition = Mathf.Clamp(handPosition, -half, half) + half;
        var normalized = clampedHandPosition / totalArcLength;

        var step = Mathf.FloorToInt(normalized * (snapPositionCount - 1));

        if (CanSnapAtStep(GetPositionAtStep(step), clampedHandPosition, snapAreaArcLength))
        {
            return (true, step);
        }
        else
        {
            return (false, step);
        }
    }

    private bool CanSnapAtStep(float targetPosition, float currentPosition, float snapZone)
    {
        float min = targetPosition - snapZone;
        float max = targetPosition + snapZone;

        return currentPosition >= min && currentPosition <= max;
    }

    protected void SetAngle(float degrees, Vector3 axis)
    {
        Quaternion q = Quaternion.AngleAxis(degrees, axis);
        goalAnimationRotation = Parent.rotation * q;
        startAnimationRotation = reAnchor.rotation;
        runAnimation = true;
        animationTimeSpent = 0;
    }

    protected float GetDegreesFromStep(int step)
    {
        return (rotationArcDegrees * GetNormalizedStep(step)) - (rotationArcDegrees / 2);
    }

    protected float GetPositionAtStep(int step)
    {
        return totalArcLength * GetNormalizedStep(step);
    }

    protected float GetNormalizedStep(int step)
    {
        return (float)step / (snapPositionCount - 1);
    }

    protected float GetArcLength(float degrees)
    {
        return 2 * Mathf.PI * switchLength * (degrees / 360);
    }

    protected float GetSignedMagnitudeOnAxis(Vector3 handPos, Vector3 worldNormal)
    {
        var projection = Vector3.Project(handPos - Parent.position, worldNormal);
        return Vector3.Dot(projection, worldNormal);
    }

    protected virtual void OnDrawGizmos()
    {

        /*
          //hand distance from center
          var signedHandPosForward = GetSignedMagnitudeOnAxis(handPos, Parent.TransformDirection(Vector3.forward));
          var signedHandPosSide = GetSignedMagnitudeOnAxis(handPos, Parent.TransformDirection(Vector3.right));
          Gizmos.color = Color.green;
          Gizmos.DrawRay(Parent.position, signedHandPosForward * Parent.TransformDirection(Vector3.forward));
          Gizmos.DrawRay(Parent.position, signedHandPosSide * Parent.TransformDirection(Vector3.right));
          */

        //draw rays to display current rotationArcDegrees
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(Parent.position, Quaternion.AngleAxis(rotationArcDegrees / 2, Parent.right) * Parent.up);
            Gizmos.DrawRay(Parent.position, Quaternion.AngleAxis(-rotationArcDegrees / 2, Parent.right) * Parent.up);
        }

        //draw ray to display total distance hands needs to travel in order to fully switch
        {
            Gizmos.color = Color.blue;
            var totalArcLength = GetArcLength(rotationArcDegrees);
            var dir = Parent.TransformDirection(Vector3.forward);
            Gizmos.DrawRay(Parent.position + dir * (-totalArcLength / 2), dir * totalArcLength);
        }

        // draw column to display virtual switch height
        {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(Parent.position, Parent.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0, switchLength / 2, 0), new Vector3(0.2f, switchLength, 0.2f));
            Gizmos.matrix = matrix;
        }
    }
}
