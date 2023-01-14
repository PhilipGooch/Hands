using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableElevator : MonoBehaviour
{
    [SerializeField]
    CombinedCableActivator leftPulley;
    [SerializeField]
    CombinedCableActivator rightPulley;
    // TODO - possibly convert to world space unit difference
    [SerializeField]
    [Tooltip("Max difference between two cable pairs. If the value is 0, both cables will be moved when pulling one cable")]
    [Range(0f,1f)]
    float maxActivationDifferenceBetweenCables = 0.2f;
    [SerializeField]
    [Range(0f, 1f)]
    float minCableActivationLimit = 0.5f;
    [SerializeField]
    [Range(0f, 1f)]
    float maxCableActivationLimit = 1f;
    //TODO: convert to a more generic collision detection component
    [SerializeField]
    [Tooltip("Optional. Platform to use for collision checking.")]
    TwoPointMovingPlatform platform;

    float lastActivationValue = 0f;

    private void Start()
    {
        lastActivationValue = leftPulley.ActivationAmount;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var firstProgress = leftPulley.ActivationAmount;
        var secondProgress = rightPulley.ActivationAmount;
        var difference = secondProgress - firstProgress;
        var moveBothCablesTogether = Mathf.Approximately(maxActivationDifferenceBetweenCables, 0);

        // Cables can't have difference - move them both if one of them moves
        if (moveBothCablesTogether)
        {
            var leftDiff = Mathf.Abs(leftPulley.ActivationAmount - lastActivationValue);
            var rightDiff = Mathf.Abs(rightPulley.ActivationAmount - lastActivationValue);
            float valueToUse = lastActivationValue;
            if (leftDiff > rightDiff)
            {
                valueToUse = leftPulley.ActivationAmount;
            }
            else
            {
                valueToUse = rightPulley.ActivationAmount;
            }

            leftPulley.UpdateAllCableActivationValues(valueToUse);
            rightPulley.UpdateAllCableActivationValues(valueToUse);
        }
        else if (Mathf.Abs(difference) > maxActivationDifferenceBetweenCables)
        {
            if (difference > 0)
            {
                leftPulley.SetBackwardMovementPrevention(true);
                rightPulley.SetForwardMovementPrevention(true);
            }
            else
            {
                leftPulley.SetForwardMovementPrevention(true);
                rightPulley.SetBackwardMovementPrevention(true);
            }
        }

        if (leftPulley.ActivationAmount <= minCableActivationLimit)
        {
            leftPulley.SetBackwardMovementPrevention(true);
            SnapToLimit(leftPulley);
        }

        if (rightPulley.ActivationAmount <= minCableActivationLimit)
        {
            rightPulley.SetBackwardMovementPrevention(true);
            SnapToLimit(rightPulley);
        }

        if (leftPulley.ActivationAmount >= maxCableActivationLimit)
        {
            leftPulley.SetForwardMovementPrevention(true);
            SnapToLimit(leftPulley);
        }

        if (rightPulley.ActivationAmount >= maxCableActivationLimit)
        {
            rightPulley.SetForwardMovementPrevention(true);
            SnapToLimit(rightPulley);
        }

        if (platform != null)
        {
            if (platform.BlockedBelow)
            {
                leftPulley.SetBackwardMovementPrevention(true);
                rightPulley.SetBackwardMovementPrevention(true);
            }

            if (platform.BlockedAbove)
            {
                leftPulley.SetForwardMovementPrevention(true);
                rightPulley.SetForwardMovementPrevention(true);
            }
        }

        if (moveBothCablesTogether)
        {
            lastActivationValue = leftPulley.ActivationAmount;
        }
    }

    void SnapToLimit(CombinedCableActivator pulley)
    {
        pulley.UpdateAllCableActivationValues(Mathf.Clamp(pulley.ActivationAmount, minCableActivationLimit, maxCableActivationLimit));
    }
}
