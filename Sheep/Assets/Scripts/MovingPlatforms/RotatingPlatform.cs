using Recoil;
using UnityEngine;
using NBG.LogicGraph;

public class RotatingPlatform : ActivatableNode
{
    [SerializeField]
    float rotationAmount = 90;
    [SerializeField]
    [Tooltip("How far can the object rotate backwards? This must be a negative value, works the same way as a hinge joint min limits.")]
    float backwardsRotationAmount = 0f;
    [SerializeField]
    float rotationSpeedPerSecond = 180;
    [SerializeField]
    Vector3 rotationAxis = Vector3.up;
    [SerializeField]
    int snapPositions = 0;

    new Rigidbody rigidbody;
    ReBody reBody;

    float currentAngle;
    Vector3 startAxisRotation;

    [NodeAPI("How much has this platform rotated from its base rotation. Outputs value from 0 to 1.")]
    public float CurrentRotationProgress { get; private set; }

    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        reBody = new ReBody(rigidbody);
        // Only remember the start rotation of the axis we want to rotate on and allow free motion on other axes
        startAxisRotation = Vector3.Scale(transform.localEulerAngles, rotationAxis);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var targetAngle = rotationAmount * ActivationValue + backwardsRotationAmount;
        targetAngle = SnapAngle(targetAngle);
        var angleDiff = targetAngle - currentAngle;
        var movement = Mathf.Min(Mathf.Abs(angleDiff), rotationSpeedPerSecond * Time.fixedDeltaTime) * Mathf.Sign(angleDiff);
        currentAngle += movement;
        var currentBaseRotation = Vector3.Scale(transform.localEulerAngles, Vector3.one - rotationAxis);
        var localEulers = currentBaseRotation + startAxisRotation + rotationAxis * currentAngle;
        var localRotation = Quaternion.Euler(localEulers);
        reBody.rotation = TransformToWorldRotation(localRotation);
        CurrentRotationProgress = (1f - currentAngle / rotationAmount);
    }

    float SnapAngle(float angle)
    {
        if (snapPositions > 1)
        {
            var anglePerPosition = rotationAmount / (snapPositions - 1);
            var closestPosition = Mathf.RoundToInt(angle / anglePerPosition);
            angle = closestPosition * anglePerPosition;
        }

        return angle;
    }

    Quaternion TransformToWorldRotation(Quaternion localRotation)
    {
        if (transform.parent != null)
        {
            return transform.parent.rotation * localRotation;
        }
        return localRotation;
    }
}
