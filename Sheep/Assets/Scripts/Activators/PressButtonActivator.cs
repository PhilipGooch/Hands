using UnityEngine;
using VR.System;

public class PressButtonActivator : ObjectActivator
{
    [SerializeField]
    Vector3 pressAxis = Vector3.forward;
    [SerializeField]
    Transform movingPart;
    [SerializeField]
    float moveDistance = 0.05f;
    [SerializeField]
    float pressSpeed = 5f;
    [SerializeField]
    [Range(0, 320)]
    int pressVibrationFrequency = 100;
    [SerializeField]
    [Range(0, 320)]
    int releaseVibrationFrequency = 250;
    [SerializeField]
    [Range(0f, 1f)]
    float pressVibrationAmplitude = 0.5f;
    [SerializeField]
    bool toggled = false;

    bool pressedThisFrame = false;
    bool currentlyToggled = false;
    Hand activatingHand;
    Vector3 originalPosition;

    const float grabAmountForFist = 0.8f;

    private void Awake()
    {
        originalPosition = movingPart.localPosition;
    }

    private void FixedUpdate()
    {
        bool isPressed = toggled ? currentlyToggled : pressedThisFrame;
        ActivationAmount = isPressed ? 1f : 0f;

        if (!pressedThisFrame)
        {
            if (activatingHand != null)
            {
                activatingHand.Vibrate(0, Time.fixedDeltaTime, releaseVibrationFrequency, pressVibrationAmplitude);
                activatingHand = null;
            }
        }

        Vector3 targetPos = isPressed ? originalPosition + pressAxis * moveDistance : originalPosition;
        movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, targetPos, pressSpeed * Time.fixedDeltaTime);

        pressedThisFrame = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckForPress(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckForPress(other);
    }

    void CheckForPress(Collider collider)
    {
        var hand = collider.GetComponentInParent<Hand>();
        var handPositions = collider.GetComponentInParent<HandPositions>();
        if (hand != null && handPositions != null)
        {
            HandleIndexFistPress(collider, hand, handPositions);
        }
    }

    void HandleIndexFistPress(Collider collider, Hand hand, HandPositions handPositions)
    {
        var animator = collider.GetComponentInParent<HandAnimator>();

        if (animator.IsPointing())
        {
            if (collider == handPositions.IndexCollider)
            {
                ActivateButton(hand);
            }
        }
        else if (animator.IsEmptyFist())
        {
            //Empty fist, slam the button!
            if (collider == handPositions.FistCollider)
            {
                ActivateButton(hand);
            }
        }
    }

    void ActivateButton(Hand hand)
    {
        ActivationAmount = 1f;
        pressedThisFrame = true;
        if (activatingHand == null) // First press
        {
            hand.Vibrate(0, Time.fixedDeltaTime, pressVibrationFrequency, pressVibrationAmplitude);
            currentlyToggled = !currentlyToggled;
        }
        activatingHand = hand;
    }
}
