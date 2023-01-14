using UnityEngine;

public class ActivationFromAngularVelocity : ObjectActivator
{
    [Tooltip("Must be bigger or the same as activationSpeedThreshold")]
    [SerializeField]
    float speedForMaxPower = 1;
    [SerializeField]
    float activationSpeedThreshold;
    [SerializeField]
    Vector3 viableAxis;
    [SerializeField]
    private Rigidbody rotationSource;

    float sqrThreshMag;

    private void OnValidate()
    {
        if (rotationSource == null)
            rotationSource = GetComponentInChildren<Rigidbody>();
    }

    private void Start()
    {
        sqrThreshMag = activationSpeedThreshold * activationSpeedThreshold;
    }

    private void FixedUpdate()
    {
        var globalAxis = rotationSource.transform.TransformDirection(viableAxis);
        var projectedVel = Vector3.Project(rotationSource.angularVelocity, globalAxis);

        if (projectedVel.sqrMagnitude >= sqrThreshMag)
        {
            ActivationAmount = Mathf.Clamp01(projectedVel.magnitude / speedForMaxPower);
        }
        else
        {
            ActivationAmount = 0;
        }

    }
}
