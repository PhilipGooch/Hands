using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WagonLiftPlatform : BaseMovingPlatform
{
    private enum Direction
    {
        Down,
        Up,
    }

    [SerializeField]
    private float moveSpeed = 1f;
    [SerializeField]
    private float maxAcceleration = 1f;
    [SerializeField]
    private PlatformState state;
    internal PlatformState State => state;
    [SerializeField]
    AdaptiveAudioSource liftSound;

    private Direction direction;
    private bool moving;

    public Action onLoweredAndNotMoving;
    public Action onRaisedAndNotMoving;

    internal bool Moving => moving || state == PlatformState.MOVING;
    internal bool LiftedOrMoving => direction == Direction.Up || state == PlatformState.AT_END;
    internal bool Lowered => state == PlatformState.AT_START;

    private void OnValidate()
    {
        if (liftSound.audioSource == null)
            liftSound.audioSource = GetComponent<AudioSource>();
    }

    private void FixedUpdate()
    {
        state = GetState();

        SetServoPositionBasedOnActivator(direction == Direction.Up ? 1 : 0);

        switch (state)
        {
            case PlatformState.AT_START:
                if (direction == Direction.Down && moving)
                {
                    liftSound.StopSound();
                    onLoweredAndNotMoving?.Invoke();
                    moving = false;
                }
                break;
            case PlatformState.MOVING:
                liftSound.UpdateSound(rePlatformToMove.velocity.magnitude / (moveSpeed / 2));
                break;
            case PlatformState.AT_END:
                if (moving && direction == Direction.Up)
                {
                    liftSound.StopSound();
                    onRaisedAndNotMoving?.Invoke();
                    moving = false;
                }
                break;
        }
    }

    private void SetServoPositionBasedOnActivator(float amount)
    {
        var targetPos = Mathf.Lerp(0f, operationRange.magnitude, amount);
        var currentPos = GetLinearPosition();
        ApplyLinearDynamics(currentPos, targetPos, maxAcceleration, moveSpeed);
    }

    public void Raise()
    {
        liftSound.PlaySound();
        direction = Direction.Up;
        moving = true;
    }

    public void Lower()
    {
        liftSound.PlaySound();
        direction = Direction.Down;
        moving = true;
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
#endif
}
