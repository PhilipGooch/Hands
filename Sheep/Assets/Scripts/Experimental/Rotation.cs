using UnityEngine;
using System.Collections;

public class Rotation : ActivatableNode
{
    enum MotionDirections
    {
        SpinX,
        SpinY,
        SpinZ,
        OscillateRotation,
        Horizontal,
        Vertical
    }

    [SerializeField]
    MotionDirections motionState = MotionDirections.Horizontal;
    [SerializeField]
    float spinSpeed = 10.0f;
    [SerializeField]
    float motionMagnitude = 0.1f;
    [SerializeField]
    Vector3 rotationVector = Vector3.right;

    Quaternion startRotation;
    float timer = 0f;

    private void Awake()
    {
        startRotation = transform.rotation;
    }

    void Update()
    {
        if (ActivationValue == 0)
            return;

        timer += Time.deltaTime * spinSpeed;
        if (timer > Mathf.PI * 2)
        {
            timer -= Mathf.PI * 2;
        }

        switch (motionState)
        {
            case MotionDirections.SpinX:
                transform.Rotate(Vector3.left * spinSpeed * Time.deltaTime * ActivationValue);
                break;

            case MotionDirections.SpinY:
                transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime * ActivationValue);
                break;

            case MotionDirections.SpinZ:
                transform.Rotate(Vector3.forward * spinSpeed * Time.deltaTime * ActivationValue);
                break;

            case MotionDirections.Vertical:
                transform.Translate(Vector3.up * Mathf.Cos(Time.timeSinceLevelLoad) * motionMagnitude * ActivationValue);
                break;

            case MotionDirections.Horizontal:
                transform.Translate(Vector3.right * Mathf.Cos(Time.timeSinceLevelLoad) * motionMagnitude * ActivationValue);
                break;

            case MotionDirections.OscillateRotation:
                transform.rotation = startRotation * Quaternion.Euler(rotationVector * Mathf.Sin(timer) * motionMagnitude * ActivationValue);
                break;
        }
    }
}
