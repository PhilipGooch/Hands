using NBG.LogicGraph;
using System;
using UnityEngine;

public class AccumulationNode : MonoBehaviour
{
    [SerializeField]
    float startingAccumulation = 0;

    [Tooltip("Instead of 0 to 1 activation value, it uses -1 to 1 activationValue")]
    [SerializeField]
    private bool biDirectional = true;
    [Tooltip("Allows to cancel out small activator movements")]
    [SerializeField]
    private float deadzone = 0;


    [Header("Output Value")]
    [SerializeField]
    private float accumulationMax = 1;
    [SerializeField]
    private float accumulationMin = 0;

    [Header("Accumulation")]
    [SerializeField]
    private float maxAccumulationPerSecond = 0.3f;
    //instant acceleration by default
    [SerializeField]
    private AnimationCurve accumulationAccelarationCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
    [SerializeField]
    private float accumulationAccelerationDuration = 1;

    [Header("Deaccumulation")]
    [Tooltip("if node doesnt receive an activation value, it will try to deaccumulate")]
    [SerializeField]
    private float maxDeaccumulationPerSecond = 0;
    [SerializeField]
    private AnimationCurve decamulationAccelerationCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
    [SerializeField]
    private float deaccumulationAccelerationDuration = 1;

    private float activation;
    private float accumulation;
    private float accumulationAccelerationTimer;
    private float deaccumulationAccelerationTimer;

    private float lastActivationValue;

    [NodeAPI("OnAccumulationChange")]
    public event Action<float> OnAccumulationChange;

    [NodeAPI("UpdateActivation")]
    public void UpdateActivation(float inputActivation)
    {
        if (biDirectional)
        {
            activation = Mathf.Clamp((inputActivation * 2) - 1, -1f, 1f);
        }
        else
        {
            activation = Mathf.Clamp(inputActivation, 0, 1);
        }

        if (Mathf.Abs(activation) < Mathf.Abs(deadzone))
        {
            activation = 0;
        }

        //accumulation direction changed
        if (Mathf.Sign(activation) != Mathf.Sign(lastActivationValue))
            accumulationAccelerationTimer = 0;

        lastActivationValue = activation;
    }

    private void Start()
    {
        accumulation = startingAccumulation;
    }

    private void FixedUpdate()
    {
        float delta;

        if (activation == 0)
        {

            accumulationAccelerationTimer = 0;
            deaccumulationAccelerationTimer = Mathf.Clamp(deaccumulationAccelerationTimer += Time.fixedDeltaTime, 0, deaccumulationAccelerationDuration);

            delta = -maxDeaccumulationPerSecond * Time.fixedDeltaTime * decamulationAccelerationCurve.Evaluate(deaccumulationAccelerationTimer / deaccumulationAccelerationDuration);
        }
        else
        {
            deaccumulationAccelerationTimer = 0;
            accumulationAccelerationTimer = Mathf.Clamp(accumulationAccelerationTimer += Time.fixedDeltaTime, 0, accumulationAccelerationDuration);

            delta = maxAccumulationPerSecond * activation * Time.fixedDeltaTime * accumulationAccelarationCurve.Evaluate(accumulationAccelerationTimer / accumulationAccelerationDuration);
        }

        accumulation = Mathf.Clamp(accumulation + delta, accumulationMin, accumulationMax);

        if (delta != 0)
            OnAccumulationChange?.Invoke(accumulation);

    }
}
