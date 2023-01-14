using UnityEngine;

[RequireComponent(typeof(PlatformNode))]
public class ActivatorMovingPlatform : BaseMovingPlatform
{
    [SerializeField]
    private ObjectActivator haltableActivator;
    [SerializeField]
    private PlatformNode activationNode;

    [SerializeField]
    private float forwardSpeed = 1;
    [SerializeField]
    private float forwardAcceleration = 1;
    [SerializeField]
    private float backwardsSpeed = 1;
    [SerializeField]
    private float backwardsAcceleration = 1;

    private float lastActivationValue;
    private float moveSpeed = 1f;
    private float maxAcceleration = 1f;

    protected override void Start()
    {
        base.Start();
        if (activationNode == null)
        {
            activationNode = GetComponent<PlatformNode>();
        }

        lastActivationValue = activationNode.ActivationValue;

    }

    private void FixedUpdate()
    {
        SetServoPositionBasedOnActivator();
    }

    private void SetServoPositionBasedOnActivator()
    {

        bool isGoingForward = activationNode.ActivationValue - lastActivationValue > 0;

        moveSpeed = isGoingForward ? forwardSpeed : backwardsSpeed;
        maxAcceleration = isGoingForward ? forwardAcceleration : backwardsAcceleration;

        var targetPos = Mathf.Lerp(0f, operationRange.magnitude, activationNode.ActivationValue);
        var currentPos = GetLinearPosition();

        ApplyLinearDynamics(currentPos, targetPos, maxAcceleration, moveSpeed);
        if (haltableActivator != null)
        {
            if (Mathf.Abs(currentPos - targetPos) > 0.5f && rePlatformToMove.velocity.magnitude < moveSpeed * 0.1f)
            {
                if (currentPos < targetPos)
                {
                    haltableActivator.HaltActivatorForwardMovement = true;
                }
                else
                {
                    haltableActivator.HaltActivatorBackwardMovement = true;
                }
            }
            else
            {
                haltableActivator.HaltActivatorForwardMovement = false;
                haltableActivator.HaltActivatorBackwardMovement = false;
            }
        }

        lastActivationValue = activationNode.ActivationValue;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (haltableActivator != null)
        {
            Gizmos.DrawLine(transform.position, haltableActivator.transform.position);
        }
    }
#endif
}
