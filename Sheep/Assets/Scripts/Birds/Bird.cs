using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bird : MonoBehaviour
{
    [SerializeField]
    float flySpeed = 3f;
    [SerializeField]
    float glideSpeed = 2f;
    [SerializeField]
    float glideDuration = 3f;
    [SerializeField]
    float minLandingSpeed = 0.5f;
    [SerializeField]
    float landingTurnDuration = 2f;
    [SerializeField]
    float distanceForLandingSlowdown = 5f;
    [SerializeField]
    float scareDuration = 0.25f;

    enum State
    {
        Idle,
        FlyingAway,
        Gliding,
        Landing
    }

    State currentState = State.Idle;

    BirdSystem birdSystem;
    Vector3 targetGlidePosition;
    Vector3 currentLandingLocation;
    float timer = 0f;
    float lastBlockCheck = 0;
    bool enteredFinalDescent = false;
    TriggerEventSender triggerEventSender;
    Animator animator;
    float randomValue = 0f;

    int animatorFlyId = Animator.StringToHash("Flying");
    int animatorLandingId = Animator.StringToHash("Landing");
    int animatorDescendingId = Animator.StringToHash("Descending");
    int animatorGlideId = Animator.StringToHash("Gliding");
    int animatorIdleId = Animator.StringToHash("Idle");
    int animatorCycleOffsetId = Animator.StringToHash("CycleOffset");

    float ActualGlideSpeed
    {
        get
        {
            return glideSpeed + randomValue;
        }
    }

    public void Initialize(BirdSystem birdSystem)
    {
        this.birdSystem = birdSystem;
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                Idle();
                break;
            case State.FlyingAway:
                FlyAway();
                break;
            case State.Gliding:
                Glide();
                break;
            case State.Landing:
                Land();
                break;
        }
    }

    private void Awake()
    {
        triggerEventSender = GetComponentInChildren<TriggerEventSender>();
        animator = GetComponent<Animator>();
        currentLandingLocation = transform.position;
    }

    private void OnEnable()
    {
        triggerEventSender.onTriggerStay += OnTriggerStay;
        RandomizeOffsetValue();
    }

    void RandomizeOffsetValue()
    {
        randomValue = Random.value;
        animator.SetFloat(animatorCycleOffsetId, randomValue);
    }

    private void OnDisable()
    {
        triggerEventSender.onTriggerStay -= OnTriggerStay;
    }

    void Idle()
    {
        // Play animations
    }

    void FlyAway()
    {
        var currentFlyingSpeed = flySpeed;
        if (timer < scareDuration)
        {
            timer += Time.deltaTime;
            currentFlyingSpeed = Mathf.Lerp(0f, flySpeed, timer / scareDuration);
        }
        UpdateTargetGlidePosition();
        var distanceFromTarget = MoveCrowTowardsPoint(targetGlidePosition, currentFlyingSpeed, 0.05f);

        if (distanceFromTarget < flySpeed * Time.deltaTime)
        {
            SwitchState(State.Gliding);
        }
    }

    void Glide()
    {
        UpdateTargetGlidePosition();
        MoveCrowTowardsPoint(targetGlidePosition, ActualGlideSpeed, 0.25f);
        timer += Time.deltaTime;
        if (timer > glideDuration + randomValue)
        {
            if (birdSystem.LandingSpotAvailable())
                SwitchState(State.Landing);
            else
                timer = 0;
        }
    }

    float MoveCrowTowardsPoint(Vector3 targetPoint, float maxSpeed, float rotationLerp)
    {
        var toTargetPos = targetPoint - transform.position;
        var distanceFromTarget = toTargetPos.magnitude;
        var toTargetDirection = toTargetPos.normalized;
        var movementAmount = Mathf.Min(distanceFromTarget, maxSpeed * Time.deltaTime);
        transform.position += toTargetDirection * movementAmount;
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(toTargetDirection, Vector3.up), rotationLerp);
        return distanceFromTarget;
    }

    float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }

    void UpdateTargetGlidePosition()
    {
        // Make sure the bird can catch up with the target position
        var maxGlideSpeed = Mathf.Min(ActualGlideSpeed * 0.8f, flySpeed * 0.8f);
        var glideDirection = birdSystem.GetGlideDirection(targetGlidePosition, randomValue);
        targetGlidePosition += glideDirection * maxGlideSpeed * Time.deltaTime;
    }

    void Land()
    {
        UpdateTargetGlidePosition();

        var toLandingPoint = currentLandingLocation - transform.position;
        var landingDirection = toLandingPoint.normalized;
        var birdDistance = toLandingPoint.magnitude;
        var turnProgress = Mathf.Clamp01(timer / landingTurnDuration);
        timer += Time.deltaTime;
        var glideDirection = birdSystem.GetGlideDirection(targetGlidePosition, randomValue);
        var finalDirection = Vector3.Lerp(glideDirection, landingDirection, Mathf.Clamp01(turnProgress));

        var landingSpeedProgress = 1f - Mathf.Clamp01(birdDistance / distanceForLandingSlowdown);
        var landingSpeed = Mathf.Lerp(flySpeed, minLandingSpeed, landingSpeedProgress * landingSpeedProgress);

        var nextPosition = transform.position + finalDirection * landingSpeed;
        var distanceFromTarget = MoveCrowTowardsPoint(nextPosition, landingSpeed, 0.25f);

        const float distanceToStartEveningRotation = 3f;
        const float distanceToFinishEveningRotation = 1f;
        var rotationLerp = Remap(distanceFromTarget, distanceToStartEveningRotation, distanceToFinishEveningRotation, 0f, 1f);
        var rotationWithoutTilt = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotationWithoutTilt, rotationLerp);


        var distanceLeft = (currentLandingLocation - transform.position).sqrMagnitude;

        if (distanceLeft < distanceForLandingSlowdown && !enteredFinalDescent)
        {
            animator.SetTrigger(animatorLandingId);
            enteredFinalDescent = true;
        }

        if ((Time.time - lastBlockCheck) >= 1)
        {
            if (birdSystem.IsPositionBlocked(currentLandingLocation))
            {
                SwitchState(State.FlyingAway);
                return;

            }

            lastBlockCheck = Time.time;
        }

        if (distanceLeft < 0.001f)
        {
            SwitchState(State.Idle);
        }

    }

    void SwitchState(State targetState)
    {
        currentState = targetState;
        if (targetState == State.FlyingAway)
        {
            RandomizeOffsetValue();
            birdSystem.AddFreeLandingLocation(currentLandingLocation);
            targetGlidePosition = birdSystem.GetClosestGlidePoint(transform.position, randomValue);
        }
        if (targetState == State.Landing)
        {
            lastBlockCheck = 0;
            enteredFinalDescent = false;
            bool locationRecieved = birdSystem.TryGettingClosestNonBlockedLandingLocation(transform.position, ref currentLandingLocation);

            if (!locationRecieved)
            {
                SwitchState(State.Gliding);
                return;
            }
        }
        timer = 0f;

        UpdateAnimation(targetState);
    }

    void UpdateAnimation(State targetState)
    {
        int? targetTrigger = null;
        switch (targetState)
        {
            case State.FlyingAway:
                targetTrigger = animatorFlyId;
                break;
            case State.Gliding:
                targetTrigger = animatorGlideId;
                break;
            case State.Idle:
                targetTrigger = animatorIdleId;
                break;
            case State.Landing:
                targetTrigger = animatorDescendingId;
                break;
        }

        if (targetTrigger.HasValue)
        {
            animator.SetTrigger(targetTrigger.Value);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetGlidePosition, 0.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(currentLandingLocation, 0.5f);
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentState == State.Idle)
        {
            var rig = other.attachedRigidbody;
            if (rig != null)
            {
                if (rig.velocity.sqrMagnitude > 1f)
                {
                    SwitchState(State.FlyingAway);
                }
            }
        }
    }
}
