using Recoil;
using UnityEngine;

public enum PlatformState
{
    AT_START,
    MOVING,
    AT_END
}

public abstract class BaseMovingPlatform : MonoBehaviour
{
    [SerializeField]
    protected Rigidbody platformToMove;
    [SerializeField]
    protected Vector3 operationRange;
    [SerializeField]
    protected bool stepper = true;

    float stepperPos;
    [SerializeField]
    protected ConfigurableJoint joint;

    protected Vector3 startPos;
    protected Vector3 localStartPos;
    protected Vector3 localEndPos;
    protected Vector3 operationDirection = Vector3.zero;
    protected ReBody rePlatformToMove;

    const float velocityThresh = 0.01f;
    const float distanceThresh = 0.2f;

    protected virtual void Start()
    {
        rePlatformToMove = new ReBody(platformToMove);
        SetupJoint();
        operationDirection = rePlatformToMove.rotation * operationRange.normalized;
    }

    protected virtual void SetupJoint()
    {
        if (joint == null)
            joint = platformToMove.gameObject.AddComponent<ConfigurableJoint>();

        joint.autoConfigureConnectedAnchor = false;

        joint.anchor = Vector3.zero;

        if (joint.connectedBody != null)
        {
            joint.connectedAnchor = joint.connectedBody.transform.InverseTransformPoint(rePlatformToMove.position + rePlatformToMove.rotation * operationRange * 0.5f);
            localStartPos = joint.connectedBody.transform.InverseTransformPoint(rePlatformToMove.position);
            localEndPos = joint.connectedBody.transform.InverseTransformPoint(rePlatformToMove.position + rePlatformToMove.rotation * operationRange);
        }
        else
        {
            joint.connectedAnchor = rePlatformToMove.position + rePlatformToMove.rotation * operationRange * 0.5f;
            localStartPos = rePlatformToMove.position;
            localEndPos = rePlatformToMove.position + rePlatformToMove.rotation * operationRange;
        }

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        var linearLimit = joint.linearLimit;
        linearLimit.limit = operationRange.magnitude / 2f;
        joint.linearLimit = linearLimit;

        joint.axis = operationRange.normalized;
        joint.secondaryAxis = Vector3.Cross(operationRange.normalized, Vector3.up);

        startPos = rePlatformToMove.TransformPoint(joint.anchor);
    }

    public float GetLinearPosition()
    {
        var axis = rePlatformToMove.rotation * operationRange.normalized;
        return Vector3.Dot(axis, rePlatformToMove.TransformPoint(joint.anchor) - startPos);
    }

    public float GetNormalizedLinearPosition()
    {
        return GetLinearPosition() / operationRange.magnitude;
    }

    void ApplyLinearDynamicsStepper(float currentPos, float targetPos, float maxAcceleration, float maxSpeed)
    {
        stepperPos = Servo.MoveStepper(stepperPos, currentPos, targetPos, maxSpeed);
        Servo.ApplyLinearDynamics(rePlatformToMove, joint, stepperPos, currentPos, maxAcceleration, 0);
    }

    protected void ApplyLinearDynamics(float currentPos, float targetPos, float maxAcceleration, float maxSpeed)
    {
        if (stepper)
        {
            ApplyLinearDynamicsStepper(currentPos, targetPos, maxAcceleration, maxSpeed);
        }
        else
        {
            Servo.ApplyLinearDynamics(rePlatformToMove, joint, targetPos, currentPos, maxAcceleration, maxSpeed);
        }
    }


    protected PlatformState GetState()
    {
        float velMag = Vector3.Scale(rePlatformToMove.velocity, operationDirection).magnitude;

        Vector3 localPos;
        if (joint.connectedBody != null)
            localPos = joint.connectedBody.transform.InverseTransformPoint(rePlatformToMove.position);
        else
            localPos = rePlatformToMove.position;

        if (Vector3.Distance(localPos, localStartPos) < distanceThresh && velMag <= velocityThresh)
            return PlatformState.AT_START;
        else if (Vector3.Distance(localPos, localEndPos) < distanceThresh && velMag <= velocityThresh)
            return PlatformState.AT_END;
        else
            return PlatformState.MOVING;

    }

#if UNITY_EDITOR
    Color gizmoColor = new Color(137f / 255f, 79f / 255f, 184f / 255f);
    protected virtual void OnDrawGizmosSelected()
    {
        if (platformToMove != null)
        {
            var previousColor = Gizmos.color;
            Gizmos.color = gizmoColor;
            // Draw preview movement gizmo
            Gizmos.color = Color.yellow;
            var previousMatrix = Gizmos.matrix;
            var gizmoStartPosition = Application.isPlaying ? startPos : platformToMove.position;

            Quaternion operatingRotation = Quaternion.LookRotation(operationRange.normalized, platformToMove.transform.up);
            Gizmos.matrix = Matrix4x4.TRS(gizmoStartPosition + platformToMove.rotation * operationRange * 0.5f, platformToMove.transform.rotation * operatingRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(0.25f, 0.25f, operationRange.magnitude));
            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }
    }
#endif
}
