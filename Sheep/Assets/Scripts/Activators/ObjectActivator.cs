using NBG.LogicGraph;
using System;
using UnityEngine;

public abstract class ObjectActivator : MonoBehaviour
{
    [NodeAPI("OnActivationChange")]
    public event Action<float> OnActivationChange;

    public bool invertValue = false;
    float activationAmount = 0f;
    public float ActivationAmount
    {
        get
        {
            return activationAmount;
        }
        set
        {
            activationAmount = Mathf.Clamp01(value);
            if (invertValue)
            {
                activationAmount = 1f - activationAmount;
            }
            OnActivationChange?.Invoke(activationAmount);
        }
    }
    public bool HaltActivatorForwardMovement { get; set; } = false;
    public bool HaltActivatorBackwardMovement { get; set; } = false;

    protected virtual void OnEnable()
    {
        OnActivationChange?.Invoke(ActivationAmount);
    }
}
