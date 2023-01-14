using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationFromActivation : ActivatableNode
{
    [SerializeField]
    float radius = 0.5f;
    [SerializeField]
    float distanceToRotate = 1f;
    [SerializeField]
    Vector3 rotationAxis = new Vector3(0, 0, 1);

    float objectLength;
    float previousActivation;

    private void Awake()
    {
        objectLength = radius * 2f * Mathf.PI;
        previousActivation = ActivationValue;
    }

    public void FixedUpdate()
    {
        var delta = ActivationValue - previousActivation;
        var distance = delta * distanceToRotate;
        var rotations = distance / objectLength;

        transform.rotation *= Quaternion.Euler(rotationAxis * 360f * rotations);
        previousActivation = ActivationValue;
    }
}
