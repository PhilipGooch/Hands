using NBG.LogicGraph;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrainEngine : TrainBase
{
    [SerializeField]
    private float maxVelocity;
    [SerializeField]
    AnimationCurve accelarationCurve;
    [SerializeField]
    private float accelerationDuration;
    [Tooltip("Local direction")]
    [SerializeField]
    Vector3 direction;

    private float accelerationTimer;
    float inputMultiplier;
    float multiplier;

    [SerializeField]
    AdaptiveAudioSource adaptiveAudioSource;

    private Vector3 driveDirection;
    private Vector3 lastDriveDirection = Vector3.zero;

    private void OnValidate()
    {
        if (adaptiveAudioSource.audioSource == null)
            adaptiveAudioSource.audioSource = GetComponent<AudioSource>();
    }

    protected override void Start()
    {
        base.Start();
        adaptiveAudioSource.PlaySound();
    }
    
    //expects 0 - 1
    [NodeAPI("Move")]
    public void Move(float amount)
    {
        var updatedValue = RemapInput(amount);
        TryMove(transform.TransformDirection(direction), updatedValue);
    }

    private float RemapInput(float input)
    {
        const float deadZone = 0.1f;
        var value = (input * 2) - 1;
        value = value.Between(-deadZone, deadZone) ? 0 : value;
        return value;
    }

    private void TryMove(Vector3 heading, float multiplier)
    {
        if (CanMove)
        {
            this.driveDirection = heading;
            this.inputMultiplier = multiplier;

            var sign = Mathf.Sign(multiplier);

            if (lastDriveDirection != driveDirection * sign)
                accelerationTimer = 0;

            lastDriveDirection = driveDirection * sign;
        }
    }

    private void FixedUpdate()
    {
        if (inputMultiplier == 0)
            accelerationTimer = 0;
        else
            accelerationTimer = Mathf.Clamp(accelerationTimer + Time.fixedDeltaTime, 0, accelerationDuration);

        multiplier = inputMultiplier * accelarationCurve.Evaluate(accelerationTimer / accelerationDuration);

        reBody.velocity = driveDirection * maxVelocity * multiplier;

        if (WasMovingLastFrame && !IsMoving)
        {
            reBody.velocity = Vector3.zero;
            MoveAnchor();
        }

        WasMovingLastFrame = IsMoving;
    }

    //update since we need to wait until physics calculations are done to actually know if we are moving
    void Update()
    {
        adaptiveAudioSource.UpdateSound(IsMoving ? multiplier : 0);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
    }
}
