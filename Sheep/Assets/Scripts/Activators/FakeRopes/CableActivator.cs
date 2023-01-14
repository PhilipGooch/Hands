using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Recoil;
using NBG.Core;

public class CableActivator : ObjectActivator, IProjectHandAnchor, IGrabNotifications
{
    public float length = 3f;
    public bool invertCableDirection = false;
    public event Action<float> onActivatorValueChanged;

    [SerializeField]
    float startingActivation = 0f;
    [SerializeField]
    [Range(0f, 0.999f)]
    float velocitySustain = 0.9f;
    [SerializeField]
    [Range(0f, 0.999f)]
    float inertia = 0.5f;
    [SerializeField]
    Vector3 axis = Vector3.up;
    [SerializeField]
    float movementLength = 1f;

    new Rigidbody rigidbody;
    ReBody reBody;

    public Vector3 Axis => axis;
    public Vector3 WorldAxis => transform.TransformVector(Axis).normalized;
    public Vector3 GroundPosition => transform.position;
    public Vector3 EndOffset { get; set; } = Vector3.zero;
    public float MovementLength {
        get => movementLength;
        set => movementLength = value;
    }

    float velocity = 0f;
    float activationOnGrab = 0f;

    float lastMovement = 0f;

    // Start is called before the first frame update
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        ActivationAmount = startingActivation;
    }

    void Start()
    {
        reBody = new ReBody(rigidbody);
    }

    public Vector3 GetCableConnectionPosition()
    {
        var inverseActivation = 1f - ActivationAmount;
        var activation = invertCableDirection ? inverseActivation : ActivationAmount;
        return GroundPosition + WorldAxis * activation * length;
    }

    void FixedUpdate()
    {
        //var grabPosDiff = currentMovement - lastMovement;
        //velocity += grabPosDiff * inertia;

        ClampVelocityToLimits();

        if (Mathf.Abs(velocity) > 0f)
        {
            var progressFromVelocity = velocity / movementLength;
            ActivationAmount += progressFromVelocity * (invertCableDirection ? -1f : 1f);
            onActivatorValueChanged?.Invoke(ActivationAmount);
        }

        velocity *= velocitySustain;
    }

    void ClampVelocityToLimits()
    {
        if (!Mathf.Approximately(Mathf.Abs(velocity), 0f))
        {
            var velocityDirection = velocity * (invertCableDirection ? -1f : 1f);
            if (HaltActivatorBackwardMovement && velocityDirection < 0f)
            {
                velocity = 0f;
            }
            else if (HaltActivatorForwardMovement && velocityDirection > 0f)
            {
                velocity = 0f;
            }
        }
        ResetMovementPrevention();
    }

    void ResetMovementPrevention()
    {
        HaltActivatorBackwardMovement = false;
        HaltActivatorForwardMovement = false;
    }

    public void Project(ref Vector3 vrPos, ref Quaternion vrRot, ref Vector3 vrVel, ref Vector3 vrAngular, Vector3 anchorPos, Quaternion anchorRot)
    {
        vrAngular = Vector3.zero;

        var worldAnchor = reBody.TransformPoint(anchorPos);
        var projectedAnchor = Vector3.Project(worldAnchor - GroundPosition, WorldAxis);
        var relativeAnchor = Vector3.Dot(projectedAnchor, WorldAxis);

        var projectedVRPos = Vector3.Project(vrPos - GroundPosition, WorldAxis);
        var vrRelativePosition = Vector3.Dot(projectedVRPos, WorldAxis);

        var remainingLowerRope = relativeAnchor;
        var remainingUpperRope = length - remainingLowerRope;

        var invertedActivation = 1f - activationOnGrab;
        var remainingLowerRopeActivationMovement = movementLength * (invertCableDirection ? invertedActivation : activationOnGrab);
        var remainingUpperRopeActivationMovement = movementLength - remainingLowerRopeActivationMovement;

        var remainingLowerMovement = Mathf.Min(remainingLowerRope, remainingLowerRopeActivationMovement);
        var remainingUpperMovement = Mathf.Min(remainingUpperRope, remainingUpperRopeActivationMovement);

        var movementDiff = vrRelativePosition - relativeAnchor;
        var clampedMovement = Mathf.Clamp(movementDiff, -remainingLowerMovement, remainingUpperMovement);
        velocity += (clampedMovement - lastMovement) * (1f - inertia);

        var activationDiff = ActivationAmount - activationOnGrab;
        vrPos = worldAnchor + activationDiff * movementLength * WorldAxis * (invertCableDirection ? -1f : 1f);
        vrVel = Vector3.zero;
        lastMovement = clampedMovement;

        var worldHandRot = vrRot * Quaternion.Inverse(anchorRot);
        var baseWorldRot = Quaternion.LookRotation(Quaternion.Euler(0f, 0f, 90f) * WorldAxis, Vector3.up);
        var diff = worldHandRot * Quaternion.Inverse(baseWorldRot);
        Math3d.QuaternionToSwingTwist(diff, WorldAxis, out var twist, out _);
        baseWorldRot = twist * baseWorldRot;
        vrRot = baseWorldRot * anchorRot;
    }

    Vector3 ClampMovementVector(Vector3 target, float maxUpwardsLength, float maxDownwardLength)
    {
        bool facingUp = Vector3.Dot(target, WorldAxis) > 0;
        return Vector3.ClampMagnitude(target, facingUp ? maxUpwardsLength : maxDownwardLength);
    }

    public void OnGrab(Hand hand, bool firstGrab)
    {
        if (firstGrab)
        {
            activationOnGrab = ActivationAmount;
            lastMovement = 0f;
        }
    }

    public void OnRelease(Hand hand, bool firstGrab)
    {
        lastMovement = 0f;
    }
}
