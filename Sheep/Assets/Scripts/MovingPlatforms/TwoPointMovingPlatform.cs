using NBG.Core;
using NBG.LogicGraph;
using Recoil;
using System.Collections.Generic;
using UnityEngine;

public class TwoPointMovingPlatform : MonoBehaviour, IManagedBehaviour
{
    [SerializeField]
    private Vector3 axis = Vector3.up;
    [SerializeField]
    private float forceToBlock = 100f;
    private List<Collider> platformColliders = new List<Collider>();
    new private Rigidbody rigidbody;

    [NodeAPI("FirstPoint")]
    public Vector3 FirstPoint { get; set; }
    [NodeAPI("SecondPoint")]
    public Vector3 SecondPoint { get; set; }

    private Vector3 worldAxis;
    private Quaternion startRotation;

    private ReBody reBody;

    public int belowBlockCount = 0;
    public int aboveBlockCount = 0;
    public bool BlockedBelow
    {
        get
        {
            return belowBlockCount > 0;
        }
    }

    public bool BlockedAbove
    {
        get
        {
            return aboveBlockCount > 0;
        }
    }

    public void OnLevelLoaded()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        platformColliders = new List<Collider>(GetComponentsInChildren<Collider>());
        axis = axis.normalized;
        worldAxis = transform.rotation * axis;
        startRotation = transform.rotation;
    }

    public void OnAfterLevelLoaded()
    {
    }

    public void OnLevelUnloaded()
    {
    }

    private void FixedUpdate()
    {
        var currentPos = reBody.rePosition;
        var center = (FirstPoint + SecondPoint) / 2f;

        var positionDelta = (center - (Vector3)currentPos.pos);
        positionDelta = StopForCollision(positionDelta);
        var linear = positionDelta / Time.fixedDeltaTime;

        var diff = SecondPoint - FirstPoint;
        var forwardAxis = Vector3.Cross(diff.normalized, worldAxis).normalized;
        var rightAxis = Vector3.Cross(worldAxis, forwardAxis);
        var angle = Vector3.SignedAngle(rightAxis, diff.normalized, forwardAxis);
        var targetLocalRotation = Quaternion.AngleAxis(angle, forwardAxis);
        var targetWorldRotation = startRotation * targetLocalRotation;
        var currentRotation = currentPos.rot;
        var rotationDiff = targetWorldRotation * Quaternion.Inverse(currentRotation);

        var angular = rotationDiff.QToAngleAxis() / Time.fixedDeltaTime;

        reBody.reVelocity = new MotionVector(angular, linear);

        if (belowBlockCount > 0)
            belowBlockCount--;
        if (aboveBlockCount > 0)
            aboveBlockCount--;
    }

    // How much can the platform move without getting blocked
    // If the value is too large, we will not be able to prevent clipping through objects
    // If the value is too small, we will prevent the platform from being able to lift objects upwards
    // The idea is that if we're moving fast and detect a collision via raycast, we stop any further movement
    // If we're moving slow, we should instead rely on the collisions themselves to drive the interaction
    private const float minPositionDeltaForBlockingMovement = 0.1f;

    private Vector3 StopForCollision(Vector3 positionDelta)
    {
        var finalDelta = positionDelta;
        var originalMagnitude = positionDelta.magnitude;
        var finalMagnitude = originalMagnitude;
        if (originalMagnitude > minPositionDeltaForBlockingMovement)
        {
            var direction = positionDelta / originalMagnitude;
            foreach (var collider in platformColliders)
            {
                var boxBounds = new BoxBounds(collider);
                Vector3 offset = boxBounds.TransformDirection(Vector3.Project(boxBounds.size, boxBounds.InverseTransformDirection(direction)));
                // Align offset to travel direction, since the projection will always be in the same direction
                offset *= Mathf.Sign(Vector3.Dot(offset, direction));
                var offsetMagnitude = offset.magnitude;
                if (Physics.BoxCast((Vector3)boxBounds.center - offset, boxBounds.extents, direction, out var hitInfo, boxBounds.rotation, finalMagnitude + offsetMagnitude * 2f, (int)(Layers.Object | Layers.Projectile)))
                {
                    var distance = Mathf.Max(hitInfo.distance - offsetMagnitude, 0f);

                    if (distance < finalMagnitude)
                    {
                        finalMagnitude = distance;
                        finalDelta = direction.normalized * distance;
                        BlockMovementForDirection(direction);
                    }
                }
            }
        }

        return finalDelta;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollisionBlocking(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollisionBlocking(collision);
    }

    private void HandleCollisionBlocking(Collision collision)
    {
        if (collision.impulse.magnitude > forceToBlock)
        {
            BlockMovementForDirection(collision.impulse);
        }
    }

    private void BlockMovementForDirection(Vector3 direction)
    {
        var blockedBelow = Vector3.Dot(direction, worldAxis) < 0;
        if (blockedBelow)
            belowBlockCount = 2;
        else
            aboveBlockCount = 2;
    }
}
