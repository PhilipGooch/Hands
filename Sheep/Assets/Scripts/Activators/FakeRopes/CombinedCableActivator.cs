using NBG.LogicGraph;
using System;
using System.Collections.Generic;
using UnityEngine;

public class CombinedCableActivator : ObjectActivator
{
    [SerializeField]
    CableActivator mainCable;
    [SerializeField]
    List<CableActivator> connectedCables = new List<CableActivator>();
    [SerializeField]
    float startingActivation = 0.5f;
    [SerializeField]
    float movementLength = 1f;

    [NodeAPI("OnPositionChange")]
    public event Action<Vector3> onPositionChange;

    private void Start()
    {
        UpdateAllCableActivationValues(startingActivation);
        SetAllCableMovementLengths();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        foreach (var cable in connectedCables)
        {
            cable.onActivatorValueChanged += UpdateAllCableActivationValues;
        }
    }

    protected void OnDisable()
    {
        foreach (var cable in connectedCables)
        {
            cable.onActivatorValueChanged -= UpdateAllCableActivationValues;
        }
    }

    public void SetForwardMovementPrevention(bool halt)
    {
        foreach (var cable in connectedCables)
        {
            cable.HaltActivatorForwardMovement = halt;
        }
    }

    public void SetBackwardMovementPrevention(bool halt)
    {
        foreach (var cable in connectedCables)
        {
            cable.HaltActivatorBackwardMovement = halt;
        }
    }

    public void UpdateAllCableActivationValues(float newActivationValue)
    {
        ActivationAmount = newActivationValue;
        foreach (var cable in connectedCables)
        {
            cable.ActivationAmount = newActivationValue;
        }
        if (mainCable != null)
        {
            onPositionChange?.Invoke(mainCable.GetCableConnectionPosition());
        }
    }

    void SetAllCableMovementLengths()
    {
        foreach (var cable in connectedCables)
        {
            cable.MovementLength = movementLength;
        }
    }
}
